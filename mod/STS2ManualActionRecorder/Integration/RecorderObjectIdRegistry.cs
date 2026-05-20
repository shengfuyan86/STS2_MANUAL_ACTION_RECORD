using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2ManualActionRecorder.Integration;

internal static class RecorderObjectIdRegistry
{
    private sealed class IdHolder
    {
        public IdHolder(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }

    private static readonly ConditionalWeakTable<CardModel, IdHolder> CardIds = new();
    private static readonly ConditionalWeakTable<Creature, IdHolder> CreatureIds = new();
    private static long nextCardId;
    private static long nextCreatureId;

    public static string GetCardId(CardModel card)
    {
        return CardIds.GetValue(card, _ => new IdHolder($"card-{Interlocked.Increment(ref nextCardId)}")).Id;
    }

    public static string GetCreatureId(Creature creature)
    {
        return CreatureIds.GetValue(creature, _ => new IdHolder($"creature-{Interlocked.Increment(ref nextCreatureId)}")).Id;
    }
}
