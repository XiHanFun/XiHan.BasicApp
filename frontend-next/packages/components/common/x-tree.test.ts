/**
 * XTree 的交互契约：点行只管选中，展开只归箭头。
 *
 * XiHan.UI 2.1.0 起 Tree 的 expandOnClick 缺省为 false，选中改为行尾对号、勾选框部件删除。
 * 封装此前把 expandOnClick 定死成 true，点目录名会顺手展开，与选中混在同一下点击里；
 * 这里钉住：点文字不动展开集合，点箭头不动选中集合，每一行（含分支）都有行尾指示位。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import XTree from './XTree.vue'

const data = [
  {
    value: 'east',
    label: '华东',
    children: [
      { value: 'sh', label: '上海' },
      { value: 'hz', label: '杭州' },
    ],
  },
  { value: 'bj', label: '北京' },
]

function branchControl(wrapper: ReturnType<typeof mount>, label: string) {
  const control = wrapper
    .findAll('[data-part="branch-control"]')
    .find(node => node.find('[data-part="branch-text"]').text() === label)
  if (!control) {
    throw new Error(`找不到分支「${label}」`)
  }
  return control
}

describe('xTree 选中与展开各管各的', () => {
  it('分支行与叶子行都在行尾放指示位，不再渲染勾选框部件', () => {
    const wrapper = mount(XTree, { props: { data, multiple: true, expandedKeys: ['east'] } })
    const rows = wrapper.findAll('[data-part="branch-control"], [data-part="item"]')
    expect(rows).toHaveLength(4)
    for (const row of rows) {
      const parts = row.findAll(':scope > [data-part]').map(node => node.attributes('data-part'))
      expect(parts.at(-1)).toBe('item-indicator')
    }
    expect(wrapper.find('[data-part$="checkbox"]').exists()).toBe(false)
  })

  it('点分支文字只选中，不展开', async () => {
    const wrapper = mount(XTree, { props: { data } })
    await branchControl(wrapper, '华东').find('[data-part="branch-text"]').trigger('click')

    expect(wrapper.emitted('update:selectedKeys')?.at(-1)).toEqual([['east']])
    expect(wrapper.emitted('update:expandedKeys')).toBeUndefined()
  })

  it('点箭头只展开，不选中', async () => {
    const wrapper = mount(XTree, { props: { data } })
    await branchControl(wrapper, '华东').find('[data-part="branch-trigger"]').trigger('click')

    expect(wrapper.emitted('update:expandedKeys')?.at(-1)).toEqual([['east']])
    expect(wrapper.emitted('update:selectedKeys')).toBeUndefined()
  })

  it('多选级联：勾目录连同整枝，部分勾中时目录报半选', async () => {
    const wrapper = mount(XTree, {
      props: { data, multiple: true, cascade: true, selectedKeys: ['sh'], expandedKeys: ['east'] },
    })
    const east = branchControl(wrapper, '华东')
    expect(east.element.closest('[role="treeitem"]')?.getAttribute('aria-checked')).toBe('mixed')

    await east.find('[data-part="branch-text"]').trigger('click')
    const selected = wrapper.emitted<[string[]]>('update:selectedKeys')?.at(-1)?.[0] ?? []
    expect([...selected].sort()).toEqual(['hz', 'sh'])
    expect(wrapper.emitted('update:expandedKeys')).toBeUndefined()
  })
})
