import { computed } from 'vue'
import { useI18n as _useI18n } from 'vue-i18n'
import { dateEnUS, dateZhCN, enUS, zhCN } from 'naive-ui'
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
  const { locale: i18nLocale } = _useI18n()

  const locale = computed(() => appStore.locale)

  function setLocale(lang: string) {
    appStore.setLocale(lang)
    i18nLocale.value = lang
  }

  return {
    locale,
    setLocale,
  }
}
