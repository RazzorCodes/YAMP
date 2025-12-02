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
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_container == null)
                {
                    _container = new ThingOwner<Thing>(_owner);
                }
            }
        }

        public List<Thing> Get()
        {
            if (_container == null) _container = new ThingOwner<Thing>(_owner);
            return _container.ToList();
        }

        public Pawn GetPawn()
        {
            if (_container == null) _container = new ThingOwner<Thing>(_owner);
            return _container.OfType<Pawn>().FirstOrDefault();
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            if (_container == null) _container = new ThingOwner<Thing>(_owner);
            return _container;
        }
    }
}
