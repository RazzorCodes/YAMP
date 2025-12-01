using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP
{
    /// <summary>
    /// Utility class to handle Med Pod operations without a surgeon (billDoer).
    /// Replicates the effects of standard recipes (Install, Remove, Administer) manually.
    /// </summary>
    public class Recipe_MedPodSurgery : Recipe_Surgery
    {
        // We don't use ApplyOnPawn directly as a recipe worker anymore, 
        // but we keep the class structure if needed for compatibility.

        public static void DoMedPodSurgery(Pawn patient, Bill_Medical bill, List<Thing> ingredients,
            ThingWithComps pod)
        {
            RecipeDef recipe = bill.recipe;
            BodyPartRecord part = bill.Part;

            // 1. Install Artificial/Natural Part
            if (recipe.workerClass == typeof(Recipe_InstallArtificialBodyPart) ||
                recipe.workerClass == typeof(Recipe_InstallNaturalBodyPart))
            {
                if (part != null)
                {
                    // Restore the part (removes MissingBodyPart hediff)
                    patient.health.RestorePart(part);
                }

                // Apply the added hediff (e.g., Bionic Leg)
                if (recipe.addsHediff != null)
                {
                    patient.health.AddHediff(recipe.addsHediff, part);
                }
            }
            // 2. Remove Body Part
            else if (recipe.workerClass == typeof(Recipe_RemoveBodyPart))
            {
                if (part != null)
                {
                    // Spawn the removed part if applicable
                    if (part.def.spawnThingOnRemoved != null)
                    {
                        Thing partThing = ThingMaker.MakeThing(part.def.spawnThingOnRemoved);
                        // Drop near the pod
                        GenPlace.TryPlaceThing(partThing, pod.Position, pod.Map, ThingPlaceMode.Near);
                    }

                    // Apply MissingBodyPart hediff
                    patient.health.AddHediff(HediffDefOf.MissingBodyPart, part);
                }
            }
            // 3. Administer Item (Drug)
            else if (recipe.workerClass == typeof(Recipe_AdministerUsableItem))
            {
                if (ingredients.Count > 0)
                {
                    // Ingest the item
                    ingredients[0].Ingested(patient, 0);
                }
            }
            // 4. Anesthetize / Euthanize (Special Cases)
            else if (recipe.defName == "Anesthetize")
            {
                patient.health.AddHediff(HediffDefOf.Anesthetic);
            }
            else if (recipe.defName == "Euthanize")
            {
                ExecutionUtility.DoExecutionByCut(null, patient);
            }
            // 5. Fallback
            else
            {
                // Try to run the worker with null billDoer. 
                // This is risky but handles other custom recipes.
                try
                {
                    recipe.Worker.ApplyOnPawn(patient, part, null, ingredients, bill);
                }
                catch (Exception e)
                {
                    Log.Error($"YAMP: Failed to perform operation {recipe.label}: {e.Message}");
                }
            }
        }
    }
}
