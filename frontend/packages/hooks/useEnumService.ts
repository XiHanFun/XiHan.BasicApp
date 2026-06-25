import type {
  AppEnumBatchQuery as EnumBatchQuery,
  AppEnumDefinition as EnumDefinition,
  AppEnumOption as EnumOption,
} from '~/types'
import { computed, effectScope, ref, watch } from 'vue'
import { useAppStore } from '~/stores'
import { useAppContext } from '~/stores/app-context'

export type { EnumBatchQuery, EnumDefinition, EnumOption }

export interface UseEnumOptions {
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCode?: string
  dictCodes?: string[]
  tenantId?: string
}

export interface EnumSelectOption {
  label: string
  value: number | string | boolean | object
  disabled?: boolean
}

// 全量枚举元数据缓存（键=枚举类型名）。后端 AllEnums 一次返回全量，且 app-context 里
// getByName/getBatch 实际都打 AllEnums——故统一「整库取一次、按语言缓存」，杜绝按枚举各取。
const enumState = ref<Record<string, EnumDefinition>>({})
// enumState 当前持有的语言；切语言后由全局监听整库重取一次。
const loadedLang = ref<string | null>(null)
const loading = ref(false)
// 同语言并发去重：多个消费者/首屏多个下拉同时 ensure 时只发一次请求。
let inflight: Promise<Record<string, EnumDefinition>> | null = null
let inflightLang: string | null = null
// 全局语言监听只装一次（应用级，脱离组件作用域）。
let localeWatcherInstalled = false

export function useEnumService() {
  const appStore = useAppStore()
  const ctx = useAppContext()

  const enumMap = computed(() => enumState.value)

  function resolveLanguage(language?: string) {
    return language ?? appStore.locale ?? 'zh-CN'
  }

  /**
   * 整库取一次：getBatch({ enumNames: [] }) 在 app-context 返回全量映射。
   * 按语言缓存 + 同语言并发去重 → 一个页面无论多少枚举下拉，只触发一次 AllEnums 请求。
   */
  async function ensureAll(language?: string): Promise<Record<string, EnumDefinition>> {
    const lang = resolveLanguage(language)
    if (loadedLang.value === lang && Object.keys(enumState.value).length > 0) {
      return enumState.value
    }
    if (inflight && inflightLang === lang) {
      return inflight
    }
    inflightLang = lang
    loading.value = true
    inflight = ctx.apis.enumApi
      .getBatch({ enumNames: [], language: lang, includeHidden: false, includeDict: true })
      .then((all) => {
        enumState.value = all
        loadedLang.value = lang
        return all
      })
      .finally(() => {
        inflight = null
        inflightLang = null
        loading.value = false
      })
    return inflight
  }

  // 全局语言监听（装一次）：切语言后整库重取一次，避免每个 useEnumOptions/useSchemaDictionaries
  // 各自监听各自重取造成 N 次请求。脱离组件作用域以存活整个应用生命周期。
  if (!localeWatcherInstalled) {
    localeWatcherInstalled = true
    const scope = effectScope(true)
    scope.run(() => {
      watch(() => appStore.locale, (lang) => {
        // 仅在已加载过枚举时重取，避免无谓请求。
        if (loadedLang.value !== null) {
          void ensureAll(lang)
        }
      })
    })
  }

  async function ensureEnum(enumName: string, options: UseEnumOptions = {}) {
    if (!enumName) {
      return null
    }
    await ensureAll(options.language)
    return enumState.value[enumName] ?? null
  }

  async function ensureBatch(_enumNames: string[], options: UseEnumOptions = {}) {
    await ensureAll(options.language)
    return enumState.value
  }

  function getDefinition(enumName: string) {
    return enumState.value[enumName]
  }

  function toSelectOptions(enumName: string, fallback: EnumSelectOption[] = []): EnumSelectOption[] {
    const definition = getDefinition(enumName)
    if (!definition || !Array.isArray(definition.items) || definition.items.length === 0) {
      return fallback
    }

    return definition.items
      .slice()
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map(item => ({
        label: item.label,
        value: item.value,
        disabled: item.disabled,
      }))
  }

  function getLabel(
    enumName: string,
    value: string | number | boolean | object | null | undefined,
    fallback = '-',
  ) {
    const definition = getDefinition(enumName)
    if (!definition || !Array.isArray(definition.items)) {
      return fallback
    }

    const matched = definition.items.find(item =>
      item.value === value || item.valueText === String(value),
    )

    return matched?.label ?? fallback
  }

  return {
    loading,
    enumMap,
    ensureEnum,
    ensureBatch,
    getDefinition,
    toSelectOptions,
    getLabel,
  }
}
