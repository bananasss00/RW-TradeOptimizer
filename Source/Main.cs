using System.Collections.Generic;
using Verse;
using HarmonyLib;
using RimWorld;

namespace TradeOptimizer;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(TraderKindDef), nameof(TraderKindDef.WillTrade))]
public static class Start
{
    static Start()
    {
        new Harmony("pirateby.tradeoptimizer").PatchAll();
        Log.Message("[TradeOptimizer] loaded successfully!");
    }

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

