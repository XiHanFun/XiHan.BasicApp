/**
 * 标签右键菜单的禁用态（getTabDisableState）：
 * 关闭类按可关闭标签的位置推出；固定 / 取消固定只对菜单固定的标签禁用，手动固定的可以取消
 */
import type { TabItem } from '~/types'
import { describe, expect, it } from 'vitest'
import { getTabDisableState } from '~/layouts/basic/composables/use-layout-tabbar-helpers'

function tab(path: string, overrides?: Partial<TabItem>): TabItem {
  return {
    key: path,
    title: path,
    path,
    closable: true,
    ...overrides,
  }
}

const tabs: TabItem[] = [
  tab('/dashboard', { affix: true, pinned: true, closable: false }),
  tab('/pinned', { pinned: true, closable: false }),
  tab('/a'),
  tab('/b'),
]

describe('getTabDisableState', () => {
  it('菜单固定的标签不能取消固定，也不能关闭', () => {
    expect(getTabDisableState(tabs, '/dashboard', false)).toMatchObject({
      pinDisabled: true,
      closeCurrentDisabled: true,
      closeLeftDisabled: true,
      closeRightDisabled: false,
    })
  })

  it('手动固定的标签可以取消固定', () => {
    expect(getTabDisableState(tabs, '/pinned', false).pinDisabled).toBe(false)
  })

  it('普通标签：左右两侧按可关闭标签推出，最右边没有右侧可关', () => {
    expect(getTabDisableState(tabs, '/b', true)).toMatchObject({
      pinDisabled: false,
      closeCurrentDisabled: false,
      closeLeftDisabled: false,
      closeRightDisabled: true,
      closeOthersDisabled: false,
      closeAllDisabled: false,
    })
  })
})
