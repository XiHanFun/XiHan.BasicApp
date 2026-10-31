import { ApiError, parseProblem } from './problem'

/**
 * 运行时客户端只做四件事：按路由表拼请求、放头（Authorization、Accept-Language、traceparent、可选 xihan-deadline）、
 * 把 Problem Details 解包成 `ApiError`、按错误码触发刷新令牌与锁屏钩子（方案 D5 / 6.3）。生成的 operations.ts 把路由与类型交到这里。
 */
export interface RouteDef {
  readonly method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  readonly template: string
  /** 从请求里取出写进路由模板的字段名 */
  readonly path: readonly string[]
  /** 从请求里取出写进查询串的字段名（GET / DELETE 的其余标量字段也进查询串） */
  readonly query: readonly string[]
  /** 成功状态码：create 201，其余 200 */
  readonly success: number
}

export interface CallOptions {
  signal?: AbortSignal
  headers?: Record<string, string>
  /** 截止时间，写进 xihan-deadline（RFC 3339，UTC） */
  deadline?: Date
  /** 覆盖本次调用的区域 */
  locale?: string
  /** 匿名调用（登录、刷新令牌）：不带 Authorization，未认证也不走刷新钩子 */
  anonymous?: boolean
}

export interface RequestHooks {
  /** 未认证（SECURITY_DENIED_001）或访问令牌被拒（SECURITY_DENIED_003：过期、签名不符）：返回新令牌则重放一次，返回 null 即登出 */
  onUnauthenticated?: () => Promise<string | null>
  /** 会话已吊销或过期（IDENTITY_DENIED_001）：刷新令牌同样失效，清会话回登录页 */
  onSessionInvalid?: (error: ApiError) => void
  /** 会话锁定（IDENTITY_DENIED_002）：拉起锁屏 */
  onLocked?: (error: ApiError) => void
  /** 暂时不可用：别的节点在升级共享库（RUNTIME_UNAVAILABLE_003），或内核还没就绪 / 正在停机（KERNEL_UNAVAILABLE_001） */
  onMaintenance?: (error: ApiError) => void
}

export interface RequestConfig {
  /** 请求前缀；留空即同源 */
  baseUrl?: string
  getToken?: () => string | null
  getLocale?: () => string
  fetch?: typeof fetch
  hooks?: RequestHooks
}

export const ErrorCodes = {
  unauthenticated: 'SECURITY_DENIED_001',
  /** 认证处理器拒收所带凭据（JWT 过期、签名不符、缺主体声明）：与未认证一样先用刷新令牌换一次 */
  tokenRejected: 'SECURITY_DENIED_003',
  sessionInvalid: 'IDENTITY_DENIED_001',
  locked: 'IDENTITY_DENIED_002',
  /** 维护模式：别的节点正在升级共享的库，除健康检查外一律 503 */
  maintenance: 'RUNTIME_UNAVAILABLE_003',
  /** 内核还没就绪或正在停机（滚动重启的间隙），同样稍后重试 */
  notServing: 'KERNEL_UNAVAILABLE_001',
} as const

let config: RequestConfig = {}

/** 应用启动时注入令牌、区域与钩子；测试里也用它换掉 fetch。 */
export function configureRequest(next: RequestConfig): void {
  config = { ...config, ...next }
}

/** 32 位 trace-id + 16 位 span-id 的 W3C traceparent；排障时以它为准，失败响应的 traceId 由 Problem Details 携带。 */
export function newTraceparent(): string {
  return `00-${hex(16)}-${hex(8)}-01`
}

function hex(bytes: number): string {
  const buffer = new Uint8Array(bytes)
  crypto.getRandomValues(buffer)
  return Array.from(buffer, b => b.toString(16).padStart(2, '0')).join('')
}

function isScalar(value: unknown): value is string | number | boolean {
  return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean'
}

/** 按路由表把请求拆成 URL（路由值、查询串）与请求体。 */
export function buildRequest(id: string, route: RouteDef, request: unknown): { url: string, body: string | undefined } {
  const data: Record<string, unknown> = request && typeof request === 'object' ? { ...(request as Record<string, unknown>) } : {}
  let path = route.template
  for (const name of route.path) {
    const value = data[name]
    if (value === undefined || value === null)
      throw new TypeError(`Operation '${id}' (${route.method} ${route.template}) requires path parameter '${name}'`)
    path = path.replace(`{${name}}`, encodeURIComponent(String(value)))
    delete data[name]
  }

  const query = new URLSearchParams()
  const readsBody = route.method !== 'GET' && route.method !== 'DELETE'
  for (const [name, value] of Object.entries(data)) {
    const toQuery = route.query.includes(name) || !readsBody
    if (!toQuery || value === undefined || value === null)
      continue
    if (Array.isArray(value)) {
      for (const item of value) {
        if (isScalar(item))
          query.append(name, String(item))
      }
    }
    else if (isScalar(value)) {
      query.append(name, String(value))
    }
    delete data[name]
  }

  const search = query.toString()
  const url = `${config.baseUrl ?? ''}${path}${search ? `?${search}` : ''}`
  return { url, body: readsBody ? JSON.stringify(data) : undefined }
}

/** 调用一个操作：成功返回响应 DTO（无正文时为 undefined），失败抛 `ApiError`。 */
export async function invoke<TRequest, TResponse>(id: string, route: RouteDef, request: TRequest, options: CallOptions = {}): Promise<TResponse> {
  const { url, body } = buildRequest(id, route, request)
  const traceparent = newTraceparent()
  const send = async (token: string | null): Promise<Response> => {
    const headers: Record<string, string> = {
      'Accept': 'application/json',
      'Accept-Language': options.locale ?? config.getLocale?.() ?? 'en-US',
      traceparent,
      ...options.headers,
    }
    if (body !== undefined)
      headers['Content-Type'] = 'application/json'
    if (token)
      headers.Authorization = `Bearer ${token}`
    if (options.deadline)
      headers['xihan-deadline'] = options.deadline.toISOString()
    return (config.fetch ?? fetch)(url, { method: route.method, headers, body, signal: options.signal })
  }

  let response = await send(options.anonymous ? null : config.getToken?.() ?? null)
  if (!response.ok) {
    let error = await parseProblem(response, traceparent.split('-')[1] ?? '')
    if ((error.code === ErrorCodes.unauthenticated || error.code === ErrorCodes.tokenRejected) && !options.anonymous && config.hooks?.onUnauthenticated) {
      const refreshed = await config.hooks.onUnauthenticated()
      if (refreshed) {
        response = await send(refreshed)
        if (response.ok)
          return readBody<TResponse>(response)
        error = await parseProblem(response, traceparent.split('-')[1] ?? '')
      }
    }
    if (error.code === ErrorCodes.sessionInvalid && !options.anonymous)
      config.hooks?.onSessionInvalid?.(error)
    if (error.code === ErrorCodes.locked)
      config.hooks?.onLocked?.(error)
    if (error.code === ErrorCodes.maintenance || error.code === ErrorCodes.notServing)
      config.hooks?.onMaintenance?.(error)
    throw error
  }
  return readBody<TResponse>(response)
}

async function readBody<T>(response: Response): Promise<T> {
  if (response.status === 204)
    return undefined as T
  const text = await response.text()
  return (text.length === 0 ? undefined : JSON.parse(text)) as T
}

export { ApiError }
export type { ProblemDetails } from './problem'

/** 普通端点（上传、下载等不在路由表里的端点）的完整地址：接口前缀 + 路径；后端签发的相对地址（如签名下载地址）也经这里补前缀。 */
export function endpointUrl(path: string): string {
  return /^https?:\/\//i.test(path) ? path : `${config.baseUrl ?? ''}${path}`
}

export interface UploadOptions {
  /** 上传进度（0–100） */
  onProgress?: (percent: number) => void
  signal?: AbortSignal
}

/**
 * 以 multipart 表单上传到普通端点：带令牌、区域与 traceparent，未认证时与 `invoke` 一样先换令牌重放一次；
 * 用 XMLHttpRequest 是为了拿上传进度（fetch 没有）。成功返回响应 JSON，失败抛 `ApiError`。
 */
export async function uploadForm<TResponse>(path: string, form: FormData, options: UploadOptions = {}): Promise<TResponse> {
  const traceparent = newTraceparent()
  const send = (token: string | null): Promise<Response> => new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest()
    xhr.open('POST', endpointUrl(path))
    xhr.setRequestHeader('Accept', 'application/json')
    xhr.setRequestHeader('Accept-Language', config.getLocale?.() ?? 'en-US')
    xhr.setRequestHeader('traceparent', traceparent)
    if (token)
      xhr.setRequestHeader('Authorization', `Bearer ${token}`)
    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable)
        options.onProgress?.(Math.round((event.loaded / event.total) * 100))
    }
    xhr.onload = () => resolve(new Response(xhr.responseText, { status: xhr.status, headers: { 'Content-Type': xhr.getResponseHeader('Content-Type') ?? 'application/json' } }))
    xhr.onerror = () => reject(new TypeError('Network request failed'))
    xhr.onabort = () => reject(new DOMException('The upload was aborted.', 'AbortError'))
    options.signal?.addEventListener('abort', () => xhr.abort(), { once: true })
    xhr.send(form)
  })

  let response = await send(config.getToken?.() ?? null)
  if (!response.ok) {
    let error = await parseProblem(response, traceparent.split('-')[1] ?? '')
    if ((error.code === ErrorCodes.unauthenticated || error.code === ErrorCodes.tokenRejected) && config.hooks?.onUnauthenticated) {
      const refreshed = await config.hooks.onUnauthenticated()
      if (refreshed) {
        response = await send(refreshed)
        if (response.ok)
          return readBody<TResponse>(response)
        error = await parseProblem(response, traceparent.split('-')[1] ?? '')
      }
    }
    if (error.code === ErrorCodes.sessionInvalid)
      config.hooks?.onSessionInvalid?.(error)
    throw error
  }
  return readBody<TResponse>(response)
}
