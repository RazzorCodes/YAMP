using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class CompProp_PodContainer : CompProperties
    {
        public CompProp_PodContainer()
        {
            compClass = typeof(Comp_PodContainer);
        }
    }

    public class Comp_PodContainer : ThingComp, IThingHolder
    {
        public CompProp_PodContainer Props => (CompProp_PodContainer)props;

        private ThingOwner<Thing> _container;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref _container, "container", this);
        }

        public Comp_PodContainer()
        {
            _container = new ThingOwner<Thing>(this);
        }

        public List<Thing> Get()
        {
            return _container.ToList();
        }

        public Pawn GetPawn()
        {
            return _container.OfType<Pawn>().FirstOrDefault();
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return _container;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            // TODO: Add fuel filter configuration UI
            // For now, filter accepts all medicine by default

            yield return new Command_Action
            {
                defaultLabel = "Open Pod",
                defaultDesc = "Open the pod to access the patient.",
                icon = TexCommand.ForbidOff,
                action = () =>
                {
                    if (_container != null)
                    {
                        var pawn = GetPawn();
                        if (pawn != null)
                        {
                            _container.TryDrop(pawn, parent.Position, parent.Map, ThingPlaceMode.Near, out _);
                        }
                    }
                }
            };
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn pawn = GetPawn();
            if (pawn != null)
            {
                pawn.Drawer.renderer.RenderPawnAt(parent.DrawPos + new Vector3(0f, 0.05f, 0f));
            }
        }
    }
}
