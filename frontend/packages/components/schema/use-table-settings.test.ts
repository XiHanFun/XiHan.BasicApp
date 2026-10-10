/**
 * 表格设置的对齐：数据列一起换，缺省靠起始边；随「保存」落地、随「恢复默认」回到靠左。
 * 跨端同步换成替身：这里只看本地状态与本地存储。
 */
import type { ListFieldSchema } from './types'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref } from 'vue'
import { storage } from '~/utils'
import { useTableSettings } from './useTableSettings'

vi.mock('./useUserSettingSync', () => ({
  useUserSettingSync: () => ({
    hydrate: async () => undefined,
    save: vi.fn(),
    subscribeRemote: () => () => {},
  }),
}))

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
