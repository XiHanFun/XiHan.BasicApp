import type { XhLocale } from '@xihan-ui/vue/locale'
import { deDE, enUS, esES, frFR, jaJP, koKR, ptBR, ruRU, zhCN, zhTW } from '@xihan-ui/vue/locale'

/**
 * 应用语言 → XiHan.UI 内建语言包。
 *
 * 组件内建文案（大多只给读屏器，少数上屏，如级联 / 树选择的空态）一律取语言包，应用不再逐条自写覆盖；
 * 个别要带业务语义的文案仍在组件实例上经 translations 覆盖。十种区域组件库都有语言包。
 */
export const xhLocales: Readonly<Record<string, XhLocale>> = {
  'zh-CN': zhCN,
  'zh-TW': zhTW,
  'en-US': enUS,
  'ja-JP': jaJP,
  'ko-KR': koKR,
  'de-DE': deDE,
  'es-ES': esES,
  'fr-FR': frFR,
  'pt-BR': ptBR,
  'ru-RU': ruRU,
}

/** 组件库没有对应语言包时用的那一份：与组件库自身没配语言包时的缺省一致 */
export const xhFallbackLocale: XhLocale = enUS
