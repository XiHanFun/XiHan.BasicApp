/**
 * 后端文件 URL 解析：把本地存储返回的「后端根相对路径」解析为可直链访问的绝对 URL。
 *
 * 本地存储（LocalFileStorageProvider）返回的是后端根下的相对路径（/uploads/...），
 * 经后端 UseStaticFiles 匿名暴露。生产环境前端与后端不同源（前端 basicapp、后端 basicappapi），
 * 需拼上后端源才能直链访问；开发环境 VITE_API_BASE_URL 为空，保持相对路径走 vite 的 /uploads 代理。
 *
 * 任何需要把本地存储文件用于 <img src> / 直链访问 的场景都应经此解析，而非各自拼接。
 */

/** 后端源（协议+主机+端口）。取自 VITE_API_BASE_URL；开发环境为空字符串。 */
export const BACKEND_ORIGIN = (() => {
  const base = String(import.meta.env.VITE_API_BASE_URL ?? '').trim()
  if (!base) {
    return ''
  }
  try {
    // VITE_API_BASE_URL 可能携带 /api 等路径，静态文件挂在后端根，只取源即可
    return new URL(base).origin
  }
  catch {
    return base.replace(/\/+$/, '')
  }
})()

/**
 * 将后端根相对路径（/uploads/...）解析为可直链访问的绝对 URL。
 * - 绝对 URL（http/https）、data:、blob: 原样返回；
 * - 以 / 开头的后端根相对路径：拼接后端源（无后端源配置的开发环境保持相对，走代理）；
 * - 空值返回空字符串。
 */
export function toAbsoluteFileUrl(value: string | null | undefined): string {
  const raw = (value ?? '').trim()
  if (!raw) {
    return ''
  }
  if (/^(?:https?:\/\/|data:|blob:)/i.test(raw)) {
    return raw
  }
  return raw.startsWith('/') && BACKEND_ORIGIN ? BACKEND_ORIGIN + raw : raw
}
