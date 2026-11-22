/**
 * 条件预设的状态：增删、覆盖快照、默认唯一且可以取消，按 pageCode 存本地
 * 跨端同步换成替身：这里只看本地状态与本地存储
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { usePresetManager } from '~/components/schema/usePresetManager'
import { storage } from '~/utils'

vi.mock('~/components/schema/useUserSettingSync', async () => {
  const { ref: vueRef } = await import('vue')
  return {
    useUserSettingSync: () => ({
      hydrate: async () => undefined,
      save: vi.fn(),
      subscribeRemote: () => () => {},
      bindSection: () => ({
        ready: vueRef(true),
        settled: Promise.resolve(),
        dispose: () => {},
      }),
    }),
  }
})

function setup(pageCode = 'presets-test') {
  const scope = effectScope()
  const manager = scope.run(() => usePresetManager(pageCode))!
  return {
    manager,
    stop: () => scope.stop(),
  }
}

afterEach(() => {
  localStorage.clear()
})

describe('usePresetManager', () => {
  it('新增的预设存到本地，按 pageCode 分开', () => {
    const { manager, stop } = setup()
    const preset = manager.addPreset('本周', {
      filters: { status: 'Failed' },
      pageSize: 50,
    })

    expect(preset.code).toBe('view-1')
    expect(storage.get('xh:views:presets-test')).toEqual([
      {
        code: 'view-1',
        name: '本周',
        snapshot: {
          filters: { status: 'Failed' },
          pageSize: 50,
        },
      },
    ])
    expect(storage.get('xh:views:other')).toBeNull()
    stop()
  })

  it('默认唯一，再设为 null 即取消；重新打开页面时默认的就是当前预设', () => {
    const first = setup()
    const a = first.manager.addPreset('A', { filters: {} })
    const b = first.manager.addPreset('B', { filters: {} })
    first.manager.setDefault(a.code)
    first.manager.setDefault(b.code)
    expect(first.manager.presets.value.map(v => !!v.isDefault)).toEqual([false, true])
    first.stop()

    const reopened = setup()
    expect(reopened.manager.activeCode.value).toBe(b.code)
    reopened.manager.setDefault(null)
    expect(reopened.manager.presets.value.some(v => v.isDefault)).toBe(false)
    reopened.stop()
  })

  it('覆盖快照、套用与删除；删掉当前预设时当前清空', () => {
    const { manager, stop } = setup()
    const preset = manager.addPreset('A', {
      filters: { status: 'Failed' },
    })
    manager.updatePreset(preset.code, {
      filters: { status: 'Success' },
    })

    expect(manager.applyPreset(preset.code)).toEqual({
      filters: { status: 'Success' },
    })
    expect(manager.activeCode.value).toBe(preset.code)
    manager.removePreset(preset.code)
    expect(manager.presets.value).toEqual([])
    expect(manager.activeCode.value).toBeUndefined()
    expect(manager.applyPreset(preset.code)).toBeUndefined()
    stop()
  })
})
