import type { ComputedRef, MaybeRefOrGetter } from 'vue'
import type { ListFieldSchema, SchemaSelectOption } from './types'
import { computed, toValue } from 'vue'
import { useEnumService } from '~/hooks'

/**
 * Schema 字典/枚举异步取值。
 *
 * 字段声明 `dictionaryCode`（枚举名或字典码）后，运行时经枚举元数据端点批量拉取选项，
 * 注入到字段 `options`：单元格按值映射 label、搜索区自动渲染为下拉。
 * dictionaryCode 解析结果优先（本地化选项）；解析为空（未加载/未部署）时回退字段静态 `options`，
 * 故字段可同时声明 `dictionaryCode` + 静态 `options`，绝不出现空下拉。
 */
export interface UseSchemaDictionaries {
  /** dictionaryCode → 选项（响应式，随元数据加载填充） */
  optionsMap: ComputedRef<Record<string, SchemaSelectOption[]>>
  /** 取字段选项：dictionaryCode 解析结果优先（非空时），否则回退静态 options */
  optionsFor: (field: ListFieldSchema) => ReadonlyArray<SchemaSelectOption> | undefined
  /** 批量拉取所有 dictionaryCode 的枚举/字典元数据 */
  resolve: () => Promise<void>
}

export function useSchemaDictionaries(
  fields: MaybeRefOrGetter<ListFieldSchema[]>,
): UseSchemaDictionaries {
  const enumService = useEnumService()

  /** 需要异步解析的字典码：所有声明了 dictionaryCode 的字段（含同时带静态 options 兜底者） */
  const codes = computed(() => {
    const set = new Set<string>()
    for (const field of toValue(fields)) {
      if (field.dictionaryCode) {
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
    // dictionaryCode 解析结果优先（本地化选项）；为空时回退静态 options 兜底
    if (field.dictionaryCode) {
      const resolved = optionsMap.value[field.dictionaryCode]
      if (resolved?.length) {
        return resolved
      }
    }
    return field.options
  }

  // 触发整库拉取（并发去重）。切语言由 useEnumService 全局监听整库重取一次，
  // optionsMap 读响应式 enumState 自动更新，故此处无需再各自监听 locale（否则每页各发一次请求）。
  async function resolve(): Promise<void> {
    if (codes.value.length > 0) {
      await enumService.ensureBatch(codes.value, { includeDict: true })
    }
  }

  return { optionsMap, optionsFor, resolve }
}
