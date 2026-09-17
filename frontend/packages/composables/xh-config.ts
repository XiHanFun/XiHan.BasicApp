import type { XhConfig, XhTranslationOverrides } from '@xihan-ui/vue'
import { i18n } from '~/locales'
import { xhTranslations } from '~/locales/xihan-ui'
import { getScrollRoot } from './useScrollRoot'

/**
 * 组件库全局配置的当前值：语言标记、内建文案覆盖与滚动源。
 *
 * 组件树内经 provideXhConfig 注入，组件树外的三个命令式服务（轻提示、确认框、
 * 顶部进度条）自带宿主应用，从各自的 options.config 喂同一份。
 * 是取值函数不是常量：切语言时两边都跟着重渲。
 */

export function xhTranslationsOfCurrentLocale(): XhTranslationOverrides {
  const locale = i18n.global.locale.value
  if (xhTranslations[locale]) {
    return xhTranslations[locale]!
  }
  // 未登记的语言（如浏览器探测出的 ja-JP/ko-KR/hi-IN）按主语言回退：
  // zh 系回退 zh-CN，其余回退 en-US（与 MdEditor.vue、ChatEmojiPicker.vue 同一约定），
  // 避免非中文界面里的组件库内建文案冒出中文
  return locale.startsWith('zh') ? xhTranslations['zh-CN']! : xhTranslations['en-US']!
}

export function xhConfigValue(): XhConfig {
  return {
    locale: i18n.global.locale.value,
    translations: xhTranslationsOfCurrentLocale(),
    // 滚动搬进了内容容器，不指过去模态浮层背后照样能滚
    scrollRoot: getScrollRoot,
  }
}
