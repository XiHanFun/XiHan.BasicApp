/**
 * 授权穿梭框中间两颗箭头钮的读屏名。
 *
 * 两颗钮只画箭头、没有文字，读屏名是它们唯一的说明；组件库内建的是英文「Move to target list」。
 * 这里按目标栏的标题取当前语言的说法：同一页两个穿梭框（直授的允许 / 拒绝）靠它分开，
 * 窄屏改上下排后也不会念成「移到右侧」这种与布局对不上的方向。
 */
import type { GrantTransferItem } from './grant-transfer'
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import XGrantTransfer from './GrantTransfer.vue'

const items: GrantTransferItem[] = [{ basicId: '1' }, { basicId: '2' }]
const names = new Map([['1', '甲'], ['2', '乙']])

function mountTransfer(sourceTitle: string, targetTitle: string) {
  return mount(XGrantTransfer, {
    props: {
      items,
      value: ['2'],
      groups: [{ key: 'all', name: '', items }],
      getLabel: (item: GrantTransferItem) => names.get(String(item.basicId)) ?? '',
      sourceTitle,
      targetTitle,
      searchPlaceholder: '搜索',
    },
    global: { plugins: [i18n] },
  })
}

function triggerLabel(wrapper: ReturnType<typeof mountTransfer>, part: string) {
  return wrapper.find(`[data-scope="transfer"][data-part="${part}"]`).attributes('aria-label')
}

describe('xGrantTransfer 箭头钮的读屏名', () => {
  it('按目标栏标题取当前语言的说法，不用组件库内建的英文', () => {
    const wrapper = mountTransfer('可允许', '已允许')

    expect(triggerLabel(wrapper, 'to-target-trigger')).toBe(i18n.global.t('component.grant_transfer.to_target', { title: '已允许' }))
    expect(triggerLabel(wrapper, 'to-source-trigger')).toBe(i18n.global.t('component.grant_transfer.to_source', { title: '可允许' }))
    expect(triggerLabel(wrapper, 'to-target-trigger')).not.toContain('Move to')
  })

  it('同一页两个穿梭框的箭头钮各有各的名字', () => {
    const allow = mountTransfer('可允许', '已允许')
    const deny = mountTransfer('可拒绝', '已拒绝')

    expect(triggerLabel(allow, 'to-target-trigger')).not.toBe(triggerLabel(deny, 'to-target-trigger'))
    expect(triggerLabel(allow, 'to-source-trigger')).not.toBe(triggerLabel(deny, 'to-source-trigger'))
  })
})
