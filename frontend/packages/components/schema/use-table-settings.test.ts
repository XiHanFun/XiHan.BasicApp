/**
 * 表格设置的对齐：数据列一起换，缺省靠起始边；随「保存」落地、随「恢复默认」回到靠左。
 * 密度与竖线：直接存组件库的尺寸档与 ruled，旧版存的 small / medium / large 与 singleLine 读到时换算。
 * 跨端同步换成替身：这里只看本地状态与本地存储。
 */
import type { ListFieldSchema } from './types'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref } from 'vue'
import { storage } from '~/utils'
import { useTableSettings } from './useTableSettings'

const remote = vi.hoisted(() => ({ apply: (_value: unknown) => {} }))

vi.mock('./useUserSettingSync', async () => {
  const { ref: vueRef } = await import('vue')
  return {
    useUserSettingSync: () => ({
      hydrate: async () => undefined,
      save: vi.fn(),
      subscribeRemote: () => () => {},
      // 关着同步的样子：立即就绪，不拉后端
      bindSection: (_section: string, target: { apply: (value: unknown) => void }) => {
        remote.apply = target.apply
        return { ready: vueRef(true), settled: Promise.resolve(), dispose: () => {} }
      },
    }),
  }
})

const fields = ref([{ key: 'name', title: '名称' }, { key: 'path', title: '路径' }] as ListFieldSchema[])

function setup(pageCode = 'align-test') {
  const scope = effectScope()
  const settings = scope.run(() => useTableSettings(pageCode, fields))!
  return { settings, stop: () => scope.stop() }
}

afterEach(() => {
  localStorage.clear()
})

describe('useTableSettings 对齐', () => {
  it('缺省靠起始边', () => {
    const { settings, stop } = setup()
    expect(settings.align.value).toBe('left')
    stop()
  })

  it('改了对齐随「保存」落地，下次打开读回来', () => {
    const first = setup()
    first.settings.setAlign('center')
    first.settings.save()
    expect(storage.get<{ align?: string }>('xh:table-settings:align-test')?.align).toBe('center')
    first.stop()

    const second = setup()
    expect(second.settings.align.value).toBe('center')
    second.stop()
  })

  it('「恢复默认」回到靠左', () => {
    const { settings, stop } = setup()
    settings.setAlign('right')
    settings.resetDefault()
    expect(settings.align.value).toBe('left')
    stop()
  })

  it('旧版本存下的设置没有对齐这一项时按靠左读', () => {
    storage.set('xh:table-settings:align-test', { columns: [{ key: 'name', visible: true }], density: 'medium' })
    const { settings, stop } = setup()
    expect(settings.align.value).toBe('left')
    stop()
  })
})

describe('useTableSettings 密度与竖线', () => {
  it('缺省中间档、显示竖线', () => {
    const { settings, stop } = setup('density-test')
    expect(settings.density.value).toBe('md')
    expect(settings.style.value.ruled).toBe(true)
    stop()
  })

  it('存下的是组件库尺寸档，保存后读回同一档', () => {
    const first = setup('density-test')
    first.settings.setDensity('sm')
    first.settings.setStyle('ruled', false)
    first.settings.save()
    expect(storage.get<{ density?: string }>('xh:table-settings:density-test')?.density).toBe('sm')
    first.stop()

    const second = setup('density-test')
    expect(second.settings.density.value).toBe('sm')
    expect(second.settings.style.value.ruled).toBe(false)
    second.stop()
  })

  it('旧版存的 small / large 换成 sm / lg', () => {
    storage.set('xh:table-settings:density-small', { columns: [{ key: 'name', visible: true }], density: 'small' })
    storage.set('xh:table-settings:density-large', { columns: [{ key: 'name', visible: true }], density: 'large' })
    const small = setup('density-small')
    const large = setup('density-large')
    expect(small.settings.density.value).toBe('sm')
    expect(large.settings.density.value).toBe('lg')
    small.stop()
    large.stop()
  })

  it('旧版存的 singleLine 反过来读成竖线开关', () => {
    storage.set('xh:table-settings:ruled-off', { columns: [{ key: 'name', visible: true }], style: { striped: true, bordered: true, singleLine: true } })
    storage.set('xh:table-settings:ruled-on', { columns: [{ key: 'name', visible: true }], style: { striped: false, bordered: true, singleLine: false } })
    const off = setup('ruled-off')
    const on = setup('ruled-on')
    expect(off.settings.style.value).toStrictEqual({ striped: true, bordered: true, ruled: false })
    expect(on.settings.style.value).toStrictEqual({ striped: false, bordered: true, ruled: true })
    off.stop()
    on.stop()
  })
})

describe('useTableSettings 序号列对齐', () => {
  it('序号与数据列分别保存和恢复；未保存的调整不落地', () => {
    const first = setup('index-align')
    first.settings.setAlign('center')
    first.settings.setIndexAlign('right')
    expect(storage.get('xh:table-settings:index-align')).toBeNull()
    first.settings.save()
    first.stop()
    const second = setup('index-align')
    expect(second.settings.align.value).toBe('center')
    expect(second.settings.indexAlign.value).toBe('right')
    second.stop()
  })

  it('隐藏再显示保留对齐；恢复默认只在保存时持久化', () => {
    const { settings, stop } = setup('index-reset')
    settings.setIndexAlign('right')
    settings.setShowIndex(false)
    settings.save()
    settings.setShowIndex(true)
    expect(settings.indexAlign.value).toBe('right')
    settings.resetDefault()
    expect(settings.indexAlign.value).toBe('left')
    expect(storage.get<{ indexAlign: string }>('xh:table-settings:index-reset')?.indexAlign).toBe('right')
    settings.save()
    expect(storage.get<{ indexAlign: string }>('xh:table-settings:index-reset')?.indexAlign).toBe('left')
    stop()
  })

  it.each([undefined, 'justify', null, 42])('旧配置/非法值 %s 恢复为居左，不继承数据列方向', (indexAlign) => {
    storage.set('xh:table-settings:index-legacy', { columns: [], align: 'right', indexAlign })
    const { settings, stop } = setup('index-legacy')
    expect(settings.align.value).toBe('right')
    expect(settings.indexAlign.value).toBe('left')
    stop()
  })

  it('远端同步应用独立对齐，旧远端载荷仍按居左读取', () => {
    const { settings, stop } = setup('index-remote')
    remote.apply({ columns: [], align: 'right', indexAlign: 'center' })
    expect(settings.indexAlign.value).toBe('center')
    expect(settings.align.value).toBe('right')
    remote.apply({ columns: [], align: 'center' })
    expect(settings.indexAlign.value).toBe('left')
    stop()
  })
})
