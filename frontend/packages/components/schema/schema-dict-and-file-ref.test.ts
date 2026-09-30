/**
 * SchemaPage 的系统字典取值与文件引用单元格。
 *
 * 字段声明 dictCode 后，列表单元格按字典项编码映射名称、搜索区渲下拉、导入按名称反查——
 * 三者都读 optionsFor 注入的 options；字典取不到时回退字段自带的静态 options。
 * image / file 字段的单元格交给文件引用单元格（缩略图 / 打开入口），空值仍显示占位。
 */
import type { VNode } from 'vue'
import type { ListFieldSchema } from './types'
import { createPinia, setActivePinia } from 'pinia'
import { describe, expect, it, vi } from 'vitest'
// 预热依赖图（见 use-enum-options.test.ts 的说明）：重置模块后再导入只剩求值开销
import './useSchemaDictionaries'

type OptionsFn = (dictCode: string) => Promise<Array<{ value: string, label: string, parentValue?: null | string, isDefault: boolean, disabled: boolean }>>

async function bootstrap(options: OptionsFn) {
  vi.resetModules()
  setActivePinia(createPinia())
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({ apis: { dictApi: { options } } as never })
  return import('./useSchemaDictionaries')
}

const levelField: ListFieldSchema = { key: 'level', title: '客户等级', dataType: 'enum', dictCode: 'demo_level' }

describe('useSchemaDictionaries 的 dictCode', () => {
  it('按字典编码取选项并注入字段（值为字典项编码）', async () => {
    const api = vi.fn<OptionsFn>(async () => [
      { value: 'vip', label: '重要客户', isDefault: false, disabled: false },
      { value: 'normal', label: '普通客户', isDefault: true, disabled: false },
    ])
    const { useSchemaDictionaries } = await bootstrap(api)
    const dictionaries = useSchemaDictionaries(() => [levelField, levelField])

    await dictionaries.resolve()

    expect(api).toHaveBeenCalledTimes(1)
    expect(dictionaries.optionsFor(levelField)).toEqual([
      { label: '重要客户', value: 'vip' },
      { label: '普通客户', value: 'normal' },
    ])
  })

  it('字典取不到时回退字段静态 options，其余字段的取值不受影响', async () => {
    const api = vi.fn<OptionsFn>(async (code) => {
      if (code === 'broken') {
        throw new Error('字典 broken 不存在。')
      }
      return [{ value: 'east', label: '华东', isDefault: false, disabled: false }]
    })
    const { useSchemaDictionaries } = await bootstrap(api)
    const broken: ListFieldSchema = { key: 'kind', title: '类别', dataType: 'enum', dictCode: 'broken', options: [{ label: '静态', value: 's' }] }
    const region: ListFieldSchema = { key: 'region', title: '区域', dataType: 'enum', dictCode: 'demo_region' }
    const dictionaries = useSchemaDictionaries(() => [broken, region])

    await expect(dictionaries.resolve()).resolves.toBeUndefined()

    expect(dictionaries.optionsFor(broken)).toEqual([{ label: '静态', value: 's' }])
    expect(dictionaries.optionsFor(region)).toEqual([{ label: '华东', value: 'east' }])
  })
})

describe('useSchemaDictionaries 的 optionsLoader', () => {
  it('同一加载器只调一次，结果注入所有引用它的字段', async () => {
    const { useSchemaDictionaries } = await bootstrap(vi.fn<OptionsFn>())
    const loader = vi.fn(async () => [{ label: '电子', value: '1' }, { label: '图书', value: '2' }])
    const category: ListFieldSchema = { key: 'categoryId', title: '所属分类', dataType: 'enum', optionsLoader: loader }
    const search: ListFieldSchema = { key: 'categoryFilter', title: '分类筛选', dataType: 'enum', optionsLoader: loader, visible: false }
    const dictionaries = useSchemaDictionaries(() => [category, search])

    await dictionaries.resolve()

    expect(loader).toHaveBeenCalledTimes(1)
    expect(dictionaries.optionsFor(category)).toEqual([{ label: '电子', value: '1' }, { label: '图书', value: '2' }])
    expect(dictionaries.optionsFor(search)).toHaveLength(2)
  })

  it('加载失败回退字段静态 options', async () => {
    const { useSchemaDictionaries } = await bootstrap(vi.fn<OptionsFn>())
    const failing: ListFieldSchema = {
      key: 'categoryId',
      title: '所属分类',
      dataType: 'enum',
      options: [{ label: '静态', value: 's' }],
      optionsLoader: async () => {
        throw new Error('没有权限')
      },
    }
    const dictionaries = useSchemaDictionaries(() => [failing])

    await expect(dictionaries.resolve()).resolves.toBeUndefined()

    expect(dictionaries.optionsFor(failing)).toEqual([{ label: '静态', value: 's' }])
  })
})

describe('文件引用单元格', () => {
  it('image / file 字段交给文件引用单元格，空值显示占位', async () => {
    const { renderFieldCell } = await import('./renderer')
    const { default: SchemaFileRefCell } = await import('./SchemaFileRefCell.vue')
    const image: ListFieldSchema = { key: 'avatar', title: '头像', dataType: 'image' }
    const file: ListFieldSchema = { key: 'attachment', title: '附件', dataType: 'file' }

    const imageCell = renderFieldCell(image, { avatar: '123' }) as VNode
    const fileCell = renderFieldCell(file, { attachment: 456 }) as VNode

    expect(imageCell.type).toBe(SchemaFileRefCell)
    expect(imageCell.props).toMatchObject({ value: '123', kind: 'image' })
    expect(fileCell.props).toMatchObject({ value: '456', kind: 'file' })
    expect(renderFieldCell(file, { attachment: '' })).toBe('-')
    expect(renderFieldCell(file, { attachment: null })).toBe('-')
  })
})
