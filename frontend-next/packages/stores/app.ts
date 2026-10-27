import type { Locale } from '~/locales'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { setLocale as applyLocale, DEFAULT_LOCALE, isLocale } from '~/locales'

const LOCALE_KEY = 'xihan.next.locale'

function readLocale(): Locale {
  try {
    const stored = localStorage.getItem(LOCALE_KEY)
    if (isLocale(stored))
      return stored
  }
  catch {}
  const browser = navigator.language
  return isLocale(browser) ? browser : DEFAULT_LOCALE
}

/** 应用偏好：区域（主题、布局、密度随壳层在 N1 补入）。 */
export const useAppStore = defineStore('app', () => {
  const locale = ref<Locale>(readLocale())

  async function setLocale(value: Locale) {
    locale.value = value
    try {
      localStorage.setItem(LOCALE_KEY, value)
    }
    catch {}
    await applyLocale(value)
  }

  return { locale, setLocale }
})
