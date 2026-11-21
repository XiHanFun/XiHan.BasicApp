/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { ApiError, parseProblem } from './problem'

/**
 * 运行时客户端只做四件事：按路由表拼请求、放头（Authorization、Accept-Language、traceparent、可选 xihan-deadline）、
 * 把 Problem Details 解包成 `ApiError`、按错误码触发刷新令牌与锁屏钩子（方案 D5 / 6.3）。生成的 operations.ts 把路由与类型交到这里
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
  /** 截止时间，写进 xihan-deadline（RFC 3339，UTC）；给了即以它为准，不再按超时推 */
  deadline?: Date
  /** 本次调用的超时（毫秒）：缺省取 `RequestConfig.timeoutMs`，再缺省是 `DEFAULT_TIMEOUT_MS`；0 表示不设截止时间 */
  timeoutMs?: number
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
  /** 普通调用的超时（毫秒），缺省 `DEFAULT_TIMEOUT_MS` */
  timeoutMs?: number
  /** 前端自己中止超时调用时给用户看的文案（后端没来得及回本地化正文） */
  getTimeoutMessage?: () => string
}

/**
 * 普通调用缺省的超时
 * 截止时间写进 xihan-deadline，后端到点取消调用并回 `KERNEL_TIMEOUT_001`
 */
export const DEFAULT_TIMEOUT_MS = 60_000

/**
 * 跑得久的调用用的超时
 * 调模型（知识库导入、问答、连通测试、助手回复）、生成代码、随文件大小增长的存储操作（合并分片、跨存储复制、校验）、重试升级独立库
 */
export const LONG_TIMEOUT_MS = 300_000

/**
 * 截止时间过后再等这么久仍没有响应，前端自己中止
 * 后端挂死或网络中断时它回不了超时，留出余量让它正常回的先到
 */
const TIMEOUT_GRACE_MS = 5_000

export const ErrorCodes = {
  unauthenticated: 'SECURITY_DENIED_001',
  /** 认证处理器拒收所带凭据（JWT 过期、签名不符、缺主体声明）：与未认证一样先用刷新令牌换一次 */
  tokenRejected: 'SECURITY_DENIED_003',
  sessionInvalid: 'IDENTITY_DENIED_001',
  locked: 'IDENTITY_DENIED_002',
  /** 维护模式：别的节点正在升级共享的库，除健康检查外一律 503 */
  maintenance: 'RUNTIME_UNAVAILABLE_003',
  /** 调用超过截止时间：后端回的，或前端等不到响应自己中止的 */
  timeout: 'KERNEL_TIMEOUT_001',
  /** 内核还没就绪或正在停机（滚动重启的间隙），同样稍后重试 */
  notServing: 'KERNEL_UNAVAILABLE_001',
} as const

let config: RequestConfig = {}

/** 应用启动时注入令牌、区域与钩子；测试里也用它换掉 fetch */
export function configureRequest(next: RequestConfig): void {
  config = {
    ...config,
    ...next,
  }
}

/**
 * 生成 W3C traceparent（32 位 trace-id + 16 位 span-id）
 * 排障以它为准，失败响应的 traceId 由 Problem Details 携带
 */
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

/** 按路由表把请求拆成 URL（路由值、查询串）与请求体 */
export function buildRequest(id: string, route: RouteDef, request: unknown): { url: string, body: string | undefined } {
  const data: Record<string, unknown> = request && typeof request === 'object'
    ? { ...(request as Record<string, unknown>) }
    : {}
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
  const url = `${config.baseUrl ?? ''}${path}${search
    ? `?${search}`
    : ''}`
  return {
    url,
    body: readsBody
      ? JSON.stringify(data)
      : undefined,
  }
}

/** 调用一个操作：成功返回响应 DTO（无正文时为 undefined），失败抛 `ApiError` */
/**
 * 截止时间的看守：到点再过宽限期仍没结束就中止请求
 * 调用方自己的取消照常传过来；`expired` 区分是看守中止的还是调用方取消的
 */
function watchDeadline(deadline: Date, outer: AbortSignal | undefined) {
  const controller = new AbortController()
  let expired = false
  const timer = setTimeout(() => {
    expired = true
    controller.abort(new DOMException('The request timed out.', 'TimeoutError'))
  }, Math.max(0, deadline.getTime() - Date.now()) + TIMEOUT_GRACE_MS)
  const forward = () => controller.abort(outer?.reason)
  if (outer?.aborted)
    forward()
  else
    outer?.addEventListener('abort', forward, { once: true })
  return {
    signal: controller.signal,
    get expired() {
      return expired
    },
    dispose() {
      clearTimeout(timer)
      outer?.removeEventListener('abort', forward)
    },
  }
}

export async function invoke<TRequest, TResponse>(id: string, route: RouteDef, request: TRequest, options: CallOptions = {}): Promise<TResponse> {
  const timeout = options.timeoutMs ?? config.timeoutMs ?? DEFAULT_TIMEOUT_MS
  const deadline = options.deadline ?? (timeout > 0
    ? new Date(Date.now() + timeout)
    : undefined)
  const watch = deadline
    ? watchDeadline(deadline, options.signal)
    : undefined
  try {
    return await invokeOnce<TResponse>(id, route, request, options, deadline, watch?.signal ?? options.signal)
  }
  catch (error) {
    // 看守中止的是超时：换成与后端同码的错误，调用方取消的照原样抛
    if (watch?.expired)
      throw new ApiError(504, { code: ErrorCodes.timeout, detail: config.getTimeoutMessage?.() ?? 'The request timed out.' })
    throw error
  }
  finally {
    watch?.dispose()
  }
}

/** 发出一次调用（未认证时换令牌重放一次），截止时间与取消信号由 `invoke` 定好 */
async function invokeOnce<TResponse>(id: string, route: RouteDef, request: unknown, options: CallOptions, deadline: Date | undefined, signal: AbortSignal | undefined): Promise<TResponse> {
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
    if (deadline)
      headers['xihan-deadline'] = deadline.toISOString()
    return (config.fetch ?? fetch)(url, {
      method: route.method,
      headers,
      body,
      signal,
    })
  }

  let response = await send(options.anonymous
    ? null
    : config.getToken?.() ?? null)
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
  return (text.length === 0
    ? undefined
    : JSON.parse(text)) as T
}

export { ApiError }
export type { ProblemDetails } from './problem'

/**
 * 普通端点（不在路由表里的）补上接口前缀
 * 后端签发的相对地址（如签名下载地址）也经这里补
 */
export function endpointUrl(path: string): string {
  return /^https?:\/\//i.test(path)
    ? path
    : `${config.baseUrl ?? ''}${path}`
}

export interface UploadOptions {
  /** 上传进度（0–100） */
  onProgress?: (percent: number) => void
  signal?: AbortSignal
}

/**
 * 以 multipart 表单上传到普通端点，失败抛 `ApiError`
 * 带令牌、区域与 traceparent，未认证时同 `invoke` 先换令牌重放一次
 * 用 XMLHttpRequest 是为拿上传进度（fetch 没有）
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
    xhr.onload = () => resolve(new Response(xhr.responseText, {
      status: xhr.status,
      headers: { 'Content-Type': xhr.getResponseHeader('Content-Type') ?? 'application/json' },
    }))
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

/** 普通端点下载的结果：内容与响应头给的文件名（没有时为空） */
export interface DownloadedFile {
  blob: Blob
  fileName: string | null
}

/** Content-Disposition 的文件名：优先 filename*（RFC 5987），否则 filename */
function fileNameOf(disposition: string | null): string | null {
  if (!disposition)
    return null
  const encoded = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(disposition)
  if (encoded?.[1]) {
    try {
      return decodeURIComponent(encoded[1].trim())
    }
    catch {
      return null
    }
  }
  const plain = /filename\s*=\s*"?([^";]+)"?/i.exec(disposition)
  return plain?.[1]?.trim() || null
}

/**
 * 从普通端点下载文件，失败抛 `ApiError`
 * 带令牌、区域与 traceparent，未认证时同 `invoke` 先换令牌重放一次
 */
export async function downloadEndpoint(path: string, options: { signal?: AbortSignal } = {}): Promise<DownloadedFile> {
  const traceparent = newTraceparent()
  const send = (token: string | null): Promise<Response> => {
    const headers: Record<string, string> = {
      'Accept-Language': config.getLocale?.() ?? 'en-US',
      traceparent,
    }
    if (token)
      headers.Authorization = `Bearer ${token}`
    return (config.fetch ?? fetch)(endpointUrl(path), { method: 'GET', headers, signal: options.signal })
  }

  let response = await send(config.getToken?.() ?? null)
  if (!response.ok) {
    let error = await parseProblem(response, traceparent.split('-')[1] ?? '')
    if ((error.code === ErrorCodes.unauthenticated || error.code === ErrorCodes.tokenRejected) && config.hooks?.onUnauthenticated) {
      const refreshed = await config.hooks.onUnauthenticated()
      if (refreshed) {
        response = await send(refreshed)
        if (response.ok) {
          return {
            blob: await response.blob(),
            fileName: fileNameOf(response.headers.get('Content-Disposition')),
          }
        }
        error = await parseProblem(response, traceparent.split('-')[1] ?? '')
      }
    }
    if (error.code === ErrorCodes.sessionInvalid)
      config.hooks?.onSessionInvalid?.(error)
    throw error
  }
  return {
    blob: await response.blob(),
    fileName: fileNameOf(response.headers.get('Content-Disposition')),
  }
}
