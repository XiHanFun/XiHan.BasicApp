/**
 * 模块登记的看板小组件：排在内置小组件之后、进缺省看板，可见性按权限码与所属页面判定
 * 登记表换成替身：清单在加载时读一次登记表（应用里模块 setup 先于看板加载）
 */
import { describe, expect, it, vi } from 'vitest'
import { canShowWidget, DEFAULT_BOARD, WIDGET_MAP, WIDGETS } from './index'

vi.mock('~/stores', async importOriginal => ({
  ...await importOriginal<typeof import('~/stores')>(),
  useWorkbenchWidgets: () => [{ key: 'pkg-todo', dataKey: 'pkg.todo', titleKey: 't', descKey: 'd', emptyKey: 'e', icon: 'lucide:box', defaultSpan: 4, page: 'pkg.page', moreLink: '/pkg' }],
}))

describe('模块登记的小组件', () => {
  it('接在内置小组件之后，也进缺省看板', () => {
    expect(WIDGETS.at(-1)?.key).toBe('pkg-todo')
    expect(WIDGET_MAP['pkg-todo']?.page).toBe('pkg.page')
    expect(DEFAULT_BOARD.at(-1)).toEqual({ key: 'pkg-todo', span: 4 })
  })

  it('所属页面不在调用方可见的页面里时不显示（可选模块关掉时后端不下发页面）', () => {
    const allowAll = () => true

    expect(canShowWidget(WIDGET_MAP['pkg-todo'], allowAll, new Set(['pkg.page']))).toBe(true)
    expect(canShowWidget(WIDGET_MAP['pkg-todo'], allowAll, new Set())).toBe(false)
    expect(canShowWidget(WIDGET_MAP.welcome, allowAll, new Set())).toBe(true)
    expect(canShowWidget(WIDGET_MAP['request-kpi'], () => false, new Set())).toBe(false)
    expect(canShowWidget(undefined, allowAll, new Set())).toBe(false)
  })
})
