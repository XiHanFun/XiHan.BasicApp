/**
 * 宿主放进设计器外框的两样（host-items.ts 的 DesignerMenubarTitle、DesignerStatusText）的单元测试。
 *
 * 锁定：标题是一个元素、写 slot="menubar-center"，名称与编码合成一行（「名称 · 编码」），放不下由外壳整体截断；
 * 状态栏里的每一样各自一个元素、写 slot="status-end"，外壳在相邻两样之间画细分隔；未保存这类提醒带语气。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { DesignerMenubarTitle, DesignerStatusText } from './host-items'

describe('菜单栏正中的标题', () => {
  it('一个元素放进 menubar-center：名称与编码合成「名称 · 编码」，悬停看全文', () => {
    const root = mount(DesignerMenubarTitle, { props: { name: '请假审批', code: 'leave-request' } }).element as HTMLElement

    expect(root.getAttribute('slot')).toBe('menubar-center')
    expect(root.textContent).toBe('请假审批 · leave-request')
    expect(root.getAttribute('title')).toBe('请假审批 · leave-request')
    expect(root.children).toHaveLength(0)
  })

  it('没有编码时只有名称', () => {
    const root = mount(DesignerMenubarTitle, { props: { name: '未命名流程' } }).element as HTMLElement

    expect(root.textContent).toBe('未命名流程')
  })
})

describe('状态栏右端的一样', () => {
  it('一个元素放进 status-end，文字与悬停说明照给', () => {
    const root = mount(DesignerStatusText, { props: { text: 'system.print-demo', title: '数据源' } }).element as HTMLElement

    expect(root.getAttribute('slot')).toBe('status-end')
    expect(root.textContent).toBe('system.print-demo')
    expect(root.getAttribute('title')).toBe('数据源')
    expect(root.hasAttribute('data-tone')).toBe(false)
  })

  it('提醒类（未保存）带警示语气', () => {
    const root = mount(DesignerStatusText, { props: { text: '未保存', tone: 'warning' } }).element as HTMLElement

    expect(root.getAttribute('data-tone')).toBe('warning')
  })
})
