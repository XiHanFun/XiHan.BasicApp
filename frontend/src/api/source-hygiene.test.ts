/**
 * `src/api` 的源码卫生守卫（源文件文本层面的回归锚点）。
 *
 * 职责边界：只查「运行时看不见、但会误导读代码的人」的两类问题——
 * 1. 悬空 JSDoc：注释块后面紧跟另一个注释块，说明它描述的方法已被删掉，
 *    读代码与 IDE 悬停都会把它错当成下一个方法的说明；
 * 2. 游离模块：`src/api` 下的顶层模块文件没有被桶文件 index.ts 转出，
 *    照文档 `import { xxx } from '@/api'` 只会拿到 undefined，且 `export *` 不给编译期提示。
 * 这两条都不发请求、不依赖运行时，直接读源文件断言。
 */
import { readdirSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import process from 'node:process'
import { describe, expect, it } from 'vitest'

// vitest 的 root 就是 frontend 目录（vitest.config.ts 复用 vite.config.ts 的根）
const apiDir = join(process.cwd(), 'src', 'api')

function listTypeScriptFiles(dir: string, recursive: boolean): string[] {
  const result: string[] = []
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name)
    if (entry.isDirectory()) {
      if (recursive) {
        result.push(...listTypeScriptFiles(full, true))
      }
      continue
    }
    if (entry.name.endsWith('.ts') && !entry.name.endsWith('.test.ts')) {
      result.push(full)
    }
  }
  return result
}

function toPosixRelative(file: string) {
  return file.slice(apiDir.length + 1).replaceAll('\\', '/')
}

describe('api 模块的注释卫生', () => {
  // 回归锚点：numbering.ts 曾留有一段「查询当前后端实例实际支持的规则时区」的 JSDoc，
  // 对应方法早已删除（该端点归 src/app/context.ts 的 timeZoneApi），
  // 注释紧贴在 preview 的 JSDoc 之上，读代码的人会误以为 preview 就是取时区选项的方法。
  it('没有悬空 JSDoc：注释块后面必须跟着声明，不能直接跟另一个注释块', () => {
    const dangling: string[] = []
    for (const file of listTypeScriptFiles(join(apiDir, 'modules'), true)) {
      const source = readFileSync(file, 'utf8')
      // 注释块结束后（允许夹若干空行）紧跟另一个注释块开头；
      // 「整行换行」与「行首空白」分成两段写，避免 \s*\n\s* 那种可回溯的歧义
      const pattern = /\*\/(?:[ \t]*\r?\n)+[ \t]*\/\*\*/g
      let matched: null | RegExpExecArray
      // eslint-disable-next-line no-cond-assign -- 逐个匹配位置换算行号，写成赋值表达式最直接
      while ((matched = pattern.exec(source)) !== null) {
        const line = source.slice(0, matched.index).split('\n').length
        dangling.push(`${toPosixRelative(file)}:${line}`)
      }
    }

    expect(dangling).toEqual([])
  })
})

describe('api 桶文件的模块覆盖', () => {
  // 回归锚点：factory.ts（defineResource 资源工厂）文件在、桶文件没挂，
  // 文档把它列为标准 CRUD 写法，照文档从 '@/api' 导入却拿到 undefined，运行时才炸。
  it('src/api 下的每个顶层模块都被 index.ts 转出，不存在取不到的游离模块', () => {
    const barrel = readFileSync(join(apiDir, 'index.ts'), 'utf8')
    const exported = new Set(
      [...barrel.matchAll(/export \* from '\.\/([\w-]+)'/g)].map(match => match[1]),
    )

    const orphans = listTypeScriptFiles(apiDir, false)
      .map(file => toPosixRelative(file).replace(/\.ts$/, ''))
      .filter(name => name !== 'index' && !exported.has(name))

    expect(orphans).toEqual([])
  })
})
