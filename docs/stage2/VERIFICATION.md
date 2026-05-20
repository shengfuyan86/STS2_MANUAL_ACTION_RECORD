# Stage 2 Verification Gates

## Gate 0: Stage 1 still works

- [ ] `dotnet build` succeeds.
- [ ] Game loads mod.
- [ ] `recorder_loaded` is written.
- [ ] Stage 1 validator passes.

## Gate 1: candidate evidence accepted

For each hook candidate:

- [ ] Exact target type recorded.
- [ ] Exact method recorded.
- [ ] Exact signature recorded.
- [ ] Evidence source recorded.
- [ ] Layer classification recorded.
- [ ] Commit-point reasoning recorded.
- [ ] Positive test recorded.
- [ ] Negative test recorded.
- [ ] Payload fields recorded.
- [ ] User marks candidate as `verified`.

## Gate 1.5: diagnostic bridge smoke test

- [ ] `diagnostic_end_turn_request` appears when the end-turn button is actually released in a playable combat turn.
- [ ] `diagnostic_end_turn_phase_one_requested` appears when the accepted request reaches `CombatManager.AfterAllPlayersReadyToEndTurn`.
- [ ] Hover / cancel do not emit either diagnostic end-turn request.
- [ ] Undo does not produce a final gameplay event.

## Gate 1.6: card-play submit diagnostic smoke test

- [ ] `diagnostic_card_after_played` appears after one successful manually played card.
- [ ] One successful manual card play produces exactly one diagnostic card event for each actual play in the card's play series.
- [ ] Drag cancel / target cancel / invalid target / unplayable card do not emit the diagnostic card event.
- [ ] Auto-play behavior is characterized with `is_auto_play` before promoting any card hook to `verified`.

## Gate 1.7: card-play result state-diff diagnostic smoke test

- [ ] A card that only deals damage records HP / Block / power state before and after.
- [ ] A Silent discard/draw card records which card instances were drawn and discarded.
- [ ] A Silent hand-wide card such as `CALCULATED_GAMBLE` or `STORM_OF_STEEL` records all affected hand cards and generated cards.
- [ ] A Defect status-generation card records the created Status card id, destination pile, and any triggered power/orb effects if visible.
- [ ] A Necrobinder Soul/summon card records generated Soul cards and summon/Osty/minion state if visible.
- [ ] A Regent top-of-draw-pile card records the moved card and `draw_pile` order change.
- [ ] An Ironclad exhaust-card card records the exact exhausted card instances.
- [ ] Auto-play children can be correlated with the source card play or explicitly marked as unresolved.
- [ ] Snapshot card entries include stable recorder-side instance ids, not only `card_id`.
- [ ] The diagnostic diff is treated as evidence only and is not promoted to final `card_played` until positive/negative tests pass.

## Gate 2: first hook smoke test

- [ ] Implement only one verified hook.
- [ ] Deploy mod.
- [ ] Perform exactly one real committed action.
- [ ] Confirm exactly one matching event.
- [ ] Perform hover/preview/cancel path.
- [ ] Confirm no final action event.
- [ ] NDJSON validator passes.
- [ ] Game behavior unchanged.

## Gate 3: expand one hook at a time

Recommended order:

1. combat lifecycle or end turn
2. card play
3. potion use
4. state snapshots
