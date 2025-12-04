using RimWorld;
using Verse;

namespace YAMP
{
    public class MedicineDefProvider : IMedicineDefProvider
    {
        public ThingDef Herbal => ThingDefOf.MedicineHerbal;
        public ThingDef Industrial => ThingDefOf.MedicineIndustrial;
        public ThingDef Ultratech => ThingDefOf.MedicineUltratech;
    }
}
