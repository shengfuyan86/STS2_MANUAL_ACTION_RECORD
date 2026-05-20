# Stage 2 Event Schema

Stage 2 keeps the Stage 1 envelope and adds action payload conventions. Field meanings are maintained in [RECORDING_FIELD_REFERENCE_ZH.md](RECORDING_FIELD_REFERENCE_ZH.md) and [RECORDING_FIELD_REFERENCE.md](RECORDING_FIELD_REFERENCE.md).

## Envelope

Required fields remain:

- `schema_version`
- `recorder_version`
- `game_version`
- `main_assembly_hash`
- `session_id`
- `seq`
- `time_utc`
- `event_type`
- `source`
- `confidence`
- `payload`

## Diagnostic end-turn event

The current 2B diagnostic bridge emits a non-final diagnostic event before the real gameplay hook is promoted:

- `diagnostic_end_turn_request`
- `diagnostic_end_turn_phase_one_requested`

`diagnostic_end_turn_request` payload shape:

```json
{
  "action": "end_turn_requested",
  "player_net_id": "...",
  "round_number": 1,
  "current_side": "Player",
  "ready_before": false,
  "can_turn_be_ended": true,
  "has_playable_cards": true,
  "source_method": "NEndTurnButton.CallReleaseLogic"
}
```

`diagnostic_end_turn_phase_one_requested` payload shape:

```json
{
  "hook": "CombatManager.AfterAllPlayersReadyToEndTurn",
  "round_number": 1,
  "current_side": "Player",
  "is_in_progress": true,
  "ending_player_turn_phase_one_before": false,
  "ending_player_turn_phase_two_before": false,
  "action_during_enemy_turn_present": false,
  "source_role": "logic_gate_after_all_players_ready"
}
```

## Diagnostic card-play event

The current card-play diagnostic bridge emits a non-final event from the core hook dispatcher:

- `diagnostic_card_after_played`

Payload shape:

```json
{
  "hook": "Hook.AfterCardPlayed",
  "card_id": "bash",
  "card_model_id": "Bash",
  "card_title": "Bash",
  "card_target_type": "AnyEnemy",
  "player_net_id": 1,
  "target_present": true,
  "target_combat_id": 2,
  "target_index": 1,
  "target_log_name": "Cultist",
  "result_pile": "Discard",
  "is_auto_play": false,
  "play_index": 0,
  "play_count": 1,
  "is_first_in_series": true,
  "is_last_in_series": true,
  "energy_spent": 2,
  "stars_spent": 0
}
```

This event is for smoke testing successful card-play hook semantics only; it is not a verified `card_played` gameplay event.

It is also not sufficient for full combat replay. Across the v0.105.1 card snapshot, all character pools contain cards that mutate other hand cards, draw/discard/exhaust piles, generate temporary or status cards, transform/upgrade cards, auto-play other cards, trigger delayed effects, or change creature/orb/summon state. Those effects require state snapshots and diffs.

## Diagnostic card-play state diff event

The next card-play diagnostic should emit a separate non-final event around `Hook.BeforeCardPlayed` / `Hook.AfterCardPlayed`:

- `diagnostic_card_play_state_diff`

Current capture-window limitation: `state_before` is captured after validation/resource spend and after the played card entered the Play pile; `state_after` is captured at the `Hook.AfterCardPlayed` prefix, after card effects but before `AfterCardPlayed` listeners and before final result-pile cleanup. The played card's intended final destination is represented by `card_play.result_pile` until a later cleanup hook is verified.

Minimum payload shape:

```json
{
  "source_card_play_seq": 42,
  "card_id": "ACROBATICS",
  "player_net_id": 1,
  "is_auto_play": false,
  "state_before": {
    "players": [
      {
        "player_net_id": 1,
        "hand": [],
        "draw_pile": [],
        "discard_pile": [],
        "exhaust_pile": []
      }
    ],
    "creatures": []
  },
  "state_after": {
    "players": [],
    "creatures": []
  },
  "diff": {
    "cards_drawn": [],
    "cards_discarded": [],
    "cards_exhausted": [],
    "cards_moved": [],
    "cards_created": [],
    "cards_transformed": [],
    "cards_upgraded": [],
    "draw_pile_order_changed": false,
    "hp_changes": [],
    "block_changes": [],
    "power_changes": [],
    "orb_changes": [],
    "summon_changes": [],
    "choices": [],
    "random_results": [],
    "auto_play_children": []
  }
}
```

Card entries in snapshots should include at least a recorder-assigned card instance id, card id/model id/title, owner net id, zone, zone index, cost if available, upgraded state if available, temporary/modifier flags if available.

These diagnostic events are for smoke testing and source confirmation only; they are not verified gameplay events.

## Future Stage 2 event types

These are schema definitions only. They are not implemented until their hook is verified.

- `combat_started`
- `combat_ended`
- `turn_started`
- `turn_ended`
- `card_played`
- `potion_used`

## Gameplay event payload

Gameplay events must include:

```json
{
  "hook": {
    "target_type": "...",
    "method": "...",
    "signature": "...",
    "evidence_id": "..."
  },
  "action": {},
  "state_before": {},
  "state_after": {}
}
```

For `card_played`, `action` must eventually include card identity and target evidence.

For `turn_ended`, `action` must identify the player/side if available.

For `potion_used`, `action` must distinguish use from discard/cancel.

## Validator stance

The validator should reject Stage 2 gameplay events that do not contain `hook`, `action`, `state_before`, and `state_after`. This prevents fake events with only an `event_type` from being accepted.
