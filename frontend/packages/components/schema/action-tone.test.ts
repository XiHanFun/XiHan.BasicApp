/**
 * 危险操作在下拉菜单里的条目语气。
 *
 * 行操作与页面「更多」都收在下拉菜单里，删除、重置密码、强制下线这类命令要靠红字从一列里跳出来；
 * 只有 error / warning 着色，其余类型与普通条目同档，否则一片彩字又分不出哪条危险。
 * 通用下拉（XDropdown / 右键菜单）经 toDropdownCollection 把条目上写的 tone 原样交给组件库。
 */
import type { MenuNodeMeta } from '@xihan-ui/headless'
import { describe, expect, it } from 'vitest'
import { toDropdownCollection } from '../common/dropdown-collection'
import { actionMenuPrefix } from './action-menu'
import { actionButtonTone, actionMenuTone } from './action-tone'

const meta = (value: string) => ({ value }) as MenuNodeMeta

describe('actionMenuTone', () => {
  it('只给破坏性与停用类着色', () => {
    expect(actionMenuTone('error')).toBe('danger')
    expect(actionMenuTone('warning')).toBe('warning')
  })

  it('其余类型不着色，与普通条目同档', () => {
    for (const type of [undefined, 'default', 'primary', 'info', 'success'] as const)
      expect(actionMenuTone(type)).toBeUndefined()
  })
})

describe('actionButtonTone', () => {
  it('按钮仍按整条 tone 轴换算', () => {
    expect(actionButtonTone('primary')).toBe('brand')
    expect(actionButtonTone('error')).toBe('danger')
    expect(actionButtonTone('success')).toBe('success')
    expect(actionButtonTone(undefined)).toBe('neutral')
  })
})

describe('actionMenuPrefix', () => {
  it('一条图标都没有时不给插槽，不白占行首一列', () => {
    expect(actionMenuPrefix([{ key: 'view' }, { key: 'edit' }])).toBeUndefined()
  })

  it('有图标的条目铺一枚图标，没图标的留空格保持对齐', () => {
    const prefix = actionMenuPrefix([{ key: 'view' }, { key: 'delete', icon: 'lucide:trash-2' }])!
    expect(prefix(meta('delete'))).toHaveLength(1)
    expect(prefix(meta('view'))).toEqual([])
  })
})

describe('toDropdownCollection', () => {
  it('条目上写的 tone 透传给组件库，没写的不带这一键', () => {
    const collection = toDropdownCollection([
      { key: 'duplicate', label: '复制' },
      { key: 'divider', type: 'divider' },
      { key: 'delete', label: '删除', tone: 'danger' },
    ])
    expect(collection).toStrictEqual([
      { value: 'duplicate', label: '复制' },
      { value: 'delete', label: '删除', tone: 'danger', separatorBefore: true },
    ])
  })
})
