namespace STS2ManualActionRecorder.Integration;

internal static class CardPlayStateDiff
{
    public static Dictionary<string, object?> Build(CardPlayStateSnapshot before, CardPlayStateSnapshot after)
    {
        Dictionary<string, CardSnapshotEntry> beforeCards = FlattenCards(before);
        Dictionary<string, CardSnapshotEntry> afterCards = FlattenCards(after);

        var cardsMoved = new List<Dictionary<string, object?>>();
        var cardsCreated = new List<Dictionary<string, object?>>();
        var cardsRemoved = new List<Dictionary<string, object?>>();
        var cardsDrawn = new List<Dictionary<string, object?>>();
        var cardsDiscarded = new List<Dictionary<string, object?>>();
        var cardsExhausted = new List<Dictionary<string, object?>>();
        var cardsUpgraded = new List<Dictionary<string, object?>>();

        foreach ((string id, CardSnapshotEntry beforeCard) in beforeCards)
        {
            if (!afterCards.TryGetValue(id, out CardSnapshotEntry? afterCard))
            {
                cardsRemoved.Add(BuildRemovedPayload(beforeCard));
                continue;
            }

            if (beforeCard.Zone != afterCard.Zone || beforeCard.ZoneIndex != afterCard.ZoneIndex)
            {
                Dictionary<string, object?> move = BuildMovePayload(beforeCard, afterCard);
                cardsMoved.Add(move);
                if (beforeCard.Zone == "draw_pile" && afterCard.Zone == "hand")
                {
                    cardsDrawn.Add(move);
                }

                if (afterCard.Zone == "discard_pile")
                {
                    cardsDiscarded.Add(move);
                }

                if (afterCard.Zone == "exhaust_pile")
                {
                    cardsExhausted.Add(move);
                }
            }

            if (IsUpgraded(beforeCard, afterCard))
            {
                cardsUpgraded.Add(new Dictionary<string, object?>
                {
                    ["recorder_card_instance_id"] = id,
                    ["card_id"] = afterCard.CardId,
                    ["owner_net_id"] = afterCard.OwnerNetId,
                    ["before_upgrade_level"] = beforeCard.CurrentUpgradeLevel,
                    ["after_upgrade_level"] = afterCard.CurrentUpgradeLevel,
                    ["before_is_upgraded"] = beforeCard.IsUpgraded,
                    ["after_is_upgraded"] = afterCard.IsUpgraded,
                    ["confidence"] = "heuristic"
                });
            }
        }

        foreach ((string id, CardSnapshotEntry afterCard) in afterCards)
        {
            if (!beforeCards.ContainsKey(id))
            {
                cardsCreated.Add(BuildCreatedPayload(afterCard));
            }
        }

        List<Dictionary<string, object?>> orderChanged = BuildOrderChanges(before, after);

        return new Dictionary<string, object?>
        {
            ["cards_moved"] = cardsMoved,
            ["cards_created"] = cardsCreated,
            ["cards_removed"] = cardsRemoved,
            ["cards_drawn"] = cardsDrawn,
            ["cards_discarded"] = cardsDiscarded,
            ["cards_exhausted"] = cardsExhausted,
            ["cards_upgraded"] = cardsUpgraded,
            ["order_changed"] = orderChanged,
            ["draw_pile_order_changed"] = orderChanged.Any(change => string.Equals(change["zone"] as string, "draw_pile", StringComparison.Ordinal)),
            ["choices"] = Array.Empty<object>(),
            ["random_results"] = Array.Empty<object>(),
            ["hp_changes"] = Array.Empty<object>(),
            ["block_changes"] = Array.Empty<object>(),
            ["power_changes"] = Array.Empty<object>(),
            ["orb_changes"] = Array.Empty<object>(),
            ["summon_changes"] = Array.Empty<object>(),
            ["auto_play_children"] = Array.Empty<object>(),
            ["unresolved"] = new[]
            {
                "creature_hp_block_powers",
                "orbs",
                "summons",
                "choices",
                "random_results",
                "auto_play_parent_child_correlation",
                "final_played_card_result_pile_cleanup"
            }
        };
    }

    private static Dictionary<string, CardSnapshotEntry> FlattenCards(CardPlayStateSnapshot snapshot)
    {
        var cards = new Dictionary<string, CardSnapshotEntry>(StringComparer.Ordinal);
        foreach (Dictionary<string, object?> player in snapshot.Players)
        {
            foreach (string zone in new[] { "hand", "draw_pile", "discard_pile", "exhaust_pile", "play_pile" })
            {
                if (player.TryGetValue(zone, out object? value) && value is IEnumerable<Dictionary<string, object?>> pileCards)
                {
                    foreach (Dictionary<string, object?> card in pileCards)
                    {
                        CardSnapshotEntry entry = CardSnapshotEntry.FromPayload(card);
                        cards[entry.RecorderCardInstanceId] = entry;
                    }
                }
            }
        }

        return cards;
    }

    private static Dictionary<string, object?> BuildMovePayload(CardSnapshotEntry beforeCard, CardSnapshotEntry afterCard)
    {
        return new Dictionary<string, object?>
        {
            ["recorder_card_instance_id"] = afterCard.RecorderCardInstanceId,
            ["card_id"] = afterCard.CardId,
            ["card_model_id"] = afterCard.CardModelId,
            ["card_title"] = afterCard.CardTitle,
            ["owner_net_id"] = afterCard.OwnerNetId,
            ["from"] = new Dictionary<string, object?>
            {
                ["zone"] = beforeCard.Zone,
                ["zone_index"] = beforeCard.ZoneIndex
            },
            ["to"] = new Dictionary<string, object?>
            {
                ["zone"] = afterCard.Zone,
                ["zone_index"] = afterCard.ZoneIndex
            }
        };
    }

    private static Dictionary<string, object?> BuildCreatedPayload(CardSnapshotEntry card)
    {
        return new Dictionary<string, object?>
        {
            ["recorder_card_instance_id"] = card.RecorderCardInstanceId,
            ["card_id"] = card.CardId,
            ["card_model_id"] = card.CardModelId,
            ["card_title"] = card.CardTitle,
            ["owner_net_id"] = card.OwnerNetId,
            ["to"] = new Dictionary<string, object?>
            {
                ["zone"] = card.Zone,
                ["zone_index"] = card.ZoneIndex
            }
        };
    }

    private static Dictionary<string, object?> BuildRemovedPayload(CardSnapshotEntry card)
    {
        return new Dictionary<string, object?>
        {
            ["recorder_card_instance_id"] = card.RecorderCardInstanceId,
            ["card_id"] = card.CardId,
            ["card_model_id"] = card.CardModelId,
            ["card_title"] = card.CardTitle,
            ["owner_net_id"] = card.OwnerNetId,
            ["from"] = new Dictionary<string, object?>
            {
                ["zone"] = card.Zone,
                ["zone_index"] = card.ZoneIndex
            }
        };
    }

    private static bool IsUpgraded(CardSnapshotEntry beforeCard, CardSnapshotEntry afterCard)
    {
        return afterCard.CurrentUpgradeLevel > beforeCard.CurrentUpgradeLevel || (!beforeCard.IsUpgraded && afterCard.IsUpgraded);
    }

    private static List<Dictionary<string, object?>> BuildOrderChanges(CardPlayStateSnapshot before, CardPlayStateSnapshot after)
    {
        var changes = new List<Dictionary<string, object?>>();
        Dictionary<string, Dictionary<string, List<string>>> beforeOrders = BuildZoneOrders(before);
        Dictionary<string, Dictionary<string, List<string>>> afterOrders = BuildZoneOrders(after);

        foreach ((string playerId, Dictionary<string, List<string>> beforeZones) in beforeOrders)
        {
            if (!afterOrders.TryGetValue(playerId, out Dictionary<string, List<string>>? afterZones))
            {
                continue;
            }

            foreach ((string zone, List<string> beforeIds) in beforeZones)
            {
                if (!afterZones.TryGetValue(zone, out List<string>? afterIds))
                {
                    continue;
                }

                if (beforeIds.SequenceEqual(afterIds))
                {
                    continue;
                }

                bool sameMembership = beforeIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(afterIds.OrderBy(id => id, StringComparer.Ordinal));
                changes.Add(new Dictionary<string, object?>
                {
                    ["player_net_id"] = playerId,
                    ["zone"] = zone,
                    ["before_card_instance_ids"] = beforeIds,
                    ["after_card_instance_ids"] = afterIds,
                    ["same_membership"] = sameMembership
                });
            }
        }

        return changes;
    }

    private static Dictionary<string, Dictionary<string, List<string>>> BuildZoneOrders(CardPlayStateSnapshot snapshot)
    {
        var result = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);
        foreach (Dictionary<string, object?> player in snapshot.Players)
        {
            string playerId = Convert.ToString(player.GetValueOrDefault("player_net_id"), System.Globalization.CultureInfo.InvariantCulture) ?? "unknown";
            var zones = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string zone in new[] { "hand", "draw_pile", "discard_pile", "exhaust_pile", "play_pile" })
            {
                zones[zone] = new List<string>();
                if (player.TryGetValue(zone, out object? value) && value is IEnumerable<Dictionary<string, object?>> pileCards)
                {
                    zones[zone].AddRange(pileCards.Select(card => Convert.ToString(card.GetValueOrDefault("recorder_card_instance_id"), System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"));
                }
            }

            result[playerId] = zones;
        }

        return result;
    }

    private sealed class CardSnapshotEntry
    {
        private CardSnapshotEntry(
            string recorderCardInstanceId,
            string? cardId,
            string? cardModelId,
            string? cardTitle,
            object? ownerNetId,
            string? zone,
            int zoneIndex,
            int currentUpgradeLevel,
            bool isUpgraded)
        {
            RecorderCardInstanceId = recorderCardInstanceId;
            CardId = cardId;
            CardModelId = cardModelId;
            CardTitle = cardTitle;
            OwnerNetId = ownerNetId;
            Zone = zone;
            ZoneIndex = zoneIndex;
            CurrentUpgradeLevel = currentUpgradeLevel;
            IsUpgraded = isUpgraded;
        }

        public string RecorderCardInstanceId { get; }

        public string? CardId { get; }

        public string? CardModelId { get; }

        public string? CardTitle { get; }

        public object? OwnerNetId { get; }

        public string? Zone { get; }

        public int ZoneIndex { get; }

        public int CurrentUpgradeLevel { get; }

        public bool IsUpgraded { get; }

        public static CardSnapshotEntry FromPayload(Dictionary<string, object?> payload)
        {
            return new CardSnapshotEntry(
                Convert.ToString(payload.GetValueOrDefault("recorder_card_instance_id"), System.Globalization.CultureInfo.InvariantCulture) ?? "unknown",
                Convert.ToString(payload.GetValueOrDefault("card_id"), System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(payload.GetValueOrDefault("card_model_id"), System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(payload.GetValueOrDefault("card_title"), System.Globalization.CultureInfo.InvariantCulture),
                payload.GetValueOrDefault("owner_net_id"),
                Convert.ToString(payload.GetValueOrDefault("zone"), System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToInt32(payload.GetValueOrDefault("zone_index") ?? -1, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToInt32(payload.GetValueOrDefault("current_upgrade_level") ?? 0, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToBoolean(payload.GetValueOrDefault("is_upgraded") ?? false, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
