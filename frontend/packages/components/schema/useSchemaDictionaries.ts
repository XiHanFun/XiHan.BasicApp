import type { ComputedRef, MaybeRefOrGetter } from 'vue'
import type { ListFieldSchema, SchemaSelectOption } from './types'
import { useEnumService } from '~/hooks'
import { computed, toValue } from 'vue'

/**
 * Schema 字典/枚举异步取值。
 *
 * 字段声明 `dictionaryCode`（枚举名或字典码）后，运行时经枚举元数据端点批量拉取选项，
 * 注入到字段 `options`：单元格按值映射 label、搜索区自动渲染为下拉。
 * 静态 `options` 优先，已声明则不再异步取值。
 */
export interface UseSchemaDictionaries {
  /** dictionaryCode → 选项（响应式，随元数据加载填充） */
  optionsMap: ComputedRef<Record<string, SchemaSelectOption[]>>
  /** 取字段选项：静态 options 优先，其次 dictionaryCode 解析结果 */
  optionsFor: (field: ListFieldSchema) => ReadonlyArray<SchemaSelectOption> | undefined
  /** 批量拉取所有 dictionaryCode 的枚举/字典元数据 */
  resolve: () => Promise<void>
}

export function useSchemaDictionaries(
  fields: MaybeRefOrGetter<ListFieldSchema[]>,
): UseSchemaDictionaries {
  const enumService = useEnumService()

  /** 需要异步解析的字典码：声明 dictionaryCode 且未内置静态 options 的字段 */
  const codes = computed(() => {
    const set = new Set<string>()
    for (const field of toValue(fields)) {
      if (field.dictionaryCode && !field.options?.length) {
        set.add(field.dictionaryCode)
      }
    }
    return [...set]
  })

  const optionsMap = computed<Record<string, SchemaSelectOption[]>>(() => {
    const map: Record<string, SchemaSelectOption[]> = {}
    for (const code of codes.value) {
      map[code] = enumService
        .toSelectOptions(code)
        .filter(opt => typeof opt.value === 'string' || typeof opt.value === 'number')
        .map(opt => ({ label: opt.label, value: opt.value as number | string }))
    }
    return map
  })

  function optionsFor(field: ListFieldSchema): ReadonlyArray<SchemaSelectOption> | undefined {
    if (field.options?.length) {
      return field.options
    }
    if (field.dictionaryCode) {
      return optionsMap.value[field.dictionaryCode]
    }
    return undefined
  }

  async function resolve(): Promise<void> {
    if (codes.value.length > 0) {
      await enumService.ensureBatch(codes.value, { includeDict: true })
    }
  }

  return { optionsMap, optionsFor, resolve }
}
