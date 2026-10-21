/**
 * 宿主放进设计器外框的几样（host-items.ts 的 DesignerMenubarTitle、DesignerStatusText、DesignerCloseButton）的单元测试。
 *
 * 锁定：标题是一个元素、写 slot="menubar-center"，名称与编码合成一行（「名称 · 编码」），放不下由外壳整体截断；
 * 状态栏里的每一样各自一个元素、写 slot="status-end"，外壳在相邻两样之间画细分隔；未保存这类提醒带语气；
 * 菜单栏右上角的关闭钮写 slot="menubar-end"，只有图标、可及名照给，点了交给宿主关掉编辑器，忙着时不可点。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { DesignerCloseButton, DesignerMenubarTitle, DesignerStatusText } from './host-items'

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

describe('菜单栏右上角的关闭钮', () => {
  it('一颗只有图标的钮放进 menubar-end，可及名与悬停说明是给的文字，点了交给宿主', async () => {
    const wrapper = mount(DesignerCloseButton, { props: { label: '关闭' } })
    const root = wrapper.element as HTMLElement

    expect(root.tagName).toBe('BUTTON')
    expect(root.getAttribute('slot')).toBe('menubar-end')
    expect(root.getAttribute('aria-label')).toBe('关闭')
    expect(root.getAttribute('title')).toBe('关闭')
    expect(root.querySelector('svg')).not.toBeNull()
    await wrapper.trigger('click')
    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('宿主忙着（保存、打印中）时不可点', async () => {
    const wrapper = mount(DesignerCloseButton, { props: { label: '关闭', disabled: true } })

    expect((wrapper.element as HTMLButtonElement).disabled).toBe(true)
    await wrapper.trigger('click')
    expect(wrapper.emitted('close')).toBeUndefined()
  })
})
