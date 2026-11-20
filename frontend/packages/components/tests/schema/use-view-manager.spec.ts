/**
 * 个人视图的状态：增删、覆盖快照、默认唯一且可以取消，按 pageCode 存本地
 * 跨端同步换成替身：这里只看本地状态与本地存储
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { useViewManager } from '~/components/schema/useViewManager'
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

function setup(pageCode = 'views-test') {
  const scope = effectScope()
  const manager = scope.run(() => useViewManager(pageCode))!
  return {
    manager,
    stop: () => scope.stop(),
  }
}

afterEach(() => {
  localStorage.clear()
})

describe('useViewManager', () => {
  it('新增的视图存到本地，按 pageCode 分开', () => {
    const { manager, stop } = setup()
    const view = manager.addView('本周', {
      filters: { status: 'Failed' },
      pageSize: 50,
    })

    expect(view.code).toBe('view-1')
    expect(storage.get('xh:views:views-test')).toEqual([
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

  it('默认唯一，再设为 null 即取消；重新打开页面时默认的就是当前视图', () => {
    const first = setup()
    const a = first.manager.addView('A', { filters: {} })
    const b = first.manager.addView('B', { filters: {} })
    first.manager.setDefault(a.code)
    first.manager.setDefault(b.code)
    expect(first.manager.views.value.map(v => !!v.isDefault)).toEqual([false, true])
    first.stop()

    const reopened = setup()
    expect(reopened.manager.activeCode.value).toBe(b.code)
    reopened.manager.setDefault(null)
    expect(reopened.manager.views.value.some(v => v.isDefault)).toBe(false)
    reopened.stop()
  })

  it('覆盖快照、套用与删除；删掉当前视图时当前清空', () => {
    const { manager, stop } = setup()
    const view = manager.addView('A', {
      filters: { status: 'Failed' },
    })
    manager.updateView(view.code, {
      filters: { status: 'Success' },
    })

    expect(manager.applyView(view.code)).toEqual({
      filters: { status: 'Success' },
    })
    expect(manager.activeCode.value).toBe(view.code)
    manager.removeView(view.code)
    expect(manager.views.value).toEqual([])
    expect(manager.activeCode.value).toBeUndefined()
    expect(manager.applyView(view.code)).toBeUndefined()
    stop()
  })
})
