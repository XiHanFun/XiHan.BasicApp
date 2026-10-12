/**
 * 工作台仪表盘的看板布局加载。
 *
 * 开着跨端同步时以后端那份为准：本地那份可能是别的设备改动之前的旧布局（A 上清空，B 刷新会先摆出旧的小组件
 * 再消失），等后端回来再渲染；后端迟迟不回才先按本地摆。后端那份与其它设备推来的都写回本地。
 * 关着同步时本地就是事实源。空数组是用户存下的布局（全部移除），不能当成「没存过」回落到缺省。
 * 编辑时可一键清空，点「完成」才落地。
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

// 分区绑定用真的 bindSyncedSetting，只把后端换成替身：拉取由用例决定何时回来，推送由用例手动发
vi.mock('~/components/schema', async () => {
  const { useAppStore: appStore } = await import('~/stores')
  const { bindSyncedSetting } = await import('~/composables/useSyncedSetting')
  return {
    useUserSettingSync: () => ({
      save: () => {},
      bindSection: (_section: string, target: Parameters<typeof bindSyncedSetting>[1]) => bindSyncedSetting({
        enabled: () => appStore().widgetsSyncEnabled,
        load: () => new Promise((resolve) => {
          remote.resolve = resolve
        }),
        subscribe: (listener) => {
          remote.listener = listener
          return () => {}
        },
      }, target),
    }),
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

function localKeys(): string[] {
  const items = (JSON.parse(localStorage.getItem(LOCAL_BOARD_KEY) ?? '{}') as { items?: Array<{ key: string }> }).items ?? []
  return items.map(item => item.key)
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
  vi.useRealTimers()
  document.body.innerHTML = ''
})

describe('开着同步：以后端那份为准，不闪出别的布局', () => {
  it('另一台设备清空后这台刷新：本地还是旧布局，也不先摆出旧的小组件', async () => {
    writeLocal([{ key: 'clock', span: 6 }, { key: 'todo', span: 4 }])
    const wrapper = mountDashboard()
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual([])

    remote.resolve?.({ items: [] })
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual([])
    expect(wrapper.text()).toContain(i18n.global.t('workbench.widgets.empty_title'))
    wrapper.unmount()
  })

  it('后端那份写回本地：下次刷新（含关掉同步之后）首屏就是最新布局', async () => {
    writeLocal([{ key: 'clock', span: 6 }])
    const wrapper = mountDashboard()
    remote.resolve?.({ items: [{ key: 'todo', span: 4 }] })
    await flushPromises()

    expect(shown(wrapper)).toStrictEqual(['todo'])
    expect(localKeys()).toStrictEqual(['todo'])
    wrapper.unmount()
  })

  it('本地没存过、后端也没有：落到缺省布局', async () => {
    const wrapper = mountDashboard()
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual([])

    remote.resolve?.(undefined)
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual(['welcome', 'clock', 'todo'])
    wrapper.unmount()
  })

  it('后端迟迟不回：1.5s 后先按本地摆出来，回来后再换成后端那份', async () => {
    vi.useFakeTimers()
    writeLocal([{ key: 'clock', span: 6 }])
    const wrapper = mountDashboard()
    await flushPromises()

    vi.advanceTimersByTime(1499)
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual([])
    vi.advanceTimersByTime(1)
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual(['clock'])

    remote.resolve?.({ items: [{ key: 'todo', span: 4 }] })
    await flushPromises()
    expect(shown(wrapper)).toStrictEqual(['todo'])
    wrapper.unmount()
  })

  it('其它设备推来的布局当场采用并写回本地，空布局同样照办', async () => {
    writeLocal([{ key: 'clock', span: 6 }])
    const wrapper = mountDashboard()
    remote.resolve?.({ items: [{ key: 'clock', span: 6 }] })
    await flushPromises()

    remote.listener?.({ items: [] })
    await flushPromises()

    expect(shown(wrapper)).toStrictEqual([])
    expect(localKeys()).toStrictEqual([])
    wrapper.unmount()
  })

  it('编辑中途后端那份才回来，不覆盖用户的改动', async () => {
    writeLocal([{ key: 'clock', span: 6 }, { key: 'todo', span: 4 }])
    vi.useFakeTimers()
    const wrapper = mountDashboard()
    vi.advanceTimersByTime(1500)
    await flushPromises()
    await startCustomizing(wrapper)
    await button(wrapper, 'workbench.widgets.clear').trigger('click')

    remote.resolve?.({ items: [{ key: 'welcome', span: 4 }] })
    await flushPromises()

    expect(shown(wrapper)).toStrictEqual([])
    wrapper.unmount()
  })
})

describe('关着同步：本地就是事实源', () => {
  it('首屏直接按本地那份渲染，不等后端', async () => {
    useAppStore().widgetsSyncEnabled = false
    writeLocal([{ key: 'clock', span: 6 }])
    const wrapper = mountDashboard()

    expect(shown(wrapper)).toStrictEqual(['clock'])
    wrapper.unmount()
  })

  it('本地没存过：缺省布局', async () => {
    useAppStore().widgetsSyncEnabled = false
    const wrapper = mountDashboard()

    expect(shown(wrapper)).toStrictEqual(['welcome', 'clock', 'todo'])
    wrapper.unmount()
  })

  it('本地存的是空布局：首屏就是空状态，不回落到全部小组件', async () => {
    useAppStore().widgetsSyncEnabled = false
    writeLocal([])
    const wrapper = mountDashboard()

    expect(shown(wrapper)).toStrictEqual([])
    expect(wrapper.text()).toContain(i18n.global.t('workbench.widgets.empty_title'))
    wrapper.unmount()
  })
})

describe('清空小组件', () => {
  it('编辑时一键清空：当场变成空状态，点「完成」才落地成空布局', async () => {
    useAppStore().widgetsSyncEnabled = false
    writeLocal([{ key: 'clock', span: 6 }, { key: 'todo', span: 4 }])
    const wrapper = mountDashboard()
    await startCustomizing(wrapper)

    await button(wrapper, 'workbench.widgets.clear').trigger('click')
    expect(shown(wrapper)).toStrictEqual([])
    expect(wrapper.text()).toContain(i18n.global.t('workbench.widgets.empty_title'))
    expect(localKeys()).toStrictEqual(['clock', 'todo'])

    await button(wrapper, 'workbench.widgets.done').trigger('click')
    expect(localKeys()).toStrictEqual([])
    wrapper.unmount()
  })

  it('看板已经空了，清空钮不可点', async () => {
    useAppStore().widgetsSyncEnabled = false
    writeLocal([])
    const wrapper = mountDashboard()
    await startCustomizing(wrapper)

    const clear = button(wrapper, 'workbench.widgets.clear').element as HTMLButtonElement
    expect(clear.disabled || clear.getAttribute('aria-disabled') === 'true').toBe(true)
    wrapper.unmount()
  })
})
