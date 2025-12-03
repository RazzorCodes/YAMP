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
            _operationalStock ??= ((Building_MedPod)parent).OperationalStock;
        private PodContainer _podConatiner;
        private PodContainer PodConatiner =>
            _podConatiner ??= ((Building_MedPod)parent).Container;

        private int currentTick = 0;
        private bool isOperating = false;
        private Bill_Medical currentBill = null;
        private List<Thing> currentParts = null;

        private ActivityOperate _currentActivity = null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentTick, "currentTick", 0);
            Scribe_Values.Look(ref isOperating, "isOperating", false);
        }

        public override void CompTickRare()
        {
            if (_currentActivity == null)
            {
                return;
            }

            if (_currentActivity.IsFinished)
            {
                _currentActivity = null;
                return;
            }

            if (PodConatiner.GetPawn() == null)
            {
                _currentActivity.Stop();
                return;
            }

            if (parent.IsHashIntervalTick(250))
            {
                _currentActivity.Update();
            }
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

        private Bill_Medical GetSurgeryBill(Pawn patient)
        {
            var medPod = parent as Building_MedPod;
            if (medPod?.BillStack == null)
            {
                return null;
            }

            Bill_Medical bill = null;
            foreach (Bill b in medPod.BillStack)
            {
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    bill = bm;
                    return bm;
                }
            }

            return bill;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Pawn patient = PodConatiner.GetPawn();
            if (patient == null)
            {
                return;
            }

            if (isOperating)
            {
                Vector3 barPos = parent.DrawPos;
                barPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                barPos += Vector3.forward * 0.25f;

                GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
                {
                    center = barPos,
                    size = new Vector2(0.8f, 0.14f),
                    fillPercent = (float)currentTick / (currentBill.recipe.workAmount / 2f),
                    filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f)),
                    unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)),
                    margin = 0.15f,
                    rotation = Rot4.North
                });
            }

            Vector3 drawPos = parent.DrawPos;
            drawPos.y += 0.04f;
            float angle = (Time.realtimeSinceStartup * 50f) % 360f;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(2f, 1f, 2f)
            );
            Graphics.DrawMesh(MeshPool.plane10, matrix, YAMP_Assets.ActiveOverlayMat, 0);
        }

        public override string CompInspectStringExtra()
        {
            // Pawn info: todo: move to building
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            Pawn patient = PodConatiner.GetPawn();
            if (patient != null)
            {
                if (isOperating && currentBill != null)
                {
                    return $"Operating: {currentBill.recipe.label} ({(float)currentTick / (currentBill.recipe.workAmount / 2f):P0})";
                }

                Bill_Medical bill = GetSurgeryBill(patient);
                if (bill != null)
                {
                    return $"Pending Operation: {bill.recipe.label}";
                }

                return "No pending operations";
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

            return sb.ToString().TrimEnd();
        }
    }
}