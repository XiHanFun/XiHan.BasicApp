import { dateEnUS, dateZhCN, enUS, zhCN } from 'naive-ui'
import { computed } from 'vue'
import { useAppStore } from '~/stores'

export function useNaiveLocale() {
  const appStore = useAppStore()
  const locale = computed(() => appStore.locale)

  const naiveLocale = computed(() => (locale.value === 'zh-CN' ? zhCN : enUS))
  const naiveDateLocale = computed(() => (locale.value === 'zh-CN' ? dateZhCN : dateEnUS))

  return {
    locale,
    naiveLocale,
    naiveDateLocale,
  }
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
