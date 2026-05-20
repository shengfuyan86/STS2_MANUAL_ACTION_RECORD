using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2ManualActionRecorder.Integration;

internal sealed class CardPlayStateSnapshot
{
    public CardPlayStateSnapshot(List<Dictionary<string, object?>> players, List<Dictionary<string, object?>> creatures, List<string> snapshotWarnings)
    {
        Players = players;
        Creatures = creatures;
        SnapshotWarnings = snapshotWarnings;
    }

    public List<Dictionary<string, object?>> Players { get; }

    public List<Dictionary<string, object?>> Creatures { get; }

    public List<string> SnapshotWarnings { get; }

    public Dictionary<string, object?> ToPayload()
    {
        return new Dictionary<string, object?>
        {
            ["players"] = Players,
            ["creatures"] = Creatures,
            ["snapshot_warnings"] = SnapshotWarnings
        };
    }

    public static CardPlayStateSnapshot Capture(ICombatState combatState, CardPlay cardPlay)
    {
        var warnings = new List<string>();
        var players = new List<Dictionary<string, object?>>();
        var creatures = new List<Dictionary<string, object?>>();

        foreach (object player in EnumeratePlayers(combatState, cardPlay, warnings))
        {
            players.Add(CapturePlayer(player, warnings));
        }

        var combatCreatures = combatState.Creatures;
        for (int index = 0; index < combatCreatures.Count; index++)
        {
            Creature creature = combatCreatures[index];
            creatures.Add(CaptureCreature(creature, index));
        }

        return new CardPlayStateSnapshot(players, creatures, warnings);
    }

    private static IEnumerable<object> EnumeratePlayers(ICombatState combatState, CardPlay cardPlay, List<string> warnings)
    {
        object? players = TryGetProperty(combatState, "Players", warnings);
        if (players is IEnumerable enumerable)
        {
            var found = false;
            foreach (object? player in enumerable)
            {
                if (player == null)
                {
                    continue;
                }

                found = true;
                yield return player;
            }

            if (found)
            {
                yield break;
            }
        }

        warnings.Add("combat_state_players_unavailable_used_card_owner_fallback");
        yield return cardPlay.Card.Owner;
    }

    private static Dictionary<string, object?> CapturePlayer(object player, List<string> parentWarnings)
    {
        var warnings = new List<string>();
        object? playerCombatState = TryGetProperty(player, "PlayerCombatState", warnings);
        var payload = new Dictionary<string, object?>
        {
            ["player_net_id"] = TryGetProperty(player, "NetId", warnings),
            ["hand"] = CapturePile(playerCombatState, "Hand", "hand", warnings),
            ["draw_pile"] = CapturePile(playerCombatState, "DrawPile", "draw_pile", warnings),
            ["discard_pile"] = CapturePile(playerCombatState, "DiscardPile", "discard_pile", warnings),
            ["exhaust_pile"] = CapturePile(playerCombatState, "ExhaustPile", "exhaust_pile", warnings),
            ["play_pile"] = CapturePile(playerCombatState, "PlayPile", "play_pile", warnings),
            ["snapshot_warnings"] = warnings
        };

        foreach (string warning in warnings)
        {
            parentWarnings.Add($"player:{payload["player_net_id"]}:{warning}");
        }

        return payload;
    }

    private static List<Dictionary<string, object?>> CapturePile(object? playerCombatState, string propertyName, string zone, List<string> warnings)
    {
        var cards = new List<Dictionary<string, object?>>();
        if (playerCombatState == null)
        {
            warnings.Add($"{zone}_player_combat_state_unavailable");
            return cards;
        }

        object? pile = TryGetProperty(playerCombatState, propertyName, warnings);
        object? pileCards = pile == null ? null : TryGetProperty(pile, "Cards", warnings);
        if (pileCards is not IEnumerable enumerable)
        {
            warnings.Add($"{zone}_cards_unavailable");
            return cards;
        }

        int index = 0;
        foreach (object? item in enumerable)
        {
            if (item is CardModel card)
            {
                cards.Add(CaptureCard(card, zone, index));
                index++;
            }
        }

        return cards;
    }

    private static Dictionary<string, object?> CaptureCard(CardModel card, string zone, int zoneIndex)
    {
        return new Dictionary<string, object?>
        {
            ["recorder_card_instance_id"] = RecorderObjectIdRegistry.GetCardId(card),
            ["card_id"] = card.Id.Entry,
            ["card_model_id"] = card.Id.ToString(),
            ["card_title"] = card.Title,
            ["owner_net_id"] = card.Owner.NetId,
            ["zone"] = zone,
            ["zone_index"] = zoneIndex,
            ["current_pile"] = card.Pile?.Type.ToString(),
            ["target_type"] = card.TargetType.ToString(),
            ["current_upgrade_level"] = card.CurrentUpgradeLevel,
            ["is_upgraded"] = card.IsUpgraded,
            ["is_clone"] = card.IsClone,
            ["is_dupe"] = card.IsDupe,
            ["exhaust_on_next_play"] = card.ExhaustOnNextPlay
        };
    }

    private static Dictionary<string, object?> CaptureCreature(Creature creature, int index)
    {
        return new Dictionary<string, object?>
        {
            ["recorder_creature_instance_id"] = RecorderObjectIdRegistry.GetCreatureId(creature),
            ["combat_id"] = creature.CombatId,
            ["index"] = index,
            ["log_name"] = creature.LogName,
            ["side"] = creature.Side.ToString(),
            ["is_alive"] = creature.IsAlive,
            ["is_dead"] = creature.IsDead
        };
    }

    private static object? TryGetProperty(object target, string propertyName, List<string> warnings)
    {
        try
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                warnings.Add($"missing_property:{target.GetType().Name}.{propertyName}");
                return null;
            }

            return property.GetValue(target);
        }
        catch (Exception ex)
        {
            warnings.Add($"property_error:{target.GetType().Name}.{propertyName}:{ex.GetType().Name}");
            return null;
        }
    }
}
