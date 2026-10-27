import type { App } from 'vue'
import { createI18n } from 'vue-i18n'

/** 与后端一致的 10 种内置区域；默认 en-US。 */
export const LOCALES = ['en-US', 'zh-CN', 'zh-TW', 'ja-JP', 'ko-KR', 'de-DE', 'es-ES', 'fr-FR', 'pt-BR', 'ru-RU'] as const
export type Locale = (typeof LOCALES)[number]
export const DEFAULT_LOCALE: Locale = 'en-US'

type Messages = Record<string, unknown>
type Loader = () => Promise<{ default: Messages }>

// 壳层文案按区域懒加载；各能力包的文案由 registerModuleLocales 以同样的方式登记
const shellLoaders = import.meta.glob<{ default: Messages }>('./langs/*.ts')
const moduleLoaders = new Map<string, Loader[]>()
const loaded = new Set<string>()

export const i18n = createI18n({
  legacy: false,
  locale: DEFAULT_LOCALE,
  fallbackLocale: DEFAULT_LOCALE,
  messages: {},
})

export function isLocale(value: string | null | undefined): value is Locale {
  return LOCALES.includes(value as Locale)
}

/** 能力包在 setup.ts 里登记自己的文案加载器（`import.meta.glob('./locales/*.ts')` 的结果）。 */
export function registerModuleLocales(loaders: Record<string, Loader>): void {
  for (const [path, loader] of Object.entries(loaders)) {
    const locale = path.match(/([a-z]{2}-[A-Z]{2})\.ts$/)?.[1]
    if (!locale)
      continue
    const list = moduleLoaders.get(locale) ?? []
    list.push(loader)
    moduleLoaders.set(locale, list)
  }
}

/** 装载并切换区域：壳层与各能力包的文案一次合并进同一实例。 */
export async function setLocale(locale: Locale): Promise<void> {
  if (!loaded.has(locale)) {
    const shell = shellLoaders[`./langs/${locale}.ts`]
    const parts = await Promise.all([shell?.(), ...(moduleLoaders.get(locale) ?? []).map(load => load())])
    for (const part of parts) {
      if (part)
        i18n.global.mergeLocaleMessage(locale, part.default)
    }
    loaded.add(locale)
  }
  i18n.global.locale.value = locale
  document.documentElement.lang = locale
}

export function setupI18n(app: App): void {
  app.use(i18n)
}
