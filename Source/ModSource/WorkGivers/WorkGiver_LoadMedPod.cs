using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class WorkGiver_LoadMedPod : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(InternalDefOf.YAMP_MedPod);
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_MedPod pod)) return false;

            OperationalStock stock = pod.Stock;
            Comp_PodOperate opsComp = pod.TryGetComp<Comp_PodOperate>();
            PodContainer container = pod.Container;

            if (stock == null || opsComp == null || container == null) return false;

            // Check if we need fuel (no max capacity check - just check if stock is low)
            if (stock.TotalStock < 100f)
            {
                Thing medicine = FindMedicine(pawn, pod);
                if (medicine != null) return true;
            }

            // Check if we need ingredients for operation
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

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_MedPod pod = (Building_MedPod)t;
            OperationalStock fuelComp = pod.Stock;
            Comp_PodOperate opsComp = pod.TryGetComp<Comp_PodOperate>();
            PodContainer podContainer = pod.Container;

            // Prioritize Fuel if very low
            if (fuelComp.TotalStock < 50f)
            {
                Thing medicine = FindMedicine(pawn, pod);
                if (medicine != null)
                {
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_LoadMedPod"), medicine, pod);
                    job.count = medicine.stackCount;
                    return job;
                }
            }

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

            // Fallback to fuel
            if (fuelComp.TotalStock < 100f)
            {
                Thing medicine = FindMedicine(pawn, pod);
                if (medicine != null)
                {
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_LoadMedPod"), medicine, pod);
                    job.count = medicine.stackCount;
                    return job;
                }
            }

            return null;
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

        private Thing FindMedicine(Pawn pawn, Building_MedPod pod)
        {
            OperationalStock fuelComp = pod.Stock;
            if (fuelComp == null) return null;

            // 1. Search nearby (linked) shelves
            // unimplemented
            // 2. Search the map
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Medicine), PathEndMode.ClosestTouch, TraverseParms.For(pawn),
                9999, x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && pod.GetStoreSettings().AllowedToAccept(x));
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
                    // 1. Check Linked Shelves
                    // unimplemented

                    // 2. Search the map
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
