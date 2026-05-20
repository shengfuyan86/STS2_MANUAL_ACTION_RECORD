using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2ManualActionRecorder.Recording;

namespace STS2ManualActionRecorder.Integration;

internal static class EndTurnDiagnosticHooks
{
    private static readonly FieldInfo? CombatStateField = typeof(NEndTurnButton).GetField("_combatState", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void BeforeCallReleaseLogic(NEndTurnButton __instance)
    {
        TryRecordEndTurnRequest(__instance, "NEndTurnButton.CallReleaseLogic");
    }

    public static void BeforeSecretEndTurnLogicViaFtue(NEndTurnButton __instance)
    {
        TryRecordEndTurnRequest(__instance, "NEndTurnButton.SecretEndTurnLogicViaFtue");
    }

    public static void BeforeAfterAllPlayersReadyToEndTurn(CombatManager __instance, Func<Task>? actionDuringEnemyTurn)
    {
        try
        {
            CombatState? combatState = __instance.DebugOnlyGetState();
            if (combatState == null)
            {
                return;
            }

            RecorderRuntime.Record(
                "diagnostic_end_turn_phase_one_requested",
                "CombatManager.AfterAllPlayersReadyToEndTurn",
                "diagnostic",
                new Dictionary<string, object?>
                {
                    ["hook"] = "CombatManager.AfterAllPlayersReadyToEndTurn",
                    ["round_number"] = combatState.RoundNumber,
                    ["current_side"] = combatState.CurrentSide.ToString(),
                    ["is_in_progress"] = __instance.IsInProgress,
                    ["ending_player_turn_phase_one_before"] = __instance.EndingPlayerTurnPhaseOne,
                    ["ending_player_turn_phase_two_before"] = __instance.EndingPlayerTurnPhaseTwo,
                    ["action_during_enemy_turn_present"] = actionDuringEnemyTurn != null,
                    ["source_role"] = "logic_gate_after_all_players_ready"
                });
        }
        catch (Exception ex)
        {
            RecorderRuntime.RecordError(ex);
        }
    }

    private static void TryRecordEndTurnRequest(NEndTurnButton button, string sourceMethod)
    {
        try
        {
            CombatState? combatState = GetCombatState(button);
            if (combatState == null)
            {
                return;
            }

            NCombatRoom? room = NCombatRoom.Instance;
            if (room?.Ui?.Hand == null)
            {
                return;
            }

            if (room.Ui.Hand.InCardPlay || room.Ui.Hand.CurrentMode != NPlayerHand.Mode.Play)
            {
                return;
            }

            Player? me = LocalContext.GetMe(combatState);
            if (me == null || CombatManager.Instance.IsPlayerReadyToEndTurn(me))
            {
                return;
            }

            RecorderRuntime.Record(
                "diagnostic_end_turn_request",
                sourceMethod,
                "diagnostic",
                new Dictionary<string, object?>
                {
                    ["action"] = "end_turn_requested",
                    ["player_net_id"] = me.NetId,
                    ["round_number"] = combatState.RoundNumber,
                    ["current_side"] = combatState.CurrentSide.ToString(),
                    ["ready_before"] = false,
                    ["can_turn_be_ended"] = true,
                    ["has_playable_cards"] = me.PlayerCombatState?.HasCardsToPlay(),
                    ["source_method"] = sourceMethod
                });
        }
        catch (Exception ex)
        {
            RecorderRuntime.RecordError(ex);
        }
    }

    private static CombatState? GetCombatState(NEndTurnButton button)
    {
        return (CombatState?)CombatStateField?.GetValue(button);
    }
}
