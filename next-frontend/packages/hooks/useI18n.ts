import type { XhConfig } from '@xihan-ui/vue'
import { computed } from 'vue'
import { xhTranslations } from '~/locales/xihan-ui'
import { useAppStore } from '~/stores'

/**
 * 喂给 provideXhConfig 的全局配置：语言标记与组件内建文案。
 *
 * 返回的是 computed，切语言时组件库跟着重渲——日期系组件按 locale 排星期、
 * 其余组件换掉 aria-label 那几句。App 根组件调一次即可。
 */
export function useXhUiConfig() {
  const appStore = useAppStore()

  return computed<XhConfig>(() => ({
    locale: appStore.locale,
    translations: xhTranslations[appStore.locale] ?? xhTranslations['zh-CN'],
  }))
}

export function useLocale() {
  const appStore = useAppStore()

  const locale = computed(() => appStore.locale)

  // 只改偏好 ref，vue-i18n 由 store 内的 watch 跟随。
  // 这里若再手动赋一次，切换语言就有了两条路径，而其它设备推来的偏好只走 ref 那条——
  // 正是「提示已同步、界面语言不变」的成因。
  function setLocale(lang: string) {
    appStore.setLocale(lang)
  }

  return {
    locale,
    setLocale,
  }
}
