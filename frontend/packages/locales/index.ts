import type { App } from 'vue'
import { createI18n } from 'vue-i18n'
import { DEFAULT_LOCALE, LOCALE_KEY } from '~/constants'
import { LocalStorage } from '~/utils'
import enUS from './langs/en-US/index'
import zhCN from './langs/zh-CN/index'

export const i18n = createI18n({
  legacy: false,
  locale: LocalStorage.get<string>(LOCALE_KEY) ?? DEFAULT_LOCALE,
  fallbackLocale: 'zh-CN',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
  },
})

export function setupI18n(app: App) {
  app.use(i18n)
}

export const { t: $t } = i18n.global
