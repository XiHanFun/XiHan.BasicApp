import { computed, ref } from 'vue'
import { useAppContext } from '~/stores/app-context'
import { useAppStore } from '~/stores'

export interface EnumOption {
  name: string
  value: number | string | boolean | object
  valueText: string
  label: string
  description: string
  theme?: string
  icon?: string
  order: number
  disabled: boolean
  source: 'enum' | 'dict' | string
  extra?: Record<string, any>
}

export interface EnumDefinition {
  enumName: string
  fullName: string
  displayName: string
  cultureName: string
  isFlags: boolean
  underlyingTypeName: string
  items: EnumOption[]
}

export interface EnumBatchQuery {
  enumNames: string[]
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCodes?: string[]
  tenantId?: number
}

export interface UseEnumOptions {
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCode?: string
  dictCodes?: string[]
  tenantId?: number
}

export interface EnumSelectOption {
  label: string
  value: number | string | boolean | object
  disabled?: boolean
}

const enumState = ref<Record<string, EnumDefinition>>({})
const loading = ref(false)

export function useEnumService() {
  const appStore = useAppStore()
  const ctx = useAppContext()

  const enumMap = computed(() => enumState.value)

  function resolveLanguage(language?: string) {
    return language ?? appStore.locale ?? 'zh-CN'
  }

  async function ensureEnum(enumName: string, options: UseEnumOptions = {}) {
    if (!enumName) {
      return null
    }

    loading.value = true
    try {
      const definition = await ctx.apis.enumApi.getByName({
        enumName,
        language: resolveLanguage(options.language),
        includeHidden: options.includeHidden ?? false,
        includeDict: options.includeDict ?? false,
        dictCode: options.dictCode,
        tenantId: options.tenantId,
      })
      enumState.value[enumName] = definition
      return definition
    }
    finally {
      loading.value = false
    }
  }

  async function ensureBatch(enumNames: string[], options: UseEnumOptions = {}) {
    const names = Array.from(new Set(enumNames.filter(Boolean)))
    if (names.length === 0) {
      return {}
    }

    loading.value = true
    try {
      const query: EnumBatchQuery = {
        enumNames: names,
        language: resolveLanguage(options.language),
        includeHidden: options.includeHidden ?? false,
        includeDict: options.includeDict ?? false,
        dictCodes: options.dictCodes,
        tenantId: options.tenantId,
      }
      const result = await ctx.apis.enumApi.getBatch(query)
      enumState.value = {
        ...enumState.value,
        ...result,
      }
      return result
    }
    finally {
      loading.value = false
    }
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
