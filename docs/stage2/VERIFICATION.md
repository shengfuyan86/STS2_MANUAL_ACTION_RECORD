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
