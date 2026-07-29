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
pnpm verify:generated-runtime
pnpm analyze:budget
```

`pnpm typecheck` 组合 app 和 E2E 类型检查；`pnpm verify` 组合 prepare、typecheck、lint、stylelint、unit test、build、生成产物契约与 budget。托管 CI 在构建后同样执行 `pnpm verify:generated-runtime`，再检查包体预算。

`pnpm lint` 调用 `lint:product`，以 `--max-warnings 0` 检查所有维护中的 `src` TypeScript/Vue、测试 TypeScript、E2E TypeScript、行业字典生成脚本及根配置文件。唯一业务代码例外是 `industries.generated.ts`：它由外部 SQL 确定性生成，生成器本身仍接受 lint，生成内容由行业字典数量、父子范围和标签测试校验。ESLint 的格式化桥已关闭，以消除 Windows 与 Linux 换行差异；Vue/CSS/SCSS 样式格式和规则由独立的 `pnpm stylelint` 负责。

## 测试层

- `tests/smoke/`：产品形状、文档一致性、运行时 E2E 合约。
- `tests/unit/features/`：字典、默认值、显隐规则、校验、Payload、Mock Repository/Service、提交控制器。
- `tests/unit/stores/`：认证和表单 Store、Storage 持久化、私有 Query 缓存。
- `tests/unit/components/`：五步组件字段、稳定 test ID、预览分组。
- `tests/unit/e2e/`：Playwright 到小程序 Automator Driver 的映射。
- `e2e/land-demand.spec.ts`：真实微信开发者工具内的登录、冷启动会话恢复、暂存恢复、联动校验、验证码提交、已提交记录修改再暂存和登录/确认页截图主流程。

单元测试使用可注入内存 Storage、时钟和验证码，不依赖生产数据。字典测试固定验证国民行业 150 个父节点/515 个叶子项、13 个宁波区域和完整 22 组产业赛道映射。

业务边界测试还覆盖：篡改草稿不能改变认证企业四个归属字段；`decimal(20,6)`/`decimal(10,2)` 的最大整数位边界；空值与零值区分；`330200` 已选时再次选择可取消；Query `AbortSignal` 传入 Service 并在服务边界终止；只读详情不暴露保存或提交动作；成功页不在缺少已提交记录时显示成功。

## 运行时 E2E

```powershell
pnpm build
pnpm test:e2e
```

运行前必须满足：Windows、微信开发者工具已登录、服务端口已启用、`dist` 可打开。配置固定 `workers: 1` 且共享一个 Automator 会话。冷启动场景调用 Driver `restart`，由 Automator `callWxMethod('restartMiniProgram', { path })` 触发真实 `wx.restartMiniProgram`，等待新运行时页面就绪后验证 Storage 会话恢复；它不使用同一运行时内的 `reLaunch` 冒充冷启动。用例还会调用 Driver 的 `screenshot` 生成工作区 `.tmp/e2e-login.png` 与 `.tmp/e2e-review.png`，它们不是基线。若 CLI 返回 `re-login`，这是开发者工具登录前置条件未满足，应记录为“未执行/受阻”，不能标记为通过。

托管 Linux CI 没有微信开发者工具，因此 CI 只运行静态门禁；运行时 E2E 是单独的 Windows DevTools 验收工作。截图使用 `wv screenshot`，对比使用 `wv compare`；只有实际生成并检查过的文件才能作为证据，不能从构建结果推断截图通过。

当前命令的真实状态、退出码和运行时阻塞见 `reports/verification.md`。
## 生成产物运行时契约

静态回归测试在构建后检查 `dist/app.json` 的登录入口和无 `tabBar` 状态，
并检查生成 WXML 的 `data-wd-change="1"` 标记令 Wevu dispatcher 负责唯一
一次 `event.detail` 解包。页面与
组件处理器接收的已经是 detail 对象（例如 `{ value }`），自定义组件事件
接收的已经是补丁本身；业务生成脚本不得再次读取 `.detail`。
