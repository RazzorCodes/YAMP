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
    public class MedPodSurgery
    {
        public static void Execute(
            Pawn patient, 
            Bill_Medical bill, 
            List<Thing> ingredients,
            ThingWithComps pod)
        {
            try
            {
                bill.recipe.Worker.ApplyOnPawn(patient, bill.Part, null, ingredients, bill);
            }
            catch (Exception e)
            {
                Log.Error($"YAMP: Failed to perform operation {bill.recipe.label}: {e.Message}");
            }
        }
    }
}
