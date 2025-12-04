using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using YAMP.Activities;

namespace YAMP
{
    public class CompProp_PodOperate : CompProperties
    {
        public float surgerySuccessChance = 0.98f; // High success chance
        public float stockConsumption = 15f; // Multiplier for stock consumption

        public CompProp_PodOperate()
        {
            compClass = typeof(Comp_PodOperate);
        }
    }

    public class Comp_PodOperate : ThingComp
    {
        public CompProp_PodOperate Props => (CompProp_PodOperate)props;

        private OperationalStock _operationalStock;
        public OperationalStock OperationalStock =>
            _operationalStock ??= ((Building_MedPod)parent).Stock;

        private PodContainer _podConatiner;
        private PodContainer PodConatiner =>
            _podConatiner ??= ((Building_MedPod)parent).Container;

        private Bill_Medical _currentBill = null;

        private YAMP.Activities.IActivity _currentActivity = null;
        public float Progress => _currentActivity?.ProgressPercentage ?? 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        public override void CompTick()
        {
            base.CompTick();

            // Handle activity completion
            if (_currentActivity != null && _currentActivity.IsFinished)
            {
                Logger.Debug("Activity finished");
                _currentActivity = null;

                // Safely remove the bill
                if (_currentBill != null)
                {
                    var medPod = (Building_MedPod)parent;
                    var billStack = medPod.BillStack;
                    if (billStack != null && billStack.Bills.Contains(_currentBill))
                    {
                        billStack.Delete(_currentBill);
                        Logger.Debug($"Removed bill: {_currentBill.Label}");
                    }
                    _currentBill = null;
                }

                // Check for next operation in queue
                CheckOperation();
                return;
            }

            // Stop activity if patient leaves
            if (_currentActivity != null && PodConatiner.GetPawn() == null)
            {
                _currentActivity.Stop();
                _currentActivity = null;
                _currentBill = null;
                return;
            }

            // Update activity progress on interval
            if (_currentActivity != null && parent.IsHashIntervalTick(250))
            {
                _currentActivity.Update(Verse.GenTicks.TicksGame);
            }

            CheckOperation();
        }

        public void CheckOperation()
        {
            if (_currentActivity != null)
            {
                return; // Already have an activity running
            }

            _currentBill ??= GetSurgeryBill();
            if (_currentBill == null)
            {
                Logger.Debug("No surgery bill found");
                return;
            }

            // Reserve parts and calculate stock cost before starting activity
            var parts = OperateHandler.ReserveParts(PodConatiner, _currentBill);
            if (parts == null)
            {
                Logger.Debug($"Missing parts for {_currentBill.recipe.label}");
                return;
            }

            var stockCost = OperationalStock.CalculateStockCost(_currentBill.recipe);
            if (stockCost > OperationalStock.TotalStock)
            {
                Logger.Debug($"Missing stock for {_currentBill.recipe.label}. Needed {stockCost}, have {OperationalStock.TotalStock}");
                OperateHandler.ReturnParts(PodConatiner, parts, _currentBill);
                return;
            }

            var facility = (Building_MedPod)parent;
            var args = new object[] { facility, _currentBill, parts, stockCost };

            _currentActivity = new Activity(
                name: $"Operate/{_currentBill.recipe.label}",
                onComplete: OperateHandler.OnCompleteHandler,
                onStop: OperateHandler.OnStopHandler,
                args: args,
                speedMultiplier: 1f
            );

            float workAmount = _currentBill.GetWorkAmount() > 100 ? _currentBill.GetWorkAmount() : 100;
            _currentActivity.Start(workAmount);
            Logger.Debug($"Started operation activity: {_currentBill.recipe.label}");
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(
                    "CannotEnter".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null
                );
                yield break;
            }

            if (PodConatiner.GetPawn() != null)
            {
                yield return new FloatMenuOption(
                    "CannotEnter".Translate() + ": " + "Full".Translate().CapitalizeFirst(),
                    null
                );
                yield break;
            }

            yield return new FloatMenuOption(
                "Enter Med Pod",
                () =>
                {
                    Job job = JobMaker.MakeJob(
                        DefDatabase<JobDef>.GetNamed("YAMP_EnterMedPod"),
                        parent
                    );
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            );
        }

        private Bill_Medical GetSurgeryBill()
        {
            var medPod = parent as Building_MedPod;
            if (medPod?.BillStack == null)
            {
                Logger.Debug("MedPod BillStack is null");
                return null;
            }

            foreach (Bill b in medPod.BillStack)
            {
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    Logger.Debug($"Found surgery bill: {bm.Label}");
                    return bm;
                }
            }
            Logger.Debug("No surgery bill found");

            return null;
        }

        public override string CompInspectStringExtra()
        {
            // Pawn info: todo: move to building
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Patient: {PodConatiner.GetPawn()?.Name?.ToStringShort}");
            if (PodConatiner.GetPawn() != null)
            {
                if (_currentActivity != null)
                {
                    if (_currentActivity.InProgress)
                    {
                        sb.AppendLine($"Operating: {_currentActivity.Name} ({_currentActivity.ProgressPercentage:P0})");
                    }
                    else
                    {
                        sb.AppendLine($"Pending Operation: {_currentActivity.Name}");
                    }
                }
                else
                {
                    sb.AppendLine("No pending operations");
                }
            }

            // Stock: todo move to building
            // 1. Current operational buffer and stock
            sb.AppendLine($"Buffer: {OperationalStock.Buffer:F1}");
            sb.AppendLine($"Stock: {OperationalStock.TotalStock:F1}");

            // 2. How many of each medicine we have
            var stockItemsInContainer = PodConatiner.Get()
                .Where(thing => thing.def.IsMedicine)
                .GroupBy(thing => thing.def)
                .Select(group => new { Def = group.Key, Count = group.Sum(thing => thing.stackCount) })
                .OrderBy(item => item.Def.label);

            if (stockItemsInContainer.Any())
            {
                sb.AppendLine("Stock Items:");
                foreach (var item in stockItemsInContainer)
                {
                    sb.AppendLine($"  - {item.Def.LabelCap}: {item.Count}");
                }
            }
            else
            {
                sb.AppendLine("Stock Items: None");
            }

            var otherItemsInContainer = PodConatiner.Get()
                .Where(thing => !thing.def.IsMedicine && thing.GetType() != typeof(Pawn))
                .GroupBy(thing => thing.def)
                .Select(group => new { Def = group.Key, Count = group.Sum(thing => thing.stackCount) })
                .OrderBy(item => item.Def.label);

            if (otherItemsInContainer.Any())
            {
                sb.AppendLine("Other Items:");
                foreach (var item in otherItemsInContainer)
                {
                    sb.AppendLine($"  - {item.Def.LabelCap}: {item.Count}");
                }
            }
            else
            {
                sb.AppendLine("Other Items: None");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
