/**
 * 视图层权限码卫生检查。
 *
 * 页面按钮的门控依据是服务端下发的**按钮码**（`{页面码}.{动作}`），不是权限码。
 * 权限码一旦写进前端就会与后端的按钮登记表各走各的：后端改了权限码，前端还在按旧码判定，
 * 界面上按钮照出、点下去 403，而且没有任何编译期或运行期信号。
 *
 * 两条断言配套才成立：① 前端不出现权限码；② 前端引用的按钮码在后端登记表里真实存在。
 * 只有第一条的话，把权限码换成一个拼错的按钮码同样能过，而那个按钮会永远不显示。
 *
 * 第三条管漏配：页面动作没写 permission 就对所有人可见，没权限的人点了才被后端拒绝。
 * 除只读动作外都要声明按钮码；刻意不门控的（自助操作、只读入口）登记在豁免表里并写明理由。
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

/** 只读动作：能进页面就有读权限，不单独门控 */
const READ_ONLY_ACTION_KEYS = new Set(['view', 'detail', 'preview', 'download', 'trace'])

/**
 * 刻意不门控的写动作（文件#动作键 → 理由）。新增前先确认后端接口确实不要权限码，
 * 或者只要页面本身的读权限；动作删掉或改名后这里的条目会被判为失效。
 */
const UNGATED_ACTIONS: Record<string, string> = {
  'views/tenant/list/index.vue#quota-audit': '只读：核对超配额租户，接口只要租户查看权限',
  'views/file/library/index.vue#storages': '只读入口：打开存储副本列表，抽屉里的写操作各自按按钮码门控',
  'views/file/export-center/index.vue#cancel': '自助：只作用于本人的导出任务，接口只要求登录',
  'views/file/export-center/index.vue#delete': '自助：只作用于本人的导出任务，接口只要求登录',
  'modules/codegen/views/develop/code-gen/components/datasource-panel.vue#test': '只读：连通性测试只要代码生成的读权限',
  'modules/codegen/views/develop/code-gen/components/table-panel.vue#runtime': '只读：运行时预览只要代码生成的读权限',
  'modules/workflow/views/workflow/todo/index.vue#approve': '自助：办理人办理自己的待办，后端按办理人校验',
  'modules/workflow/views/workflow/todo/index.vue#reject': '自助：办理人办理自己的待办，后端按办理人校验',
  'modules/workflow/views/workflow/todo/index.vue#transfer': '自助：办理人转办自己的待办，后端按办理人校验',
  'modules/workflow/views/workflow/todo/index.vue#addSign': '自助：办理人给自己的待办加签，后端按办理人校验',
}

/** 动作对象：从 scope 出现处向外找包住它的对象字面量 */
function enclosingObject(source: string, at: number): string | null {
  let depth = 0
  let start = -1
  for (let i = at; i >= 0; i--) {
    if (source[i] === '}') {
      depth++
    }
    else if (source[i] === '{') {
      if (depth === 0) {
        start = i
        break
      }
      depth--
    }
  }
  if (start < 0) {
    return null
  }
  depth = 0
  for (let i = start; i < source.length; i++) {
    if (source[i] === '{') {
      depth++
    }
    else if (source[i] === '}') {
      depth--
      if (depth === 0) {
        return source.slice(start, i + 1)
      }
    }
  }
  return null
}

/** 扫出页面 schema 里的全部动作：文件、动作键、是否声明了 permission */
function listSchemaActions(): Array<{ file: string, key: string, gated: boolean }> {
  const actions: Array<{ file: string, key: string, gated: boolean }> = []
  for (const file of listSourceFiles(SRC_ROOT)) {
    if (!file.endsWith('.vue')) {
      continue
    }
    const rel = relative(SRC_ROOT, file).replaceAll('\\', '/')
    const source = readFileSync(file, 'utf8')
    for (const match of source.matchAll(/scope:\s*'(?:page|row|batch)'/g)) {
      const action = enclosingObject(source, match.index)
      const key = action ? /\bkey:\s*'([^']+)'/.exec(action)?.[1] : undefined
      if (action && key) {
        actions.push({ file: rel, key, gated: /\bpermission:/.test(action) })
      }
    }
  }
  return actions
}

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

  it('页面的写动作必须声明按钮码，否则没有权限的人也看得到', () => {
    const actions = listSchemaActions()
    expect(actions.length, '没扫到页面动作，检查 schema 的写法是否变了').toBeGreaterThan(200)

    const offenders = actions
      .filter(action => !action.gated
        && !READ_ONLY_ACTION_KEYS.has(action.key)
        && !(`${action.file}#${action.key}` in UNGATED_ACTIONS))
      .map(action => `${action.file}#${action.key}`)

    expect(offenders, `以下动作没有声明 permission（按钮码见后端 PageRegistry 的 Buttons；确实不需门控的登记到 UNGATED_ACTIONS 并写明理由）：\n${offenders.join('\n')}`).toEqual([])
  })

  it('豁免表里的动作必须还在且确实没门控，失效的条目要删掉', () => {
    const ungated = new Set(listSchemaActions()
      .filter(action => !action.gated)
      .map(action => `${action.file}#${action.key}`))

    const stale = Object.keys(UNGATED_ACTIONS).filter(entry => !ungated.has(entry))
    expect(stale, `以下豁免条目已失效（动作不在了或已声明 permission）：\n${stale.join('\n')}`).toEqual([])
  })

  it('扫描确实覆盖到了视图文件，否则以上用例是空跑', () => {
    expect(listSourceFiles(SRC_ROOT).length).toBeGreaterThan(100)
  })
})
