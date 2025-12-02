using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Central registry mapping vanilla recipe workers to custom operation implementations
    /// </summary>
    [StaticConstructorOnStartup]
    public static class OperationRegistry
    {
        private static Dictionary<Type, IOperation> _handlers = new Dictionary<Type, IOperation>();

        static OperationRegistry()
        {
            RegisterDefaults();
            Log.Message("[YAMP] Operation registry initialized");
        }

        private static void RegisterDefaults()
        {
            // ===== INSTALL OPERATIONS =====
            Register(typeof(Recipe_InstallArtificialBodyPart), new InstallArtificialPartOperation());
            Register(typeof(Recipe_InstallNaturalBodyPart), new InstallNaturalPartOperation());
            Register(typeof(Recipe_InstallImplant), new InstallImplantOperation());
            Register(typeof(Recipe_ImplantIUD), new InstallIUDOperation());

            // ===== REMOVE OPERATIONS =====
            Register(typeof(Recipe_RemoveBodyPart), new RemovePartOperation());
            Register(typeof(Recipe_RemoveBodyPart_Cut), new RemovePartOperation());
            Register(typeof(Recipe_RemoveBodyPart_CutMany), new RemovePartOperation());
            Register(typeof(Recipe_RemoveImplant), new RemovePartOperation());

            // ===== EXTRACT OPERATIONS =====
            Register(typeof(Recipe_ExtractHemogen), new ExtractHemogenOperation());
            Register(typeof(Recipe_ExtractOvum), new ExtractOvumOperation());

            // ===== EXECUTE OPERATIONS =====
            Register(typeof(Recipe_ExecuteByCut), new ExecuteByCutOperation());
            Register(typeof(Recipe_TerminatePregnancy), new TerminatePregnancyOperation());

            // ===== ADMINISTER OPERATIONS =====
            Register(typeof(Recipe_AdministerIngestible), new AdministerIngestibleOperation());
            Register(typeof(Recipe_AdministerUsableItem), new AdministerUsableItemOperation());
            Register(typeof(Recipe_BloodTransfusion), new BloodTransfusionOperation());
            Register(typeof(Recipe_ImplantEmbryo), new ImplantEmbryoOperation());
            Register(typeof(Recipe_Surgery), new AnesthetizeOperation());
        }

        public static void Register(Type recipeWorkerType, IOperation handler)
        {
            _handlers[recipeWorkerType] = handler;
            Log.Message($"[YAMP] Registered handler for: {recipeWorkerType.Name}");
        }

        public static IOperation GetHandler(Type recipeWorkerType)
        {
            // Exact match
            if (_handlers.TryGetValue(recipeWorkerType, out var handler))
                return handler;

            // Base class match - walk up the hierarchy to find the most specific handler
            Type currentType = recipeWorkerType.BaseType;
            while (currentType != null && currentType != typeof(object))
            {
                if (_handlers.TryGetValue(currentType, out handler))
                    return handler;
                currentType = currentType.BaseType;
            }

            return null; // Use vanilla if no handler
        }
    }
}
