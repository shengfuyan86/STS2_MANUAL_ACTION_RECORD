# Stage 2 Plan

## 原则

阶段 2 只接受真实游戏逻辑提交点 hook。实现顺序是：

1. 收集候选。
2. 验证 type/method/signature。
3. 验证它是提交点，不是 UI 预览点。
4. 用户确认 `verified`。
5. 只为该 verified 目标写 hook。
6. 部署后正负向测试。

## Stage 2A：发现与验证

可以做：

- 从 `sts2.dll` 静态字符串生成候选线索。
- 记录反编译、STS2_MCP、运行时反射证据。
- 校验候选证据是否完整。
- 定义 Stage 2 event schema。

不可以做：

- 创建 gameplay patch 类。
- 调用 `PatchAll()`。
- 使用猜测方法名。
- 把 UI 事件当作玩家动作提交。

## Stage 2B：已验证 hook 实现

只在候选被标记为 `verified` 后开始。

推荐第一个 hook：combat lifecycle 或 end turn。原因是比 card play 更容易做正负向验证。

## 证据要求

每个 verified 候选必须记录：

- `area`
- `target_type`
- `method`
- `signature`
- `source_evidence`
- `layer`
- `commit_point_reasoning`
- `positive_test`
- `negative_test`
- `payload_fields`
- `status=verified`

## 风险控制

- 版本/hash 不匹配时不得 patch gameplay。
- hook 异常只能写 `errors.log`，不得影响游戏。
- 一次只加一个 hook。
- 每个 hook 都必须证明取消路径不会写最终 action event。