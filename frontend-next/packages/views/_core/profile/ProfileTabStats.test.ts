/**
 * 个人中心「统计」（ProfileTabStats.vue，4.x 原样搬入）：
 * 本月三张卡（登录、访问、操作；6.0 不记在线时长，没有 4.x 的「本月在线」与今日 / 本周的「在线」一栏）、今日 / 本周三行、
 * 最近活动时间（没有即「—」）；热力图铺近一年、逐日操作量交给组件库，底部摘要累计操作与有操作的天数；加载被拒即提示后端文案。
 */
import type { AppContextApis, UserActivity } from '~/types'
import { flushPromises, mount } from '@vue/test-utils'
import { XhHeatmapRoot } from '@xihan-ui/vue'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext } from '~/stores'
import { formatDate } from '~/utils'
import ProfileTabStats from './ProfileTabStats.vue'

const apis = {
  getActivityApi: vi.fn(),
}

const activity: UserActivity = {
  today: { loginCount: 1, accessCount: 20, operationCount: 3 },
  thisWeek: { loginCount: 4, accessCount: 120, operationCount: 9 },
  thisMonth: { loginCount: 12, accessCount: 480, operationCount: 30 },
  lastLoginTime: '2026-10-30T01:00:00Z',
  lastOperationTime: '2026-10-30T02:30:00Z',
  trend: [
    { date: '2026-10-01', accessCount: 8, operationCount: 3 },
    { date: '2026-10-02', accessCount: 5, operationCount: 0 },
    { date: '2026-10-29', accessCount: 40, operationCount: 5 },
    { date: '2026-10-30', accessCount: 20, operationCount: 1 },
  ],
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  apis.getActivityApi.mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.getActivityApi.mockResolvedValue(activity)
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(2026, 9, 30, 12, 0, 0))
})

afterEach(() => {
  vi.useRealTimers()
})

async function render() {
  const wrapper = mount(ProfileTabStats, { global: { plugins: [i18n] } })
  await flushPromises()
  return wrapper
}

describe('统计页签', () => {
  it('本月三张卡、今日 / 本周三行（没有在线时长），最近活动没有的显示「—」', async () => {
    const wrapper = await render()
    expect(apis.getActivityApi).toHaveBeenCalledTimes(1)
    expect(wrapper.findAll('.pf-stat-card').map(card => [card.find('.pf-stat-card__label').text(), card.find('.pf-stat-card__value').text()])).toEqual([
      ['本月登录', '12'],
      ['本月访问', '480'],
      ['本月操作', '30'],
    ])
    expect(wrapper.findAll('.pf-period').map(period => ({
      title: period.find('.pf-period__title').text(),
      rows: period.findAll('.pf-period__row').map(row => [row.find('span').text(), row.find('b').text()]),
    }))).toEqual([
      { title: '今日', rows: [['登录', '1'], ['访问', '20'], ['操作', '3']] },
      { title: '本周', rows: [['登录', '4'], ['访问', '120'], ['操作', '9']] },
      { title: '最近活动', rows: [['最后登录', formatDate(activity.lastLoginTime!)], ['最后访问', '—'], ['最后操作', formatDate(activity.lastOperationTime!)]] },
    ])
    expect(wrapper.text()).not.toContain('在线')
    expect(wrapper.text()).not.toContain('component.profile')
  })

  it('热力图铺近一年，逐日操作量（只有访问的日子是 0）交给组件库；底部摘要累计操作与有操作的天数', async () => {
    const wrapper = await render()
    const heatmap = wrapper.findComponent(XhHeatmapRoot)
    expect(heatmap.props()).toMatchObject({
      startDate: '2025-10-31',
      endDate: '2026-10-30',
      value: [
        { date: '2026-10-01', count: 3 },
        { date: '2026-10-02', count: 0 },
        { date: '2026-10-29', count: 5 },
        { date: '2026-10-30', count: 1 },
      ],
      thresholds: [1, 2, 4, 6],
    })
    expect(wrapper.find('.pf-heat__foot').text()).toBe('近一年共 9 次操作 · 3 天活跃')
  })

  it('加载被拒即提示后端文案', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    apis.getActivityApi.mockRejectedValue(new ApiError(400, { code: 'AUDITING_VALIDATION_001', detail: '时区无效。' }))
    await render()
    expect(danger).toHaveBeenCalledWith('时区无效。')
  })
})
