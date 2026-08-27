import type { ComputedRef } from 'vue'
import type { EnumOptionItem } from './useEnumOptions'
/**
 * useEnumOptions 下拉选项单元测试。
 * 职责：锁定「挂载即触发整库拉取」「后端元数据优先、为空才回退静态兜底」
 * 「非 string/number 的枚举值被剔除」「返回的兜底项是副本而非原引用」这几条约定，
 * 以及数据到达后 computed 自动重算。
 */
import type { AppEnumBatchQuery, AppEnumDefinition, AppEnumOption } from '~/types'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'

type BatchFn = (query: AppEnumBatchQuery) => Promise<Record<string, AppEnumDefinition>>

function makeOption(partial: Partial<AppEnumOption> & Pick<AppEnumOption, 'label' | 'value'>): AppEnumOption {
  return {
    name: String(partial.value),
    valueText: String(partial.value),
    description: '',
    order: 0,
    disabled: false,
    source: 'enum',
    ...partial,
  }
}

function makeDefinition(enumName: string, items: AppEnumOption[]): AppEnumDefinition {
  return {
    enumName,
    fullName: `XiHan.${enumName}`,
    displayName: enumName,
    cultureName: 'zh-CN',
    isFlags: false,
    underlyingTypeName: 'Int32',
    items,
  }
}

/** 每个用例一份全新模块状态；返回挂载后的 options 与假接口 */
async function mountOptions(
  getBatch: BatchFn,
  enumName: string,
  fallback: ReadonlyArray<EnumOptionItem> = [],
) {
  vi.resetModules()
  setActivePinia(createPinia())
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({ apis: { enumApi: { getBatch, getByName: () => Promise.reject(new Error('未使用')) } } as never })
  const { useEnumOptions } = await import('./useEnumOptions')

  let captured: ComputedRef<EnumOptionItem[]> | null = null
  const wrapper = mount(defineComponent({
    setup() {
      captured = useEnumOptions(enumName, fallback)
      return () => h('div')
    },
  }))
  return { options: captured as unknown as ComputedRef<EnumOptionItem[]>, wrapper }
}

describe('useEnumOptions 拉取时机', () => {
  it('组件挂载时触发一次整库拉取', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({}))

    await mountOptions(getBatch, 'EnableStatus')

    expect(getBatch).toHaveBeenCalledTimes(1)
  })

  it('同一页面多个下拉共用一次拉取，不按下拉数量放大请求', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({}))
    vi.resetModules()
    setActivePinia(createPinia())
    const { registerAppContext } = await import('~/stores/app-context')
    registerAppContext({ apis: { enumApi: { getBatch, getByName: () => Promise.reject(new Error('未使用')) } } as never })
    const { useEnumOptions } = await import('./useEnumOptions')

    mount(defineComponent({
      setup() {
        useEnumOptions('EnableStatus')
        useEnumOptions('PermissionType')
        useEnumOptions('EnableStatus')
        return () => h('div')
      },
    }))
    await Promise.resolve()

    expect(getBatch).toHaveBeenCalledTimes(1)
  })
})

describe('useEnumOptions 元数据优先与兜底', () => {
  it('元数据为空时返回静态兜底选项', async () => {
    const fallback = [{ label: '启用', value: 1 }, { label: '禁用', value: 0 }]
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({})), 'EnableStatus', fallback)

    expect(options.value).toEqual(fallback)
  })

  it('兜底项返回的是浅拷贝，改动结果不会污染常量数组', async () => {
    const fallback = [{ label: '启用', value: 1 }]
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({})), 'EnableStatus', fallback)

    expect(options.value[0]).not.toBe(fallback[0])
    expect(options.value[0]).toEqual(fallback[0])
  })

  it('兜底项的附加字段（索引签名）被完整保留', async () => {
    const fallback = [{ label: '启用', value: 1, tagType: 'success' }]
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({})), 'EnableStatus', fallback)

    expect(options.value[0]?.tagType).toBe('success')
  })

  it('未传兜底且元数据为空时返回空数组', async () => {
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({})), 'EnableStatus')

    expect(options.value).toEqual([])
  })

  it('元数据到达前用兜底、到达后自动重算切换到后端标签', async () => {
    const definition = makeDefinition('EnableStatus', [
      makeOption({ label: '禁用', value: 0, order: 2 }),
      makeOption({ label: '启用', value: 1, order: 1 }),
    ])
    let resolveBatch: ((value: Record<string, AppEnumDefinition>) => void) | null = null
    const getBatch = vi.fn<BatchFn>(() => new Promise((resolve) => {
      resolveBatch = resolve
    }))
    const { options } = await mountOptions(getBatch, 'EnableStatus', [{ label: '兜底', value: -1 }])

    // 请求还挂着，先给兜底，不能让下拉空着
    expect(options.value).toEqual([{ label: '兜底', value: -1 }])

    resolveBatch!({ EnableStatus: definition })
    await Promise.resolve()
    await Promise.resolve()

    expect(options.value).toEqual([{ label: '启用', value: 1 }, { label: '禁用', value: 0 }])
  })
})

describe('useEnumOptions 值类型过滤', () => {
  it('剔除布尔值与对象值条目，只保留 string / number', async () => {
    const definition = makeDefinition('Mixed', [
      makeOption({ label: '数字', value: 1, order: 1 }),
      makeOption({ label: '布尔', value: true, order: 2 }),
      makeOption({ label: '对象', value: { a: 1 }, order: 3 }),
      makeOption({ label: '字符串', value: 'x', order: 4 }),
    ])
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({ Mixed: definition })), 'Mixed')
    await Promise.resolve()
    await Promise.resolve()

    expect(options.value).toEqual([{ label: '数字', value: 1 }, { label: '字符串', value: 'x' }])
  })

  it('条目全部被过滤掉时退回静态兜底', async () => {
    const definition = makeDefinition('AllBool', [
      makeOption({ label: '真', value: true }),
      makeOption({ label: '假', value: false }),
    ])
    const { options } = await mountOptions(
      vi.fn<BatchFn>(async () => ({ AllBool: definition })),
      'AllBool',
      [{ label: '兜底', value: 'fb' }],
    )
    await Promise.resolve()
    await Promise.resolve()

    expect(options.value).toEqual([{ label: '兜底', value: 'fb' }])
  })

  it('只保留 label 与 value 两个字段，丢弃 disabled 等元数据附加项', async () => {
    const definition = makeDefinition('Flag', [makeOption({ label: '停用中', value: 9, disabled: true })])
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({ Flag: definition })), 'Flag')
    await Promise.resolve()
    await Promise.resolve()

    expect(options.value).toEqual([{ label: '停用中', value: 9 }])
    expect(Object.keys(options.value[0] ?? {})).toEqual(['label', 'value'])
  })

  it('值为 0 与空串的条目不会被误当作空值剔除', async () => {
    const definition = makeDefinition('Zeroish', [
      makeOption({ label: '零', value: 0, order: 1 }),
      makeOption({ label: '空串', value: '', order: 2 }),
    ])
    const { options } = await mountOptions(vi.fn<BatchFn>(async () => ({ Zeroish: definition })), 'Zeroish')
    await Promise.resolve()
    await Promise.resolve()

    expect(options.value).toEqual([{ label: '零', value: 0 }, { label: '空串', value: '' }])
  })
})
