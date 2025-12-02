using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YAMP
{

    public class PodContainer : IExposable
    {
        private ThingOwner<Thing> _container;
        private IThingHolder _owner;

        public PodContainer(IThingHolder owner)
        {
            _owner = owner;
            _container = new ThingOwner<Thing>(owner);
        }

        public PodContainer()
        {
            _container = new ThingOwner<Thing>();
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref _container, "innerContainer", _owner);
        }

        public List<Thing> Get()
        {
            return _container.ToList();
        }

        public Pawn GetPawn()
        {
            return _container.OfType<Pawn>().FirstOrDefault();
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return _container;
        }
    }
}
