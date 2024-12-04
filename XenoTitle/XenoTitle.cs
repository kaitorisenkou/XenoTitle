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
            MethodInfo[] methodsPatch = { 
                AccessTools.Method(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.GetTitleAwardedWhenUpdating), null, null),
                AccessTools.Method(typeof(Pawn_RoyaltyTracker), "UpdateRoyalTitle", null, null),
                AccessTools.Method(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.CanUpdateTitle), null, null),
            };
            foreach(var i in methodsPatch) {
                harmony.Patch(i, null, null,
                    new HarmonyMethod(typeof(XenoTitle), nameof(Patch_RoyaltyTracker), null)
                    , null);
            }

            Log.Message("[XenoTitle] Harmony patch complete!");
        }


        static IEnumerable<CodeInstruction> Patch_RoyaltyTracker(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionList = instructions.ToList();
            int patchCount = 0;
            MethodInfo targetInfo = AccessTools.Method(typeof(RoyalTitleDefExt), nameof(RoyalTitleDefExt.GetNextTitle));
            //MethodInfo insertInfo1 = AccessTools.Method(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.GetCurrentTitle));
            MethodInfo insertInfo2 = AccessTools.Method(typeof(XenoTitle), nameof(GetModExt_XenoTitle));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList.RemoveAt(i);
                    instructionList.InsertRange(i, new CodeInstruction[]{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, insertInfo2)
                    });
                    patchCount++;
                }
            }
            if (patchCount < 1) {
                Log.Error("[XenoTitle] Patch_GetTitleAwardedWhenUpdating seems failed!");
            }
            return instructionList;
        }
        static RoyalTitleDef GetModExt_XenoTitle(RoyalTitleDef title, Faction faction, Pawn_RoyaltyTracker tracker) {
            var currentExt = title?.GetModExtension<ModExtension_XenoTitle>();
            if (currentExt != null && currentExt.noFurtherTitles) {
                return null;
            }
            RoyalTitleDef currentTitle = title.GetNextTitle(faction);
            XenotypeDef xenotype = tracker.pawn.genes.Xenotype;
            while (currentTitle != null) {
                var ext = currentTitle.GetModExtension<ModExtension_XenoTitle>();
                if (ext == null)
                    return currentTitle;
                if (ext.IsAllowed(xenotype)) {
                    return currentTitle;
                }
                var nextTitle = currentTitle.GetNextTitle(faction);
                if (nextTitle == null)
                    return currentTitle;
                currentTitle = nextTitle;
            }
            return currentTitle;
        }
    }
    public class ModExtension_XenoTitle : DefModExtension {
        public List<XenotypeDef> xenotypeDefs = null;
        public bool isDenylist = false;
        public bool noFurtherTitles = false;

        public bool IsAllowed(XenotypeDef xenotype) {
            if (xenotype == null) return false;
            if (xenotypeDefs == null) return isDenylist;
            return xenotypeDefs.Contains(xenotype) ^ isDenylist;
        }
    }
}
