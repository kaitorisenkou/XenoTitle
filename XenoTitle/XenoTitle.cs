using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace XenoTitle {
    [StaticConstructorOnStartup]
    public class XenoTitle {
        static XenoTitle() {
            Log.Message("[XenoTitle] Now active");
            var harmony = new Harmony("kaitorisenkou.XenoTitle");

            harmony.Patch(
                AccessTools.Method(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.GetTitleAwardedWhenUpdating), null, null),
                null,
                null,
                new HarmonyMethod(typeof(XenoTitle), nameof(Patch_GetTitleAwardedWhenUpdating), null),
                null
                );

            Log.Message("[XenoTitle] Harmony patch complete!");
        }


        static IEnumerable<CodeInstruction> Patch_GetTitleAwardedWhenUpdating(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionList = instructions.ToList();
            int patchCount = 0;
            MethodInfo targetInfo = AccessTools.Method(typeof(RoyalTitleDefExt), nameof(RoyalTitleDefExt.GetNextTitle));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i + 1, new CodeInstruction[]{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XenoTitle),nameof(GetModExt_XenoTitle)))
                    });
                    patchCount++;
                    if (patchCount > 1)
                        break;
                }
            }
            if (patchCount < 2) {
                Log.Error("[XenoTitle] Patch_GetTitleAwardedWhenUpdating seems failed!");
            }
            return instructionList;
        }
        static RoyalTitleDef GetModExt_XenoTitle(RoyalTitleDef title, Pawn_RoyaltyTracker tracker, Faction faction) {
            RoyalTitleDef currentTitle = title;
            XenotypeDef xenotype = tracker.pawn.genes.Xenotype;
            while (currentTitle.GetNextTitle(faction) != null) {
                var ext = currentTitle.GetModExtension<ModExtension_XenoTitle>();
                if (ext == null)
                    return currentTitle;
                if (ext.IsAllowed(xenotype)) {
                    return currentTitle;
                }
                currentTitle = currentTitle.GetNextTitle(faction);
            }
            return currentTitle;
        }
    }
    public class ModExtension_XenoTitle : DefModExtension {
        public List<XenotypeDef> xenotypeDefs = null;
        public bool isDenylist = false;

        public bool IsAllowed(XenotypeDef xenotype) {
            if (xenotype == null) return false;
            if (xenotypeDefs == null) return isDenylist;
            return xenotypeDefs.Contains(xenotype) ^ isDenylist;
        }
    }
}
