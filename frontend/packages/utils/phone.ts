import type { CountryCode } from 'libphonenumber-js/min'
import { getCountries, getCountryCallingCode, parsePhoneNumberFromString } from 'libphonenumber-js/min'

/** 记住用户选过的国家所用的 localStorage 键（与仓库其他缓存键同前缀） */
const COUNTRY_STORAGE_KEY = 'xihan_phone_country'

/** 存储 / 浏览器语言都取不到地区时的兜底国家：固定值，当前没有后端可配置的默认国码 */
const FALLBACK_COUNTRY = 'CN'

/** 读取用户上次选过的国家；读取失败（隐私模式/存储被禁用）或未曾选过均返回 null */
function readStoredPhoneCountry(): string | null {
  try {
    return typeof localStorage === 'undefined' ? null : localStorage.getItem(COUNTRY_STORAGE_KEY)
  }
  catch {
    return null
  }
}

/** 记住用户本次选择的国家，供下次默认使用；写入失败（隐私模式/存储被禁用）静默忽略 */
export function rememberPhoneCountry(country: string): void {
  try {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(COUNTRY_STORAGE_KEY, country)
    }
  }
  catch {
    // 隐私模式/存储被禁用时静默忽略，不影响当次选择
  }
}

/**
 * 本地号码 + 国家 → E.164；号码在该国家不成立时返回 null。
 *
 * 落库与登录比对一律用这个结果：库里存 0912345678 而登录提交 +886912345678，
 * 比对不上且不会有任何报错。
 */
export function normalizePhone(national: string, country: string): string | null {
  const trimmed = national.trim()
  if (!trimmed) {
    return null
  }

  const parsed = parsePhoneNumberFromString(trimmed, country as CountryCode)
  return parsed?.isValid() ? parsed.number : null
}

/** E.164 → 国家 + 本地号码，供编辑表单回填；无法解析返回 null */
export function parsePhone(e164: string | null | undefined): { country: string, national: string } | null {
  if (!e164) {
    return null
  }

  const parsed = parsePhoneNumberFromString(e164)
  if (!parsed?.isValid() || !parsed.country) {
    return null
  }

  return { country: parsed.country, national: parsed.formatNational().replace(/\D/g, '') }
}

/** 默认国家：用户存过的选择 → 浏览器语言里第一个带地区的语言（zh-TW → TW） → CN */
export function defaultPhoneCountry(
  languages: readonly string[] | undefined = typeof navigator === 'undefined' ? undefined : navigator.languages,
): string {
  const supported = new Set<string>(getCountries())

  const stored = readStoredPhoneCountry()
  if (stored && supported.has(stored)) {
    return stored
  }

  for (const language of languages ?? []) {
    let region: string | undefined
    try {
      region = new Intl.Locale(language).region
    }
    catch {
      continue
    }

    if (region && supported.has(region)) {
      return region
    }
  }

  return FALLBACK_COUNTRY
}

/** 国家选项：国家名跟随界面语言（Intl.DisplayNames，不自维护译名表） */
export function phoneCountryOptions(locale: string): Array<{ value: string, label: string, dialCode: string }> {
  const display = new Intl.DisplayNames([locale], { type: 'region' })
  return getCountries()
    .map((country) => {
      const dialCode = `+${getCountryCallingCode(country)}`
      return {
        value: country,
        dialCode,
        label: `${display.of(country) ?? country} ${dialCode}`,
      }
    })
    .sort((a, b) => a.label.localeCompare(b.label, locale))
}
