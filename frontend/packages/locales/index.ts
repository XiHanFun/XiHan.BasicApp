import type { App } from 'vue'
import { createI18n } from 'vue-i18n'
import { DEFAULT_LOCALE, LOCALE_KEY } from '~/constants'
import { LocalStorage } from '~/utils'
import enUS from './langs/en-US'
import zhCN from './langs/zh-CN'

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

/**
 * 应用侧注册自己的业务文案。
 *
 * packages 只带 shell 自身的命名空间（common/component/menu/header/...）；
 * 业务命名空间（identity/setting/log/tenant/...）住在 src/locales，启动时经此合并进同一实例。
 * 底层包因此不必知道本应用有哪些业务模块——换一个应用复用 packages 时，它注册它自己的那套。
 *
 * 深合并：同名命名空间下的键会合并而非整体覆盖，应用可按需补充/覆写 shell 文案。
 * 必须在 app.mount() 之前调用，否则首屏会渲染出裸 key。
 */
export function registerLocaleMessages(messages: Record<string, Record<string, unknown>>) {
  for (const [locale, message] of Object.entries(messages)) {
    i18n.global.mergeLocaleMessage(locale, message)
  }
}

export const { t: $t } = i18n.global
