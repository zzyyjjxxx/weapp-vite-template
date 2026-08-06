import { mkdir, mkdtemp, readdir, readFile, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join, resolve } from 'node:path'

import { afterEach, describe, expect, it } from 'vitest'
import {
  getNpxInvocation,
  PROJECT_SKILL_TARGETS,
  resolveMainWorktreeRoot,
  syncProjectSkills,
} from '../../../scripts/sync-codex-project-skills.mjs'

const temporaryDirectories: string[] = []

async function makeTemporaryDirectory(): Promise<string> {
  const directory = await mkdtemp(join(tmpdir(), 'codex-project-skills-'))
  temporaryDirectories.push(directory)
  return directory
}

afterEach(async () => {
  await Promise.all(
    temporaryDirectories
      .splice(0)
      .map(directory => rm(directory, { recursive: true, force: true })),
  )
})

describe('Codex project skill synchronization', () => {
  it('resolves the main worktree from the Git common directory', () => {
    expect(resolveMainWorktreeRoot('D:/repo/.worktrees/task', 'D:/repo/.git')).toBe(resolve('D:/repo'))
  })

  it('installs at the main root and refreshes all three independent targets', async () => {
    const mainRoot = await makeTemporaryDirectory()
    const worktreeRoot = await makeTemporaryDirectory()
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
      install: async (root) => {
        installRoots.push(root)
      },
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
    const mainRoot = await makeTemporaryDirectory()
    const source = join(mainRoot, '.agents', 'skills')

    await mkdir(source, { recursive: true })
    await writeFile(join(source, 'SKILL.md'), 'main source')

    const result = await syncProjectSkills({
      worktreeRoot: mainRoot,
      mainRoot,
      install: async () => undefined,
    })

    expect(result.targetDirs).toHaveLength(2)
    expect(await readFile(join(source, 'SKILL.md'), 'utf8')).toBe('main source')
    expect(await readFile(join(mainRoot, '.codex', 'skills', 'SKILL.md'), 'utf8')).toBe('main source')
    expect(await readFile(join(mainRoot, '.claude', 'skills', 'SKILL.md'), 'utf8')).toBe('main source')
  })

  it('wraps the Windows npx batch file with ComSpec', () => {
    expect(getNpxInvocation('win32', 'C:\\Windows\\System32\\cmd.exe')).toEqual({
      command: 'C:\\Windows\\System32\\cmd.exe',
      args: ['/d', '/s', '/c', 'npx.cmd skills experimental_install'],
    })
    expect(getNpxInvocation('linux')).toEqual({
      command: 'npx',
      args: ['skills', 'experimental_install'],
    })
  })
})
