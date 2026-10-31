/**
 * 列表页表格的对齐落到组件库的列契约上：表格设置选居中、居右时数据列一起换，
 * 靠左是库的缺省，不写出来；勾选列始终居中，展开列不跟着换，序号列通过独立设置控制。
 */
import type { TableColumnDef } from '@xihan-ui/headless'
import type { SchemaColumn } from './types'
import { mount } from '@vue/test-utils'
import { XhTableRoot } from '@xihan-ui/vue'
import { describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import SchemaTablePanel from './SchemaTablePanel.vue'

interface Row { basicId: string, name: string, path: string }

// 面板按 object 收行：这里在渲染时再认回具体行型
const columns: SchemaColumn<object>[] = [
  { key: 'name', title: '名称', render: row => (row as Row).name },
  { key: 'path', title: '路径', render: row => (row as Row).path },
]
const data: Row[] = [{ basicId: '1', name: '甲', path: '/a' }]

function columnDefs(props: Record<string, unknown> = {}): TableColumnDef[] {
  const wrapper = mount(SchemaTablePanel, {
    props: { columns, data, selectable: true, showIndex: true, ...props },
    global: { plugins: [i18n] },
  })
  const defs = wrapper.findComponent(XhTableRoot).props('columns') ?? []
  wrapper.unmount()
  return defs
}

describe('schemaTablePanel 的对齐', () => {
  it('缺省靠左：数据列不写对齐，交给库的起始边', () => {
    const defs = columnDefs()
    expect(defs.find(def => def.id === 'name')).not.toHaveProperty('align')
    expect(defs.find(def => def.id === 'path')).not.toHaveProperty('align')
  })

  it.each([['center', 'center'], ['right', 'end']] as const)('选 %s 时数据列都按 %s 排，前缀列不跟着换', (align, expected) => {
    const defs = columnDefs({ align })
    expect(defs.find(def => def.id === 'name')).toMatchObject({ align: expected })
    expect(defs.find(def => def.id === 'path')).toMatchObject({ align: expected })
    expect(defs.find(def => def.id === '__select__')?.align).toBe('center')
    expect(defs.find(def => def.id === '__index__')).not.toHaveProperty('align')
  })
})

describe('序号列独立对齐', () => {
  it.each([['left', undefined], ['center', 'center'], ['right', 'end']] as const)('序号 %s 不改变数据列与勾选/展开列', (indexAlign, expected) => {
    const defs = columnDefs({ align: 'right', indexAlign, renderExpand: () => '展开内容' })
    const index = defs.find(def => def.id === '__index__')
    expect(index?.align).toBe(expected)
    expect(defs.find(def => def.id === 'name')?.align).toBe('end')
    expect(defs.find(def => def.id === '__select__')?.align).toBe('center')
    expect(defs.find(def => def.id === '__expand__')).not.toHaveProperty('align')
  })

  it('隐藏序号时不创建该列，树表也使用同一独立对齐契约', () => {
    expect(columnDefs({ showIndex: false, indexAlign: 'right' }).some(def => def.id === '__index__')).toBe(false)
    expect(columnDefs({ tree: true, indexAlign: 'center' }).find(def => def.id === '__index__')).toMatchObject({ align: 'center', width: 90 })
  })
})

describe('逐列对齐优先级', () => {
  it('显式单列对齐优先于表格，未覆盖的列跟随表格，序号独立', () => {
    const defs = columnDefs({ align: 'center', indexAlign: 'right', columnAlign: { name: 'left' } })
    expect(defs.find(def => def.id === 'name')).not.toHaveProperty('align')
    expect(defs.find(def => def.id === 'path')?.align).toBe('center')
    expect(defs.find(def => def.id === '__index__')?.align).toBe('end')
    expect(defs.find(def => def.id === '__select__')?.align).toBe('center')
  })

  it('不同数据列可独立对齐，清除覆盖恢复整表方向', () => {
    const defs = columnDefs({ align: 'left', columnAlign: { name: 'center', path: 'right' } })
    expect(defs.find(def => def.id === 'name')?.align).toBe('center')
    expect(defs.find(def => def.id === 'path')?.align).toBe('end')
    const inherited = columnDefs({ align: 'right', columnAlign: {} })
    expect(inherited.find(def => def.id === 'name')?.align).toBe('end')
  })
})

describe('勾选列固定居中', () => {
  it.each(['left', 'center', 'right'] as const)('表格 %s 和任意序号设置均不能覆盖勾选列', (align) => {
    const defs = columnDefs({ align, indexAlign: 'right', columnAlign: { __select__: 'left', name: 'right' } })
    expect(defs.find(def => def.id === '__select__')).toMatchObject({ align: 'center', width: 48 })
  })

  it('真实表头和行单元格均接收到居中对齐，关闭多选时不创建勾选列', async () => {
    const wrapper = mount(SchemaTablePanel, { props: { columns, data, selectable: true }, global: { plugins: [i18n] } })
    const cells = wrapper.findAll('[data-value="__select__"]').filter(node => ['columnheader', 'gridcell'].includes(node.attributes('role') ?? ''))
    expect(cells).toHaveLength(2)
    expect(cells.every(node => node.attributes('data-align') === 'center')).toBe(true)
    await wrapper.setProps({ selectable: false })
    expect(wrapper.findComponent(XhTableRoot).props('columns')?.some(def => def.id === '__select__')).toBe(false)
    wrapper.unmount()
  })
})
