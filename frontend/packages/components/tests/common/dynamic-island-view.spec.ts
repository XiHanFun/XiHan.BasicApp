/**
 * 灵动岛呈现的缺省规则：不写两侧时按状态取指示器与百分比，计时读数，以及何时需要每秒走时钟
 */
import type { IslandTask } from '~/composables/useDynamicIsland'
import { describe, expect, it } from 'vitest'
import { defaultLeading, defaultTrailing, formatElapsed, usesElapsed } from '~/components/common/dynamic-island-view'

function task(patch: Partial<IslandTask> = {}): IslandTask {
  return {
    id: 't',
    label: '任务',
    state: 'loading',
    order: 1,
    kind: 'event',
    source: 'local',
    startedAt: 0,
    ...patch,
  }
}

describe('灵动岛缺省两侧', () => {
  it('进行中是进度环，环心放调用方给的图标；没进度时是不确定态', () => {
    expect(defaultLeading(task({ progress: 40, icon: 'lucide:download' }))).toEqual({ type: 'ring', value: 40, icon: 'lucide:download' })
    expect(defaultLeading(task())).toEqual({ type: 'ring', value: undefined, icon: undefined })
  })

  it('终态是状态图标，调用方给了 icon 就用它', () => {
    expect(defaultLeading(task({ state: 'success' }))).toEqual({ type: 'icon', icon: 'lucide:check' })
    expect(defaultLeading(task({ state: 'error' }))).toEqual({ type: 'icon', icon: 'lucide:triangle-alert' })
    expect(defaultLeading(task({ state: 'info', icon: 'lucide:wifi-off' }))).toEqual({ type: 'icon', icon: 'lucide:wifi-off' })
  })

  it('右侧只在进行中且有进度时给百分比', () => {
    expect(defaultTrailing(task({ progress: 37.5 }))).toEqual({ type: 'text', text: '38%', tone: 'brand' })
    expect(defaultTrailing(task())).toBeUndefined()
    expect(defaultTrailing(task({ state: 'success', progress: 100 }))).toBeUndefined()
  })
})

describe('灵动岛计时', () => {
  it('不满一小时读 m:ss，满一小时读 h:mm:ss，负数按 0', () => {
    expect(formatElapsed(5_000)).toBe('0:05')
    expect(formatElapsed(65_000)).toBe('1:05')
    expect(formatElapsed(3_725_000)).toBe('1:02:05')
    expect(formatElapsed(-1_000)).toBe('0:00')
  })

  it('两侧、数值块或列表行用到计时才需要走时钟', () => {
    expect(usesElapsed(task())).toBe(false)
    expect(usesElapsed(task({
      trailing: { type: 'elapsed' },
    }))).toBe(true)
    expect(usesElapsed(task({
      blocks: [
        {
          type: 'metric',
          value: 1,
          accessory: { type: 'elapsed' },
        },
      ],
    }))).toBe(true)
    expect(usesElapsed(task({
      blocks: [
        {
          type: 'list',
          items: [
            {
              key: 'a',
              label: 'A',
              leading: { type: 'elapsed' },
            },
          ],
        },
      ],
    }))).toBe(true)
  })
})
