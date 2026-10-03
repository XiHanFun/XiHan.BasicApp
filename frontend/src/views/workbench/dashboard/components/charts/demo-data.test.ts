/**
 * 仪表盘示例数据单元测试。
 * 职责边界：只覆盖 demo-data 的生成规则——同一天生成的数固定、逐日数据与区间长度无关、
 * 各图表数据自身的约束（漏斗逐级收窄、流向守恒、瀑布小计、K 线高低包住开收）。不涉及小组件渲染。
 */
import { describe, expect, it } from 'vitest'
import {
  AMOUNT_CHANNELS,
  CATEGORIES,
  CHANNELS,
  conversionFunnel,
  customerMix,
  dailySales,
  dayNumber,
  isMemberDay,
  isoDate,
  materialPrices,
  orderAmounts,
  orderFlow,
  orderWeekHours,
  products,
  profitWaterfall,
  storeScores,
  supplyNetwork,
  targets,
} from './demo-data'

const TODAY = new Date(2026, 9, 3, 15, 30)

describe('种子与日期', () => {
  it('同一天里不论几点生成，快照数据都一样；换一天就变', () => {
    expect(conversionFunnel(new Date(2026, 9, 3, 0, 5))).toEqual(conversionFunnel(new Date(2026, 9, 3, 23, 55)))
    expect(conversionFunnel(new Date(2026, 9, 4))).not.toEqual(conversionFunnel(TODAY))
  })

  it('dayNumber 按本地日历日计，isoDate 写本地日期', () => {
    expect(dayNumber(new Date(2026, 9, 4)) - dayNumber(new Date(2026, 9, 3, 23, 59))).toBe(1)
    expect(isoDate(new Date(2026, 0, 5))).toBe('2026-01-05')
  })
})

describe('逐日销售', () => {
  it('同一天的数与取多长的区间无关：长区间的尾部就是短区间', () => {
    const month = dailySales(30, TODAY)
    const week = dailySales(7, TODAY)

    expect(month.slice(-7)).toEqual(week)
    expect(isoDate(month.at(-1)!.date)).toBe('2026-10-03')
  })

  it('销售额是各渠道之和，访客多于订单，退款率落在 0–1', () => {
    for (const row of dailySales(90, TODAY)) {
      expect(row.sales).toBe(CHANNELS.reduce((sum, channel) => sum + row.channels[channel], 0))
      expect(row.visitors).toBeGreaterThan(row.orders)
      expect(row.refundRate).toBeGreaterThan(0)
      expect(row.refundRate).toBeLessThan(1)
    }
  })

  it('销售目标按周定：同一周（周一起）里每天相同', () => {
    const rows = dailySales(28, TODAY)
    for (let index = 1; index < rows.length; index++) {
      if (rows[index]!.date.getDay() !== 1) {
        expect(rows[index]!.target).toBe(rows[index - 1]!.target)
      }
    }
  })

  it('会员日每 28 天连着两天，当天销售额明显放量', () => {
    const rows = dailySales(56, TODAY)
    const memberDays = rows.filter(row => isMemberDay(row.date))
    const average = (list: typeof rows) => list.reduce((sum, row) => sum + row.sales, 0) / list.length

    expect(memberDays).toHaveLength(4)
    expect(average(memberDays)).toBeGreaterThan(average(rows.filter(row => !isMemberDay(row.date))) * 1.3)
  })
})

describe('各图表的数据约束', () => {
  it('漏斗逐级收窄', () => {
    const values = conversionFunnel(TODAY).map(stage => stage.value)

    expect(values).toHaveLength(6)
    values.slice(1).forEach((value, index) => expect(value).toBeLessThan(values[index]!))
  })

  it('订单流向守恒：流入品类的订单数等于流出到履约结果的订单数', () => {
    const flow = orderFlow(TODAY)
    for (const category of CATEGORIES) {
      const inflow = flow.channelCategory.filter(row => row.category === category).reduce((sum, row) => sum + row.orders, 0)
      const outflow = flow.categoryFulfillment.filter(row => row.category === category).reduce((sum, row) => sum + row.orders, 0)
      expect(outflow).toBe(inflow)
    }
    expect(flow.categoryFulfillment.every(row => row.orders >= 0)).toBe(true)
  })

  it('利润瀑布的小计等于前面各步的累计', () => {
    const steps = profitWaterfall(TODAY)
    let running = 0
    for (const step of steps) {
      if (step.total) {
        expect(step.value).toBe(running)
      }
      else {
        running += step.value
      }
    }
    expect(steps.at(-1)!.step).toBe('net')
  })

  it('k 线的最高最低包住开盘收盘，且只取工作日', () => {
    const candles = materialPrices(60, TODAY)

    expect(candles).toHaveLength(60)
    for (const candle of candles) {
      expect(candle.high).toBeGreaterThanOrEqual(Math.max(candle.open, candle.close))
      expect(candle.low).toBeLessThanOrEqual(Math.min(candle.open, candle.close))
      expect([0, 6]).not.toContain(candle.date.getDay())
    }
  })

  it('供应网络的连线两端都是已有节点，以中央仓为根', () => {
    const network = supplyNetwork(TODAY)
    const ids = new Set(network.nodes.map(node => node.id))

    expect(ids.has('hub')).toBe(true)
    expect(network.links.every(link => ids.has(link.source) && ids.has(link.target))).toBe(true)
  })

  it('商品、评分、会员、时段与目标的数值都在合理范围内', () => {
    const rows = products(TODAY)
    expect(new Set(rows.map(row => row.sku)).size).toBe(rows.length)
    expect(rows.every(row => row.sales === row.price * row.units && row.grossProfit > 0)).toBe(true)

    expect(orderAmounts(TODAY)).toHaveLength(AMOUNT_CHANNELS.length * 60)
    expect(orderAmounts(TODAY).every(row => row.amount > 0)).toBe(true)
    expect(storeScores(TODAY).every(row => Object.values(row).every(value => typeof value === 'string' || (value >= 0 && value <= 100)))).toBe(true)
    expect(customerMix(TODAY).every(row => row.regular > row.diamond)).toBe(true)
    expect(orderWeekHours(TODAY)).toHaveLength(7 * 24)

    const goal = targets(TODAY)
    expect(goal.budgetApproved).toBeGreaterThan(goal.budgetUsed)
    expect(goal.okrDone).toBeLessThanOrEqual(goal.okrTotal)
  })
})
