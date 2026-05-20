using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2ManualActionRecorder.Integration;

internal static class HarmonyBootstrap
{
    private const string HarmonyId = "local.sts2.manual_action_recorder";

    public static string Initialize()
    {
        Harmony harmony = new(HarmonyId);
        PatchEndTurnDiagnostics(harmony);
        PatchCardPlayDiagnostics(harmony);
        return HarmonyId;
    }

    private static void PatchEndTurnDiagnostics(Harmony harmony)
    {
        MethodInfo? callReleaseLogic = AccessTools.Method(typeof(NEndTurnButton), nameof(NEndTurnButton.CallReleaseLogic));
        if (callReleaseLogic != null)
        {
            harmony.Patch(
                callReleaseLogic,
                prefix: new HarmonyMethod(typeof(EndTurnDiagnosticHooks).GetMethod(nameof(EndTurnDiagnosticHooks.BeforeCallReleaseLogic), BindingFlags.Static | BindingFlags.Public)!));
        }

        MethodInfo? secretEndTurnLogic = AccessTools.Method(typeof(NEndTurnButton), nameof(NEndTurnButton.SecretEndTurnLogicViaFtue));
        if (secretEndTurnLogic != null)
        {
            harmony.Patch(
                secretEndTurnLogic,
                prefix: new HarmonyMethod(typeof(EndTurnDiagnosticHooks).GetMethod(nameof(EndTurnDiagnosticHooks.BeforeSecretEndTurnLogicViaFtue), BindingFlags.Static | BindingFlags.Public)!));
        }

        MethodInfo? afterAllPlayersReadyToEndTurn = AccessTools.Method(typeof(CombatManager), "AfterAllPlayersReadyToEndTurn");
        if (afterAllPlayersReadyToEndTurn != null)
        {
            harmony.Patch(
                afterAllPlayersReadyToEndTurn,
                prefix: new HarmonyMethod(typeof(EndTurnDiagnosticHooks).GetMethod(nameof(EndTurnDiagnosticHooks.BeforeAfterAllPlayersReadyToEndTurn), BindingFlags.Static | BindingFlags.Public)!));
        }
    }

    private static void PatchCardPlayDiagnostics(Harmony harmony)
    {
        MethodInfo? beforeCardPlayed = AccessTools.Method(typeof(Hook), nameof(Hook.BeforeCardPlayed));
        if (beforeCardPlayed != null)
        {
            harmony.Patch(
                beforeCardPlayed,
                prefix: new HarmonyMethod(typeof(CardPlayDiagnosticHooks).GetMethod(nameof(CardPlayDiagnosticHooks.BeforeBeforeCardPlayed), BindingFlags.Static | BindingFlags.Public)!));
        }

        MethodInfo? afterCardPlayed = AccessTools.Method(typeof(Hook), nameof(Hook.AfterCardPlayed));
        if (afterCardPlayed != null)
        {
            harmony.Patch(
                afterCardPlayed,
                prefix: new HarmonyMethod(typeof(CardPlayDiagnosticHooks).GetMethod(nameof(CardPlayDiagnosticHooks.BeforeAfterCardPlayed), BindingFlags.Static | BindingFlags.Public)!));
        }
    }
}
