import { execFileSync, spawn } from 'node:child_process'
import { cp, rm, stat } from 'node:fs/promises'
import { dirname, isAbsolute, relative, resolve, sep } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

export const PROJECT_SKILL_TARGETS = Object.freeze([
  '.agents/skills',
  '.codex/skills',
  '.claude/skills',
])

function comparablePath(value) {
  const normalized = resolve(value)
  return process.platform === 'win32' ? normalized.toLowerCase() : normalized
}

function samePath(left, right) {
  return comparablePath(left) === comparablePath(right)
}

function assertTargetInsideWorktree(worktreeRoot, targetDir, relativeTarget) {
  const actualRelativeTarget = relative(resolve(worktreeRoot), targetDir)
  const expectedRelativeTarget = relativeTarget.split('/').join(sep)
  const outsideWorktree = actualRelativeTarget === ''
    || actualRelativeTarget === '..'
    || actualRelativeTarget.startsWith(`..${sep}`)
    || isAbsolute(actualRelativeTarget)

  if (outsideWorktree || actualRelativeTarget !== expectedRelativeTarget) {
    throw new Error(`Refusing to copy outside the configured skill target: ${targetDir}`)
  }
}

async function assertDirectory(directory) {
  let details
  try {
    details = await stat(directory)
  }
  catch (error) {
    throw new Error(`Project skill source does not exist: ${directory}`, { cause: error })
  }

  if (!details.isDirectory()) {
    throw new Error(`Project skill source is not a directory: ${directory}`)
  }
}

export function resolveMainWorktreeRoot(worktreeRoot, gitCommonDir) {
  if (!gitCommonDir) {
    throw new Error('Git did not return a common directory.')
  }

  const absoluteCommonDir = isAbsolute(gitCommonDir)
    ? resolve(gitCommonDir)
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

    if (samePath(targetDir, sourceDir)) {
      continue
    }

    await rm(targetDir, { recursive: true, force: true })
    await cp(sourceDir, targetDir, {
      recursive: true,
      dereference: true,
    })
    targetDirs.push(targetDir)
  }

  return { sourceDir, targetDirs }
}

function readGitCommonDirectory(worktreeRoot) {
  return execFileSync(
    'git',
    ['-C', worktreeRoot, 'rev-parse', '--git-common-dir'],
    { encoding: 'utf8' },
  ).trim()
}

function runProcess(command, args, options) {
  return new Promise((resolveProcess, rejectProcess) => {
    const child = spawn(command, args, {
      ...options,
      stdio: 'inherit',
      windowsHide: true,
    })

    child.once('error', rejectProcess)
    child.once('close', (code) => {
      if (code === 0) {
        resolveProcess()
        return
      }

      rejectProcess(new Error(`${command} ${args.join(' ')} exited with code ${code ?? 'unknown'}.`))
    })
  })
}

export function getNpxInvocation(platform = process.platform, comSpec = process.env.ComSpec) {
  if (platform === 'win32') {
    return {
      command: comSpec || 'cmd.exe',
      args: ['/d', '/s', '/c', 'npx.cmd skills experimental_install'],
    }
  }

  return {
    command: 'npx',
    args: ['skills', 'experimental_install'],
  }
}

async function installSkills(mainRoot) {
  const invocation = getNpxInvocation()
  await runProcess(invocation.command, invocation.args, { cwd: mainRoot })
}

export async function runCli() {
  const configuredWorktree = process.env.CODEX_WORKTREE_PATH?.trim()
  const worktreeRoot = resolve(configuredWorktree || process.cwd())
  const gitCommonDir = readGitCommonDirectory(worktreeRoot)
  const mainRoot = resolveMainWorktreeRoot(worktreeRoot, gitCommonDir)
  const result = await syncProjectSkills({
    worktreeRoot,
    mainRoot,
    install: installSkills,
  })

  console.log(`Project skills refreshed in main worktree: ${mainRoot}`)
  console.log(`Project skill source: ${result.sourceDir}`)
  for (const targetDir of result.targetDirs) {
    console.log(`Project skill copy: ${targetDir}`)
  }

  return result
}

const invokedScript = process.argv[1]
if (invokedScript && samePath(fileURLToPath(import.meta.url), invokedScript)) {
  runCli().catch((error) => {
    console.error(error instanceof Error ? error.message : error)
    process.exitCode = 1
  })
}
