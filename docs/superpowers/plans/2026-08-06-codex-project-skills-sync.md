# Codex Project Skills Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Codex install and refresh project skills only in the main Git worktree, then copy them into each task worktree's `.agents/skills`, `.codex/skills`, and `.claude/skills` directories.

**Architecture:** A Node ESM helper resolves the main worktree from Git, runs `npx skills experimental_install` with the main worktree as its working directory, and copies the refreshed main `.agents/skills` directory into three fixed, project-local targets. The Codex environment TOML calls the helper before the existing worktree-local dependency installation.

**Tech Stack:** Node.js 26 ESM, `node:fs/promises`, `node:child_process`, Vitest, PowerShell checks, Codex `environment.toml`, Git worktrees.

## Global Constraints

- Only the main worktree may execute `npx skills experimental_install`.
- The source is `<main-worktree>/.agents/skills`; `skills-lock.json` is read from the main worktree and is never copied or automatically staged.
- The three generated project directories are `.agents/skills`, `.codex/skills`, and `.claude/skills`.
- `pnpm install --frozen-lockfile --config.confirmModulesPurge=false` remains in the current task worktree.
- Preserve the pre-existing `reports/verification.md` modification and untracked files `-` and `-.res`; never stage them in feature commits.
- Keep `.codex/config.toml`, `.mcp.json`, `CLAUDE.md`, `pnpm-lock.yaml`, application code, and unrelated worktree changes untouched.
- Follow TDD for the Node helper: write and observe the focused test fail before adding the helper implementation.

---

### Task 1: Add and test the project skill synchronization helper

**Files:**
- Create: `scripts/sync-codex-project-skills.mjs`
- Create: `tests/unit/tooling/codex-project-skills.test.ts`

**Interfaces:**
- `PROJECT_SKILL_TARGETS`: readonly relative target paths `['.agents/skills', '.codex/skills', '.claude/skills']`.
- `resolveMainWorktreeRoot(worktreeRoot: string, gitCommonDir: string): string`: resolves a relative Git common directory against `worktreeRoot` and returns its parent directory.
- `syncProjectSkills(options: { worktreeRoot: string; mainRoot: string; install: (mainRoot: string) => Promise<void> }): Promise<{ sourceDir: string; targetDirs: string[] }>`: runs the injected main-root installer, refreshes the three targets, and returns the copied paths.
- CLI behavior: the script reads `CODEX_WORKTREE_PATH` or uses `process.cwd()`, resolves `git-common-dir` with `git -C`, runs the real `npx` command only with `cwd=mainRoot`, then calls `syncProjectSkills`.

- [ ] **Step 1: Write the failing tests for main-root-only installation and three-target copying.**

The test must create temporary source and worktree directories, seed each target
with `stale.md`, inject an installer that records its root, and assert that
`SKILL.md` is copied to all targets while the stale file is removed. It must
also assert that modifying a target copy does not modify the source, and cover
the `worktreeRoot === mainRoot` branch without deleting the source.

The focused test file should use this concrete structure:

```ts
import { mkdtemp, mkdir, readFile, readdir, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { afterEach, describe, expect, it } from 'vitest'
import {
  PROJECT_SKILL_TARGETS,
  resolveMainWorktreeRoot,
  syncProjectSkills,
} from '../../../scripts/sync-codex-project-skills.mjs'

const tempDirectories: string[] = []
const makeTempDirectory = async () => {
  const directory = await mkdtemp(join(tmpdir(), 'codex-project-skills-'))
  tempDirectories.push(directory)
  return directory
}

afterEach(async () => {
  await Promise.all(tempDirectories.splice(0).map(directory => rm(directory, { recursive: true, force: true })))
})

describe('Codex project skill synchronization', () => {
  it('resolves the main worktree from the Git common directory', () => {
    expect(resolveMainWorktreeRoot('D:/repo/.worktrees/task', 'D:/repo/.git')).toBe('D:/repo')
  })

  it('installs at the main root and refreshes all three independent targets', async () => {
    const mainRoot = await makeTempDirectory()
    const worktreeRoot = await makeTempDirectory()
    const source = join(mainRoot, '.agents', 'skills')
    await mkdir(source, { recursive: true })
    await writeFile(join(source, 'SKILL.md'), 'current')
    for (const relativeTarget of PROJECT_SKILL_TARGETS) {
      const target = join(worktreeRoot, relativeTarget)
      await mkdir(target, { recursive: true })
      await writeFile(join(target, 'stale.md'), 'stale')
    }

    const installRoots: string[] = []
    await syncProjectSkills({
      worktreeRoot,
      mainRoot,
      install: async root => { installRoots.push(root) },
    })

    expect(installRoots).toEqual([mainRoot])
    for (const relativeTarget of PROJECT_SKILL_TARGETS) {
      const target = join(worktreeRoot, relativeTarget)
      expect(await readFile(join(target, 'SKILL.md'), 'utf8')).toBe('current')
      expect(await readdir(target)).toEqual(['SKILL.md'])
    }
    await writeFile(join(worktreeRoot, '.agents', 'skills', 'SKILL.md'), 'changed copy')
    expect(await readFile(join(source, 'SKILL.md'), 'utf8')).toBe('current')
  })

  it('preserves the source when the main root is also the current worktree', async () => {
    const mainRoot = await makeTempDirectory()
    const source = join(mainRoot, '.agents', 'skills')
    await mkdir(source, { recursive: true })
    await writeFile(join(source, 'SKILL.md'), 'main source')

    const result = await syncProjectSkills({ worktreeRoot: mainRoot, mainRoot, install: async () => undefined })

    expect(result.targetDirs).toHaveLength(2)
    expect(await readFile(join(source, 'SKILL.md'), 'utf8')).toBe('main source')
    expect(await readFile(join(mainRoot, '.codex', 'skills', 'SKILL.md'), 'utf8')).toBe('main source')
    expect(await readFile(join(mainRoot, '.claude', 'skills', 'SKILL.md'), 'utf8')).toBe('main source')
  })
})
```

- [ ] **Step 2: Run the focused test to verify the expected RED result.**

Run: `pnpm exec vitest run tests/unit/tooling/codex-project-skills.test.ts`

Expected: FAIL because `scripts/sync-codex-project-skills.mjs` and its exported
interfaces do not exist yet; do not proceed with implementation until the
failure is attributable to the missing helper rather than a test typo.

- [ ] **Step 3: Implement the minimal helper to satisfy the tests.**

The module must export the following behavior:

```js
export const PROJECT_SKILL_TARGETS = Object.freeze([
  '.agents/skills',
  '.codex/skills',
  '.claude/skills',
])

export function resolveMainWorktreeRoot(worktreeRoot, gitCommonDir) {
  const absoluteCommonDir = isAbsolute(gitCommonDir)
    ? normalize(gitCommonDir)
    : resolve(worktreeRoot, gitCommonDir)
  return dirname(absoluteCommonDir)
}

export async function syncProjectSkills({ worktreeRoot, mainRoot, install }) {
  await install(mainRoot)
  const sourceDir = resolve(mainRoot, '.agents', 'skills')
  await assertDirectory(sourceDir)

  const targetDirs = []
  for (const relativeTarget of PROJECT_SKILL_TARGETS) {
    const targetDir = resolve(worktreeRoot, relativeTarget)
    assertTargetInsideWorktree(worktreeRoot, targetDir, relativeTarget)
    if (samePath(targetDir, sourceDir)) continue
    await rm(targetDir, { recursive: true, force: true })
    await cp(sourceDir, targetDir, { recursive: true, dereference: true })
    targetDirs.push(targetDir)
  }
  return { sourceDir, targetDirs }
}
```

The CLI installer must use `npx.cmd` on Windows and `npx` elsewhere, call
`skills experimental_install` with `{ cwd: mainRoot, stdio: 'inherit' }`, throw
on a non-zero exit, and never invoke that command with `worktreeRoot`.

- [ ] **Step 4: Run the focused test and targeted lint to verify GREEN.**

Run:

```powershell
pnpm exec vitest run tests/unit/tooling/codex-project-skills.test.ts
pnpm exec eslint scripts/sync-codex-project-skills.mjs tests/unit/tooling/codex-project-skills.test.ts
```

Expected: all focused tests pass with zero ESLint errors or warnings.

- [ ] **Step 5: Commit the helper and its tests.**

```powershell
git add -- scripts/sync-codex-project-skills.mjs tests/unit/tooling/codex-project-skills.test.ts
git commit -m "feat: sync project skills from the main worktree"
```

### Task 2: Wire Codex setup and ignore generated project directories

**Files:**
- Modify: `.codex/environments/environment.toml`
- Modify: `.gitignore`

**Interfaces:**
- Setup command: `node scripts/sync-codex-project-skills.mjs` followed by the existing `pnpm install` command.
- Manual action: `node scripts/sync-codex-project-skills.mjs`.
- Git policy: ignore exactly `/.agents/skills/`, `/.codex/skills/`, and `/.claude/skills/`; keep `.codex/config.toml` trackable.

- [ ] **Step 1: Replace the direct worktree skill install and add explicit ignore rules.**

The setup block must become:

```toml
[setup]
script = '''
cd "$CODEX_WORKTREE_PATH"
node scripts/sync-codex-project-skills.mjs
pnpm install --frozen-lockfile --config.confirmModulesPurge=false
'''
```

Replace the skill action with:

```toml
[[actions]]
name = "刷新并同步项目 skills"
icon = "tool"
command = "node scripts/sync-codex-project-skills.mjs"
```

Keep the dependency action unchanged. Add these root-anchored rules below the
existing local AI/planning rules:

```gitignore
/.agents/skills/
/.codex/skills/
/.claude/skills/
```

- [ ] **Step 2: Run configuration and ignore checks.**

Run:

```powershell
$environment = Get-Content -LiteralPath '.codex/environments/environment.toml' -Raw
if ($environment -match 'npx skills experimental_install') { throw 'setup must not install skills directly' }
if ($environment -notmatch 'node scripts/sync-codex-project-skills\.mjs') { throw 'sync helper is not wired' }

foreach ($path in @('.agents/skills', '.codex/skills', '.claude/skills')) {
  git check-ignore -q --no-index -- "$path/"
  if ($LASTEXITCODE -ne 0) { throw "generated skill directory is not ignored: $path" }
}
git check-ignore -q -- '.codex/config.toml'
if ($LASTEXITCODE -eq 0) { throw 'project MCP config must remain trackable' }
```

Expected: the PowerShell process exits `0`; the first check finds no direct
`npx` install command, all three generated directories are ignored, and
`.codex/config.toml` is not ignored.

- [ ] **Step 3: Run the focused helper test and commit the integration wiring.**

Run: `pnpm exec vitest run tests/unit/tooling/codex-project-skills.test.ts`

Expected: the focused suite remains green.

```powershell
git add -- .codex/environments/environment.toml .gitignore
git commit -m "fix: wire Codex setup to project skill synchronization"
```

### Task 3: Verify the real main-to-worktree flow and record evidence

**Files:**
- Append only: `reports/verification.md` (preserve its existing user-owned diff; do not stage it with feature files)

**Interfaces:**
- Integration source: `D:\WorkProject\weapp-vite-template\.agents\skills`.
- Integration target: existing `D:\WorkProject\weapp-vite-template\.worktrees\land-demand-mini-program`.

- [ ] **Step 1: Run the real helper against the existing linked worktree.**

Run from the main worktree:

```powershell
$env:CODEX_WORKTREE_PATH = 'D:\WorkProject\weapp-vite-template\.worktrees\land-demand-mini-program'
node scripts/sync-codex-project-skills.mjs
Remove-Item Env:CODEX_WORKTREE_PATH
```

Expected: the log shows `npx skills experimental_install` executing from the
main worktree, then reports copies into the three existing worktree targets;
the command exits `0` if the configured remote skill sources are available.

- [ ] **Step 2: Verify copied content and clean Git state boundaries.**

Run:

```powershell
$worktree = 'D:\WorkProject\weapp-vite-template\.worktrees\land-demand-mini-program'
foreach ($relative in @('.agents/skills', '.codex/skills', '.claude/skills')) {
  $target = Join-Path $worktree $relative
  if (-not (Test-Path -LiteralPath $target -PathType Container)) { throw "missing $target" }
  if (-not (Get-ChildItem -LiteralPath $target -Recurse -File | Select-Object -First 1)) { throw "empty $target" }
}
git status --short -- . '.agents/skills' '.codex/skills' '.claude/skills'
git -C $worktree status --short -- '.agents/skills' '.codex/skills' '.claude/skills'
```

Expected: generated directories are present and populated. The current main
worktree must not report generated files as changes; an older linked worktree
whose branch predates the new `.gitignore` may show the generated copies as
untracked and must not be staged or modified as part of this task.

- [ ] **Step 3: Run repository verification.**

Run:

```powershell
pnpm exec vitest run tests/unit/tooling/codex-project-skills.test.ts
pnpm verify
git diff --check
```

Record each actual command, exit code, test count, and any environment blocker
in `reports/verification.md`. Do not claim DevTools runtime acceptance; this
change only needs static/unit/build evidence.

- [ ] **Step 4: Recheck the final diff and preserve unrelated changes.**

Run: `git status --short --branch; git diff --stat; git diff --cached --stat`

Expected: only the two feature commits contain the helper, focused tests,
environment wiring, and ignore rules; the pre-existing report modification and
untracked `-` / `-.res` remain unstaged and untouched.
