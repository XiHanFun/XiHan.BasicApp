/**
 * 全量 API 契约结构化测试。
 *
 * 职责边界：不逐个模块写用例，而是把 src/api/modules 与 src/modules/<模块>/api 下
 * 全部 `*Api` 门面对象一次性遍历、逐个方法探针调用，用记录下来的「HTTP 谓词 + URL + 参数位置」
 * 断言全仓统一的路由与命名约定。任何新增接口只要违反约定就会在这里失败。
 * 不发真实请求：requestClient 被整体替换为记录器。
 */
import type { AxiosRequestConfig } from '~/request'
import { beforeAll, describe, expect, it, vi } from 'vitest'

interface RecordedCall {
  method: 'DELETE' | 'GET' | 'POST' | 'PUT'
  url: string
  body?: unknown
  params?: unknown
}

const hoisted = vi.hoisted(() => ({ calls: [] as RecordedCall[] }))

vi.mock('@/api/request', () => ({
  requestClient: {
    get(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'GET', url, params: config?.params })
      return Promise.resolve(null)
    },
    post(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'POST', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    put(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'PUT', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    delete(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'DELETE', url, params: config?.params })
      return Promise.resolve(undefined)
    },
  },
}))

/** 零代码只读运行时的分页：全仓唯一一条走 GET + 查询串的分页，其余一律 POST + body */
const READONLY_RUNTIME_PAGE = '/DynamicRuntime/Page'

/**
 * 已知的「同一门面内同谓词同 URL」重复项，正常应为空。
 *
 * 回归锚点：曾经 userApi.resetPassword 与 userSecurityApi.resetPassword 都打
 * POST /UserSecurity/ResetUserPassword，被 userManagementApi 同时展开（`...userApi` +
 * `security: userSecurityApi`）后在一个门面里出现两个等价入口、且两者声明的返回 DTO 不同。
 * 前者已删除，这里锁定「不再有重复路由」；新增重复项必须先消除，而不是往这张清单里追加。
 */
const KNOWN_DUPLICATE_ROUTES: string[] = []

/** 分页探针入参：用同一个对象实例，便于断言「原样作 body 上送」而不是被拆成查询串 */
const PAGE_QUERY = {
  conditions: { filters: [], keyword: null, sorts: [] },
  page: { pageIndex: 1, pageSize: 20 },
}

/** 一次探针调用的结果：门面来源 + 方法名 + 记录到的请求 */
interface Endpoint extends RecordedCall {
  /** 门面导出名，如 userApi / permissionCenterApi */
  facade: string
  /** 门面内的访问路径，如 detail / inbox.markRead */
  path: string
}

const endpoints: Endpoint[] = []
/** 探针调用后没有发出任何请求的方法（理应为空） */
const silent: string[] = []
/** 探针调用直接抛错的方法（理应为空） */
const failed: string[] = []

function isPageLike(name: string) {
  return name === 'page' || name.endsWith('Page')
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/** DynamicApiClient 本体（get/post/put/delete 四件套）不是业务门面，不参与探针 */
function isDynamicApiClient(value: Record<string, unknown>) {
  return ['get', 'post', 'put', 'delete'].every(key => typeof value[key] === 'function')
}

function probe(facade: string, path: string, fn: (...args: unknown[]) => unknown) {
  const before = hoisted.calls.length
  const firstArg: unknown = isPageLike(path.split('.').at(-1) ?? '') ? PAGE_QUERY : 'probe-1'
  try {
    void fn(firstArg, 'probe-2', 'probe-3')
  }
  catch (error) {
    failed.push(`${facade}.${path} → ${String(error)}`)
    return
  }

  const produced = hoisted.calls.slice(before)
  if (produced.length === 0) {
    silent.push(`${facade}.${path}`)
    return
  }
  for (const call of produced) {
    endpoints.push({ ...call, facade, path })
  }
}

function walk(facade: string, prefix: string, node: Record<string, unknown>, seen: Set<object>) {
  if (seen.has(node)) {
    return
  }
  seen.add(node)

  for (const [key, value] of Object.entries(node)) {
    const path = prefix ? `${prefix}.${key}` : key
    if (typeof value === 'function') {
      probe(facade, path, value as (...args: unknown[]) => unknown)
    }
    else if (isPlainObject(value) && !isDynamicApiClient(value)) {
      walk(facade, path, value, seen)
    }
  }
}

beforeAll(() => {
  // 排除 *.types.ts：它们不导出门面对象，且 src/modules/chat/api/chat.types.ts 会值转出
  // `~/chat`（链到没有构建产物的 @xihan-ui），直接 glob 会让整份用例文件解析失败。
  const globbed = {
    ...import.meta.glob<Record<string, unknown>>(
      ['/src/api/modules/**/*.ts', '!/src/api/modules/**/*.types.ts'],
      { eager: true },
    ),
    ...import.meta.glob<Record<string, unknown>>(
      ['/src/modules/*/api/**/*.ts', '!/src/modules/*/api/**/*.types.ts'],
      { eager: true },
    ),
  }

  const visitedFacades = new Set<object>()
  for (const moduleExports of Object.values(globbed)) {
    for (const [exportName, exported] of Object.entries(moduleExports)) {
      if (!exportName.endsWith('Api') || !isPlainObject(exported) || visitedFacades.has(exported)) {
        continue
      }
      visitedFacades.add(exported)
      walk(exportName, '', exported, new Set<object>())
    }
  }
})

describe('探针本身的完整性', () => {
  it('每个门面方法都实际发出了请求，没有静默方法', () => {
    expect(silent).toEqual([])
  })

  it('没有任何门面方法在探针调用下抛错', () => {
    expect(failed).toEqual([])
  })

  it('遍历到的端点数量与门面数量都在合理量级——门面被整体漏掉时这里先失败', () => {
    const facades = new Set(endpoints.map(item => item.facade))

    expect(endpoints.length).toBeGreaterThan(300)
    expect(facades.size).toBeGreaterThan(45)
  })
})

describe('接口地址形状约定', () => {
  it('每条地址都是「/控制器/动作」形态，控制器为大驼峰且不含分隔符', () => {
    const bad = endpoints.filter(item => !/^\/[A-Z][A-Za-z0-9]*\//.test(item.url))

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })

  it('地址至少两段且每段非空，不出现连续斜杠或结尾斜杠', () => {
    const bad = endpoints.filter((item) => {
      const segments = item.url.split('/')
      return segments.length < 3 || segments.slice(1).some(segment => segment.length === 0)
    })

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })

  it('地址里没有硬编码 host、协议或端口——baseURL 由 request 层注入', () => {
    const bad = endpoints.filter(item =>
      item.url.includes('://') || item.url.startsWith('//') || /localhost|127\.0\.0\.1/i.test(item.url))

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })

  it('地址里没有自带 /api 前缀——重复拼前缀会被 request 层的去重逻辑掩盖成难查的 404', () => {
    const bad = endpoints.filter(item => item.url.startsWith('/api/'))

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })

  it('地址里不带查询串——查询参数一律走 params 而不是拼进路径', () => {
    const bad = endpoints.filter(item => item.url.includes('?') || item.url.includes('&'))

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })

  it('动作段不以 Async 结尾——后端方法名的 Async 后缀不进路由', () => {
    const bad = endpoints.filter(item => item.url.split('/').some(segment => segment.endsWith('Async')))

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })
})

describe('方法命名与 HTTP 谓词的一致性', () => {
  it('名为 detail 的方法一律 GET', () => {
    const bad = endpoints.filter(item => item.path.split('.').at(-1) === 'detail' && item.method !== 'GET')

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.method}`)).toEqual([])
  })

  it('以 get 开头的方法一律走 GET', () => {
    const bad = endpoints.filter((item) => {
      const name = item.path.split('.').at(-1) ?? ''
      return /^get[A-Z]/.test(name) && item.method !== 'GET'
    })

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.method}`)).toEqual([])
  })

  it('名为 create 的方法一律 POST', () => {
    const bad = endpoints.filter(item => item.path.split('.').at(-1) === 'create' && item.method !== 'POST')

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.method}`)).toEqual([])
  })

  it('update 开头的方法一律 PUT', () => {
    const bad = endpoints.filter((item) => {
      const name = item.path.split('.').at(-1) ?? ''
      return name.startsWith('update') && item.method !== 'PUT'
    })

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.method}`)).toEqual([])
  })

  it('名为 delete 的方法一律 DELETE', () => {
    const bad = endpoints.filter(item => item.path.split('.').at(-1) === 'delete' && item.method !== 'DELETE')

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.method}`)).toEqual([])
  })

  it('分页方法的动作段必须以 Page 结尾——后端分页端点靠这个后缀识别', () => {
    const bad = endpoints.filter(item => isPageLike(item.path.split('.').at(-1) ?? '')
      && !(item.url.split('/').at(-1) ?? '').endsWith('Page'))

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.url}`)).toEqual([])
  })
})

describe('参数位置约定', () => {
  it('读取与删除谓词不带请求体——后端 DELETE 只做查询串绑定', () => {
    const bad = endpoints.filter(item => (item.method === 'GET' || item.method === 'DELETE')
      && item.body !== undefined)

    expect(bad.map(item => `${item.facade}.${item.path}`)).toEqual([])
  })

  it('分页查询对象原样作 body 上送，不被拆成查询串', () => {
    const pageCalls = endpoints.filter(item => isPageLike(item.path.split('.').at(-1) ?? '')
      && item.url !== READONLY_RUNTIME_PAGE)
    const bad = pageCalls.filter(item => item.body !== PAGE_QUERY || item.params !== undefined)

    expect(pageCalls.length).toBeGreaterThan(40)
    expect(bad.map(item => `${item.facade}.${item.path}`)).toEqual([])
  })

  it('除零代码运行时只读分页外，分页一律走 POST', () => {
    const bad = endpoints.filter(item => isPageLike(item.path.split('.').at(-1) ?? '')
      && item.method !== 'POST'
      && item.url !== READONLY_RUNTIME_PAGE)

    expect(bad.map(item => `${item.facade}.${item.path} → ${item.method} ${item.url}`)).toEqual([])
  })

  it('唯一走 GET 的分页只有零代码运行时——多出第二个就说明有人绕开了统一约定', () => {
    const runtimePages = endpoints.filter(item => item.url === READONLY_RUNTIME_PAGE)

    expect(runtimePages.map(item => `${item.facade}.${item.path} → ${item.method}`))
      .toEqual(['codeGenRuntimeApi.page → GET'])
  })

  it('查询参数的键名不含空格与斜杠，能安全拼进查询串', () => {
    const bad: string[] = []
    for (const item of endpoints) {
      if (!isPlainObject(item.params)) {
        continue
      }
      for (const key of Object.keys(item.params)) {
        if (key.length === 0 || /[\s/?&=#]/.test(key)) {
          bad.push(`${item.facade}.${item.path} → ${key}`)
        }
      }
    }

    expect(bad).toEqual([])
  })
})

describe('门面内部的路由唯一性', () => {
  it('同一个门面对象内不出现重复的「谓词 + URL」组合——重复即意味着某个方法拼错了动作名', () => {
    const duplicates: string[] = []
    const byFacade = new Map<string, Map<string, string>>()

    for (const item of endpoints) {
      const table = byFacade.get(item.facade) ?? new Map<string, string>()
      byFacade.set(item.facade, table)
      const key = `${item.method} ${item.url}`
      const owner = table.get(key)
      if (owner && owner !== item.path) {
        duplicates.push(`${item.facade}: ${owner} 与 ${item.path} 同为 ${key}`)
      }
      else {
        table.set(key, item.path)
      }
    }

    expect(duplicates).toEqual(KNOWN_DUPLICATE_ROUTES)
  })
})

describe('控制器命名约定', () => {
  it('只读控制器（仅出现 GET / 分页 POST）与写控制器不混用同一个名字下的删除动作', () => {
    const controllers = new Map<string, Set<string>>()
    for (const item of endpoints) {
      const controller = item.url.split('/')[1] ?? ''
      const methods = controllers.get(controller) ?? new Set<string>()
      methods.add(item.method)
      controllers.set(controller, methods)
    }

    const bad = [...controllers.entries()]
      .filter(([name, methods]) => name.endsWith('Query') && (methods.has('DELETE') || methods.has('PUT')))
      .map(([name]) => name)

    expect(bad).toEqual([])
  })

  it('每个控制器名都不以 Controller / Service / AppService 结尾', () => {
    const bad = [...new Set(endpoints.map(item => item.url.split('/')[1] ?? ''))]
      .filter(name => /(?:Controller|AppService|Service)$/.test(name))

    expect(bad).toEqual([])
  })
})
