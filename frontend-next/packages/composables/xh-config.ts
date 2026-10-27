import type { XhConfig, XhTranslationOverrides } from '@xihan-ui/vue'
import { i18n } from '~/locales'
import { xhFallbackLocale, xhLocales } from '~/locales/xihan-ui'
import { getScrollRoot } from './useScrollRoot'

/**
 * 组件库全局配置的当前值：语言标记、内建文案与滚动源。
 *
 * 组件树内经 provideXhConfig 注入，组件树外的三个命令式服务（轻提示、确认框、
 * 顶部进度条）自带宿主应用，从各自的 options.config 喂同一份。
 * 是取值函数不是常量：切语言时两边都跟着重渲。
 */

export function xhTranslationsOfCurrentLocale(): XhTranslationOverrides {
  // 组件库没有语言包的应用语言（hi-IN）取英文，不冒出中文
  return (xhLocales[i18n.global.locale.value] ?? xhFallbackLocale).translations
}

export function xhConfigValue(): XhConfig {
  return {
    // 语言标记取应用语言而非语言包自带的：没有语言包的 hi-IN 文案用英文，日期时间仍按印地语排
    locale: i18n.global.locale.value,
    translations: xhTranslationsOfCurrentLocale(),
    // 滚动搬进了内容容器，不指过去模态浮层背后照样能滚
    scrollRoot: getScrollRoot,
  }
}
