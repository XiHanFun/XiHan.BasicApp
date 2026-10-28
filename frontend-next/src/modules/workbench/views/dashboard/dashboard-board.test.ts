/**
 * 工作台看板的布局：本机就是事实源（6.0 还没有用户设置的后端，不跨端同步）。
 * 本地没存过即缺省布局；空数组是用户存下的布局（全部移除），不能当成「没存过」回落到缺省；
 * 编辑时的改动只在内存预览，点「完成」才落地。
 */
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, markRaw } from 'vue'
import { i18n } from '~/locales'
import Dashboard from './index.vue'

const LOCAL_BOARD_KEY = 'xh:workbench:widgets:board'

vi.mock('./components', () => {
  const stub = (key: string) => markRaw(defineComponent({ name: `Stub-${key}`, render: () => h('div', { 'data-widget': key }) }))
  const WIDGETS = ['welcome', 'clock', 'todo'].map(key => ({ key, titleKey: key, descKey: key, icon: 'lucide:box', defaultSpan: 4, component: stub(key) }))
  return {
    WIDGETS,
    WIDGET_MAP: Object.fromEntries(WIDGETS.map(widget => [widget.key, widget])),
    DEFAULT_BOARD: WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan })),
  }
})

vi.mock('~/hooks', async importOriginal => ({
  ...await importOriginal<typeof import('~/hooks')>(),
  usePermission: () => ({ hasPermission: () => true }),
}))

function shown(wrapper: ReturnType<typeof mount>): string[] {
  return wrapper.findAll('[data-widget]').map(node => node.attributes('data-widget')!)
}

function button(wrapper: ReturnType<typeof mount>, key: string) {
  const found = wrapper.findAll('button').find(node => node.text() === i18n.global.t(key))
  if (!found)
    throw new Error(`找不到按钮 ${key}`)
  return found
}

/** 展开右侧悬浮操作栏，进入编辑 */
async function startCustomizing(wrapper: ReturnType<typeof mount>): Promise<void> {
  await wrapper.find(`button[title="${i18n.global.t('workbench.widgets.customize')}"]`).trigger('click')
  await button(wrapper, 'workbench.widgets.customize').trigger('click')
}

function writeLocal(items: unknown[]): void {
  localStorage.setItem(LOCAL_BOARD_KEY, JSON.stringify({ items }))
}

function localKeys(): string[] | null {
  const raw = localStorage.getItem(LOCAL_BOARD_KEY)
  if (!raw)
    return null
  return ((JSON.parse(raw) as { items?: Array<{ key: string }> }).items ?? []).map(item => item.key)
}

function mountDashboard() {
  return mount(Dashboard, { global: { plugins: [i18n] }, attachTo: document.body })
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('工作台看板：本机布局', () => {
  it('本地没存过：缺省布局', () => {
    const wrapper = mountDashboard()
    expect(shown(wrapper)).toStrictEqual(['welcome', 'clock', 'todo'])
    wrapper.unmount()
  })

  it('首屏直接按本地那份渲染，丢掉不认识的小组件', () => {
    writeLocal([{ key: 'clock', span: 6 }, { key: 'sales-kpi', span: 4 }])
    const wrapper = mountDashboard()
    expect(shown(wrapper)).toStrictEqual(['clock'])
    wrapper.unmount()
  })

  it('本地存的是空布局：显示空状态，不回落到缺省', () => {
    writeLocal([])
    const wrapper = mountDashboard()
    expect(shown(wrapper)).toStrictEqual([])
    expect(wrapper.text()).toContain(i18n.global.t('workbench.widgets.empty_title'))
    wrapper.unmount()
  })

  it('编辑时清空只在内存预览，点「完成」才写本地；再「恢复默认」回到缺省', async () => {
    const wrapper = mountDashboard()
    await startCustomizing(wrapper)
    await button(wrapper, 'workbench.widgets.clear').trigger('click')
    expect(shown(wrapper)).toStrictEqual([])
    expect(localKeys()).toBeNull()

    await button(wrapper, 'workbench.widgets.done').trigger('click')
    expect(localKeys()).toStrictEqual([])

    await startCustomizing(wrapper)
    await button(wrapper, 'workbench.widgets.reset').trigger('click')
    await button(wrapper, 'workbench.widgets.done').trigger('click')
    expect(shown(wrapper)).toStrictEqual(['welcome', 'clock', 'todo'])
    expect(localKeys()).toStrictEqual(['welcome', 'clock', 'todo'])
    wrapper.unmount()
  })

  it('没有改动就点「完成」不写本地', async () => {
    const wrapper = mountDashboard()
    await startCustomizing(wrapper)
    await button(wrapper, 'workbench.widgets.done').trigger('click')
    expect(localKeys()).toBeNull()
    wrapper.unmount()
  })
})
