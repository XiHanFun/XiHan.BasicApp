import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios'
import type { ApiResponse } from '~/types'
import type { Router } from 'vue-router'
import axios from 'axios'
import { BIZ_CODE, LOGIN_PATH, REFRESH_TOKEN_KEY, TOKEN_KEY } from '~/constants'
import { appendRequestLog, LocalStorage, updateRequestLog } from '~/utils'
import {
  applyApiSecurityToRequest,
  resolveApiSecurityRuntimeConfig,
  tryDecryptSecureResponse,
} from './security'

type AnyRecord = Record<string, unknown>
interface RequestMeta {
  requestId: string
  startedAt: number
  method: string
  url: string
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

export class RequestClient {
  private instance: AxiosInstance
  private apiPrefix: string
  private isRefreshing = false
  private pendingRequests: Array<(token: string | null) => void> = []
  private readonly securityConfig = resolveApiSecurityRuntimeConfig()

  constructor(config?: AxiosRequestConfig & { apiPrefix?: string }) {
    this.apiPrefix = config?.apiPrefix ?? '/api'
    this.instance = axios.create({
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
      ...config,
    })

    this.setupInterceptors()
  }

  private resolveUrl(url: string) {
    if (!url.startsWith('/'))
      return url
    if (url.startsWith('/api/'))
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

  private tryExtractMeta(config: unknown): RequestMeta | null {
    const raw = (config as AnyRecord | undefined)?._meta as RequestMeta | undefined
    return raw ?? null
  }

  private setupInterceptors() {
    this.instance.interceptors.request.use(
      async (config: InternalAxiosRequestConfig) => {
        const token = LocalStorage.get<string>(TOKEN_KEY)
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
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
          Object.assign(config as AnyRecord, { _meta: meta })
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
          const payload = response.data as AnyRecord | undefined
          updateRequestLog(meta.requestId, {
            finishedAt: now,
            duration: Math.max(0, now - meta.startedAt),
            status: 'success',
            statusCode: response.status,
            responseCode: payload?.code,
            message: payload?.message,
            traceId: payload?.traceId,
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
          const payload = error?.response?.data as AnyRecord | undefined
          updateRequestLog(meta.requestId, {
            finishedAt: now,
            duration: Math.max(0, now - meta.startedAt),
            status: 'error',
            statusCode: error?.response?.status,
            responseCode: payload?.code,
            message: payload?.message ?? error?.message ?? '请求失败',
            traceId: payload?.traceId,
          })
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
    if (_router) {
      _router.replace(LOGIN_PATH)
    }
    else {
      window.location.href = LOGIN_PATH
    }
  }

  private async refreshAccessToken(): Promise<string | null> {
    const refreshToken = LocalStorage.get<string>(REFRESH_TOKEN_KEY)
    if (!refreshToken)
      return null

    if (this.isRefreshing) {
      return new Promise((resolve) => {
        this.pendingRequests.push(resolve)
      })
    }

    this.isRefreshing = true
    try {
      const { data } = await this.instance.post(
        this.resolveUrl('/auth/refreshtoken'),
        { refreshToken },
        { _isRefresh: true } as any,
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
    const typed = data as AnyRecord
    const meta = this.tryExtractMeta(response.config)

    if (typed && typeof typed === 'object') {
      const responseData = typed.data
      const responseCode = typed.code
      const responseSuccess = typed.isSuccess
      const responseMessage = typed.message
      const hasEnvelope = responseData !== undefined && (responseCode !== undefined || responseSuccess !== undefined)

      if (hasEnvelope) {
        if (responseSuccess === true || responseCode === BIZ_CODE.SUCCESS || responseCode === 0) {
          return responseData as T
        }
        if (meta) {
          const now = Date.now()
          updateRequestLog(meta.requestId, {
            finishedAt: now,
            duration: Math.max(0, now - meta.startedAt),
            status: 'error',
            statusCode: response.status,
            responseCode,
            message: (responseMessage as string) || '请求失败',
            traceId: typed.traceId,
          })
        }
        return Promise.reject(new Error((responseMessage as string) || '请求失败'))
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
