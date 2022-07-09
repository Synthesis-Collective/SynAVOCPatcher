using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Fallout4;

namespace AVOCPatcher
{
    public class Program
    {
        const string CoreEspName = "A Variety of Containers.esp";

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<IFallout4Mod, IFallout4ModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.Fallout4, "AVOCPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
        {
            Console.WriteLine("AVOCPatcher - RunPatch - START");

            var avop = state.LoadOrder.GetIfEnabled(CoreEspName);

            if (avop.Mod == null)
            {
                return;
            }

            Console.WriteLine($"Processing {avop.ModKey}");

            foreach (var avopContext in avop.Mod.EnumerateMajorRecordContexts<IPlacedObject, IPlacedObjectGetter>(state.LinkCache))
            {
                // Check if record has a material swap value.
                if (avopContext.Record.MaterialSwap == null || avopContext.Record.MaterialSwap.IsNull)
                {
                    Console.WriteLine($"Skipping {avopContext.Record.FormKey} due to no material swap value.");
                    continue;
                }

                // Get winning record.
                if (state.LinkCache.TryResolveContext<IPlacedObject, IPlacedObjectGetter>(avopContext.Record.FormKey, out var winner))
                {
                    // Check if avop is already the winning record.
                    if (winner.ModKey == avopContext.ModKey)
                    {
                        Console.WriteLine($"Skipping {avopContext.Record.FormKey} as it is already winning.");
                        continue;
                    }

                    Console.WriteLine($"Copying {winner.Record.FormKey} from {winner.ModKey}");

                    // Copy winning record into patch mod.
                    var copied = winner.GetOrAddAsOverride(state.PatchMod);

                    Console.WriteLine($"Updating material swap from {winner.Record.MaterialSwap.FormKeyNullable?.ToString() ?? "NULL"} to {avopContext.Record.MaterialSwap.FormKeyNullable?.ToString() ?? "NULL"}");

                    // Update the material swap value.
                    copied.MaterialSwap.SetTo(avopContext.Record.MaterialSwap.FormKey);
                }
            }

            Console.WriteLine("AVOCPatcher - RunPatch - END");
        }
    }
}
