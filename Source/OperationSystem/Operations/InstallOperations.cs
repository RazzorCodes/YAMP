using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== INSTALL OPERATIONS ====================

    /// <summary>
    /// Install artificial body part (bionics)
    /// </summary>
    public class InstallArtificialPartOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Artificial Body Part";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return part != null && !patient.health.hediffSet.PartIsMissing(part);
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            // Use vanilla's surgery success calculation
            return 1f; // Vanilla Recipe_InstallArtificialBodyPart uses default surgery chance
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // The actual installation is handled by vanilla's ApplyOnPawn
            // We just need to mark success
            Log.Message($"[YAMP] Successfully installed artificial part on {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Install natural body part (organs)
    /// </summary>
    public class InstallNaturalPartOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Natural Body Part";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return part != null;
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f; // Uses vanilla surgery success
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            Log.Message($"[YAMP] Successfully installed natural part on {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Install implant
    /// </summary>
    public class InstallImplantOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Implant";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return true;
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            Log.Message($"[YAMP] Successfully installed implant on {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Install IUD
    /// </summary>
    public class InstallIUDOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install IUD";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return patient.gender == Gender.Female;
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            Log.Message($"[YAMP] Successfully installed IUD on {context.Patient.LabelShort}");
        }
    }
}
