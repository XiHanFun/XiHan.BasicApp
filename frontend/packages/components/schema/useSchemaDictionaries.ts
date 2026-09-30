import type { ComputedRef, MaybeRefOrGetter } from 'vue'
import type { ListFieldSchema, SchemaSelectOption } from './types'
import { computed, shallowReactive, toValue } from 'vue'
import { ensureDictOptions, getDictOptions, loadAsyncOptions } from '~/composables'
import { useEnumService } from '~/hooks'

/**
 * Schema 字典/枚举异步取值。
 *
 * 字段声明 `dictionaryCode`（后端枚举类型名）、`dictCode`（系统字典编码）或 `optionsLoader`（异步加载器，
 * 如外键关联记录）后，运行时拉取选项并注入到字段 `options`：单元格按值映射 label、
 * 搜索区自动渲染为下拉、导入按选项文本反查值。
 * 解析结果优先；解析为空（未加载/未部署）时回退字段静态 `options`，
 * 故字段可同时声明取值来源 + 静态 `options`，绝不出现空下拉。
 */
export interface UseSchemaDictionaries {
  /** dictionaryCode → 选项（响应式，随元数据加载填充） */
  optionsMap: ComputedRef<Record<string, SchemaSelectOption[]>>
  /** 取字段选项：optionsLoader / dictCode / dictionaryCode 解析结果优先（非空时），否则回退静态 options */
  optionsFor: (field: ListFieldSchema) => ReadonlyArray<SchemaSelectOption> | undefined
  /** 拉取所有 dictionaryCode 的枚举元数据、dictCode 的字典选项与 optionsLoader 的异步选项 */
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

  type Loader = NonNullable<ListFieldSchema['optionsLoader']>

  /** 需要调用的异步选项加载器（同一加载器只调一次） */
  const loaders = computed(() => [...new Set(toValue(fields).map(field => field.optionsLoader).filter((loader): loader is Loader => !!loader))])
  /** 本页已加载的异步选项（按加载器；不跨页缓存，关联数据改了重进页面即是新的） */
  const loaded = shallowReactive(new Map<Loader, ReadonlyArray<SchemaSelectOption>>())

  /** 需要取字典选项的系统字典编码 */
  const dictCodes = computed(() => [...new Set(toValue(fields).map(field => field.dictCode).filter((code): code is string => !!code))])

  function optionsFor(field: ListFieldSchema): ReadonlyArray<SchemaSelectOption> | undefined {
    if (field.optionsLoader) {
      const resolved = loaded.get(field.optionsLoader)
      if (resolved?.length) {
        return resolved
      }
    }
    if (field.dictCode) {
      const resolved = getDictOptions(field.dictCode).map(option => ({ label: option.label, value: option.value }))
      if (resolved.length) {
        return resolved
      }
    }
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
    await Promise.all([
      codes.value.length > 0 ? enumService.ensureBatch(codes.value) : undefined,
      // 字典加载失败已在 ensureDictOptions 里提示，这里不让一个字典的失败拖住其余取值
      ...dictCodes.value.map(code => ensureDictOptions(code).catch(() => undefined)),
      // 加载失败已在 loadAsyncOptions 里提示
      ...loaders.value.map(loader => loadAsyncOptions(loader)
        .then((options) => {
          loaded.set(loader, options)
        })
        .catch(() => undefined)),
    ])
  }

  return { optionsMap, optionsFor, resolve }
}
