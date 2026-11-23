/**
 * 授权穿梭框的段标题
 *
 * 有名的段出段标题：段名是给人读的显示名（权限穿梭框里是资源的显示名），资源码作为次要信息跟在后面；
 * 段名同时是这一段（role=group）的读屏名，资源码不混进去。无名段（不分段）不出段标题
 */
import type { GrantTransferGroup, GrantTransferItem } from '~/components/common/grant-transfer'
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import XGrantTransfer from '~/components/common/GrantTransfer.vue'
import { i18n } from '~/locales'

const requests: GrantTransferItem[] = [
  { basicId: 'a1' },
  { basicId: 'a2' },
]
const roles: GrantTransferItem[] = [
  { basicId: 'b1' },
]
const items = [
  ...requests,
  ...roles,
]

function mountTransfer(groups: GrantTransferGroup<GrantTransferItem>[], value: string[] = []) {
  return mount(XGrantTransfer, {
    props: {
      items,
      value,
      groups,
      getLabel: (item: GrantTransferItem) => `条目${item.basicId}`,
      sourceTitle: '可授予',
      targetTitle: '已授予',
      searchPlaceholder: '搜索',
    },
    global: {
      plugins: [i18n],
    },
  })
}

function heads(wrapper: ReturnType<typeof mountTransfer>, side: 'source' | 'target') {
  return wrapper.findAll(`[data-part="${side}-panel"] .x-grant-transfer__group-head`).map(head => [
    head.find('[data-part="group-label"]').text(),
    head.find('.x-grant-transfer__group-code').exists()
      ? head.find('.x-grant-transfer__group-code').text()
      : null,
  ])
}

describe('xGrantTransfer 段标题', () => {
  it('段名在前、资源码跟在后面；段的读屏名只取段名', async () => {
    const wrapper = mountTransfer([
      {
        key: 'access.permission-requests',
        name: '授权申请',
        description: 'access.permission-requests',
        items: requests,
      },
      { key: 'outside', name: '目录外', items: roles },
    ])
    await flushPromises()

    expect(heads(wrapper, 'source')).toEqual([
      ['授权申请', 'access.permission-requests'],
      ['目录外', null],
    ])
    const group = wrapper.find('[data-part="source-panel"] [role="group"]')
    const label = wrapper.find(`[id="${group.attributes('aria-labelledby')}"]`)
    expect(label.text()).toBe('授权申请')
  })

  it('两栏各出本侧有条目的段', async () => {
    const wrapper = mountTransfer([
      {
        key: 'access.permission-requests',
        name: '授权申请',
        description: 'access.permission-requests',
        items: requests,
      },
      {
        key: 'access.roles',
        name: '角色',
        description: 'access.roles',
        items: roles,
      },
    ], ['b1'])
    await flushPromises()

    expect(heads(wrapper, 'source').map(([name]) => name)).toEqual(['授权申请'])
    expect(heads(wrapper, 'target').map(([name]) => name)).toEqual(['角色'])
  })

  it('无名段不出段标题', async () => {
    const wrapper = mountTransfer([
      { key: 'all', name: '', items },
    ])
    await flushPromises()

    expect(heads(wrapper, 'source')).toEqual([])
    expect(wrapper.findAll('[data-part="source-panel"] [role="option"]')).toHaveLength(3)
  })
})
