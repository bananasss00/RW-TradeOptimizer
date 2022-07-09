using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;
using System;
using System.Threading.Tasks;

namespace VanillaOptimizations;

[StaticConstructorOnStartup]
public static class Start
{
    static Start()
    {
        new Harmony("pirateby.vanillaoptimizations").PatchAll();
        Log.Message("[VanillaOptimizations] loaded successfully!");
    }
}

[HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.SetAllow), new [] {typeof(ThingDef), typeof(bool)})]
static class Delayed_ThingFilter_SetAllow_Patch
{
    static object locker = new();
    static List<ThingFilter> _delayedCallbacks = new();

    [HarmonyPrefix]
    static void SetAllowPrefix(ThingFilter __instance, ref Action __state)
	{
        if ((__state = __instance.settingsChangedCallback) != null) {
            lock (locker)
            {
                if (!_delayedCallbacks.Contains(__instance))
                {
                    // set delayed callback
                    _delayedCallbacks.Add(__instance);
                    
                    Action callback = __state;
                    
                    Task.Run(async () => {
                        await Task.Delay(1000);
                        
                        callback();

                        #if  DEBUG
                        Log.Warning($"[VanillaOptimizations] Storage settingsChangedCallback called!");
                        #endif

                        lock (locker)
                        {
                            _delayedCallbacks.Remove(__instance);
                        }
                    });
                }
            }
            __instance.settingsChangedCallback = null; // dont call callback from original
        }
    }

    [HarmonyPostfix]
    static void SetAllowPostfix(ThingFilter __instance, Action __state)
    {
        if (__state != null) { // disable callback
            __instance.settingsChangedCallback = __state;
        }
    }
}

[HarmonyPatch(typeof(TraderKindDef), nameof(TraderKindDef.WillTrade))]
static class TradeOptimization_Patch {
    static Dictionary<TraderKindDef, Dictionary<ThingDef, bool>> _willTrade = new();

    [HarmonyPrefix]
    static bool WillTradePrefix(TraderKindDef __instance, ThingDef td, ref bool __result, ref bool __state /* true for cache result */)
    {
        __state = true;
        if (!_willTrade.TryGetValue(__instance, out var tradeKind))
        {
#if  DEBUG
            Log.Warning($"Add traderKind = {__instance}");
#endif
            tradeKind = new(ThingDefComparer.Instance);
            _willTrade.Add(__instance, tradeKind);
            return true;
        }

        if (!tradeKind.TryGetValue(td, out bool result))
        {
#if DEBUG
            Log.Warning($"Add traderKind({__instance}), thingDef = {td}");
#endif
            return true;
        }

        __state = false;
        __result = result;
        return false;
    }

    [HarmonyPostfix]
    static void WillTradePostfix(TraderKindDef __instance, ThingDef td, ref bool __result, bool __state)
    {
        // cache result
        if (__state)
        {
            _willTrade[__instance].Add(td, __result);
        }
    }
}
