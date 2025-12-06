using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using System;

namespace YAMP
{
    public class WorkGiver_LoadMedPod : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(InternalDefOf.YAMP_MedPod);
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;



        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_MedPod pod = (Building_MedPod)t;
            OperationalStock stock = pod.Stock;
            Comp_PodOperate opsComp = pod.TryGetComp<Comp_PodOperate>();
            PodContainer container = pod.Container;

            // 1. Check Medicine Refueling based on settings
            if (pod.medicineRanges != null)
            {
                foreach (var kvp in pod.medicineRanges)
                {
                    string defName = kvp.Key;
                    IntRange range = kvp.Value;

                    ThingDef medDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                    if (medDef == null) continue;

                    int currentCount = container.Get()
                        .Where(x => x.def == medDef)
                        .Sum(x => x.stackCount);

                    if (currentCount < range.min)
                    {
                        // Needs refueling
                        int shortage = range.max - currentCount;
                        Thing medicine = FindMedicine(pawn, pod, medDef, shortage);
                        if (medicine != null)
                        {
                            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_LoadMedPod"), medicine, pod);
                            job.count = Math.Min(medicine.stackCount, shortage);
                            return job;
                        }
                    }
                }
            }
            // Fallback for legacy saves or uninitialized settings? 
            // If medicineRanges is empty, maybe we should default to old behavior or just do nothing?
            // User requested "ignore current implementatons", implying we should switch fully.
            // But if user hasn't configured anything, nothing will happen. That's fine.

            // 2. Check Surgery Ingredients
            Pawn patient = pod.GetCurOccupant(0);
            if (patient != null)
            {
                Bill_Medical bill = GetFirstSurgeryBill(pod);
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

            return null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_MedPod pod)) return false;

            OperationalStock stock = pod.Stock;
            Comp_PodOperate opsComp = pod.TryGetComp<Comp_PodOperate>();
            PodContainer container = pod.Container;

            if (stock == null || opsComp == null || container == null) return false;

            // Check medicine needs
            if (pod.medicineRanges != null)
            {
                foreach (var kvp in pod.medicineRanges)
                {
                    string defName = kvp.Key;
                    IntRange range = kvp.Value;
                    ThingDef medDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                    if (medDef == null) continue;

                    int currentCount = container.Get().Where(x => x.def == medDef).Sum(x => x.stackCount);
                    if (currentCount < range.min)
                    {
                        if (FindMedicine(pawn, pod, medDef, range.max - currentCount) != null) return true;
                    }
                }
            }

            // Check surgery needs
            Pawn patient = pod.GetCurOccupant(0);
            if (patient != null)
            {
                Bill_Medical bill = GetFirstSurgeryBill(pod);
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

        private Bill_Medical GetFirstSurgeryBill(Building_MedPod pod)
        {
            if (pod?.BillStack == null) return null;
            foreach (Bill b in pod.BillStack)
            {
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    return bm;
                }
            }

            return null;
        }

        private Thing FindMedicine(Pawn pawn, Building_MedPod pod, ThingDef specificDef, int countNeeded)
        {
            // Search the map for specific medicine
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForDef(specificDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn),
                9999, x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && x.stackCount > 0);
        }

        private Thing FindIngredientForRecipe(Pawn pawn, Comp_PodOperate ops, RecipeDef recipe)
        {
            Building_MedPod pod = ops.parent as Building_MedPod;
            if (pod?.Container == null) return null;

            foreach (IngredientCount ing in recipe.ingredients)
            {
                if (ing.filter.AllowedThingDefs.Any(t => t.IsMedicine)) continue;

                float needed = ing.GetBaseCount();
                float has = 0;

                foreach (Thing t in pod.Container.Get())
                {
                    if (ing.filter.Allows(t)) has += t.stackCount;
                }

                if (has < needed)
                {
                    return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.ClosestTouch,
                        TraverseParms.For(pawn), 9999,
                        x => ing.filter.Allows(x) && !x.IsForbidden(pawn) && pawn.CanReserve(x));
                }
            }

            return null;
        }

        private bool MissingIngredients(Comp_PodOperate ops, RecipeDef recipe, out Thing foundIngredient)
        {
            foundIngredient = null;
            Building_MedPod pod = ops.parent as Building_MedPod;
            if (pod?.Container == null) return false;

            foreach (IngredientCount ing in recipe.ingredients)
            {
                if (ing.filter.AllowedThingDefs.Any(t => t.IsMedicine)) continue;

                float needed = ing.GetBaseCount();
                float has = 0;
                foreach (Thing t in pod.Container.Get())
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
