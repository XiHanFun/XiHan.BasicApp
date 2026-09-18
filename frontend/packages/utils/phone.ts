import type { CountryCode } from 'libphonenumber-js/min'
import { getCountries, getCountryCallingCode, parsePhoneNumberFromString } from 'libphonenumber-js/min'

/** 浏览器语言里取不到地区时的兜底国家（与后端默认国码一致） */
const FALLBACK_COUNTRY = 'CN'

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

/** 默认国家：浏览器语言里第一个带地区的语言（zh-TW → TW），取不到则 CN */
export function defaultPhoneCountry(
  languages: readonly string[] | undefined = typeof navigator === 'undefined' ? undefined : navigator.languages,
): string {
  const supported = new Set<string>(getCountries())
  for (const language of languages ?? []) {
    const region = language.split('-')[1]?.toUpperCase()
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
