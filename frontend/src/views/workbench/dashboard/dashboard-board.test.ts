/**
 * 工作台仪表盘的看板布局加载。
 *
 * 首屏直接取本地那份布局：先摆缺省布局再等后端回来替换，没添加的小组件会闪一下再消失。
 * 本地没存过又开着跨端同步时，等后端那份回来再渲染。空数组是用户存下的布局（全部移除），
 * 不能当成「没存过」回落到缺省；其它设备推来的空布局同样照办。
 */
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, markRaw } from 'vue'
import { i18n } from '~/locales'
import { useAppStore } from '~/stores'
import Dashboard from './index.vue'

const LOCAL_BOARD_KEY = 'xh:workbench:widgets:board'

const remote = vi.hoisted(() => ({
  resolve: null as null | ((value: unknown) => void),
  listener: null as null | ((value: unknown) => void),
}))

vi.mock('./components', () => {
  const stub = (key: string) => markRaw(defineComponent({ name: `Stub-${key}`, render: () => h('div', { 'data-widget': key }) }))
  const WIDGETS = ['welcome', 'clock', 'todo'].map(key => ({ key, titleKey: key, descKey: key, icon: 'lucide:box', defaultSpan: 4, component: stub(key) }))
  return {
    WIDGETS,
    WIDGET_MAP: Object.fromEntries(WIDGETS.map(widget => [widget.key, widget])),
    DEFAULT_BOARD: WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan })),
  }
})

// 后端那份由用例决定何时回来
vi.mock('~/components/schema', () => ({
  useUserSettingSync: () => ({
    hydrate: () => new Promise((resolve) => {
      remote.resolve = resolve
    }),
    save: () => {},
    subscribeRemote: (_section: string, listener: (value: unknown) => void) => {
      remote.listener = listener
      return () => {}
    },
  }),
}))

vi.mock('~/hooks', async importOriginal => ({
  ...await importOriginal<typeof import('~/hooks')>(),
  usePermission: () => ({ hasPermission: () => true }),
}))

function shown(wrapper: ReturnType<typeof mount>): string[] {
  return wrapper.findAll('[data-widget]').map(node => node.attributes('data-widget')!)
}

function mountDashboard() {
  return mount(Dashboard, { global: { plugins: [i18n] }, attachTo: document.body })
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  remote.resolve = null
  remote.listener = null
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('首屏不闪出缺省布局', () => {
  it('本地存过：首屏就是本地那份，等后端时也不出现没添加的小组件', async () => {
    localStorage.setItem(LOCAL_BOARD_KEY, JSON.stringify({ items: [{ key: 'clock', span: 6 }] }))
    const wrapper = mountDashboard()

    expect(shown(wrapper)).toStrictEqual(['clock'])
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual(['clock'])

    remote.resolve?.({ items: [{ key: 'clock', span: 6 }] })
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual(['clock'])
    wrapper.unmount()
  })

  it('本地没存过、开着同步：后端回来之前一个小组件也不摆，回来后按后端那份', async () => {
    const wrapper = mountDashboard()
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual([])

    remote.resolve?.({ items: [{ key: 'todo', span: 4 }] })
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual(['todo'])
    wrapper.unmount()
  })

  it('本地没存过、后端也没有：落到缺省布局', async () => {
    const wrapper = mountDashboard()
    remote.resolve?.(undefined)
    await flushPromises()

    expect(shown(wrapper)).toStrictEqual(['welcome', 'clock', 'todo'])
    wrapper.unmount()
  })

  it('关着同步：直接按本地或缺省渲染，不等后端', async () => {
    useAppStore().widgetsSyncEnabled = false
    const wrapper = mountDashboard()

    expect(shown(wrapper)).toStrictEqual(['welcome', 'clock', 'todo'])
    wrapper.unmount()
  })
})

describe('全部移除也是一份布局', () => {
  it('后端存的是空布局：显示空状态，不回落到全部小组件', async () => {
    const wrapper = mountDashboard()
    remote.resolve?.({ items: [] })
    await flushPromises()

    expect(shown(wrapper)).toStrictEqual([])
    expect(wrapper.text()).toContain(i18n.global.t('workbench.widgets.empty_title'))
    wrapper.unmount()
  })

  it('本地存的是空布局：首屏就是空状态', async () => {
    localStorage.setItem(LOCAL_BOARD_KEY, JSON.stringify({ items: [] }))
    const wrapper = mountDashboard()

    expect(shown(wrapper)).toStrictEqual([])
    expect(wrapper.text()).toContain(i18n.global.t('workbench.widgets.empty_title'))
    wrapper.unmount()
  })

  it('其它设备推来空布局：照样清空', async () => {
    localStorage.setItem(LOCAL_BOARD_KEY, JSON.stringify({ items: [{ key: 'clock', span: 6 }] }))
    const wrapper = mountDashboard()
    remote.resolve?.({ items: [{ key: 'clock', span: 6 }] })
    await flushPromises()

    remote.listener?.({ items: [] })
    await flushPromises()

    expect(shown(wrapper)).toStrictEqual([])
    wrapper.unmount()
  })
})
