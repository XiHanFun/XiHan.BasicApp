import type { XhConfig } from '@xihan-ui/web-components'
import { mergeXhConfig, setXhConfig } from '@xihan-ui/web-components'
import { watch } from 'vue'
import { getScrollRoot } from '~/composables/useScrollRoot'
import { i18n } from '~/locales'
import { xhFallbackLocale, xhLocales } from '~/locales/xihan-ui'

/**
 * 设计器（Web Components）的全局配置：跟着应用语言写进 web-components 的 setXhConfig。
 *
 * 应用自己的组件经 provideXhConfig 拿配置，Web Components 元素读的是另一份全局配置，两边不通；
 * 每款设计器装上时登记自己的语言包，这里把 UI 语言包与它们合并后整份写入，切语言时重写一遍。
 * 滚动源与 Vue 一侧取同一个，设计器里的对话框锁的也是内容容器。
 */

/** 一款设计器按应用语言给出的语言包；没有这种语言时返回 undefined，界面落到元素缺省的英文。 */
export type XhElementsLocale = (locale: string) => XhConfig | undefined

const contributions = new Set<XhElementsLocale>()
let watching = false

function apply(locale: string): void {
  let config: XhConfig = { locale, translations: (xhLocales[locale] ?? xhFallbackLocale).translations, scrollRoot: getScrollRoot }
  for (const contribution of contributions)
    config = mergeXhConfig(config, contribution(locale))
  setXhConfig(config)
}

/** 登记一款设计器的语言包，同一个函数登记多次只算一次；首次登记时开始跟随应用语言。 */
export function registerXhElementsLocale(contribution: XhElementsLocale): void {
  if (contributions.has(contribution))
    return
  contributions.add(contribution)
  if (watching) {
    apply(i18n.global.locale.value)
    return
  }
  watching = true
  watch(() => i18n.global.locale.value, apply, { immediate: true })
}
