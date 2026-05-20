# Hook Discovery Evidence

本文件记录阶段 2 的候选 hook 证据。任何 `status` 不是 `verified` 的行都不能实现为 gameplay patch。

## Status values

- `unverified`：只有静态线索或未检查。
- `candidate`：方法/类型存在，但提交点语义未验证完整。
- `verified`：已验证为游戏逻辑提交点，并完成正负向测试。
- `rejected`：确认是 UI、预览、取消路径或不可靠。

## Evidence table

| Area | Target type | Method | Signature | Source evidence | Layer | Commit-point reasoning | Positive test | Negative test | Payload fields | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| combat_lifecycle | TBD | TBD | TBD | static hint only | TBD | TBD | TBD | TBD | TBD | unverified | Validate first |
| end_turn | TBD | TBD | TBD | static hint only | TBD | TBD | TBD | TBD | TBD | unverified | Validate first |
| card_play | TBD | TBD | TBD | static hint only | TBD | TBD | TBD | TBD | TBD | unverified | Validate after lifecycle/end turn |
| potion_use | TBD | TBD | TBD | static hint only | TBD | TBD | TBD | TBD | TBD | unverified | Validate after lifecycle/end turn |

## Verification checklist

A row can be changed to `verified` only when all answers below are concrete:

- [ ] Exact type exists in `v0.105.1` / main assembly hash `1363691567`.
- [ ] Exact method exists.
- [ ] Exact signature is recorded.
- [ ] Evidence source is recorded.
- [ ] It is a game logic commit point.
- [ ] One real committed action triggers one event.
- [ ] Hover/drag/preview/cancel does not trigger final action event.
- [ ] Payload fields are known and accessible.
- [ ] User approves implementing this hook.
