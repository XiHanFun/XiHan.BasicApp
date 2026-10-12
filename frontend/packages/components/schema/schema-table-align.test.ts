/**
 * 列表页表格的对齐落到组件库的列契约上：表格设置选居中、居右时数据列一起换，
 * 靠左是库的缺省，不写出来；勾选、序号这些前缀列不跟着换。
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
  return wrapper.findComponent(XhTableRoot).props('columns') ?? []
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
    const prefix = defs.filter(def => def.id !== 'name' && def.id !== 'path')
    expect(prefix.length).toBeGreaterThan(0)
    expect(prefix.every(def => !('align' in def))).toBe(true)
  })
})
