/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { DEFAULT_LOCALE, FALLBACK_LOCALE, LOCALE_KEY, SUPPORTED_LOCALES } from '~/constants'
import { LocalStorage } from './storage'

/** 繁体中文：台湾 / 香港 / 澳门地区码，或 zh-Hant 文字码 */
const TRADITIONAL_CHINESE = /^zh-(?:tw|hk|mo|hant)(?:-|$)/i

/**
 * 按浏览器语言清单的偏好顺序比对出一个已上架语言
 * 逐项试：完全相符 → 中文分繁简 → 主语言相符
 * 全不支持退回 FALLBACK_LOCALE；清单为空（或拿不到 navigator）返回 null，由调用方定默认值
 */
export function resolveBrowserLocale(
  languages: readonly string[] | undefined = typeof navigator === 'undefined'
    ? undefined
    : navigator.languages,
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
      return TRADITIONAL_CHINESE.test(lang)
        ? 'zh-TW'
        : 'zh-CN'
    }

    const samePrimary = SUPPORTED_LOCALES.find(locale => locale.split('-')[0]!.toLowerCase() === primary)
    if (samePrimary) {
      return samePrimary
    }
  }

  return FALLBACK_LOCALE
}

/**
 * 初始界面语言：用户选过的 → 浏览器语言 → DEFAULT_LOCALE
 * 检测结果不写回本地存储，没选过时每次载入都跟随浏览器；登录后账号偏好再覆盖（见 hydratePreferencesFromBackend）
 */
export function resolveInitialLocale(): string {
  return LocalStorage.get<string>(LOCALE_KEY) ?? resolveBrowserLocale() ?? DEFAULT_LOCALE
}
