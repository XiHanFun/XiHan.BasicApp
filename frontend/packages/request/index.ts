import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios'
import type { Router } from 'vue-router'
import type { ApiResponse } from '~/types'
import axios from 'axios'
import { APP_TIMEZONE_KEY, BIZ_CODE, DEFAULT_LOCALE, LOCALE_KEY, LOGIN_PATH, REFRESH_TOKEN_KEY, TOKEN_KEY } from '~/constants'
import { i18n } from '~/locales'
import { appendRequestLog, LocalStorage, updateRequestLog } from '~/utils'
import {
  applyApiSecurityToRequest,
  resolveApiSecurityRuntimeConfig,
  tryDecryptSecureResponse,
} from './security'

type AnyRecord = Record<string, unknown>
type HeaderBag = Record<string, unknown> & {
  delete?: (name: string) => void
}

interface RequestMeta {
  requestId: string
  startedAt: number
  method: string
  url: string
}

function asRecord(value: unknown): AnyRecord | undefined {
  return value && typeof value === 'object' ? value as AnyRecord : undefined
}

function readResponseLogFields(payload: unknown) {
  const record = asRecord(payload)
  return {
    code: record?.code as string | number | undefined,
    message: typeof record?.message === 'string' ? record.message : undefined,
    traceId: typeof record?.traceId === 'string' ? record.traceId : undefined,
  }
}

/** Flat 请求的返回结构：data 和 error 互斥 */
export interface FlatRequestResult<T> {
  data: T | null
  error: Error | null
}

/** 全局 Router 引用，由应用层调用 bindRouter 注入 */
let _router: Router | null = null
export function bindRouter(router: Router) {
  _router = router
}

/** 强制登出时的清理回调，由应用层注入以重置 Pinia stores */
let _logoutHook: (() => void) | null = null
export function bindLogoutHook(hook: () => void) {
  _logoutHook = hook
}

/** HTTP 状态码 → 兜底中文文案（i18n 缺键时回退；正常走 error.http_<status> 键，随 locale 切换） */
const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: '请求参数有误',
  401: '登录已过期，请重新登录',
  403: '没有操作权限',
  404: '请求的资源不存在',
  408: '请求超时，请稍后重试',
  409: '请求冲突，请刷新后重试',
  422: '请求参数校验失败',
  429: '请求过于频繁，请稍后再试',
  500: '服务器内部错误',
  502: '网关错误',
  503: '服务暂时不可用',
  504: '网关超时',
}

/**
 * 走 i18n 取兜底文案：用 i18n.global.t（非组件内 useI18n，请求层无组件上下文）按 key 取值；
 * 缺键时 t 返回 key 本身，故回退到传入的中文 fallback，保证始终有可读文案。
 */
function tRequestError(key: string, fallback: string, params?: Record<string, unknown>): string {
  const translated = params
    ? i18n.global.t(`error.${key}`, params)
    : i18n.global.t(`error.${key}`)
  return translated === `error.${key}` ? fallback : translated
}

/** 从响应体提取后端业务错误消息（非二进制响应时优先采用） */
function extractBackendMessage(data: unknown): string | undefined {
  if (data && typeof data === 'object' && !(data instanceof Blob)) {
    const record = data as Record<string, unknown>
    // 后端错误工厂把具体错误放在 data（message 仅为通用码描述，如「服务器内部错误」），故优先取 data，其次 message
    for (const candidate of [record.data, record.message]) {
      if (typeof candidate === 'string' && candidate.trim()) {
        return candidate.trim()
      }
    }
  }
  return undefined
}

/** 将请求错误归一为明确提示：区分网络错误/超时/取消与各 HTTP 状态码 */
function resolveRequestErrorMessage(error: unknown): string {
  const err = error as { code?: string, message?: string, response?: { status: number, data?: unknown } }
  if (!err?.response) {
    if (err?.code === 'ECONNABORTED' || /timeout/i.test(err?.message ?? '')) {
      return tRequestError('timeout', '请求超时，请稍后重试')
    }
    if (err?.code === 'ERR_CANCELED') {
      return tRequestError('canceled', '请求已取消')
    }
    return tRequestError('network_error', '网络连接失败，请检查网络后重试')
  }
  // 后端业务消息优先（extractBackendMessage 已优先读 data）；无后端消息时走 i18n 的 http 错误文案
  const backendMessage = extractBackendMessage(err.response.data)
  if (backendMessage) {
    return backendMessage
  }
  const { status } = err.response
  const fallback = HTTP_ERROR_MESSAGES[status]
  if (fallback) {
    return tRequestError(`http_${status}`, fallback)
  }
  return tRequestError('request_failed', `请求失败（${status}）`, { status })
}

export class RequestClient {
  private instance: AxiosInstance
  private apiPrefix: string
  private refreshTokenUrl: string
  private isRefreshing = false
  private pendingRequests: Array<(token: string | null) => void> = []
  private readonly securityConfig = resolveApiSecurityRuntimeConfig()

  constructor(config?: AxiosRequestConfig & { apiPrefix?: string, refreshTokenUrl?: string }) {
    this.apiPrefix = config?.apiPrefix ?? '/api'
    // 刷新令牌端点路径可配置（request 层不可反向依赖 api/modules，故以构造选项集中管理，避免散落硬编码）
    this.refreshTokenUrl = config?.refreshTokenUrl ?? '/Auth/RefreshToken'
    this.instance = axios.create({
      timeout: 30000,
      ...config,
    })

    this.setupInterceptors()
  }

  private resolveUrl(url: string) {
    if (!url.startsWith('/'))
      return url
    // 已带 apiPrefix 前缀则不重复拼接（用配置值判断，避免硬编码 '/api/' 在自定义网关前缀下绕过拼接）
    if (this.apiPrefix && url.startsWith(`${this.apiPrefix}/`))
      return url
    return `${this.apiPrefix}${url}`
  }

  // 统一保持请求体字段原样（camelCase）。
  private normalizeRequestData<T = unknown>(input: T): T {
    return input
  }

  private createRequestId() {
    return `req_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`
  }

  private normalizeMethod(method: unknown) {
    return String(method ?? 'GET').toUpperCase()
  }

  private isFormData(input: unknown): input is FormData {
    return typeof FormData !== 'undefined' && input instanceof FormData
  }

  private removeContentTypeHeader(headers: unknown) {
    const rawHeaders = headers as HeaderBag | null | undefined
    if (!rawHeaders) {
      return
    }

    if (typeof rawHeaders.delete === 'function') {
      rawHeaders.delete('Content-Type')
      rawHeaders.delete('content-type')
      return
    }

    delete rawHeaders['Content-Type']
    delete rawHeaders['content-type']
  }

  private tryExtractMeta(config: unknown): RequestMeta | null {
    const raw = asRecord(config)?._meta as RequestMeta | undefined
    return raw ?? null
  }

  private setupInterceptors() {
    this.instance.interceptors.request.use(
      async (config: InternalAxiosRequestConfig) => {
        if (this.isFormData(config.data)) {
          this.removeContentTypeHeader(config.headers)
        }

        const token = LocalStorage.get<string>(TOKEN_KEY)
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }

        // 用户时区（已选优先，否则跟随浏览器）：后端据此将 UTC 时间换算为该时区返回
        const timezone = LocalStorage.get<string>(APP_TIMEZONE_KEY)
          || (typeof Intl !== 'undefined' ? new Intl.DateTimeFormat().resolvedOptions().timeZone : '')
        if (timezone) {
          config.headers['X-Timezone'] = timezone
        }

        // 用户语言（当前 locale，如 'zh-CN'/'en-US'）：后端据此返回本地化文案（取值方式同 X-Timezone）
        const language = LocalStorage.get<string>(LOCALE_KEY) || DEFAULT_LOCALE
        if (language) {
          config.headers['X-Language'] = language
        }

        const existingMeta = this.tryExtractMeta(config)
        if (!existingMeta) {
          const requestId = this.createRequestId()
          const method = this.normalizeMethod(config.method)
          const url = String(config.url ?? '')
          const meta: RequestMeta = {
            requestId,
            startedAt: Date.now(),
            method,
            url,
          }
          Object.assign(config as unknown as AnyRecord, { _meta: meta })
          config.headers['X-Request-Id'] = requestId

          appendRequestLog({
            requestId,
            method,
            url,
            startedAt: meta.startedAt,
            status: 'pending',
          })
        }

        if (this.securityConfig.enabled) {
          const requestUri = this.instance.getUri(config)
          await applyApiSecurityToRequest(config, requestUri, this.securityConfig)
        }

        return config
      },
      error => Promise.reject(error),
    )

    this.instance.interceptors.response.use(
      async (response: AxiosResponse<ApiResponse>) => {
        if (this.securityConfig.enabled) {
          await tryDecryptSecureResponse(response, this.securityConfig)
        }

        const meta = this.tryExtractMeta(response.config)
        if (meta) {
          const now = Date.now()
          const payload = readResponseLogFields(response.data)
          updateRequestLog(meta.requestId, {
            finishedAt: now,
            duration: Math.max(0, now - meta.startedAt),
            status: 'success',
            statusCode: response.status,
            responseCode: payload.code,
            message: payload.message,
            traceId: payload.traceId,
          })
        }
        return response
      },
      async (error) => {
        if (this.securityConfig.enabled && error?.response) {
          try {
            await tryDecryptSecureResponse(error.response, this.securityConfig)
          }
          catch {
            // 解密失败时不阻断原始错误流程
          }
        }

        const meta = this.tryExtractMeta(error?.config)
        if (meta) {
          const now = Date.now()
          const payload = readResponseLogFields(error?.response?.data)
          updateRequestLog(meta.requestId, {
            finishedAt: now,
            duration: Math.max(0, now - meta.startedAt),
            status: 'error',
            statusCode: error?.response?.status,
            responseCode: payload.code,
            message: payload.message ?? error?.message ?? '请求失败',
            traceId: payload.traceId,
          })
        }

        // 统一化错误提示：覆盖 axios 默认英文消息，业务 catch 可直接用 error.message
        if (error) {
          error.message = resolveRequestErrorMessage(error)
        }

        if (error.response) {
          const { status } = error.response
          if (status === BIZ_CODE.UNAUTHORIZED) {
            const originalRequest = error.config as InternalAxiosRequestConfig & {
              _retry?: boolean
              _isRefresh?: boolean
            }

            if (originalRequest?._isRefresh) {
              this.forceLogout()
              return Promise.reject(error)
            }

            if (!originalRequest?._retry) {
              originalRequest._retry = true
              const nextToken = await this.refreshAccessToken()
              if (nextToken) {
                originalRequest.headers.Authorization = `Bearer ${nextToken}`
                return this.instance(originalRequest)
              }
            }
            this.forceLogout()
          }
        }
        return Promise.reject(error)
      },
    )
  }

  private forceLogout() {
    LocalStorage.remove(TOKEN_KEY)
    LocalStorage.remove(REFRESH_TOKEN_KEY)
    this.pendingRequests.forEach(cb => cb(null))
    this.pendingRequests = []
    this.isRefreshing = false
    _logoutHook?.()
    if (_router) {
      _router.replace(LOGIN_PATH)
    }
    else {
      window.location.href = LOGIN_PATH
    }
  }

  private async refreshAccessToken(): Promise<string | null> {
    const refreshToken = LocalStorage.get<string>(REFRESH_TOKEN_KEY)
    const accessToken = LocalStorage.get<string>(TOKEN_KEY)
    if (!refreshToken || !accessToken)
      return null

    if (this.isRefreshing) {
      return new Promise((resolve) => {
        this.pendingRequests.push(resolve)
      })
    }

    this.isRefreshing = true
    try {
      const { data } = await this.instance.post(
        this.resolveUrl(this.refreshTokenUrl),
        { accessToken, refreshToken },
        { _isRefresh: true } as Record<string, unknown>,
      )
      const payload = (data?.data ?? data) as {
        accessToken?: string
        refreshToken?: string
      }
      const nextAccessToken = payload?.accessToken ?? null
      if (!nextAccessToken) {
        this.pendingRequests.forEach(cb => cb(null))
        this.pendingRequests = []
        return null
      }

      LocalStorage.set(TOKEN_KEY, nextAccessToken)
      const nextRefreshToken = payload?.refreshToken
      if (nextRefreshToken) {
        LocalStorage.set(REFRESH_TOKEN_KEY, nextRefreshToken)
      }

      this.pendingRequests.forEach(cb => cb(nextAccessToken))
      this.pendingRequests = []
      return nextAccessToken
    }
    catch {
      this.pendingRequests.forEach(cb => cb(null))
      this.pendingRequests = []
      return null
    }
    finally {
      this.isRefreshing = false
    }
  }

  /** Flat 模式：返回 { data, error }，不抛异常 */
  async requestFlat<T = unknown>(config: AxiosRequestConfig): Promise<FlatRequestResult<T>> {
    try {
      const data = await this.request<T>(config)
      return { data, error: null }
    }
    catch (err) {
      return { data: null, error: err instanceof Error ? err : new Error(String(err)) }
    }
  }

  async request<T = unknown>(config: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.request<ApiResponse<T> | T>(config)
    const { data } = response
    const typed = asRecord(data)
    const meta = this.tryExtractMeta(response.config)

    if (typed) {
      const responseData = typed.data
      const responseCode = typed.code
      const responseSuccess = typed.isSuccess
      const responseMessage = typed.message
      const traceId = typeof typed.traceId === 'string' ? typed.traceId : undefined
      // 信封判定不能依赖 data 字段存在：后端序列化 WhenWritingNull 会把 data=null 整个省略
      // （如秒传探测未命中）；isSuccess 是非空布尔、信封必写，作为主判据
      const hasEnvelope = typeof responseSuccess === 'boolean' || (responseData !== undefined && responseCode !== undefined)

      if (hasEnvelope) {
        if (responseSuccess === true || responseCode === BIZ_CODE.SUCCESS || responseCode === 0) {
          return (responseData ?? null) as T
        }
        if (meta) {
          const now = Date.now()
          updateRequestLog(meta.requestId, {
            finishedAt: now,
            duration: Math.max(0, now - meta.startedAt),
            status: 'error',
            statusCode: response.status,
            responseCode: typeof responseCode === 'string' || typeof responseCode === 'number' ? responseCode : undefined,
            message: typeof responseMessage === 'string' ? responseMessage : '请求失败',
            traceId,
          })
        }
        return Promise.reject(new Error(typeof responseMessage === 'string' ? responseMessage : '请求失败'))
      }
    }

    return data as T
  }

  get<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({ ...config, method: 'GET', url: this.resolveUrl(url) })
  }

  post<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({
      ...config,
      method: 'POST',
      url: this.resolveUrl(url),
      data: this.normalizeRequestData(data),
    })
  }

  put<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({
      ...config,
      method: 'PUT',
      url: this.resolveUrl(url),
      data: this.normalizeRequestData(data),
    })
  }

  delete<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const normalizedData = config?.data ? this.normalizeRequestData(config.data) : undefined

    return this.request<T>({
      ...config,
      method: 'DELETE',
      url: this.resolveUrl(url),
      data: normalizedData,
    })
  }

  patch<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({
      ...config,
      method: 'PATCH',
      url: this.resolveUrl(url),
      data: this.normalizeRequestData(data),
    })
  }

  /** Flat 快捷方法 */
  getFlat<T = unknown>(url: string, config?: AxiosRequestConfig) {
    return this.requestFlat<T>({ ...config, method: 'GET', url: this.resolveUrl(url) })
  }

  postFlat<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig) {
    return this.requestFlat<T>({ ...config, method: 'POST', url: this.resolveUrl(url), data })
  }
}

export function createRequestClient(baseURL: string, apiPrefix = '/api') {
  return new RequestClient({ baseURL, apiPrefix })
}
