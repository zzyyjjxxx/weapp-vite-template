# 用地需求 Mock Service 与 Repository

当前产品没有 HTTP Client，也不启动本地后端。登录、用地需求保存/修改、草稿和验证码均通过领域 Service 调用可替换 Repository；默认实现使用微信小程序 Storage，是开发与自动化测试专用 Mock。

## 接口边界

- `AuthRepository.login(input)`：只接受 Mock 账号 `demo / demo123`，返回脱敏后的企业会话。
- `LandDemandRepository.get/save/update`：按信用代码读取、新增或修改记录。
- `getDraft/setDraft/removeDraft`：管理只在本机使用的步骤草稿。
- `sendCode/verifyCode`：模拟六位验证码、5 分钟有效期、60 秒重发间隔与最多 5 次错误尝试。

页面不得直接实例化 Repository。页面通过 `queries.ts` 的 Query/Mutation 调用 Service，Service 再使用配置的 Repository。测试可注入内存 Storage 和确定性时钟/验证码。

## Storage 键

- `land-demand.auth`：版本化认证会话。
- `mock:land-demand:{creditcode}`：Mock 持久化用地需求记录。
- `draft:land-demand:{creditcode}`：本地步骤、表单和保存时间。
- `mock:verification:{phone}`：短期验证码挑战与尝试次数。

这些键仅是当前 Mock 协议，不应作为生产接口契约。退出登录会清除认证会话与私有 Query 缓存；企业记录仍按信用代码保留，以便重新登录回显。

## Payload 与业务状态

`landusedemand=2` 表示暂存，`landusedemand=1` 表示正式提交。新增 Payload 包含企业基础信息；修改 Payload 使用信用代码并保留页面不展示的旧字段。`deploy_park` 在表单内为数组，接口层按逗号序列化；`deploy_landtype` 是单个名称。

## 替换为真实后端

接入真实接口时新增实现同一 `AuthRepository` 与 `LandDemandRepository` 的适配器，并在应用初始化处配置，不改页面、Store、校验或 Payload 规则。真实适配器应负责：

1. 将现有保存/修改命令及 `landusedemand_info` 查询映射到领域模型。
2. 统一响应解码、错误脱敏、超时和取消；不得记录凭据、手机号或完整表单。
3. 由后端生成 `updatetime/updateuser`，并保留隐藏字段而非用空字符串或数字 0 覆盖。
4. 将验证码替换为服务端发送和校验；界面不得再显示 Mock 验证码。
5. 保持 Query 键、私有缓存清理和新增后切换修改语义。

生产请求域名、鉴权协议、真实短信和后端部署不属于当前用地需求 Mock 项目。
