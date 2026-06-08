import type { MaybeRefOrGetter, Ref } from 'vue'
import { ref, toValue, watchEffect } from 'vue'
import { useAppContext } from '~/stores/app-context'
import { toAbsoluteFileUrl } from '~/utils'

/**
 * 头像 URL 解析。
 *
 * user.avatar 的语义为「文件主键(fileId)」。但出于兼容考虑，avatar 也可能是：
 * - 空值 → 返回空字符串，由消费方（如 XUserAvatar）以首字母兜底
 * - http(s):// / data: / blob: / 开头（旧数据 / 外链 / dicebear 等）→ 不走预签名，直接显示
 * - 其它 → 视为 fileId，调用预签名 URL 端点换取临时可显示 URL
 *
 * 直链与换取结果统一经 utils/toAbsoluteFileUrl 解析：后端根相对路径（/uploads/...）
 * 会拼上后端源（生产环境前后端跨源），绝对 URL 原样返回。
 * 预签名 URL 会过期，因此仅做内存缓存（同一 fileId 不重复请求），不持久化。
 */

const presignedCache = new Map<string, string>()
const inflight = new Map<string, Promise<string>>()

/** 判断是否为可直接显示的 URL（外链 / 绝对路径 / data: 等），无需走预签名换取 */
function isDirectUrl(value: string): boolean {
  return /^(?:https?:\/\/|\/|data:|blob:)/i.test(value)
}

/**
 * 将 avatar 原始值解析为可直接用于 <img src> 的 URL。
 * 空值返回 ''；直链原样返回；fileId 经预签名端点换取（带内存缓存与并发去重）。
 */
export async function resolveAvatarUrl(avatar: null | string | undefined): Promise<string> {
  const raw = (avatar ?? '').trim()
  if (!raw) {
    return ''
  }
  if (isDirectUrl(raw)) {
    return toAbsoluteFileUrl(raw)
  }

  const cached = presignedCache.get(raw)
  if (cached) {
    return cached
  }

  let pending = inflight.get(raw)
  if (!pending) {
    const { apis } = useAppContext()
    pending = apis
      .getFilePresignedUrlApi(raw)
      .then((url) => {
        const absolute = toAbsoluteFileUrl(url ?? '')
        if (absolute) {
          presignedCache.set(raw, absolute)
        }
        return absolute
      })
      .finally(() => {
        inflight.delete(raw)
      })
    inflight.set(raw, pending)
  }

  try {
    return await pending
  }
  catch {
    // 换取失败时回退为空，由消费方（如 XUserAvatar）以首字母兜底
    return ''
  }
}

/**
 * 响应式头像 URL。传入 avatar 原始值（ref / getter / 普通值），
 * 内部解析为可显示 URL；解析中或失败时为 ''（组件用 fallback-src 兜底）。
 */
export function useAvatarUrl(source: MaybeRefOrGetter<null | string | undefined>): Ref<string> {
  const url = ref('')

  watchEffect(() => {
    const raw = toValue(source)
    // 直链可同步解析，避免无谓的闪烁与请求
    const trimmed = (raw ?? '').trim()
    if (!trimmed) {
      url.value = ''
      return
    }
    if (isDirectUrl(trimmed)) {
      url.value = toAbsoluteFileUrl(trimmed)
      return
    }
    const cached = presignedCache.get(trimmed)
    if (cached) {
      url.value = cached
      return
    }
    // 需要异步换取：先清空，换到后再赋值（避免把上一个头像的 URL 错绑到新 fileId）
    url.value = ''
    void resolveAvatarUrl(trimmed).then((resolved) => {
      // 解析期间 source 可能已变化，仅当仍指向同一 fileId 时才赋值
      if ((toValue(source) ?? '').trim() === trimmed) {
        url.value = resolved
      }
    })
  })

  return url
}
