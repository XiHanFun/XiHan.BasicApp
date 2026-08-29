/**
 * 视图层权限码卫生检查。
 *
 * 页面按钮的门控依据是服务端下发的**按钮码**（`{页面码}.{动作}`），不是权限码。
 * 权限码一旦写进前端就会与后端的按钮登记表各走各的：后端改了权限码，前端还在按旧码判定，
 * 界面上按钮照出、点下去 403，而且没有任何编译期或运行期信号。
 *
 * 两条断言配套才成立：① 前端不出现权限码；② 前端引用的按钮码在后端登记表里真实存在。
 * 只有第一条的话，把权限码换成一个拼错的按钮码同样能过，而那个按钮会永远不显示。
 */
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const SRC_ROOT = join(process.cwd(), 'src')
const BACKEND_MODULES_ROOT = resolve(process.cwd(), '..', 'backend', 'src', 'modules')

/**
 * 后端各模块的权限码前缀（与各模块 PermissionCodes 的 Module 常量一致）。
 * 只认这些前缀，避免把 X6 事件名一类的冒号分隔字符串误判成权限码。
 */
const MODULE_PREFIXES = ['saas', 'ai', 'chat', 'code_gen', 'print-template', 'workflow']

const PERMISSION_CODE = new RegExp(`'(?:${MODULE_PREFIXES.join('|')}):[a-z0-9-]+:[a-z0-9-]+'`, 'g')

/** 门控入口：schema 的四个 *Permission 字段、动作项的 permission、以及命令式的 hasPermission('x') */
const GATE_LITERAL = /(?:(?:export|import|remove|status)?[Pp]ermission\s*:\s*|hasPermission\(\s*)'([^']+)'/g

/**
 * 码形字面量：按钮码含点、权限码含冒号，二者都全小写无空格。
 * 语言包里 `permission: 'Permission'` 这类文案不符合，据此排除。
 */
const CODE_SHAPED = /^[a-z][a-z0-9_-]*(?:[.:][a-z0-9_-]+)+$/

/** 后端按钮登记：new("按钮码", "标题", "父页面码", XxxPermissionCodes.（可带嵌套类）, 排序) */
const BUTTON_DESCRIPTOR = /new\("([a-z][\w.-]*)",\s*"[^"]*",\s*"[a-z][\w.-]*",\s*\w+PermissionCodes\.[\w.]+,\s*\d+\)/g

/**
 * 允许保留权限码的文件：这些不是门控，而是把权限码当业务数据传给接口。
 */
const ALLOWED_PERMISSION_CODE_FILES = new Set<string>([
  'api/base.test.ts',
  // 本文件自身带着用于匹配的正则与示例串
  'views/view-permission-hygiene.test.ts',
])

function listSourceFiles(dir: string, acc: string[] = []): string[] {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry)
    if (statSync(full).isDirectory()) {
      if (entry === 'node_modules' || entry === 'dist') {
        continue
      }
      listSourceFiles(full, acc)
      continue
    }
    if (/\.(?:vue|ts)$/.test(entry)) {
      acc.push(full)
    }
  }
  return acc
}

/** 扫出后端六个模块页面登记表里登记的全部按钮码 */
function readRegisteredButtonCodes(): Set<string> {
  const codes = new Set<string>()
  for (const module of readdirSync(BACKEND_MODULES_ROOT)) {
    const registry = join(BACKEND_MODULES_ROOT, module, 'Application', 'Pages', 'PageRegistry.cs')
    if (!existsSync(registry)) {
      continue
    }
    const source = readFileSync(registry, 'utf8')
    const buttonsAt = source.indexOf('ButtonDescriptor> Buttons')
    if (buttonsAt === -1) {
      continue
    }
    for (const match of source.slice(buttonsAt).matchAll(BUTTON_DESCRIPTOR)) {
      if (match[1]) {
        codes.add(match[1])
      }
    }
  }
  return codes
}

describe('视图层权限码卫生', () => {
  it('页面不得硬编码权限码，门控一律用服务端下发的按钮码', () => {
    const offenders: string[] = []

    for (const file of listSourceFiles(SRC_ROOT)) {
      const rel = relative(SRC_ROOT, file).replaceAll('\\', '/')
      if (ALLOWED_PERMISSION_CODE_FILES.has(rel)) {
        continue
      }

      const matches = readFileSync(file, 'utf8').match(PERMISSION_CODE)
      if (matches) {
        offenders.push(`${rel}: ${[...new Set(matches)].join(', ')}`)
      }
    }

    expect(offenders, `以下文件硬编码了权限码，请改用按钮码：\n${offenders.join('\n')}`).toEqual([])
  })

  it('页面引用的按钮码必须在后端页面登记表里存在', () => {
    const registered = readRegisteredButtonCodes()
    expect(registered.size, '没扫到后端按钮登记，检查 PageRegistry 的路径或格式').toBeGreaterThan(100)

    const offenders: string[] = []
    for (const file of listSourceFiles(SRC_ROOT)) {
      const rel = relative(SRC_ROOT, file).replaceAll('\\', '/')
      if (ALLOWED_PERMISSION_CODE_FILES.has(rel)) {
        continue
      }

      for (const match of readFileSync(file, 'utf8').matchAll(GATE_LITERAL)) {
        const code = match[1] ?? ''
        if (CODE_SHAPED.test(code) && !registered.has(code)) {
          offenders.push(`${rel}: ${code}`)
        }
      }
    }

    expect(offenders, `以下按钮码在后端登记表里不存在（按钮将永不显示）：\n${offenders.join('\n')}`).toEqual([])
  })

  it('扫描确实覆盖到了视图文件，否则以上用例是空跑', () => {
    expect(listSourceFiles(SRC_ROOT).length).toBeGreaterThan(100)
  })
})
