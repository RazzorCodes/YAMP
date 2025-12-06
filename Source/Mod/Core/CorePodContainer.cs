using System.Collections.Generic;
using System.Linq;
using Verse;

namespace YAMP
{

    public class PodContainer : IExposable, IThingHolder
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
            Scribe_Deep.Look(ref _container, "container", _owner);
        }

        public List<Thing> Get()
        {
            _container ??= new ThingOwner<Thing>(_owner);
            return [.. _container];
        }

        // GetPawn removed - use Building_Bed.GetCurOccupant(0)

        public ThingOwner GetDirectlyHeldThings()
        {
            _container ??= new ThingOwner<Thing>(_owner);
            return _container;
        }

        // IThingHolder interface
        public IThingHolder ParentHolder => _owner;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            // Container doesn't have nested holders
        }
    }
}
