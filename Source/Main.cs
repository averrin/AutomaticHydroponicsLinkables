using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using PipeSystem;

namespace AutomaticHydroponicsLinkables
{
    public class FacilitySpeedBoostExtension : DefModExtension
    {
        public float tickFactorPerLink = 0.5f;
        public int maxLinks = 2;
        public float speedMultPerLink = 0f; // multiplicative on ticks; >1 slows; <1 speeds; 0 to ignore
        public float yieldMultPerLink = 1f; // multiplicative on result yield
    }

    [StaticConstructorOnStartup]
    public static class Init
    {
        static Init()
        {
            try
            {
                Log.Message("[AutomaticHydroponicsLinkables] Initializing...");
                var harmony = new Harmony("AutomaticHydroponicsLinkables");
                harmony.Patch(
                    AccessTools.Method(typeof(PipeSystem.Process), nameof(PipeSystem.Process.Tick)),
                    prefix: new HarmonyMethod(typeof(ProcessSpeedPatch), nameof(ProcessSpeedPatch.Prefix))
                );
                harmony.Patch(
                    AccessTools.Method(typeof(PipeSystem.Process), "DoInterface"),
                    prefix: new HarmonyMethod(typeof(ProcessDoInterfaceEffectiveTime), nameof(ProcessDoInterfaceEffectiveTime.Prefix)),
                    postfix: new HarmonyMethod(typeof(ProcessDoInterfaceEffectiveTime), nameof(ProcessDoInterfaceEffectiveTime.Postfix))
                );
                harmony.Patch(
                    AccessTools.PropertyGetter(typeof(PipeSystem.CompAdvancedResourceProcessor), "ProcessesOptions"),
                    prefix: new HarmonyMethod(typeof(ProcessesOptionsPatch), nameof(ProcessesOptionsPatch.Prefix)),
                    postfix: new HarmonyMethod(typeof(ProcessesOptionsPatch), nameof(ProcessesOptionsPatch.Postfix))
                );
            }
            catch (Exception ex)
            {
                Log.Error("[AutomaticHydroponicsLinkables] init failed: " + ex);
            }
        }
    }

    public static class ProcessSpeedPatch
    {
        public static float ComputeFactorFromParent(Thing parent)
        {
            var affected = parent.TryGetComp<CompAffectedByFacilities>();
            var linked = affected?.LinkedFacilitiesListForReading;
            if (linked == null || linked.Count == 0) return 1f;

            float bonus = 0f;
            float mult = 1f;
            // group by facility def to enforce per-def maxLinks
            foreach (var group in linked.Where(b => b?.def != null).GroupBy(b => b.def))
            {
                var def = group.Key;
                var ext = def.GetModExtension<FacilitySpeedBoostExtension>();
                if (ext == null)
                    continue;
                int appliedCount = Mathf.Min(group.Count(), Mathf.Max(0, ext.maxLinks));
                if (appliedCount <= 0)
                    continue;
                if (Mathf.Abs(ext.speedMultPerLink) > 1e-4f)
                {
                    mult *= Mathf.Pow(ext.speedMultPerLink, appliedCount);
                }
                else if (Mathf.Abs(ext.tickFactorPerLink) > 1e-4f)
                {
                    bonus += ext.tickFactorPerLink * appliedCount;
                }
            }

            var factor = mult * (1f + bonus);
            return factor <= 0f ? 1f : factor;
        }

        public static void Prefix(PipeSystem.Process __instance, ref int ticks)
        {
            try
            {
                var parent = __instance?.advancedProcessor?.parent;
                if (parent == null) return;
                float factor = ComputeFactorFromParent(parent);
                if (factor <= 1f) return;
                Log.Message($"[AutomaticHydroponicsLinkables] Tick boost factor {factor:0.##} for {parent.def?.defName}");
                ticks = Mathf.CeilToInt(ticks * factor);
            }
            catch (Exception)
            {
                Log.Error("[AutomaticHydroponicsLinkables] Tick prefix failed");
            }
        }
    }

    public static class YieldUtil
    {
        public static float ComputeYieldMultiplierFromParent(Thing parent)
        {
            var affected = parent.TryGetComp<CompAffectedByFacilities>();
            var linked = affected?.LinkedFacilitiesListForReading;
            if (linked == null || linked.Count == 0) return 1f;

            float mult = 1f;
            foreach (var group in linked.Where(b => b?.def != null).GroupBy(b => b.def))
            {
                var def = group.Key;
                var ext = def.GetModExtension<FacilitySpeedBoostExtension>();
                if (ext == null) continue;
                int appliedCount = Mathf.Min(group.Count(), Mathf.Max(0, ext.maxLinks));
                if (appliedCount <= 0) continue;
                if (Mathf.Abs(ext.yieldMultPerLink - 1f) > 1e-4f)
                {
                    mult *= Mathf.Pow(ext.yieldMultPerLink, appliedCount);
                }
            }
            return mult <= 0f ? 1f : mult;
        }
    }

    [HarmonyPatch(typeof(PipeSystem.Process))]
    [HarmonyPatch("DoInterface", MethodType.Normal)]
    public static class ProcessDoInterfaceEffectiveTime
    {
        public static void Prefix(PipeSystem.Process __instance, float x, float y, float width, int index, ref int __state)
        {
            try
            {
                __state = __instance.ticksOrQualityTicks;
                var parent = __instance?.advancedProcessor?.parent;
                if (parent == null) return;
                float factor = ProcessSpeedPatch.ComputeFactorFromParent(parent);
                float yieldMult = YieldUtil.ComputeYieldMultiplierFromParent(parent);
                if (factor <= 1f && Mathf.Abs(yieldMult - 1f) < 1e-4f) return;
                int displayTicks = Mathf.Max(1, Mathf.RoundToInt(__instance.ticksOrQualityTicks / factor));
                __instance.ticksOrQualityTicks = displayTicks;
            }
            catch (Exception ex)
            {
                Log.Error("[AutomaticHydroponicsLinkables] prefix failed: " + ex.Message);
            }
        }

        public static void Postfix(PipeSystem.Process __instance, int __state)
        {
            try
            {
                __instance.ticksOrQualityTicks = __state;
            }
            catch (Exception ex)
            {
                Log.Error("[AutomaticHydroponicsLinkables] postfix failed: " + ex.Message);
            }
        }
    }

    public static class ProcessesOptionsPatch
    {
        private static System.Collections.Generic.Dictionary<PipeSystem.ProcessDef, int> baseTicksCache = new System.Collections.Generic.Dictionary<PipeSystem.ProcessDef, int>();
        private static System.Collections.Generic.List<PipeSystem.ProcessDef> filteredOrdered = new System.Collections.Generic.List<PipeSystem.ProcessDef>();

        public static void Prefix(PipeSystem.CompAdvancedResourceProcessor __instance)
        {
            try
            {
                // Build a cache of base ticks for each process def (from ticksQuality or ticks)
                baseTicksCache.Clear();
                filteredOrdered.Clear();
                var processes = __instance.Props?.processes;
                if (processes == null) return;
                var list = processes.OrderBy(x => x.priorityInBillList).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var pd = list[i];
                    if (pd.researchPrerequisites != null && pd.researchPrerequisites.Any(p => !p.IsFinished)) continue;
                    filteredOrdered.Add(pd);
                    int baseTicks = !pd.ticksQuality.NullOrEmpty() ? pd.ticksQuality[(int)QualityCategory.Normal] : pd.ticks;
                    baseTicksCache[pd] = baseTicks;
                }
            }
            catch (Exception)
            {
                Log.Error("[AutomaticHydroponicsLinkables] ProcessesOptions prefix failed");
            }
        }

        public static void Postfix(PipeSystem.CompAdvancedResourceProcessor __instance, ref System.Collections.Generic.List<FloatMenuOption> __result)
        {
            try
            {
                var parent = __instance?.parent;
                if (parent == null || __result == null || __result.Count == 0) return;
                float factor = ProcessSpeedPatch.ComputeFactorFromParent(parent);
                float yieldMult = YieldUtil.ComputeYieldMultiplierFromParent(parent);
                if (factor <= 1f && Mathf.Abs(yieldMult - 1f) < 1e-4f) return;
                int count = Math.Min(__result.Count, filteredOrdered.Count);
                for (int i = 0; i < count; i++)
                {
                    var pd = filteredOrdered[i];
                    var opt = __result[i];
                    var labelField = AccessTools.Field(opt.GetType(), "label") ?? AccessTools.Field(opt.GetType(), "labelInt");
                    var current = labelField != null ? (string)labelField.GetValue(opt) ?? string.Empty : opt.Label ?? string.Empty;

                    if (!baseTicksCache.TryGetValue(pd, out var baseTicks))
                    {
                        baseTicks = !pd.ticksQuality.NullOrEmpty() ? pd.ticksQuality[(int)QualityCategory.Normal] : pd.ticks;
                    }
                    int effectiveTicks = Mathf.Max(1, Mathf.RoundToInt(baseTicks / factor));
                    string suffix = yieldMult > 1.0001f
                        ? $"  ({effectiveTicks.ToStringTicksToDays()}, x{yieldMult:0.##} yield)"
                        : $"  ({effectiveTicks.ToStringTicksToDays()})";

                    if (current.EndsWith(suffix)) continue;

                    if (labelField != null)
                    {
                        try
                        {
                            labelField.SetValue(opt, current + suffix);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("[AutomaticHydroponicsLinkables] Failed to set label via field: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Log.Error("[AutomaticHydroponicsLinkables] ProcessesOptions postfix failed");
            }
        }
    }
}



