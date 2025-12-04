using Verse;

namespace YAMP
{
    public interface IMedicineDefProvider
    {
        ThingDef Herbal { get; }
        ThingDef Industrial { get; }
        ThingDef Ultratech { get; }
    }
}
