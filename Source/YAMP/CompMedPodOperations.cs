using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class CompProperties_MedPodOperations : CompProperties
    {
        public float surgerySuccessChance = 0.98f; // High success chance
        public float stockConsumptionFactor = 1.0f; // Multiplier for stock consumption

        public CompProperties_MedPodOperations()
        {
            compClass = typeof(CompMedPodOperations);
        }
    }

    public class CompMedPodOperations : ThingComp, IThingHolder
    {
        public ThingOwner innerContainer; // Holds Patient and Ingredients
        
        public CompProperties_MedPodOperations Props => (CompProperties_MedPodOperations)props;
        
        private CompMedPodFuel fuelComp;
        public CompMedPodFuel FuelComp
        {
            get
            {
                if (fuelComp == null) fuelComp = parent.GetComp<CompMedPodFuel>();
                return fuelComp;
            }
        }

        public CompMedPodOperations()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(250)) // Check every few seconds
            {
                TryPerformOperation();
            }
        }

        public void TryPerformOperation()
        {
            Pawn patient = innerContainer.OfType<Pawn>().FirstOrDefault();
            if (patient == null) return;

            // Get the first medical bill from the patient
            Bill_Medical bill = GetFirstSurgeryBill(patient);
            if (bill == null) return;
            
            RecipeDef recipe = bill.recipe;

            // Check if we have ingredients (EXCLUDING medicine - that comes from fuel)
            if (!HasIngredients(recipe))
            {
                // Log what we're missing for debugging
                Log.Warning($"YAMP: Missing ingredients for {recipe.label}");
                return;
            }

            // Check stock
            float stockCost = CalculateStockCost(recipe);
            if (FuelComp.stock < stockCost)
            {
                Log.Warning($"YAMP: Not enough stock ({FuelComp.stock}/{stockCost}) for {recipe.label}");
                return;
            }

            // Perform Operation
            Log.Message($"YAMP: Performing operation {recipe.label} on {patient.LabelShort}");
            PerformOperation(patient, bill, stockCost);
        }

        private Bill_Medical GetFirstSurgeryBill(Pawn patient)
        {
            if (patient.BillStack == null) return null;
            foreach (Bill b in patient.BillStack)
            {
                // Accept all medical bills, not just surgeries
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    return bm;
                }
            }
            return null;
        }

        private bool HasIngredients(RecipeDef recipe)
        {
            foreach (IngredientCount ing in recipe.ingredients)
            {
                // Skip medicine - it's provided by the fuel system
                if (ing.filter.AllowedThingDefs.Any(t => t.IsMedicine)) continue;
                
                float neededCount = ing.GetBaseCount();
                float hasCount = 0;
                
                foreach (Thing t in innerContainer)
                {
                    if (ing.filter.Allows(t))
                    {
                        hasCount += t.stackCount;
                    }
                }

                if (hasCount < neededCount)
                {
                    // Get a sample item name from the filter
                    string itemName = ing.filter.Summary;
                    Log.Warning($"YAMP: Missing {itemName} (need {neededCount}, have {hasCount}) for {recipe.label}");
                    return false;
                }
            }
            return true;
        }

        private float CalculateStockCost(RecipeDef recipe)
        {
            return 10f * Props.stockConsumptionFactor;
        }

        private void PerformOperation(Pawn patient, Bill_Medical bill, float stockCost)
        {
            RecipeDef recipe = bill.recipe;
            
            // Consume Stock
            FuelComp.stock -= stockCost;

            // Prepare ingredients list - including medicine from fuel stock
            List<Thing> ingredients = new List<Thing>();
            
            foreach (IngredientCount ing in recipe.ingredients)
            {
                if (ing.filter.AllowedThingDefs.Any(t => t.IsMedicine))
                {
                    // Get medicine from fuel comp
                    if (FuelComp.innerContainer.Any)
                    {
                        Thing medicine = FuelComp.innerContainer.First();
                        int needed = (int)ing.GetBaseCount();
                        if (medicine.stackCount >= needed)
                        {
                            Thing taken = medicine.SplitOff(needed);
                            ingredients.Add(taken);
                            Log.Message($"YAMP: Added {needed}x {taken.def.label} for {recipe.label}");
                        }
                        else
                        {
                            Log.Warning($"YAMP: Not enough medicine in fuel ({medicine.stackCount}/{needed}) for {recipe.label}");
                        }
                    }
                    else
                    {
                        Log.Warning($"YAMP: No medicine in fuel container for {recipe.label}");
                    }
                }
                else
                {
                    // Get non-medicine ingredients from operations container
                    float neededCount = ing.GetBaseCount();
                    
                    for (int i = innerContainer.Count - 1; i >= 0; i--)
                    {
                        Thing t = innerContainer[i];
                        if (ing.filter.Allows(t))
                        {
                            int toTake = Mathf.Min(t.stackCount, (int)neededCount);
                            Thing taken = t.SplitOff(toTake);
                            ingredients.Add(taken);
                            neededCount -= toTake;
                            if (neededCount <= 0) break;
                        }
                    }
                }
            }

            // Apply Surgery/Medical Operation
            bool success = Rand.Value <= Props.surgerySuccessChance;
            
            if (success)
            {
                // Use the bill's part
                BodyPartRecord part = bill.Part;
                
                // Create a dummy surgeon pawn to avoid giving thoughts to the patient
                Pawn dummySurgeon = CreateDummySurgeon();
                
                // Apply the recipe
                recipe.Worker.ApplyOnPawn(patient, part, dummySurgeon, ingredients, bill); 
                Messages.Message($"Operation {recipe.label} on {patient.LabelShort} completed successfully.", parent, MessageTypeDefOf.PositiveEvent);
                
                // Destroy the dummy surgeon
                dummySurgeon.Destroy();
                
                // Complete the bill
                patient.BillStack.Delete(bill);
            }
            else
            {
                Messages.Message($"Operation {recipe.label} on {patient.LabelShort} failed.", parent, MessageTypeDefOf.NegativeEvent);
                patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 10, 0, -1, null, null));
                // Keep bill on failure so they can try again
            }
            
            // Destroy consumed ingredients
            foreach (Thing t in ingredients)
            {
                t.Destroy();
            }
            
            // Eject patient if dead
            if (patient.Dead)
            {
                innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
            }
        }

        private Pawn CreateDummySurgeon()
        {
            // Create a minimal dummy pawn to act as surgeon
            PawnKindDef kind = PawnKindDefOf.Colonist;
            Faction faction = Faction.OfPlayer;
            
            Pawn dummy = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind,
                faction,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f
            ));
            
            // Spawn the dummy on the map temporarily so organs can spawn at their position
            GenSpawn.Spawn(dummy, parent.Position, parent.Map);
            
            return dummy;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            Pawn patient = innerContainer.OfType<Pawn>().FirstOrDefault();
            if (patient != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Manage Operations",
                    defaultDesc = "Select and jump to the patient to manage their health and operations.",
                    icon = TexCommand.Draft,
                    action = () =>
                    {
                        // Select the patient and jump camera to them
                        CameraJumper.TryJumpAndSelect(patient);
                    }
                };
                
                yield return new Command_Action
                {
                    defaultLabel = "Eject Patient",
                    action = () => innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near)
                };
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnter".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            
            if (innerContainer.Any(t => t is Pawn))
            {
                yield return new FloatMenuOption("CannotEnter".Translate() + ": " + "Full".Translate().CapitalizeFirst(), null);
                yield break;
            }

            yield return new FloatMenuOption("Enter Med Pod", () =>
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_EnterMedPod"), parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
        
        public override string CompInspectStringExtra()
        {
             Pawn patient = innerContainer.OfType<Pawn>().FirstOrDefault();
             if (patient != null)
             {
                 Bill_Medical bill = GetFirstSurgeryBill(patient);
                 if (bill != null)
                 {
                     return $"Operation: {bill.recipe.label}";
                 }
                 return "No pending operations";
             }
             return null;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn patient = innerContainer.OfType<Pawn>().FirstOrDefault();
            if (patient != null)
            {
                // Only animate if we have a bill? Or always if patient inside?
                // Let's animate if patient is inside.
                
                Vector3 drawPos = parent.DrawPos;
                drawPos.y += 0.04f; // Slightly above building
                
                // Rotate based on time
                float angle = (Time.realtimeSinceStartup * 50f) % 360f;
                
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(2f, 1f, 2f)); // 2x2 size
                
                Graphics.DrawMesh(MeshPool.plane10, matrix, YAMP_Assets.ActiveOverlayMat, 0);
            }
        }
    }
}
