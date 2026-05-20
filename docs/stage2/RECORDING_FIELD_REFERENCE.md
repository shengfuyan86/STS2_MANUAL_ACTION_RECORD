# Stage 2 Recording Field Reference

This document explains the meaning of every field currently written by the recorder. When an event type or recorded field is added, removed, renamed, or semantically changed, update this document in the same change.

## Event envelope

Every line in `events.ndjson` is one JSON object with the Stage 1 envelope.

| Field | Meaning |
| --- | --- |
| `schema_version` | Recorder event schema version. This describes the log format, not the game version. |
| `recorder_version` | Version of this recorder mod/tooling. |
| `game_version` | Slay the Spire 2 version observed by the recorder when available. |
| `main_assembly_hash` | Hash/fingerprint of the game main assembly used to detect whether hook evidence still matches the local game build. |
| `session_id` | Unique recorder session directory/id for one recorder run. |
| `run_id` | Optional higher-level run identifier. It may be `null` when not assigned yet. |
| `seq` | Monotonic event sequence number within the recorder session. Use this to order events. |
| `time_utc` | UTC timestamp when the recorder wrote the event. |
| `event_type` | Event name, for example `recorder_loaded` or `diagnostic_card_play_state_diff`. |
| `source` | Hook, subsystem, or recorder component that emitted the event. |
| `confidence` | Evidence level. `diagnostic` means smoke-test/evidence only, not final verified gameplay semantics. |
| `payload` | Event-specific data. |

## `recorder_loaded`

Emitted when the mod initializes successfully.

| Payload field | Meaning |
| --- | --- |
| `diagnostic_hooks` | List of diagnostic hooks the current mod build attempted to register. Presence here means the mod build knows about the hook, not that the hook fired during gameplay. |

Current diagnostic hooks include end-turn and card-play candidates such as `NEndTurnButton.CallReleaseLogic`, `CombatManager.AfterAllPlayersReadyToEndTurn`, `Hook.BeforeCardPlayed`, and `Hook.AfterCardPlayed`.

## `diagnostic_end_turn_request`

Emitted from the end-turn button request path. It is a diagnostic request event, not a verified `turn_ended` gameplay event.

| Payload field | Meaning |
| --- | --- |
| `action` | Diagnostic action label. Current value is `end_turn_requested`. |
| `player_net_id` | Runtime net id of the player associated with the request, when available. |
| `round_number` | Combat round number observed at the hook point. |
| `current_side` | Current acting side observed at the hook point, for example `Player`. |
| `ready_before` | Whether the player was already marked ready before this request. |
| `can_turn_be_ended` | Whether game logic reported the turn could be ended at this point. |
| `has_playable_cards` | Whether the game reported playable cards were available. |
| `source_method` | Exact source method used by the diagnostic bridge. |

## `diagnostic_end_turn_phase_one_requested`

Emitted from `CombatManager.AfterAllPlayersReadyToEndTurn`. It is a deeper logic-layer diagnostic showing that the all-players-ready gate was reached.

| Payload field | Meaning |
| --- | --- |
| `hook` | Hook/method name that emitted the event. |
| `round_number` | Combat round number at the hook point. |
| `current_side` | Current acting side at the hook point. |
| `is_in_progress` | Whether combat was in progress at the hook point. |
| `ending_player_turn_phase_one_before` | Internal phase-one ending flag before the method body ran. |
| `ending_player_turn_phase_two_before` | Internal phase-two ending flag before the method body ran. |
| `action_during_enemy_turn_present` | Whether an enemy-turn action marker was present. |
| `source_role` | Recorder classification of why this hook matters. Current value marks it as a logic gate after all players are ready. |

## `diagnostic_card_after_played`

Emitted from `Hook.AfterCardPlayed`. It proves a card play reached the successful card-play hook dispatcher. It records submit semantics only: who played what, against what target, with what resources. It does not by itself prove every resulting state change.

| Payload field | Meaning |
| --- | --- |
| `hook` | Hook name. Current value is `Hook.AfterCardPlayed`. |
| `card_instance_id` | Recorder-assigned stable id for the runtime `CardModel` object reference. Stable only within the recorder session. |
| `card_id` | Game card id entry, for example `MANIFEST_AUTHORITY`. |
| `card_model_id` | Full game model id string, for example `CARD.MANIFEST_AUTHORITY`. |
| `card_title` | Localized card title shown by the game. |
| `card_target_type` | Card target type reported by the game model. |
| `player_net_id` | Net id of the card owner/player. |
| `target_present` | Whether the `CardPlay` had a non-null target. |
| `target_combat_id` | Target creature combat id when a target exists. |
| `target_index` | Target creature index in the current creature list when resolvable. |
| `target_log_name` | Target creature log/display name when a target exists. |
| `result_pile` | Intended final pile from `CardPlay.ResultPile`, such as `Discard`, `Exhaust`, or `None`. The state-diff `state_after` is currently captured before final result-pile cleanup. |
| `is_auto_play` | Whether the card play was auto-played by game logic rather than directly manually played. |
| `play_index` | Index of this play within the current play series when available. |
| `play_count` | Total count in the current play series when available. |
| `is_first_in_series` | Whether this is the first play in the series. |
| `is_last_in_series` | Whether this is the last play in the series. |
| `energy_spent` | Energy spent for this card play as reported by `CardPlay`. |
| `stars_spent` | Stars spent for this card play as reported by `CardPlay`. |

## `diagnostic_card_play_state_diff`

Emitted by correlating `Hook.BeforeCardPlayed` and `Hook.AfterCardPlayed` for the same `CardPlay` object. It captures observable state before and after card effects and calculates a generic diff.

This is still diagnostic. It is not the final `card_played` gameplay event.

| Payload field | Meaning |
| --- | --- |
| `source_card_play_seq` | Recorder-side sequence id for this correlated card-play capture. Used to connect before/after snapshots and future child events. |
| `capture_status` | Capture result, for example `complete` or a partial/unavailable status. |
| `hook_window` | Explains exactly where `state_before` and `state_after` were captured. |
| `card_play` | Card-play identity/action data for the source card play. |
| `capture_capabilities` | Boolean capability map declaring what this diagnostic currently claims to capture. |
| `state_before` | Snapshot captured at `Hook.BeforeCardPlayed` prefix. |
| `state_after` | Snapshot captured at `Hook.AfterCardPlayed` prefix. |
| `diff` | Generic state diff computed from `state_before` and `state_after`. |

### `hook_window`

| Field | Meaning |
| --- | --- |
| `state_before_hook` | Hook used for the before snapshot. Current value: `Hook.BeforeCardPlayed prefix`. |
| `state_after_hook` | Hook used for the after snapshot. Current value: `Hook.AfterCardPlayed prefix`. |
| `state_before_semantics` | `state_before` is after validation/resource spend and after the played card entered the Play pile, but before card effects. |
| `state_after_semantics` | `state_after` is after card effects and `History.CardPlayFinished`, but before `AfterCardPlayed` listeners and final result-pile cleanup. |

### `card_play`

The `card_play` object uses the same meaning as `diagnostic_card_after_played`, with `card_instance_id`, card identity fields, target fields, result pile, play-series fields, and resource-spend fields.

### `capture_capabilities`

| Field | Meaning |
| --- | --- |
| `card_piles` | The diagnostic attempts to capture card piles. |
| `card_instance_ids` | Cards receive recorder-side object ids for before/after matching. |
| `creature_identities` | Creatures receive minimal identity fields. |
| `creature_hp_block_powers` | Whether HP/block/power values are fully captured. Currently false. |
| `orbs` | Whether Defect orb/channel/evoke state is fully captured. Currently false. |
| `summons` | Whether summon/Osty/minion state is fully captured. Currently false. |
| `choices` | Whether player choice candidates/results are fully captured. Currently false. |
| `random_results` | Whether RNG inputs/results are fully captured. Currently false. |

## State snapshots

`state_before` and `state_after` have the same shape.

| Field | Meaning |
| --- | --- |
| `players` | List of captured player state objects. |
| `creatures` | List of captured creature identity objects. |
| `snapshot_warnings` | Recorder warnings from reflection/fallback capture. Warnings mean the snapshot may be partial. |

### Player snapshot

| Field | Meaning |
| --- | --- |
| `player_net_id` | Runtime net id for the player object. |
| `hand` | Ordered card entries currently in hand. |
| `draw_pile` | Ordered card entries currently in the draw pile. |
| `discard_pile` | Ordered card entries currently in the discard pile. |
| `exhaust_pile` | Ordered card entries currently in the exhaust pile. |
| `play_pile` | Ordered card entries currently in the play pile. The currently played card is expected here during the current capture window. |
| `snapshot_warnings` | Warnings specific to this player snapshot. |

### Card snapshot entry

| Field | Meaning |
| --- | --- |
| `recorder_card_instance_id` | Recorder-assigned id for this runtime card object reference, for example `card-25`. |
| `card_id` | Game card id entry. |
| `card_model_id` | Full game model id string. |
| `card_title` | Localized card title. |
| `owner_net_id` | Net id of the owning player. |
| `zone` | Recorder snapshot zone containing this entry: `hand`, `draw_pile`, `discard_pile`, `exhaust_pile`, or `play_pile`. |
| `zone_index` | Zero-based index within the zone list at snapshot time. |
| `current_pile` | Game-reported current pile type, when available. |
| `target_type` | Card target type reported by the game model. |
| `current_upgrade_level` | Current upgrade level reported by the card model. |
| `is_upgraded` | Whether the card is upgraded. |
| `is_clone` | Whether the card is marked as a clone by the game model. |
| `is_dupe` | Whether the card is marked as a duplicate by the game model. |
| `exhaust_on_next_play` | Whether the card is marked to exhaust on next play. |

### Creature snapshot entry

| Field | Meaning |
| --- | --- |
| `recorder_creature_instance_id` | Recorder-assigned id for this runtime creature object reference. |
| `combat_id` | Game combat id for the creature. |
| `index` | Creature index in the combat creature list at snapshot time. |
| `log_name` | Game log/display name for the creature. |
| `side` | Creature side, such as player or enemy side. |
| `is_alive` | Whether the creature is alive. |
| `is_dead` | Whether the creature is dead. |

## Diff fields

The diff compares card entries by `recorder_card_instance_id`.

| Field | Meaning |
| --- | --- |
| `cards_moved` | Cards present in both snapshots whose zone or zone index changed. |
| `cards_created` | Cards present after but absent before. This includes generated cards and may include transform results. |
| `cards_removed` | Cards present before but absent after. This may include consumed/transformed/otherwise no-longer-visible cards. |
| `cards_drawn` | Derived card movements from `draw_pile` to `hand`. |
| `cards_discarded` | Derived card movements into `discard_pile`. |
| `cards_exhausted` | Derived card movements into `exhaust_pile`. |
| `cards_upgraded` | Cards whose upgrade level or upgraded flag changed. |
| `order_changed` | Zones whose ordered card instance list changed. `same_membership` says whether only order changed or membership also changed. |
| `draw_pile_order_changed` | Convenience boolean showing whether draw-pile order changed. |
| `choices` | Reserved for future choice result/candidate capture. Currently empty. |
| `random_results` | Reserved for future RNG result capture. Currently empty. |
| `hp_changes` | Reserved for future HP diff capture. Currently empty. |
| `block_changes` | Reserved for future block diff capture. Currently empty. |
| `power_changes` | Reserved for future power diff capture. Currently empty. |
| `orb_changes` | Reserved for future orb/channel/evoke diff capture. Currently empty. |
| `summon_changes` | Reserved for future summon/Osty/minion diff capture. Currently empty. |
| `auto_play_children` | Reserved for future parent-child auto-play correlation. Currently empty. |
| `unresolved` | Explicit notes for effects the current diagnostic knows it cannot fully classify. |

### Common nested diff fields

Movement-like entries use:

| Field | Meaning |
| --- | --- |
| `recorder_card_instance_id` | Card instance that moved/changed. |
| `card_id` | Game card id entry. |
| `card_model_id` | Full game model id string. |
| `card_title` | Localized card title. |
| `owner_net_id` | Owning player net id. |
| `from` | Previous location object with `zone` and `zone_index`. |
| `to` | New location object with `zone` and `zone_index`. |

Created-card entries usually have `to` but no `from`. Removed-card entries usually have `from` but no `to`.

## Current interpretation rules

- `diagnostic_*` events are evidence/smoke-test events. They must not be treated as final verified gameplay events.
- A card-play diff with no card pile changes does not mean the card did nothing. It may have changed HP, block, powers, or other systems not yet captured.
- `state_after` currently occurs before final result-pile cleanup, so use `card_play.result_pile` for the intended final destination of the played card.
- Transform effects may appear as `cards_removed` plus `cards_created` until a stable transform hook is verified.
- Recorder instance ids are stable within one session for the same runtime object reference, but they are not stable across sessions.
