#!/usr/bin/env node
// 把 package.json 的 gitHooks 段写进本仓真正的钩子目录。由 postinstall 在每次安装后跑。
import { execFileSync } from 'node:child_process'
import { chmodSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(fileURLToPath(new URL('..', import.meta.url)))

function git(...args) {
  return execFileSync('git', args, {
    cwd: ROOT,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'ignore'],
  }).trim()
}

/**
 * 钩子脚本：git 在工作树顶层跑钩子，而依赖装在清单所在目录下，
 * 命令里的 npx 要在那里才找得到。换目录前先把消息文件路径转成绝对的。
 */
function hookScript(command, workdir) {
  if (workdir === '')
    return `#!/bin/sh\n${command}\n`
  return [
    '#!/bin/sh',
    'case "$1" in',
    '  /* | ?:*) _xh_arg="$1" ;;',
    '  *) _xh_arg="$PWD/$1" ;;',
    'esac',
    `cd "${workdir}" || exit 1`,
    'set -- "$_xh_arg"',
    command,
    '',
  ].join('\n')
}

const hooks = JSON.parse(readFileSync(join(ROOT, 'package.json'), 'utf8')).gitHooks ?? {}
const names = Object.keys(hooks)
if (names.length === 0) {
  console.log('[install-git-hooks] package.json 没有 gitHooks 段，跳过')
  process.exit(0)
}

let dir
let workdir
try {
  dir = resolve(ROOT, git('rev-parse', '--git-path', 'hooks'))
  workdir = relative(git('rev-parse', '--show-toplevel'), ROOT).replaceAll('\\', '/')
}
catch {
  // 镜像构建、CI 浅拷贝之类没有 git 仓，那不是错
  console.log('[install-git-hooks] 不在 git 仓里，跳过')
  process.exit(0)
}

mkdirSync(dir, { recursive: true })
for (const [name, command] of Object.entries(hooks)) {
  const path = join(dir, name)
  writeFileSync(path, hookScript(command, workdir))
  chmodSync(path, 0o755)
}

console.log(`[install-git-hooks] 已装 ${names.join('、')} → ${dir}`)
