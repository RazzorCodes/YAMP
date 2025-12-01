using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class WorkGiver_LoadMedPod : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("YAMP_MedPod"));
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_MedPod pod)) return false;
            
            CompMedPodFuel fuelComp = pod.TryGetComp<CompMedPodFuel>();
            CompMedPodOperations opsComp = pod.TryGetComp<CompMedPodOperations>();

            if (fuelComp == null || opsComp == null) return false;

            // Check if we need fuel
            if (fuelComp.StockPercent < 1.0f)
            {
                Thing medicine = FindMedicine(pawn);
                if (medicine != null) return true;
            }

            // Check if we need ingredients for operation
            Pawn patient = opsComp.innerContainer.OfType<Pawn>().FirstOrDefault();
            if (patient != null)
            {
                Bill_Medical bill = GetFirstSurgeryBill(patient);
                if (bill != null)
                {
                    RecipeDef recipe = bill.recipe;
                    if (MissingIngredients(opsComp, recipe, out Thing ingredient))
                    {
                        if (ingredient != null) return true;
                        if (FindIngredientForRecipe(pawn, opsComp, recipe) != null) return true;
                    }
                }
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_MedPod pod = (Building_MedPod)t;
            CompMedPodFuel fuelComp = pod.TryGetComp<CompMedPodFuel>();
            CompMedPodOperations opsComp = pod.TryGetComp<CompMedPodOperations>();

            // Prioritize Fuel if very low
            if (fuelComp.StockPercent < 0.5f)
            {
                Thing medicine = FindMedicine(pawn);
                if (medicine != null)
                {
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_LoadMedPod"), medicine, pod);
                    job.count = medicine.stackCount;
                    return job;
                }
            }

            Pawn patient = opsComp.innerContainer.OfType<Pawn>().FirstOrDefault();
            if (patient != null)
            {
                Bill_Medical bill = GetFirstSurgeryBill(patient);
                if (bill != null)
                {
                    RecipeDef recipe = bill.recipe;
                    Thing ingredient = FindIngredientForRecipe(pawn, opsComp, recipe);
                    if (ingredient != null)
                    {
                         Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_LoadMedPod"), ingredient, pod);
                         job.count = ingredient.stackCount;
                         return job;
                    }
                }
            }
            
            // Fallback to fuel
            if (fuelComp.StockPercent < 1.0f)
            {
                 Thing medicine = FindMedicine(pawn);
                if (medicine != null)
                {
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_LoadMedPod"), medicine, pod);
                    job.count = medicine.stackCount;
                    return job;
                }
            }

            return null;
        }

        private Bill_Medical GetFirstSurgeryBill(Pawn patient)
        {
            if (patient.BillStack == null) return null;
            foreach (Bill b in patient.BillStack)
            {
                // Accept all medical bills
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    return bm;
                }
            }
            return null;
        }

        private Thing FindMedicine(Pawn pawn)
        {
            Building_MedPod pod = pawn.Map.thingGrid.ThingsListAtFast(pawn.Position).OfType<Building_MedPod>().FirstOrDefault();
            if (pod == null) return null;
            
            CompMedPodFuel fuelComp = pod.TryGetComp<CompMedPodFuel>();
            if (fuelComp == null) return null;
            
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Medicine), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999, x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && fuelComp.fuelFilter.Allows(x));
        }

        private Thing FindIngredientForRecipe(Pawn pawn, CompMedPodOperations ops, RecipeDef recipe)
        {
            foreach (IngredientCount ing in recipe.ingredients)
            {
                float needed = ing.GetBaseCount();
                float has = 0;
                
                // Check what we already have in the pod
                foreach (Thing t in ops.innerContainer)
                {
                    if (ing.filter.Allows(t)) has += t.stackCount;
                }

                if (has < needed)
                {
                    // First check adjacent shelves
                    Thing shelfItem = FindInAdjacentShelves(ops.parent, ing);
                    if (shelfItem != null && pawn.CanReserve(shelfItem))
                    {
                        return shelfItem;
                    }
                    
                    // Then search the map
                    return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999, x => ing.filter.Allows(x) && !x.IsForbidden(pawn) && pawn.CanReserve(x));
                }
            }
            return null;
        }

        private Thing FindInAdjacentShelves(Thing pod, IngredientCount ing)
        {
            // Check left and right cells for shelves
            IntVec3[] adjacentCells = new IntVec3[]
            {
                pod.Position + IntVec3.East,
                pod.Position + IntVec3.West,
                pod.Position + IntVec3.North,
                pod.Position + IntVec3.South
            };

            foreach (IntVec3 cell in adjacentCells)
            {
                if (!cell.InBounds(pod.Map)) continue;
                
                foreach (Thing t in cell.GetThingList(pod.Map))
                {
                    // Check if it's a shelf/storage building
                    if (t.def.building != null && t.def.building.isInert)
                    {
                        ISlotGroupParent slotParent = t as ISlotGroupParent;
                        if (slotParent != null && slotParent.GetSlotGroup() != null)
                        {
                            foreach (Thing item in slotParent.GetSlotGroup().HeldThings)
                            {
                                if (ing.filter.Allows(item))
                                {
                                    return item;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private bool MissingIngredients(CompMedPodOperations ops, RecipeDef recipe, out Thing foundIngredient)
        {
            foundIngredient = null;
            foreach (IngredientCount ing in recipe.ingredients)
            {
                float needed = ing.GetBaseCount();
                float has = 0;
                foreach (Thing t in ops.innerContainer)
                {
                    if (ing.filter.Allows(t)) has += t.stackCount;
                }

                if (has < needed)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
