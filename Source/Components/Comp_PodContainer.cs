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
    }
}
