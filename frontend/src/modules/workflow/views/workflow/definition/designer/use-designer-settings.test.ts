import type { XhFlowDesigner } from '~/designers/flow'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FLOW_DESIGNER_DEFAULT_SETTINGS, useFlowDesignerSettings } from './use-designer-settings'

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

function designer(): XhFlowDesigner {
  return { settings: undefined } as unknown as XhFlowDesigner
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

describe('流程设计器设置接用户设置', () => {
  it('打开时先按宿主缺省赋上，取回用户存过的那份后合并再赋，用户的值压过缺省', async () => {
    const element = designer()
    sync.stored = { 'flow.layoutDirection': 'TB', 'flow.minimap': false }
    useFlowDesignerSettings('page').attach(element)
    expect(element.settings).toEqual(FLOW_DESIGNER_DEFAULT_SETTINGS)

    await loaded()
    expect(element.settings).toEqual({ ...FLOW_DESIGNER_DEFAULT_SETTINGS, 'flow.layoutDirection': 'TB', 'flow.minimap': false })
  })

  it('只存用户改过的条目，宿主缺省不进用户设置', async () => {
    const settings = useFlowDesignerSettings('page')
    settings.attach(designer())
    await loaded()

    settings.onSettingsChange(change('flow.snap', false))
    expect(sync.saved.at(-1)).toEqual({ section: 'designer', value: { 'flow.snap': false } })
  })

  it('用户清空一项时删掉存的值，设计器回到宿主缺省', async () => {
    const element = designer()
    sync.stored = { 'flow.layerGap': 120 }
    const settings = useFlowDesignerSettings('page')
    settings.attach(element)
    await loaded()

    settings.onSettingsChange(change('flow.layerGap', undefined))
    expect(sync.saved.at(-1)?.value).toEqual({})
    expect(element.settings).toEqual(FLOW_DESIGNER_DEFAULT_SETTINGS)
  })

  it('取回之前用户已经改过或清空的条目，以这次的操作为准', async () => {
    const element = designer()
    sync.stored = { 'flow.layoutDirection': 'TB', 'flow.nodeGap': 12 }
    const settings = useFlowDesignerSettings('page')
    settings.attach(element)

    settings.onSettingsChange(change('flow.layoutDirection', 'RL'))
    settings.onSettingsChange(change('flow.nodeGap', undefined))
    await loaded()
    expect(element.settings).toEqual({ ...FLOW_DESIGNER_DEFAULT_SETTINGS, 'flow.layoutDirection': 'RL' })
  })

  it('取回的不是对象时照用宿主缺省', async () => {
    const element = designer()
    sync.stored = '坏数据'
    useFlowDesignerSettings('page').attach(element)
    await loaded()
    expect(element.settings).toEqual(FLOW_DESIGNER_DEFAULT_SETTINGS)
  })
})
