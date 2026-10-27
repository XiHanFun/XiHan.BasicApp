import { DEFAULT_LOCALE, FALLBACK_LOCALE, LOCALE_KEY, SUPPORTED_LOCALES } from '~/constants'
import { LocalStorage } from './storage'

/** 繁体中文：台湾 / 香港 / 澳门地区码，或 zh-Hant 文字码 */
const TRADITIONAL_CHINESE = /^zh-(?:tw|hk|mo|hant)(?:-|$)/i

/**
 * 按浏览器语言清单（依偏好顺序）比对出一个已上架语言。
 *
 * 逐项依序尝试：完全相符 → 中文分繁简 → 主语言相符，取第一个比对到的。
 * 清单非空但全部不受支持时退回 FALLBACK_LOCALE；清单为空（或拿不到 navigator）回传 null，由调用方决定默认值。
 */
export function resolveBrowserLocale(
  languages: readonly string[] | undefined = typeof navigator === 'undefined' ? undefined : navigator.languages,
): string | null {
  const candidates = (languages ?? []).map(lang => lang.trim()).filter(Boolean)
  if (candidates.length === 0) {
    return null
  }

  for (const lang of candidates) {
    const exact = SUPPORTED_LOCALES.find(locale => locale.toLowerCase() === lang.toLowerCase())
    if (exact) {
      return exact
    }

    const primary = lang.split('-')[0]!.toLowerCase()
    if (primary === 'zh') {
      return TRADITIONAL_CHINESE.test(lang) ? 'zh-TW' : 'zh-CN'
    }

    const samePrimary = SUPPORTED_LOCALES.find(locale => locale.split('-')[0]!.toLowerCase() === primary)
    if (samePrimary) {
      return samePrimary
    }
  }

  return FALLBACK_LOCALE
}

/**
 * 初始界面语言：本地存储（用户手动选过）→ 浏览器语言 → DEFAULT_LOCALE。
 *
 * 偵测结果刻意不写回本地存储：用户没选过时，每次载入都重新跟随浏览器；
 * 登录后后端账号偏好会再覆盖一次（见 stores/helpers.ts 的 hydratePreferencesFromBackend）。
 */
export function resolveInitialLocale(): string {
  return LocalStorage.get<string>(LOCALE_KEY) ?? resolveBrowserLocale() ?? DEFAULT_LOCALE
}
