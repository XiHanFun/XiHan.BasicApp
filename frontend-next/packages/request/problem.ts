/**
 * RFC 9457 Problem Details（后端失败响应的唯一形态）解包成 `ApiError`。
 * 客户端只依赖 `code` 与 `errors`：`detail` 已按 Accept-Language 本地化，直接展示；`errors` 是字段键 → 文案数组，交给表单按字段显示。
 */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  code?: string
  traceId?: string
  errors?: Record<string, string[]>
  details?: Record<string, string | number | boolean | null>
}

export class ApiError extends Error {
  readonly code: string
  readonly status: number
  readonly detail: string
  readonly traceId: string
  readonly errors?: Record<string, string[]>
  readonly details?: Record<string, string | number | boolean | null>

  constructor(status: number, problem: ProblemDetails, fallbackTraceId = '') {
    const code = problem.code ?? (status === 401 ? 'SECURITY_DENIED_001' : status === 404 ? 'PROTOCOLS_NOTFOUND_000' : 'KERNEL_UNEXPECTED_000')
    const detail = problem.detail ?? problem.title ?? `HTTP ${status}`
    super(`[${code}] ${detail}`)
    this.name = 'ApiError'
    this.code = code
    this.status = problem.status ?? status
    this.detail = detail
    this.traceId = problem.traceId ?? fallbackTraceId
    this.errors = problem.errors
    this.details = problem.details
  }

  /** 错误 KIND：错误码的中段（VALIDATION、NOTFOUND、DENIED…）。 */
  get kind(): string {
    return this.code.split('_')[1] ?? ''
  }

  /** 是否带字段级错误（VALIDATION 种类）。 */
  get hasFieldErrors(): boolean {
    return !!this.errors && Object.keys(this.errors).length > 0
  }

  /** 某个字段的第一条文案；表单项按它显示。 */
  fieldError(field: string): string | undefined {
    return this.errors?.[field]?.[0]
  }
}

/** 把失败响应解析成 `ApiError`；不是 Problem Details 的正文也不丢状态码。 */
export async function parseProblem(response: Response, traceId: string): Promise<ApiError> {
  let problem: ProblemDetails = {}
  const text = await response.text().catch(() => '')
  if (text.length > 0) {
    try {
      const parsed = JSON.parse(text) as unknown
      if (parsed && typeof parsed === 'object')
        problem = parsed as ProblemDetails
    }
    catch {
      problem = { detail: text }
    }
  }
  return new ApiError(response.status, problem, traceId)
}
