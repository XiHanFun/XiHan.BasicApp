/**
 * 授权穿梭框两栏的在途与空态。
 *
 * 穿梭框没有自动铺开的结构，empty / loading 两个部件得自己摆；漏了它们，
 * 授权清单加载中与一侧没有条目时两栏都是一片空白，分不清是还在取还是真的没有。
 */
import type { GrantTransferItem } from './grant-transfer'
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import XGrantTransfer from './GrantTransfer.vue'

function mountTransfer(items: GrantTransferItem[], value: string[], loading = false) {
  return mount(XGrantTransfer, {
    props: {
      items,
      value,
      loading,
      groups: [{ key: 'all', name: '', items }],
      getLabel: (item: GrantTransferItem) => `条目${item.basicId}`,
      sourceTitle: '可授予',
      targetTitle: '已授予',
      searchPlaceholder: '搜索',
    },
    global: { plugins: [i18n] },
  })
}

function placeholder(wrapper: ReturnType<typeof mountTransfer>, part: 'empty' | 'loading', side: 'source' | 'target') {
  return wrapper.find(`[data-scope="transfer"][data-part="${part}"][data-side="${side}"]`)
}

describe('xGrantTransfer 在途与空态', () => {
  it('首次加载、还没有条目时两栏都露出在途占位，空态让位', async () => {
    const wrapper = mountTransfer([], [], true)
    await flushPromises()

    for (const side of ['source', 'target'] as const) {
      expect(placeholder(wrapper, 'loading', side).attributes('hidden')).toBeUndefined()
      expect(placeholder(wrapper, 'loading', side).text()).toBe(i18n.global.t('common.loading'))
      expect(placeholder(wrapper, 'empty', side).attributes('hidden')).toBeDefined()
    }
  })

  it('一侧没有条目时那一侧露出空态，另一侧照常列条目', async () => {
    const wrapper = mountTransfer([{ basicId: '1' }, { basicId: '2' }], [])
    await flushPromises()

    expect(placeholder(wrapper, 'empty', 'target').attributes('hidden')).toBeUndefined()
    expect(placeholder(wrapper, 'empty', 'target').text()).toBe(i18n.global.t('common.no_data'))
    expect(placeholder(wrapper, 'empty', 'source').attributes('hidden')).toBeDefined()
  })

  it('搜不出时空态说的是「没有匹配的结果」', async () => {
    const wrapper = mountTransfer([{ basicId: '1' }], [])
    await wrapper.find('[data-scope="transfer"][data-part="search"][data-side="source"]').setValue('不存在')
    await flushPromises()

    expect(placeholder(wrapper, 'empty', 'source').attributes('hidden')).toBeUndefined()
    expect(placeholder(wrapper, 'empty', 'source').text()).toBe(i18n.global.t('common.no_result'))
  })
})
