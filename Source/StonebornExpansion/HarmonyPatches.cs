using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace StonebornExpansion
{
    public class StonebornExpansionPatches : Mod
    {
        public Harmony harmonyInstance;

        public StonebornExpansionPatches(ModContentPack content) : base(content)
        {
            harmonyInstance = new Harmony(id: "rimworld.detvisor.stonebornexpansion.main");
            harmonyInstance.PatchAll();
        }
    }

    [HarmonyPatch(typeof(CompCreatesInfestations), nameof(CompCreatesInfestations.CanCreateInfestationNow), MethodType.Getter)]
    public static class CompCreatesInfestations_CanCreateInfestationNow
    {
        public static void Postfix(CompCreatesInfestations __instance, ref bool __result)
        {
            CompAutoDrill comp = __instance.parent.TryGetComp<CompAutoDrill>();

            if (comp != null && !comp.usedLastTick)
            {
                __result = false;
            }
        }
    }
}
