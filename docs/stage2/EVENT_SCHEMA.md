# Stage 2 Event Schema

Stage 2 keeps the Stage 1 envelope and adds action payload conventions.

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

These events are for smoke testing and source confirmation only; they are not verified gameplay events.

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
