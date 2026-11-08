/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { MaybeRefOrGetter } from 'vue'
import type { ListFieldSchema, SchemaSelectOption } from './types'
import { computed, shallowReactive, toValue } from 'vue'
import { ensureDictOptions, getDictOptions, loadAsyncOptions } from '~/composables'

/**
 * Schema 字段的异步选项
 *
 * 字段声明 `dictCode`（系统字典编码）或 `optionsLoader`（异步加载器，如外键关联记录）后，运行时取选项并注入到字段 `options`：
 * 单元格按值映射名称、搜索区渲染为下拉、导入按选项文本反查值
 * 取到的优先；取不到时回退字段静态 `options`，故字段可同时声明取值来源与静态 `options`
 */
export interface UseSchemaDictionaries {
  /** 取字段选项：optionsLoader / dictCode 取到的优先（非空时），否则回退静态 options */
  optionsFor: (field: ListFieldSchema) => ReadonlyArray<SchemaSelectOption> | undefined
  /** 取所有 dictCode 的字典选项与 optionsLoader 的异步选项 */
  resolve: () => Promise<void>
}

export function useSchemaDictionaries(
  fields: MaybeRefOrGetter<ListFieldSchema[]>,
): UseSchemaDictionaries {
  type Loader = NonNullable<ListFieldSchema['optionsLoader']>

  /** 需要调用的异步选项加载器（同一加载器只调一次） */
  const loaders = computed(() => [
    ...new Set(toValue(fields).map(field => field.optionsLoader).filter((loader): loader is Loader => !!loader)),
  ])
  /** 本页已加载的异步选项（按加载器；不跨页缓存，关联数据改了重进页面即是新的） */
  const loaded = shallowReactive(new Map<Loader, ReadonlyArray<SchemaSelectOption>>())

  /** 需要取字典选项的系统字典编码 */
  const dictCodes = computed(() => [
    ...new Set(toValue(fields).map(field => field.dictCode).filter((code): code is string => !!code)),
  ])

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
    return field.options
  }

  async function resolve(): Promise<void> {
    await Promise.all([
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

  return { optionsFor, resolve }
}
