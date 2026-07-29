# 用地需求填报测试

## 静态与单元门禁

```powershell
pnpm install --frozen-lockfile
pnpm prepare
pnpm typecheck:app
pnpm typecheck:e2e
pnpm lint
pnpm stylelint
pnpm test
pnpm test:coverage
pnpm build
pnpm analyze:budget
```

`pnpm typecheck` 组合 app 和 E2E 类型检查；`pnpm verify` 组合 prepare、typecheck、lint、stylelint、unit test、build 与 budget。CI 对上述静态层逐项运行，便于定位失败。

## 测试层

- `tests/smoke/`：产品形状、文档一致性、运行时 E2E 合约。
- `tests/unit/features/`：字典、默认值、显隐规则、校验、Payload、Mock Repository/Service、提交控制器。
- `tests/unit/stores/`：认证和表单 Store、Storage 持久化、私有 Query 缓存。
- `tests/unit/components/`：五步组件字段、稳定 test ID、预览分组。
- `tests/unit/e2e/`：Playwright 到小程序 Automator Driver 的映射。
- `e2e/land-demand.spec.ts`：真实微信开发者工具内的登录、暂存恢复、联动校验和验证码提交主流程。

单元测试使用可注入内存 Storage、时钟和验证码，不依赖生产数据。字典测试固定验证国民行业 150 个父节点/515 个叶子项、13 个宁波区域和完整 22 组产业赛道映射。

## 运行时 E2E

```powershell
pnpm build
pnpm test:e2e
```

运行前必须满足：Windows、微信开发者工具已登录、服务端口已启用、`dist` 可打开。配置固定 `workers: 1` 且共享一个 Automator 会话。若 CLI 返回 `re-login`，这是开发者工具登录前置条件未满足，应记录为“未执行/受阻”，不能标记为通过。

托管 Linux CI 没有微信开发者工具，因此 CI 只运行静态门禁；运行时 E2E 是单独的 Windows DevTools 验收工作。截图使用 `wv screenshot`，对比使用 `wv compare`；只有实际生成并检查过的文件才能作为证据，不能从构建结果推断截图通过。

当前命令的真实状态、退出码和运行时阻塞见 `reports/verification.md`。
