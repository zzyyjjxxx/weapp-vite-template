# Codex 项目级 Skill 初始化同步设计

## 目标

优化 Codex 项目环境初始化流程，使项目 skill 每次只在主工作树中通过
`npx skills experimental_install` 安装或刷新一次，然后将刷新后的 skill
复制到当前任务工作树。工作树不再执行 skill 安装或访问远程 skill 源。

## 范围与约束

- 主工作树是当前 Git 仓库的 common Git 目录的父目录，本项目的主工作树为
  `D:\WorkProject\weapp-vite-template`。
- 当前工作树由 `CODEX_WORKTREE_PATH` 指定；缺少该变量时使用脚本当前工作目录。
- skill 源目录固定为主工作树的 `.agents/skills`。
- 当前工作树接收三个项目级副本：
  - `.agents/skills`
  - `.codex/skills`
  - `.claude/skills`
- 三个目录只属于项目工作树，不使用或修改用户级
  `C:\Users\18556\.codex\skills`、`C:\Users\18556\.claude\skills` 或
  `C:\Users\18556\.agents\skills`。
- 项目依赖 `pnpm install --frozen-lockfile --config.confirmModulesPurge=false`
  仍在当前工作树中执行。
- `.codex/config.toml` 保持不变；`skills-lock.json` 只从主工作树读取，不复制到
  工作树，也不由初始化脚本自动暂存。如果 `experimental_install` 按 CLI 行为更新
  主目录锁文件，该修改仍只留在主目录，不能被初始化流程静默带入功能提交。
- 不修改现有用户文件。

## 方案

新增 `scripts/sync-codex-project-skills.mjs`，由
`.codex/environments/environment.toml` 的初始化脚本和快捷操作调用。

脚本按以下顺序运行：

1. 解析当前工作树路径，并使用
   `git -C <worktree> rev-parse --git-common-dir` 找到主工作树。
2. 在主工作树中启动 `npx skills experimental_install`，继承终端输出和退出码。
3. 确认主工作树 `.agents/skills` 存在；不存在或安装失败时立即以非零状态退出。
4. 对当前工作树的三个目标目录逐一执行同步：
   - 目标目录必须位于当前工作树根目录下，并且只能是上述三个固定目录之一。
   - 删除目标目录中的旧副本，避免已经移除的 skill 残留。
   - 使用递归、解引用的文件复制，从主工作树 `.agents/skills` 生成独立副本。
5. 输出主工作树、当前工作树和三个目标目录，便于初始化日志确认实际路径。

当当前工作树就是主工作树时，脚本不会删除或覆盖作为源的
`.agents/skills`，只将它复制到主工作树的 `.codex/skills` 和
`.claude/skills`；在链接工作树场景下，三个目标目录都会被刷新。

`environment.toml` 的初始化流程变为：

```text
cd "$CODEX_WORKTREE_PATH"
node scripts/sync-codex-project-skills.mjs
pnpm install --frozen-lockfile --config.confirmModulesPurge=false
```

快捷操作也只调用该 Node 脚本；不再暴露直接在工作树运行
`npx skills experimental_install` 的操作。

## Git 忽略

在根目录 `.gitignore` 中明确加入以下项目级规则：

```gitignore
/.agents/skills/
/.codex/skills/
/.claude/skills/
```

现有 `.agents/` 忽略规则保持不变，以避免改变其他本地 AI 辅助文件的现有跟踪策略。

## 错误处理

- Git 主目录解析失败：不执行复制，返回非零状态并说明工作树路径。
- 主工作树 skill 安装失败：不执行复制，保留工作树原有副本并返回非零状态。
- 源目录缺失：不执行复制并返回非零状态。
- 任一目标目录复制失败：立即失败并返回非零状态；错误信息包含目标目录，不能静默降级为重新安装。
- 脚本不执行全局 skill 安装，不删除主工作树源目录，不操作工作树根目录之外的路径。

## 验证策略

- 脚本单元测试使用临时源目录和工作树目录，验证主目录安装注入、三个目标目录生成独立副本，以及旧文件被移除；Git 主目录解析和真实链接工作树复制通过集成命令验证。
- 测试覆盖当前工作树等于主工作树时不删除源目录的分支。
- 配置检查确认 `environment.toml` 不再包含工作树内直接执行
  `npx skills experimental_install` 的命令，而是调用同步脚本。
- Git 检查确认三个目标路径均被忽略，且 `.codex/config.toml` 仍可被 Git 跟踪。
- 完成后运行仓库规定的 focused checks，并将实际命令、退出码和结果追加到
  `reports/verification.md`；现有报告内容不重写。

## 预期修改文件

- Create: `scripts/sync-codex-project-skills.mjs`
- Create: `tests/scripts/sync-codex-project-skills.test.ts` 或等价的项目测试文件
- Modify: `.codex/environments/environment.toml`
- Modify: `.gitignore`
- Append: `reports/verification.md`

不修改应用页面、业务服务、`.codex/config.toml`、`.mcp.json`，不复制或自动暂存
锁文件，也不修改用户已有的未跟踪文件。
