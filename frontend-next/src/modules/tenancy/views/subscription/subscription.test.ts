import type { Subscription } from './subscription'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { BYTES_PER_MB, formatPrice, quotaLevel, storageLimitBytes, subscriptionExpiry, subscriptionPricing, subscriptionQuotas, usageText, useSubscription } from './subscription'

/** 假翻译：键原样返回，带参数时拼上参数，断言能看出用了哪个键与什么值 */
function t(key: string, values?: Record<string, unknown>): string {
  return values ? `${key}${JSON.stringify(values)}` : key
}

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function subscription(extra: Partial<Subscription> = {}): Subscription {
  return {
    tenantId: 'contoso',
    tenantName: 'Contoso',
    status: 'Active',
    expiresAt: null,
    isExpired: false,
    editionCode: 'pro',
    editionName: 'Pro',
    editionDescription: null,
    isFree: false,
    price: null,
    billingPeriodMonths: null,
    userLimit: null,
    usedSeats: 0,
    storageLimitMb: null,
    usedStorageBytes: 0,
    ...extra,
  }
}

describe('quotaLevel', () => {
  it('不限没有比例；到 80% 吃紧，到上限即超限；上限为 0 视同已满', () => {
    expect(quotaLevel(5, null)).toEqual({ ratio: null, tone: 'muted' })
    expect(quotaLevel(5, undefined)).toEqual({ ratio: null, tone: 'muted' })
    expect(quotaLevel(79, 100)).toEqual({ ratio: 0.79, tone: 'muted' })
    expect(quotaLevel(80, 100)).toEqual({ ratio: 0.8, tone: 'warning' })
    expect(quotaLevel(100, 100)).toEqual({ ratio: 1, tone: 'danger' })
    expect(quotaLevel(120, 100)).toEqual({ ratio: 1.2, tone: 'danger' })
    expect(quotaLevel(0, 0)).toEqual({ ratio: 1, tone: 'danger' })
  })

  it('存储上限按 1024² 换成字节；用量文本在不限时用给定文案；价格两位小数', () => {
    expect(storageLimitBytes(2)).toBe(2 * 1024 * 1024)
    expect(storageLimitBytes(null)).toBeNull()
    expect(usageText(3, 10, String, '∞')).toBe('3 / 10')
    expect(usageText(3, null, String, '∞')).toBe('3 / ∞')
    expect(formatPrice(99)).toBe('¥ 99.00')
    expect(formatPrice(1234.5)).toBe('¥ 1234.50')
  })
})

describe('subscriptionQuotas', () => {
  it('席位按有效上限算剩余，存储按 MB 上限换算字节后比较；不限只出提示、不出比例', () => {
    const [seats, storage] = subscriptionQuotas(t, subscription({ userLimit: 10, usedSeats: 3, storageLimitMb: null, usedStorageBytes: 5 * BYTES_PER_MB }))
    expect(seats).toMatchObject({ key: 'seats', usedText: '3', limitText: '10', ratio: 0.3, tone: 'muted', hint: 'tenancy.subscription.remaining{"value":"7"}' })
    expect(storage).toMatchObject({ key: 'storage', usedText: '5 MB', limitText: null, ratio: null, tone: 'muted', hint: 'tenancy.subscription.unlimited' })

    const [fullSeats, tightStorage] = subscriptionQuotas(t, subscription({ userLimit: 10, usedSeats: 10, storageLimitMb: 1024, usedStorageBytes: 900 * BYTES_PER_MB }))
    expect(fullSeats).toMatchObject({ ratio: 1, tone: 'danger', hint: 'tenancy.subscription.over_limit' })
    expect(tightStorage).toMatchObject({ usedText: '900 MB', limitText: '1 GB', tone: 'warning', hint: 'tenancy.subscription.remaining{"value":"124 MB"}' })
  })
})

describe('subscriptionExpiry', () => {
  const now = Date.UTC(2026, 9, 29, 12)

  it('没有到期时间即长期有效；已到期标红；不足 30 天标黄，剩余天数向上取整', () => {
    expect(subscriptionExpiry(t, { expiresAt: null, isExpired: false }, now)).toEqual({ text: 'tenancy.subscription.never_expires', hint: null, tone: 'muted' })
    expect(subscriptionExpiry(t, { expiresAt: '2026-10-01T12:00:00Z', isExpired: true }, now)).toMatchObject({ text: '2026-10-01', hint: 'tenancy.subscription.expired', tone: 'danger' })
    expect(subscriptionExpiry(t, { expiresAt: '2026-11-08T00:00:00Z', isExpired: false }, now)).toMatchObject({ hint: 'tenancy.subscription.days_left{"days":10}', tone: 'warning' })
    expect(subscriptionExpiry(t, { expiresAt: '2026-11-28T12:00:00Z', isExpired: false }, now)).toMatchObject({ hint: 'tenancy.subscription.days_left{"days":30}', tone: 'warning' })
    expect(subscriptionExpiry(t, { expiresAt: '2027-06-15T12:00:00Z', isExpired: false }, now)).toMatchObject({ text: '2027-06-15', tone: 'muted' })
  })
})

describe('subscriptionPricing', () => {
  it('免费只标免费；有价格标价格、有周期标周期；都没有不显示', () => {
    expect(subscriptionPricing(t, { isFree: true, price: null, billingPeriodMonths: 12 })).toEqual({ value: 'tenancy.subscription.free', period: null })
    expect(subscriptionPricing(t, { isFree: false, price: 199, billingPeriodMonths: 12 })).toEqual({ value: '¥ 199.00', period: 'tenancy.subscription.billing_period{"count":12}' })
    expect(subscriptionPricing(t, { isFree: false, price: 5.5, billingPeriodMonths: null })).toEqual({ value: '¥ 5.50', period: null })
    expect(subscriptionPricing(t, { isFree: false, price: null, billingPeriodMonths: 1 })).toEqual({ value: 'tenancy.subscription.unpriced', period: 'tenancy.subscription.billing_period{"count":1}' })
    expect(subscriptionPricing(t, { isFree: false, price: null, billingPeriodMonths: null })).toBeNull()
  })
})

describe('useSubscription', () => {
  it('取 GET /api/subscription；平台一侧被拒时给出原因并清掉旧值', async () => {
    const calls: string[] = []
    let respond = () => json(200, subscription({ userLimit: 5 }))
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push(`${init?.method ?? 'GET'} ${String(url)}`)
        return respond()
      }) as unknown as typeof fetch,
    })
    const state = useSubscription()
    await state.load()
    expect(calls).toEqual(['GET /api/subscription'])
    expect(state.subscription.value?.userLimit).toBe(5)
    expect(state.error.value).toBeNull()

    respond = () => json(403, { code: 'TENANCY_DENIED_002', status: 403, detail: 'tenant side only', traceId: 'x' }, 'application/problem+json')
    await state.load()
    expect(state.subscription.value).toBeNull()
    expect(state.error.value).toBe('tenant side only')
    expect(state.loading.value).toBe(false)
  })
})
