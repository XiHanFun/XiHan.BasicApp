/**
 * 仪表盘示例数据：虚构的一家零售企业（线上商城、线下门店与分销）的经营数据，只用来展示图表组件的各种画法。
 * 全部在前端按种子生成，不调接口，不代表任何真实业务。
 * 逐日数据按「数据名 + 当天日期」取种子，同一天的数取多长的区间都一样；其余快照按「数据名 + 今天」取种子，当天内刷新不跳。
 * 这里只给键和数值，显示名由小组件按当前语言翻译（workbench.charts.*）。
 */

export const CHANNELS = ['web', 'mini', 'live', 'store', 'distribution'] as const
export const PAYMENTS = ['wechat', 'alipay', 'card', 'installment', 'cod'] as const
export const REGIONS = ['east', 'south', 'north', 'central', 'southwest', 'northwest', 'northeast'] as const
export const TIERS = ['regular', 'silver', 'gold', 'diamond'] as const
export const STAGES = ['impression', 'visit', 'cart', 'order', 'pay', 'repurchase'] as const
export const STORES = ['flagship', 'community', 'outlet'] as const
export const SCORES = ['sales', 'margin', 'traffic', 'repurchase', 'rating', 'turnover'] as const
export const FULFILLMENTS = ['delivered', 'returned', 'cancelled'] as const
export const PROFIT_STEPS = ['revenue', 'cost', 'gross', 'marketing', 'logistics', 'labor', 'net'] as const
export const CATEGORY_ITEMS = {
  digital: ['phone', 'laptop', 'accessory'],
  appliance: ['fridge', 'washer', 'aircon'],
  apparel: ['menswear', 'womenswear', 'kidswear'],
  beauty: ['skincare', 'makeup'],
  food: ['snack', 'drink', 'fresh'],
  home: ['furniture', 'textile'],
} as const

export type Channel = typeof CHANNELS[number]
export type Payment = typeof PAYMENTS[number]
export type Region = typeof REGIONS[number]
export type Tier = typeof TIERS[number]
export type Stage = typeof STAGES[number]
export type Store = typeof STORES[number]
export type Score = typeof SCORES[number]
export type Fulfillment = typeof FULFILLMENTS[number]
export type ProfitStep = typeof PROFIT_STEPS[number]
export type Category = keyof typeof CATEGORY_ITEMS
export type Item = typeof CATEGORY_ITEMS[Category][number]

export const CATEGORIES = Object.keys(CATEGORY_ITEMS) as Category[]

/** 品类下的子类 */
export function itemsOf(category: Category): readonly Item[] {
  return CATEGORY_ITEMS[category]
}

const DAY_MS = 86_400_000

// ---------------------------------------------------------------- 种子与日期

/** FNV-1a：把数据名散列成种子 */
function hash(text: string): number {
  let value = 0x811C9DC5
  for (let index = 0; index < text.length; index++) {
    value ^= text.charCodeAt(index)
    value = Math.imul(value, 0x01000193)
  }
  return value >>> 0
}

/** 本地日历日的序号（距 1970-01-01 的天数）：种子按它取，同一天里不变 */
export function dayNumber(date: Date): number {
  return Math.floor(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) / DAY_MS)
}

/** mulberry32 伪随机数：同一个数据名与日期每次得到同一串数 */
export function seeded(name: string, day: number) {
  let state = (hash(name) ^ Math.imul(day, 0x9E3779B1)) >>> 0
  const next = () => {
    state = (state + 0x6D2B79F5) >>> 0
    let value = state
    value = Math.imul(value ^ (value >>> 15), value | 1)
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61)
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296
  }
  return {
    next,
    /** [min, max) 里均匀取一个数 */
    between: (min: number, max: number) => min + (max - min) * next(),
    /** 在 value 上下 ratio 的幅度里浮动 */
    vary: (value: number, ratio: number) => value * (1 + (next() * 2 - 1) * ratio),
  }
}

export function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days)
}

/** 本地日期写成 YYYY-MM-DD */
export function isoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

function sum(values: readonly number[]): number {
  return values.reduce((total, value) => total + value, 0)
}

// ---------------------------------------------------------------- 逐日销售

/** 各渠道平日的日销售额（元） */
const CHANNEL_BASE: Record<Channel, number> = { web: 182_000, mini: 126_000, live: 94_000, store: 151_000, distribution: 68_000 }
/** 会员日各渠道的放量倍数：直播与线上放得最多，分销几乎不受影响 */
const MEMBER_DAY_BOOST: Record<Channel, number> = { web: 1.8, mini: 1.7, live: 2.4, store: 1.15, distribution: 1.05 }
/** 周末各渠道的倍数：门店客流最旺，分销跟着工作日走 */
const WEEKEND_FACTOR: Record<Channel, number> = { web: 1.18, mini: 1.22, live: 1.15, store: 1.38, distribution: 0.7 }
const BASE_TOTAL = sum(Object.values(CHANNEL_BASE))

/** 会员日：每 28 天连着两天 */
export function isMemberDay(date: Date): boolean {
  return dayNumber(date) % 28 < 2
}

/** 一年一个周期的淡旺季起伏 */
function seasonOf(date: Date): number {
  return 1 + 0.08 * Math.sin((2 * Math.PI * dayNumber(date)) / 365)
}

export interface DailySales {
  date: Date
  channels: Record<Channel, number>
  /** 当日销售额（元），各渠道之和 */
  sales: number
  orders: number
  visitors: number
  /** 退款率 0–1 */
  refundRate: number
  /** 当日销售目标（元）：按周定，周一起换档 */
  target: number
}

function salesOn(date: Date): DailySales {
  const random = seeded('daily-sales', dayNumber(date))
  const weekend = date.getDay() === 0 || date.getDay() === 6
  const memberDay = isMemberDay(date)
  const season = seasonOf(date)
  const channels = Object.fromEntries(CHANNELS.map((channel) => {
    const factor = (weekend ? WEEKEND_FACTOR[channel] : 1) * (memberDay ? MEMBER_DAY_BOOST[channel] : 1)
    return [channel, Math.round(random.vary(CHANNEL_BASE[channel] * season * factor, 0.09))]
  })) as Record<Channel, number>
  const sales = sum(Object.values(channels))
  const orders = Math.round(sales / random.vary(memberDay ? 205 : 232, 0.06))
  const monday = addDays(date, -((date.getDay() + 6) % 7))
  return {
    date,
    channels,
    sales,
    orders,
    visitors: Math.round(orders / random.vary(0.032, 0.1)),
    refundRate: random.vary(memberDay ? 0.041 : 0.026, 0.25),
    target: Math.round((BASE_TOTAL * seasonOf(monday) * 1.06) / 10_000) * 10_000,
  }
}

/** 截至今天的最近 days 天，按日期升序 */
export function dailySales(days: number, today: Date): DailySales[] {
  const end = startOfDay(today)
  return Array.from({ length: days }, (_, index) => salesOn(addDays(end, index - days + 1)))
}

/** 各渠道在这段日子里的销售额合计 */
export function channelTotals(rows: readonly DailySales[]): Record<Channel, number> {
  return Object.fromEntries(CHANNELS.map(channel => [channel, sum(rows.map(row => row.channels[channel]))])) as Record<Channel, number>
}

// ---------------------------------------------------------------- 构成与漏斗

const PAYMENT_WEIGHT: Record<Payment, number> = { wechat: 46, alipay: 33, card: 11, installment: 7, cod: 3 }
/** 各大区的客户规模（千人） */
const REGION_SIZE: Record<Region, number> = { east: 31, south: 22, north: 17, central: 11, southwest: 9, northwest: 5, northeast: 5 }

function weighted<K extends string>(name: string, today: Date, weights: Record<K, number>, total: number): Array<{ key: K, value: number }> {
  const random = seeded(name, dayNumber(today))
  const keys = Object.keys(weights) as K[]
  const raw = keys.map(key => random.vary(weights[key], 0.15))
  const rawTotal = sum(raw)
  return keys.map((key, index) => ({ key, value: Math.round((raw[index]! / rawTotal) * total) }))
}

/** 支付方式的销售额分布，合计为 total */
export function paymentShare(today: Date, total: number) {
  return weighted('payments', today, PAYMENT_WEIGHT, total)
}

/** 收货地区的销售额分布，合计为 total */
export function regionShare(today: Date, total: number) {
  return weighted('regions', today, REGION_SIZE, total)
}

/** 从曝光到复购的逐级人数：后一级都是前一级的一部分 */
export function conversionFunnel(today: Date): Array<{ stage: Stage, value: number }> {
  const random = seeded('funnel', dayNumber(today))
  const rates: Record<Stage, number> = { impression: 1, visit: 0.31, cart: 0.27, order: 0.56, pay: 0.87, repurchase: 0.34 }
  let value = random.vary(1_280_000, 0.05)
  return STAGES.map((stage) => {
    value *= stage === 'impression' ? 1 : random.vary(rates[stage], 0.06)
    return { stage, value: Math.round(value) }
  })
}

const STORE_PROFILE: Record<Store, Record<Score, number>> = {
  flagship: { sales: 93, margin: 78, traffic: 90, repurchase: 71, rating: 86, turnover: 64 },
  community: { sales: 66, margin: 74, traffic: 79, repurchase: 88, rating: 91, turnover: 83 },
  outlet: { sales: 72, margin: 57, traffic: 63, repurchase: 62, rating: 75, turnover: 93 },
}

/** 三类门店的六项评分（0–100） */
export function storeScores(today: Date): Array<{ store: Store } & Record<Score, number>> {
  const random = seeded('store-scores', dayNumber(today))
  return STORES.map(store => ({
    store,
    ...Object.fromEntries(SCORES.map(score => [score, Math.min(100, Math.round(random.vary(STORE_PROFILE[store][score], 0.05)))])) as Record<Score, number>,
  }))
}

const TIER_SHARE: Record<Tier, number> = { regular: 0.58, silver: 0.25, gold: 0.12, diamond: 0.05 }

/** 各大区的会员人数，按等级分开 */
export function customerMix(today: Date): Array<{ region: Region } & Record<Tier, number>> {
  const random = seeded('customers', dayNumber(today))
  return REGIONS.map(region => ({
    region,
    ...Object.fromEntries(TIERS.map(tier => [tier, Math.round(REGION_SIZE[region] * 1000 * random.vary(TIER_SHARE[tier], 0.3))])) as Record<Tier, number>,
  }))
}

// ---------------------------------------------------------------- 商品

/** 各子类的售价区间（元）：A 款最便宜，C 款最贵 */
const ITEM_PRICE: Record<Item, readonly [number, number]> = {
  phone: [1499, 6999],
  laptop: [3999, 9999],
  accessory: [49, 399],
  fridge: [1999, 7999],
  washer: [1499, 4999],
  aircon: [2299, 6999],
  menswear: [129, 699],
  womenswear: [159, 899],
  kidswear: [79, 399],
  skincare: [99, 899],
  makeup: [69, 459],
  snack: [9, 69],
  drink: [5, 49],
  fresh: [19, 129],
  furniture: [599, 4999],
  textile: [99, 899],
}
const CATEGORY_MARGIN: Record<Category, number> = { digital: 0.11, appliance: 0.17, apparel: 0.46, beauty: 0.58, food: 0.24, home: 0.36 }
export const SKU_VARIANTS = ['A', 'B', 'C'] as const

export interface ProductRow {
  /** 唯一身份：子类-款式 */
  sku: string
  item: Item
  variant: typeof SKU_VARIANTS[number]
  category: Category
  price: number
  units: number
  sales: number
  grossProfit: number
}

/** 每个子类 A、B、C 三款：越贵卖得越少，销售额大致随价格缓升 */
export function products(today: Date): ProductRow[] {
  const random = seeded('products', dayNumber(today))
  return CATEGORIES.flatMap(category => itemsOf(category).flatMap(item => SKU_VARIANTS.map((variant, index) => {
    const [min, max] = ITEM_PRICE[item]
    const price = Math.round(min + (max - min) * ((index + random.next()) / SKU_VARIANTS.length))
    const units = Math.max(1, Math.round(random.vary(2_400_000 / price ** 0.9, 0.35)))
    const sales = price * units
    return {
      sku: `${item}-${variant}`,
      item,
      variant,
      category,
      price,
      units,
      sales,
      grossProfit: Math.round(sales * random.vary(CATEGORY_MARGIN[category], 0.15)),
    }
  })))
}

/**
 * 各零售渠道单笔订单金额的对数正态分布：[中位数, 离散度]；门店是双峰（日常小单与家电大单），用来画箱线与小提琴。
 * 分销是批量订单，量级差得太远，不放进来。
 */
const ORDER_AMOUNT_PROFILE: Record<Exclude<Channel, 'distribution'>, ReadonlyArray<readonly [number, number, number]>> = {
  web: [[260, 0.45, 1]],
  mini: [[150, 0.4, 1]],
  live: [[99, 0.22, 1]],
  store: [[70, 0.35, 0.6], [380, 0.25, 0.4]],
}
export const AMOUNT_CHANNELS = Object.keys(ORDER_AMOUNT_PROFILE) as Array<keyof typeof ORDER_AMOUNT_PROFILE>

/** 各渠道的单笔订单金额抽样：每个渠道 60 笔，偶有大单 */
export function orderAmounts(today: Date): Array<{ channel: Channel, amount: number }> {
  const random = seeded('order-amounts', dayNumber(today))
  // Box-Muller：两个均匀数换一个标准正态数
  const normal = () => Math.sqrt(-2 * Math.log(1 - random.next())) * Math.cos(2 * Math.PI * random.next())
  return AMOUNT_CHANNELS.flatMap(channel => Array.from({ length: 60 }, () => {
    const modes = ORDER_AMOUNT_PROFILE[channel]
    let pick = random.next()
    const [median, spread] = modes.find(([, , weight]) => (pick -= weight) < 0) ?? modes.at(-1)!
    const bulk = random.next() < 0.04 ? random.between(2.5, 4) : 1
    return { channel, amount: Math.round(median * Math.exp(spread * normal()) * bulk) }
  }))
}

// ---------------------------------------------------------------- 时段与流向

/** 平日各小时的下单热度（相对值） */
const HOUR_CURVE = [6, 3, 2, 1, 1, 2, 4, 9, 17, 28, 41, 49, 46, 38, 40, 43, 39, 37, 42, 55, 71, 79, 62, 31]

/** 一周各时段的下单量：weekday 0 是周一；周末早上起得晚，白天更旺 */
export function orderWeekHours(today: Date): Array<{ weekday: number, hour: number, orders: number }> {
  const random = seeded('week-hours', dayNumber(today))
  return Array.from({ length: 7 * 24 }, (_, index) => {
    const weekday = Math.floor(index / 24)
    const hour = index % 24
    const weekend = weekday >= 5
    const factor = weekend ? (hour < 10 ? 0.6 : hour < 19 ? 1.35 : 1.2) : 1
    return { weekday, hour, orders: Math.round(random.vary(HOUR_CURVE[hour]! * 12 * factor, 0.18)) }
  })
}

const CHANNEL_CATEGORY_WEIGHT: Record<Channel, Record<Category, number>> = {
  web: { digital: 30, appliance: 22, apparel: 16, beauty: 10, food: 8, home: 14 },
  mini: { digital: 12, appliance: 8, apparel: 22, beauty: 18, food: 28, home: 12 },
  live: { digital: 8, appliance: 5, apparel: 34, beauty: 38, food: 12, home: 3 },
  store: { digital: 14, appliance: 18, apparel: 20, beauty: 8, food: 26, home: 14 },
  distribution: { digital: 26, appliance: 40, apparel: 6, beauty: 4, food: 10, home: 14 },
}
const RETURN_RATE: Record<Category, number> = { digital: 0.035, appliance: 0.02, apparel: 0.095, beauty: 0.045, food: 0.012, home: 0.03 }
const CANCEL_RATE = 0.028

export interface OrderFlow {
  channelCategory: Array<{ channel: Channel, category: Category, orders: number }>
  categoryFulfillment: Array<{ category: Category, fulfillment: Fulfillment, orders: number }>
}

/** 近 30 天的订单：渠道 → 品类 → 履约结果，前后两段的订单数守恒 */
export function orderFlow(today: Date): OrderFlow {
  const random = seeded('order-flow', dayNumber(today))
  const rows = dailySales(30, today)
  const ordersPerYuan = sum(rows.map(row => row.orders)) / sum(rows.map(row => row.sales))
  const totals = channelTotals(rows)

  const channelCategory = CHANNELS.flatMap((channel) => {
    const weights = CHANNEL_CATEGORY_WEIGHT[channel]
    const weightTotal = sum(Object.values(weights))
    return CATEGORIES.map(category => ({
      channel,
      category,
      orders: Math.round(totals[channel] * ordersPerYuan * random.vary(weights[category] / weightTotal, 0.1)),
    }))
  })

  const categoryFulfillment = CATEGORIES.flatMap((category) => {
    const orders = sum(channelCategory.filter(row => row.category === category).map(row => row.orders))
    const returned = Math.round(orders * random.vary(RETURN_RATE[category], 0.2))
    const cancelled = Math.round(orders * random.vary(CANCEL_RATE, 0.2))
    return [
      { category, fulfillment: 'delivered' as const, orders: orders - returned - cancelled },
      { category, fulfillment: 'returned' as const, orders: returned },
      { category, fulfillment: 'cancelled' as const, orders: cancelled },
    ]
  })

  return { channelCategory, categoryFulfillment }
}

// ---------------------------------------------------------------- 供应网络

export type SupplyNodeKind = 'supplier' | 'hub' | 'warehouse' | 'store'

export interface SupplyNode {
  id: string
  kind: SupplyNodeKind
  /** 供应商写字母，区域仓写大区，门店写「大区-序号」 */
  key: string
  /** 吞吐量，决定节点大小 */
  value: number
}

const SUPPLIERS = ['A', 'B', 'C', 'D', 'E'] as const
const WAREHOUSE_REGIONS = ['east', 'south', 'north', 'southwest'] as const satisfies readonly Region[]
/** 供应商绕过中央仓直供区域仓的线路 */
const DIRECT_SUPPLY = [['B', 'east'], ['D', 'south']] as const
/** 区域仓给邻区门店补货的线路 */
const CROSS_SUPPLY = [['south', 'southwest-1'], ['east', 'north-2']] as const

export interface SupplyLink {
  source: string
  target: string
  value: number
  /** 属于配送树（中央仓 → 区域仓 → 本区门店）：树形布局只画这些线 */
  tree: boolean
}

/** 供应商 → 中央仓 → 区域仓 → 门店，外加几条直供与跨区补货 */
export function supplyNetwork(today: Date): { nodes: SupplyNode[], links: SupplyLink[] } {
  const random = seeded('supply', dayNumber(today))
  const nodes: SupplyNode[] = []
  const links: SupplyLink[] = []

  const stores = WAREHOUSE_REGIONS.flatMap(region => [1, 2].map(index => ({
    id: `store:${region}-${index}`,
    kind: 'store' as const,
    key: `${region}-${index}`,
    value: Math.round(random.between(20, 60)),
  })))
  const warehouses = WAREHOUSE_REGIONS.map((region) => {
    const value = sum(stores.filter(store => store.key.startsWith(`${region}-`)).map(store => store.value))
    return { id: `warehouse:${region}`, kind: 'warehouse' as const, key: region, value }
  })
  const hubValue = sum(warehouses.map(warehouse => warehouse.value))
  const suppliers = SUPPLIERS.map(letter => ({
    id: `supplier:${letter}`,
    kind: 'supplier' as const,
    key: letter,
    value: Math.round(hubValue * random.vary(0.2, 0.4)),
  }))

  nodes.push(...suppliers, { id: 'hub', kind: 'hub', key: 'hub', value: hubValue }, ...warehouses, ...stores)
  links.push(...suppliers.map(supplier => ({ source: supplier.id, target: 'hub', value: supplier.value, tree: false })))
  links.push(...warehouses.map(warehouse => ({ source: 'hub', target: warehouse.id, value: warehouse.value, tree: true })))
  links.push(...stores.map(store => ({ source: `warehouse:${store.key.split('-')[0]}`, target: store.id, value: store.value, tree: true })))
  links.push(...DIRECT_SUPPLY.map(([letter, region]) => ({ source: `supplier:${letter}`, target: `warehouse:${region}`, value: Math.round(random.between(8, 20)), tree: false })))
  links.push(...CROSS_SUPPLY.map(([region, store]) => ({ source: `warehouse:${region}`, target: `store:${store}`, value: Math.round(random.between(4, 12)), tree: false })))
  return { nodes, links }
}

// ---------------------------------------------------------------- 行情、利润与目标

export interface Candle {
  date: Date
  open: number
  high: number
  low: number
  close: number
}

/** 原料（铜，元/吨）最近 tradingDays 个交易日的日 K：周末休市 */
export function materialPrices(tradingDays: number, today: Date): Candle[] {
  const random = seeded('copper', dayNumber(today))
  const dates: Date[] = []
  for (let date = startOfDay(today); dates.length < tradingDays; date = addDays(date, -1)) {
    if (date.getDay() !== 0 && date.getDay() !== 6) {
      dates.unshift(date)
    }
  }
  const round = (value: number) => Math.round(value / 10) * 10
  let close = random.vary(71_800, 0.03)
  return dates.map((date) => {
    const open = close * random.vary(1, 0.004)
    close = open * (1 + (random.next() - 0.48) * 0.028)
    return {
      date,
      open: round(open),
      close: round(close),
      high: round(Math.max(open, close) * (1 + random.next() * 0.009)),
      low: round(Math.min(open, close) * (1 - random.next() * 0.009)),
    }
  })
}

/** 本月利润：营业收入减去各项成本费用，毛利与净利润是小计行 */
export function profitWaterfall(today: Date): Array<{ step: ProfitStep, value: number, total: boolean }> {
  const random = seeded('profit', dayNumber(today))
  const revenue = Math.round(random.vary(18_600_000, 0.04))
  const cost = -Math.round(revenue * random.vary(0.68, 0.03))
  const marketing = -Math.round(revenue * random.vary(0.1, 0.1))
  const logistics = -Math.round(revenue * random.vary(0.06, 0.1))
  const labor = -Math.round(revenue * random.vary(0.08, 0.1))
  const gross = revenue + cost
  return [
    { step: 'revenue', value: revenue, total: false },
    { step: 'cost', value: cost, total: false },
    { step: 'gross', value: gross, total: true },
    { step: 'marketing', value: marketing, total: false },
    { step: 'logistics', value: logistics, total: false },
    { step: 'labor', value: labor, total: false },
    { step: 'net', value: gross + marketing + logistics + labor, total: true },
  ]
}

export interface Targets {
  /** 本月销售额 / 本月目标，百分数 */
  salesRate: number
  monthTarget: number
  /** 毛利率，百分数 */
  margin: number
  marginTarget: number
  /** 好评率，百分数 */
  rating: number
  reviews: number
  okrDone: number
  okrTotal: number
  /** 大促备货进度，百分数 */
  stocking: number
  /** 年度预算：已执行与已批复，百分数 */
  budgetUsed: number
  budgetApproved: number
}

export function targets(today: Date): Targets {
  const random = seeded('targets', dayNumber(today))
  const monthTarget = Math.round((BASE_TOTAL * 30 * 1.06) / 100_000) * 100_000
  const budgetUsed = Math.round(random.between(48, 66))
  return {
    salesRate: Math.round(random.between(78, 112) * 10) / 10,
    monthTarget,
    margin: Math.round(random.between(26, 36) * 10) / 10,
    marginTarget: 30,
    rating: Math.round(random.between(88, 97) * 10) / 10,
    reviews: Math.round(random.between(3200, 5600)),
    okrDone: 3,
    okrTotal: 5,
    stocking: Math.round(random.between(55, 85)),
    budgetUsed,
    budgetApproved: budgetUsed + Math.round(random.between(10, 25)),
  }
}
