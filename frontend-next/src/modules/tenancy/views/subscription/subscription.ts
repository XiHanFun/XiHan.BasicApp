import type { ResponseOf } from '~/contracts'
import { ref } from 'vue'
import { subscription as subscriptionApi } from '~/contracts'
import { ApiError } from '~/request'
import { formatDate, formatFileSize } from '~/utils'

export type Subscription = ResponseOf<'subscription.get'>
export type SubscriptionStatus = Subscription['status']

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 存储上限以 MB 表达、已用量以字节统计（1 MB = 1024²，与后端一致） */
export const BYTES_PER_MB = 1024 * 1024
/** 用量到这一比例就提示，免得毫无预兆地被拒（4.x 同；平台租户列表与本页同一水位） */
export const QUOTA_WARNING_RATIO = 0.8
/** 离到期不足这么多天就提示（4.x 同） */
export const EXPIRY_WARNING_DAYS = 30
const MS_PER_DAY = 24 * 60 * 60 * 1000

/** 提示语气：普通、吃紧、超限 */
export type HintTone = 'danger' | 'muted' | 'warning'

/** 一项配额的水位 */
export interface QuotaLevel {
  /** 有上限时的占用比例（超过 1 即超出）；不限为 null */
  ratio: number | null
  tone: HintTone
}

/**
 * 配额水位（4.x 同）：不限时没有比例；上限为 0 视同已满；到上限即超限，到 80% 即吃紧。
 */
export function quotaLevel(used: number, limit: number | null | undefined): QuotaLevel {
  if (limit === null || limit === undefined)
    return { ratio: null, tone: 'muted' }
  const ratio = limit <= 0 ? 1 : used / limit
  if (ratio >= 1)
    return { ratio, tone: 'danger' }
  return { ratio, tone: ratio >= QUOTA_WARNING_RATIO ? 'warning' : 'muted' }
}

/** 存储上限（MB）换成字节；为空即不限 */
export function storageLimitBytes(limitMb: number | null | undefined): number | null {
  return limitMb === null || limitMb === undefined ? null : limitMb * BYTES_PER_MB
}

/** 席位数原样显示 */
export function formatSeats(value: number): string {
  return String(value)
}

/** 「已用 / 上限」：上限为空时显示不限文案 */
export function usageText(used: number, limit: number | null, format: (value: number) => string, unlimited: string): string {
  return `${format(used)} / ${limit === null ? unlimited : format(limit)}`
}

/** 价格：人民币、两位小数（4.x 列表同一符号；后端按两位小数存） */
export function formatPrice(price: number): string {
  return `¥ ${price.toFixed(2)}`
}

/** 订阅页的一项配额 */
export interface QuotaView {
  key: 'seats' | 'storage'
  icon: string
  label: string
  usedText: string
  /** 上限文本；不限为 null */
  limitText: string | null
  /** 有上限时的占用比例（0~1，超过 1 即超限）；不限为 null */
  ratio: number | null
  hint: string
  tone: HintTone
}

function toQuotaView(t: Translate, key: QuotaView['key'], icon: string, label: string, used: number, limit: number | null, format: (value: number) => string): QuotaView {
  const usedText = format(used)
  const { ratio, tone } = quotaLevel(used, limit)
  if (limit === null || ratio === null)
    return { key, icon, label, usedText, limitText: null, ratio: null, hint: t('tenancy.subscription.unlimited'), tone }
  const hint = tone === 'danger'
    ? t('tenancy.subscription.over_limit')
    : t('tenancy.subscription.remaining', { value: format(limit - used) })
  return { key, icon, label, usedText, limitText: format(limit), ratio, hint, tone }
}

/** 席位与存储两项（4.x 同）：已用 / 上限、占用比例、剩余或超限提示 */
export function subscriptionQuotas(t: Translate, value: Subscription): QuotaView[] {
  return [
    toQuotaView(t, 'seats', 'lucide:users', t('tenancy.subscription.seats'), value.usedSeats, value.userLimit ?? null, formatSeats),
    toQuotaView(t, 'storage', 'lucide:hard-drive', t('tenancy.subscription.storage'), value.usedStorageBytes, storageLimitBytes(value.storageLimitMb), formatFileSize),
  ]
}

/** 到期：日期与提示 */
export interface ExpiryView {
  text: string
  hint: null | string
  tone: HintTone
}

/** 到期（4.x 同）：没有到期时间即长期有效；已到期标红；不足 30 天标黄，剩余天数向上取整 */
export function subscriptionExpiry(t: Translate, value: Pick<Subscription, 'expiresAt' | 'isExpired'>, now = Date.now()): ExpiryView {
  if (!value.expiresAt)
    return { text: t('tenancy.subscription.never_expires'), hint: null, tone: 'muted' }
  const text = formatDate(value.expiresAt, 'YYYY-MM-DD')
  if (value.isExpired)
    return { text, hint: t('tenancy.subscription.expired'), tone: 'danger' }
  const days = Math.max(0, Math.ceil((new Date(value.expiresAt).getTime() - now) / MS_PER_DAY))
  return { text, hint: t('tenancy.subscription.days_left', { days }), tone: days <= EXPIRY_WARNING_DAYS ? 'warning' : 'muted' }
}

/** 套餐的价格与计费周期 */
export interface PricingView {
  value: string
  /** 计费周期文本；没有为 null */
  period: null | string
}

/**
 * 价格与计费周期（4.x 的订阅页不显示，6.0 随订阅一起返回）：免费套餐只标免费；
 * 有价格标价格、有周期标周期，都没有为 null（不显示）。
 */
export function subscriptionPricing(t: Translate, value: Pick<Subscription, 'billingPeriodMonths' | 'isFree' | 'price'>): null | PricingView {
  if (value.isFree)
    return { value: t('tenancy.subscription.free'), period: null }
  const price = value.price ?? null
  const months = value.billingPeriodMonths ?? null
  if (price === null && months === null)
    return null
  return {
    value: price === null ? t('tenancy.subscription.unpriced') : formatPrice(price),
    period: months === null ? null : t('tenancy.subscription.billing_period', { count: months }),
  }
}

/** 租户状态的语气 */
export function statusTone(status: SubscriptionStatus): 'danger' | 'success' {
  return status === 'Active' ? 'success' : 'danger'
}

/**
 * 本租户的订阅（`subscription.get`，只在租户侧）：加载失败给出原因（平台一侧调用即 TENANCY_DENIED_002）。
 */
export function useSubscription() {
  const subscription = ref<null | Subscription>(null)
  const loading = ref(false)
  const error = ref<null | string>(null)

  async function load() {
    loading.value = true
    error.value = null
    try {
      subscription.value = await subscriptionApi.get({})
    }
    catch (e) {
      subscription.value = null
      error.value = e instanceof ApiError ? e.detail : String(e)
    }
    finally {
      loading.value = false
    }
  }

  return { subscription, loading, error, load }
}
