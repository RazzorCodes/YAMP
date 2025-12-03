using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YAMP
{

    public class PodContainer
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

        public List<Thing> Get()
        {
            _container ??= new ThingOwner<Thing>(_owner);
            return [.. _container];
        }

        public Pawn GetPawn()
        {
            _container ??= new ThingOwner<Thing>(_owner);
            return _container.OfType<Pawn>().FirstOrDefault();
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            _container ??= new ThingOwner<Thing>(_owner);
            return _container;
        }
    }
}
