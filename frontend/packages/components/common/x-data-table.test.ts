/**
 * XDataTable 的外观与分页契约：弹窗、抽屉里的次级表格与列表页表格同一副样式。
 *
 * 缺省是中档行高 + 列间分隔线（与 SchemaTablePanel 缺省的非单行模式一致）；
 * 给了 pagination 就在表格下方出与列表页同一副底栏（条数与页码 + 分页），
 * 此前 pagination 不是本组件的 prop，会被当成属性透传到包裹层，调用方以为有分页、界面上却没有。
 */
import type { TableColumnDef } from '@xihan-ui/headless'
import { mount } from '@vue/test-utils'
import { XhTableRoot } from '@xihan-ui/vue'
import { describe, expect, it, vi } from 'vitest'
import { i18n } from '~/locales'
import XDataTable from './XDataTable.vue'

const columns = [{ key: 'name', title: '名称' }]
const data = [{ basicId: '1', name: '甲' }, { basicId: '2', name: '乙' }]

function mountTable(props: Record<string, unknown> = {}) {
  return mount(XDataTable, { props: { columns, data, ...props }, global: { plugins: [i18n] } })
}

describe('xDataTable 与列表页同一副样式', () => {
  it('缺省中档行高并画列间分隔线', () => {
    const root = mountTable().find('[data-scope="table"][data-part="root"]')

    expect(root.attributes('data-size')).toBe('md')
    expect(root.attributes()).toHaveProperty('data-split')
  })

  it('没给分页就不出底栏', () => {
    const wrapper = mountTable()

    expect(wrapper.find('.x-data-table__footer').exists()).toBe(false)
  })

  it('给了分页就出条数与页码，翻页回调调用方，且不再透传成属性', async () => {
    const onUpdatePage = vi.fn()
    const wrapper = mountTable({ pagination: { page: 1, pageSize: 10, itemCount: 25, onUpdatePage } })

    expect(wrapper.attributes('pagination')).toBeUndefined()
    const count = wrapper.find('.x-data-table__count').text().replace(/\s+/g, '')
    expect(count).toContain('25')
    expect(count).toContain('3')

    const next = wrapper.findAll('.x-data-table__footer button').find(button => button.text() === '2')
    await next!.trigger('click')

    expect(onUpdatePage).toHaveBeenCalledWith(2)
  })

  it('列的对齐与下限交给组件库的列契约，对齐按逻辑方向转写', () => {
    const wrapper = mountTable({
      columns: [
        { key: 'name', title: '名称', minWidth: 64 },
        { key: 'flag', title: '标记', align: 'center' },
        { key: 'amount', title: '金额', align: 'right' },
      ],
    })

    const defs: TableColumnDef[] = wrapper.findComponent(XhTableRoot).props('columns') ?? []
    expect(defs.find(def => def.id === 'name')).toMatchObject({ minWidth: 64 })
    expect(defs.find(def => def.id === 'name')).not.toHaveProperty('align')
    expect(defs.find(def => def.id === 'flag')).toMatchObject({ align: 'center' })
    expect(defs.find(def => def.id === 'flag')).not.toHaveProperty('minWidth')
    expect(defs.find(def => def.id === 'amount')).toMatchObject({ align: 'end' })
  })

  it('居中列的列头与数据格都带上对齐标记，格内控件随之居中', () => {
    const wrapper = mountTable({ columns: [{ key: 'name', title: '名称', align: 'center' }] })

    const header = wrapper.find('[data-scope="table"][data-part="column-header"][data-value="name"]')
    const cells = wrapper.findAll('[data-scope="table"][data-part="cell"][data-value="name"]')
    expect(header.attributes('data-align')).toBe('center')
    expect(cells).toHaveLength(2)
    expect(cells.every(cell => cell.attributes('data-align') === 'center')).toBe(true)
  })

  it('调用方不接条数变更时不出条数选择器', () => {
    const withoutSizeChange = mountTable({ pagination: { page: 1, pageSize: 10, itemCount: 25 } })
    const withSizeChange = mountTable({ pagination: { page: 1, pageSize: 10, itemCount: 25, onUpdatePageSize: vi.fn() } })

    expect(withoutSizeChange.find('.x-data-table__footer [data-scope="select"]').exists()).toBe(false)
    expect(withSizeChange.find('.x-data-table__footer [data-scope="select"]').exists()).toBe(true)
  })
})
