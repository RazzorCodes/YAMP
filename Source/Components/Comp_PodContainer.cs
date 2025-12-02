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

            // Eject Products gizmo - Only for surgery products (not pawns, not medicine)
            var products = _container.Where(t => !(t is Pawn) && !t.def.IsMedicine).ToList();
            if (products.Any())
            {
                yield return new Command_Action
                {
                    defaultLabel = "Eject Products",
                    defaultDesc = $"Eject {products.Count} surgery product(s) from the pod.",
                    icon = TexCommand.GatherSpotActive,
                    action = () =>
                    {
                        int ejected = 0;
                        foreach (Thing product in products.ToList())
                        {
                            if (_container.TryDrop(product, parent.Position, parent.Map, ThingPlaceMode.Near, out _))
                            {
                                ejected++;
                            }
                        }

                        Messages.Message($"Ejected {ejected} product(s) from med pod.", parent,
                            MessageTypeDefOf.TaskCompletion);
                    }
                };
            }
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
