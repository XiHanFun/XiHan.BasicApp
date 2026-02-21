import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios'
import type { ApiResponse } from '~/types'
import { BIZ_CODE, REFRESH_TOKEN_KEY, TOKEN_KEY } from '~/constants'
import { storage } from '~/utils'

type AnyRecord = Record<string, any>

export class RequestClient {
  private instance: AxiosInstance
  private apiPrefix: string
  private isRefreshing = false
  private pendingRequests: Array<(token: string | null) => void> = []

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
    if (!url.startsWith('/')) return url
    if (url.startsWith('/api/')) return url
    return `${this.apiPrefix}${url}`
  }

  private isPlainObject(value: unknown): value is AnyRecord {
    return Object.prototype.toString.call(value) === '[object Object]'
  }

  private toPascalCaseKey(key: string) {
    if (!key) return key
    return `${key.slice(0, 1).toUpperCase()}${key.slice(1)}`
  }

  // XiHan 后端 JSON 绑定大小写严格，统一将请求体字段转为 PascalCase。
  private normalizeRequestData<T = unknown>(input: T): T {
    if (Array.isArray(input)) {
      return input.map((item) => this.normalizeRequestData(item)) as T
    }

    if (!this.isPlainObject(input)) {
      return input
    }

    const output: AnyRecord = {}
    for (const [key, value] of Object.entries(input)) {
      output[this.toPascalCaseKey(key)] = this.normalizeRequestData(value)
    }
    return output as T
  }

  private setupInterceptors() {
    this.instance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = storage.get<string>(TOKEN_KEY)
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => Promise.reject(error),
    )

    this.instance.interceptors.response.use(
      (response: AxiosResponse<ApiResponse>) => response,
      async (error) => {
        if (error.response) {
          const { status } = error.response
          if (status === BIZ_CODE.UNAUTHORIZED) {
            const originalRequest = error.config as InternalAxiosRequestConfig & {
              _retry?: boolean
            }
            if (!originalRequest?._retry) {
              originalRequest._retry = true
              const nextToken = await this.refreshAccessToken()
              if (nextToken) {
                originalRequest.headers.Authorization = `Bearer ${nextToken}`
                return this.instance(originalRequest)
              }
            }
            storage.remove(TOKEN_KEY)
            storage.remove(REFRESH_TOKEN_KEY)
            window.location.href = '/login'
          }
        }
        return Promise.reject(error)
      },
    )
  }

  private async refreshAccessToken(): Promise<string | null> {
    const refreshToken = storage.get<string>(REFRESH_TOKEN_KEY)
    if (!refreshToken) return null

    if (this.isRefreshing) {
      return new Promise((resolve) => {
        this.pendingRequests.push(resolve)
      })
    }

    this.isRefreshing = true
    try {
      const { data } = await this.instance.post(this.resolveUrl('/auth/refresh-token'), {
        RefreshToken: refreshToken,
      })
      const payload = (data?.data ?? data) as { accessToken?: string; refreshToken?: string }
      const nextAccessToken = payload?.accessToken ?? (payload as any)?.AccessToken ?? null
      if (!nextAccessToken) {
        this.pendingRequests.forEach((cb) => cb(null))
        this.pendingRequests = []
        return null
      }

      storage.set(TOKEN_KEY, nextAccessToken)
      const nextRefreshToken = payload.refreshToken ?? (payload as any)?.RefreshToken
      if (nextRefreshToken) {
        storage.set(REFRESH_TOKEN_KEY, nextRefreshToken)
      }

      this.pendingRequests.forEach((cb) => cb(nextAccessToken))
      this.pendingRequests = []
      return nextAccessToken
    } catch {
      this.pendingRequests.forEach((cb) => cb(null))
      this.pendingRequests = []
      return null
    } finally {
      this.isRefreshing = false
    }
  }

  async request<T = any>(config: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.request<ApiResponse<T> | T>(config)
    const { data } = response
    const typed = data as ApiResponse<T>

    if (
      typed &&
      typeof typed === 'object' &&
      'data' in typed &&
      ('code' in typed || 'success' in typed)
    ) {
      if (typed.success || typed.code === BIZ_CODE.SUCCESS || typed.code === 0) {
        return typed.data
      }
      return Promise.reject(new Error(typed.message || '请求失败'))
    }

    return data as T
  }

  get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({ ...config, method: 'GET', url: this.resolveUrl(url) })
  }

  post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({
      ...config,
      method: 'POST',
      url: this.resolveUrl(url),
      data: this.normalizeRequestData(data),
    })
  }

  put<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({
      ...config,
      method: 'PUT',
      url: this.resolveUrl(url),
      data: this.normalizeRequestData(data),
    })
  }

  delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const normalizedData = config?.data ? this.normalizeRequestData(config.data) : undefined

    return this.request<T>({
      ...config,
      method: 'DELETE',
      url: this.resolveUrl(url),
      data: normalizedData,
    })
  }

  patch<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.request<T>({
      ...config,
      method: 'PATCH',
      url: this.resolveUrl(url),
      data: this.normalizeRequestData(data),
    })
  }
}

export const createRequestClient = (baseURL: string, apiPrefix = '/api') =>
  new RequestClient({ baseURL, apiPrefix })
