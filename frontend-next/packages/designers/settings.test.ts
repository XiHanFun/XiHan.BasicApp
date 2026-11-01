import type { DesignerSettingsTarget } from './settings'
import { DESIGNER_SETTING_KEYS } from '@xihan-ui/designer-shell'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useDesignerSettings } from './settings'

const sync = vi.hoisted(() => ({
  stored: undefined as unknown,
  resolve: undefined as undefined | (() => void),
  saved: [] as { section: string, value: unknown }[],
}))

vi.mock('~/components/schema', () => ({
  useUserSettingSync: () => ({
    hydrate: () => new Promise((resolve) => {
      sync.resolve = () => resolve(sync.stored)
    }),
    save: (section: string, value: unknown) => sync.saved.push({ section, value }),
  }),
}))

const DEFAULTS = { 'flow.layoutDirection': 'LR', 'flow.layerGap': 88 }
const LAYOUT = { panels: { palette: { position: 'left', order: 0 } } }

function designer(): DesignerSettingsTarget {
  return { settings: undefined }
}

function change(key: string, value: unknown): Event {
  return new CustomEvent('settings-change', { detail: { key, value, settings: {} } })
}

async function loaded(): Promise<void> {
  sync.resolve?.()
  await Promise.resolve()
  await Promise.resolve()
}

beforeEach(() => {
  sync.stored = undefined
  sync.resolve = undefined
  sync.saved = []
})

describe('设计器设置接用户设置', () => {
  it('取回用户存过的那份之前 ready 为 false，取回后合并再赋，用户的值压过缺省', async () => {
    const element = designer()
    sync.stored = { 'flow.layoutDirection': 'TB', [DESIGNER_SETTING_KEYS.layout]: LAYOUT }
    const settings = useDesignerSettings('page', DEFAULTS)
    settings.attach(element)
    expect(settings.ready.value).toBe(false)
    expect(element.settings).toEqual(DEFAULTS)

    await loaded()
    expect(settings.ready.value).toBe(true)
    expect(element.settings).toEqual({ ...DEFAULTS, 'flow.layoutDirection': 'TB', [DESIGNER_SETTING_KEYS.layout]: LAYOUT })
  })

  it('等 ready 再挂上的设计器一挂上就拿到用户存过的 help.tourDone', async () => {
    sync.stored = { 'help.tourDone': true }
    const settings = useDesignerSettings('page')
    await loaded()

    const element = designer()
    settings.attach(element)
    expect(element.settings).toEqual({ 'help.tourDone': true })
  })

  it('只存用户改过的条目，宿主缺省不进用户设置；面板布局与工具栏照样存', async () => {
    const settings = useDesignerSettings('page', DEFAULTS)
    settings.attach(designer())
    await loaded()

    settings.onSettingsChange(change(DESIGNER_SETTING_KEYS.layout, LAYOUT))
    settings.onSettingsChange(change(DESIGNER_SETTING_KEYS.toolbarDisplay, 'both'))
    expect(sync.saved.at(-1)).toEqual({ section: 'designer', value: { [DESIGNER_SETTING_KEYS.layout]: LAYOUT, [DESIGNER_SETTING_KEYS.toolbarDisplay]: 'both' } })
  })

  it('用户清空一项时删掉存的值，设计器回到宿主缺省', async () => {
    const element = designer()
    sync.stored = { 'flow.layerGap': 120 }
    const settings = useDesignerSettings('page', DEFAULTS)
    settings.attach(element)
    await loaded()

    settings.onSettingsChange(change('flow.layerGap', undefined))
    expect(sync.saved.at(-1)?.value).toEqual({})
    expect(element.settings).toEqual(DEFAULTS)
  })

  it('取回之前用户已经改过或清空的条目，以这次的操作为准', async () => {
    const element = designer()
    sync.stored = { 'flow.layoutDirection': 'TB', 'flow.nodeGap': 12 }
    const settings = useDesignerSettings('page', DEFAULTS)
    settings.attach(element)

    settings.onSettingsChange(change('flow.layoutDirection', 'RL'))
    settings.onSettingsChange(change('flow.nodeGap', undefined))
    await loaded()
    expect(element.settings).toEqual({ ...DEFAULTS, 'flow.layoutDirection': 'RL' })
  })

  it('取回的不是对象时照用宿主缺省，ready 照样变 true', async () => {
    const element = designer()
    sync.stored = '坏数据'
    const settings = useDesignerSettings('page', DEFAULTS)
    settings.attach(element)
    await loaded()
    expect(settings.ready.value).toBe(true)
    expect(element.settings).toEqual(DEFAULTS)
  })
})
