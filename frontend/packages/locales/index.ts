import type { App } from 'vue'
import { createI18n } from 'vue-i18n'
import { DEFAULT_LOCALE } from '~/constants'
import { storage } from '~/utils'
import { LOCALE_KEY } from '~/constants'
import zhCN from './langs/zh-CN'
import enUS from './langs/en-US'

export const i18n = createI18n({
  legacy: false,
  locale: storage.get<string>(LOCALE_KEY) ?? DEFAULT_LOCALE,
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
