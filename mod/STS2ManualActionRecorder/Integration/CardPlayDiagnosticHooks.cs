using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2ManualActionRecorder.Recording;

namespace STS2ManualActionRecorder.Integration;

internal static class CardPlayDiagnosticHooks
{
    private static readonly ConditionalWeakTable<CardPlay, PendingCardPlayCapture> PendingCaptures = new();
    private static long nextSourceCardPlaySeq;

    public static void BeforeBeforeCardPlayed(ICombatState combatState, CardPlay cardPlay)
    {
        try
        {
            var capture = new PendingCardPlayCapture(
                Interlocked.Increment(ref nextSourceCardPlaySeq),
                CardPlayStateSnapshot.Capture(combatState, cardPlay),
                DateTimeOffset.UtcNow);
            PendingCaptures.Remove(cardPlay);
            PendingCaptures.Add(cardPlay, capture);
        }
        catch (Exception ex)
        {
            RecorderRuntime.RecordError(ex);
        }
    }

    public static void BeforeAfterCardPlayed(ICombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        try
        {
            RecorderRuntime.Record(
                "diagnostic_card_after_played",
                "Hook.AfterCardPlayed",
                "diagnostic",
                BuildCardAfterPlayedPayload(cardPlay));
        }
        catch (Exception ex)
        {
            RecorderRuntime.RecordError(ex);
        }

        try
        {
            RecordStateDiff(combatState, cardPlay);
        }
        catch (Exception ex)
        {
            RecorderRuntime.RecordError(ex);
        }
    }

    private static Dictionary<string, object?> BuildCardAfterPlayedPayload(CardPlay cardPlay)
    {
        Creature? target = cardPlay.Target;
        int? targetIndex = GetTargetIndex(cardPlay);

        return new Dictionary<string, object?>
        {
            ["hook"] = "Hook.AfterCardPlayed",
            ["card_instance_id"] = RecorderObjectIdRegistry.GetCardId(cardPlay.Card),
            ["card_id"] = cardPlay.Card.Id.Entry,
            ["card_model_id"] = cardPlay.Card.Id.ToString(),
            ["card_title"] = cardPlay.Card.Title,
            ["card_target_type"] = cardPlay.Card.TargetType.ToString(),
            ["player_net_id"] = cardPlay.Card.Owner.NetId,
            ["target_present"] = target != null,
            ["target_combat_id"] = target?.CombatId,
            ["target_index"] = targetIndex,
            ["target_log_name"] = target?.LogName,
            ["result_pile"] = cardPlay.ResultPile.ToString(),
            ["is_auto_play"] = cardPlay.IsAutoPlay,
            ["play_index"] = cardPlay.PlayIndex,
            ["play_count"] = cardPlay.PlayCount,
            ["is_first_in_series"] = cardPlay.IsFirstInSeries,
            ["is_last_in_series"] = cardPlay.IsLastInSeries,
            ["energy_spent"] = cardPlay.Resources.EnergySpent,
            ["stars_spent"] = cardPlay.Resources.StarsSpent
        };
    }

    private static void RecordStateDiff(ICombatState combatState, CardPlay cardPlay)
    {
        bool hasBefore = PendingCaptures.TryGetValue(cardPlay, out PendingCardPlayCapture? pending);
        CardPlayStateSnapshot stateAfter = CardPlayStateSnapshot.Capture(combatState, cardPlay);
        CardPlayStateSnapshot stateBefore = pending?.StateBefore ?? new CardPlayStateSnapshot(
            new List<Dictionary<string, object?>>(),
            new List<Dictionary<string, object?>>(),
            new List<string> { "before_snapshot_missing" });

        Dictionary<string, object?> diff = hasBefore
            ? CardPlayStateDiff.Build(stateBefore, stateAfter)
            : BuildUnavailableDiff("before_snapshot_missing");

        if (hasBefore)
        {
            PendingCaptures.Remove(cardPlay);
        }

        RecorderRuntime.Record(
            "diagnostic_card_play_state_diff",
            "Hook.BeforeCardPlayed/Hook.AfterCardPlayed",
            "diagnostic",
            new Dictionary<string, object?>
            {
                ["source_card_play_seq"] = pending?.SourceCardPlaySeq ?? Interlocked.Increment(ref nextSourceCardPlaySeq),
                ["capture_status"] = hasBefore ? "complete" : "missing_before_snapshot",
                ["before_captured_at_utc"] = pending?.CapturedAtUtc,
                ["after_captured_at_utc"] = DateTimeOffset.UtcNow,
                ["hook_window"] = BuildHookWindowPayload(),
                ["card_play"] = BuildCardAfterPlayedPayload(cardPlay),
                ["capture_capabilities"] = BuildCaptureCapabilitiesPayload(),
                ["state_before"] = stateBefore.ToPayload(),
                ["state_after"] = stateAfter.ToPayload(),
                ["diff"] = diff
            });
    }

    private static Dictionary<string, object?> BuildHookWindowPayload()
    {
        return new Dictionary<string, object?>
        {
            ["state_before_hook"] = "Hook.BeforeCardPlayed prefix",
            ["state_after_hook"] = "Hook.AfterCardPlayed prefix",
            ["state_before_semantics"] = "after validation/resource spend and after card entered Play pile, before BeforeCardPlayed listeners and card effects",
            ["state_after_semantics"] = "after card effects and History.CardPlayFinished, before AfterCardPlayed listeners and final result-pile cleanup",
            ["after_excludes"] = new[]
            {
                "AfterCardPlayed listeners",
                "AfterCardPlayedLate listeners",
                "played-card final move to ResultPile/Discard/Exhaust/None",
                "CheckForEmptyHand",
                "after-card-played energy/star temporary cost cleanup"
            }
        };
    }

    private static Dictionary<string, object?> BuildCaptureCapabilitiesPayload()
    {
        return new Dictionary<string, object?>
        {
            ["card_piles"] = true,
            ["card_instance_ids"] = true,
            ["creature_identities"] = true,
            ["creature_hp_block_powers"] = false,
            ["orbs"] = false,
            ["summons"] = false,
            ["choices"] = false,
            ["random_results"] = false
        };
    }

    private static Dictionary<string, object?> BuildUnavailableDiff(string reason)
    {
        return new Dictionary<string, object?>
        {
            ["cards_moved"] = Array.Empty<object>(),
            ["cards_created"] = Array.Empty<object>(),
            ["cards_removed"] = Array.Empty<object>(),
            ["cards_drawn"] = Array.Empty<object>(),
            ["cards_discarded"] = Array.Empty<object>(),
            ["cards_exhausted"] = Array.Empty<object>(),
            ["cards_upgraded"] = Array.Empty<object>(),
            ["order_changed"] = Array.Empty<object>(),
            ["draw_pile_order_changed"] = false,
            ["choices"] = Array.Empty<object>(),
            ["random_results"] = Array.Empty<object>(),
            ["hp_changes"] = Array.Empty<object>(),
            ["block_changes"] = Array.Empty<object>(),
            ["power_changes"] = Array.Empty<object>(),
            ["orb_changes"] = Array.Empty<object>(),
            ["summon_changes"] = Array.Empty<object>(),
            ["auto_play_children"] = Array.Empty<object>(),
            ["unresolved"] = new[] { reason }
        };
    }

    private sealed class PendingCardPlayCapture
    {
        public PendingCardPlayCapture(long sourceCardPlaySeq, CardPlayStateSnapshot stateBefore, DateTimeOffset capturedAtUtc)
        {
            SourceCardPlaySeq = sourceCardPlaySeq;
            StateBefore = stateBefore;
            CapturedAtUtc = capturedAtUtc;
        }

        public long SourceCardPlaySeq { get; }

        public CardPlayStateSnapshot StateBefore { get; }

        public DateTimeOffset CapturedAtUtc { get; }
    }

    private static int? GetTargetIndex(CardPlay cardPlay)
    {
        Creature? target = cardPlay.Target;
        if (target == null)
        {
            return null;
        }

        var creatures = cardPlay.Card.Owner.Creature.CombatState?.Creatures;
        if (creatures == null)
        {
            return null;
        }

        for (int index = 0; index < creatures.Count; index++)
        {
            if (ReferenceEquals(creatures[index], target))
            {
                return index;
            }
        }

        return null;
    }
}
