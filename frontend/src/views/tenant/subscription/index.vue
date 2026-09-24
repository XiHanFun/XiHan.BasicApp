<script setup lang="ts">
import type { TenantSubscriptionDto } from '@/api'
import { XhButton, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhProgress, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { tenantApi, TenantStatus } from '@/api'
import { TENANT_STATUS_OPTIONS } from '@/constants'
import { Icon } from '~/components'
import { useEnumOptions } from '~/hooks'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'TenantSubscriptionPage' })

const { t } = useI18n()

/** 套餐存储上限以 MB 表达，已用量以字节统计 */
const BYTES_PER_MB = 1024 * 1024
/** 用量到达这一比例就提示，避免在毫无预兆的情况下被拒（与平台租户列表同一水位） */
const QUOTA_WARNING_RATIO = 0.8
const MS_PER_DAY = 24 * 60 * 60 * 1000

type HintTone = 'muted' | 'warning' | 'danger'

interface QuotaView {
  key: string
  icon: string
  label: string
  usedText: string
  /** 上限文本；不限时为空 */
  limitText: string | null
  /** 有上限时的占用比例（0~1，超过 1 即已超限） */
  ratio: number | null
  hint: string
  hintTone: HintTone
}

const tenantStatusOptions = useEnumOptions('TenantStatus', TENANT_STATUS_OPTIONS)

const subscription = ref<TenantSubscriptionDto | null>(null)
const loading = ref(false)
const failed = ref(false)

async function load() {
  loading.value = true
  failed.value = false
  try {
    subscription.value = await tenantApi.mySubscription()
  }
  catch {
    failed.value = true
  }
  finally {
    loading.value = false
  }
}

onMounted(load)

/**
 * 一项配额的展示：已用 / 上限、占用比例、剩余或超限提示
 * @param used 已用量（已换成数字）
 * @param limit 上限（已换成与已用量同一单位的数字），空表示不限
 * @param format 数量格式化（席位原样，存储按字节换算）
 */
function toQuota(key: string, icon: string, label: string, used: number, limit: number | null, format: (value: number) => string): QuotaView {
  if (limit == null) {
    return { key, icon, label, usedText: format(used), limitText: null, ratio: null, hint: t('tenant.subscription.unlimited'), hintTone: 'muted' }
  }
  const ratio = limit <= 0 ? 1 : used / limit
  if (ratio >= 1) {
    return { key, icon, label, usedText: format(used), limitText: format(limit), ratio, hint: t('tenant.subscription.over_limit'), hintTone: 'danger' }
  }
  return {
    key,
    icon,
    label,
    usedText: format(used),
    limitText: format(limit),
    ratio,
    hint: t('tenant.subscription.remaining', { value: format(limit - used) }),
    hintTone: ratio >= QUOTA_WARNING_RATIO ? 'warning' : 'muted',
  }
}

// 后端 long（已用席位、存储上限与已用量）按字符串传输，这里换成数字再算
const quotas = computed<QuotaView[]>(() => {
  const value = subscription.value
  if (!value) {
    return []
  }
  const storageLimitBytes = value.effectiveStorageLimit == null ? null : Number(value.effectiveStorageLimit) * BYTES_PER_MB
  return [
    toQuota('seats', 'lucide:users', t('tenant.subscription.seats'), Number(value.usedUserCount), value.effectiveUserLimit ?? null, count => String(count)),
    toQuota('storage', 'lucide:hard-drive', t('tenant.subscription.storage'), Number(value.usedStorageBytes), storageLimitBytes, formatFileSize),
  ]
})

/** 到期：日期与提示（长期有效 / 已到期 / 剩余天数） */
const expiration = computed(() => {
  const value = subscription.value
  if (!value?.expirationTime) {
    return { text: t('tenant.subscription.never_expires'), hint: null, hintTone: 'muted' as HintTone }
  }
  if (value.isExpired) {
    return { text: formatDate(value.expirationTime, 'YYYY-MM-DD'), hint: t('tenant.subscription.expired'), hintTone: 'danger' as HintTone }
  }
  const days = Math.ceil((new Date(value.expirationTime).getTime() - Date.now()) / MS_PER_DAY)
  return {
    text: formatDate(value.expirationTime, 'YYYY-MM-DD'),
    hint: t('tenant.subscription.days_left', { days }),
    hintTone: (days <= 30 ? 'warning' : 'muted') as HintTone,
  }
})

function tenantStatusTone(status: TenantStatus) {
  if (status === TenantStatus.Normal) {
    return 'success'
  }
  return status === TenantStatus.Disabled ? 'danger' : 'warning'
}
</script>

<template>
  <div class="xh-subscription">
    <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
      <div class="xh-loading-stage__veil">
        <XhSpinner />
      </div>

      <XhEmptyStateRoot v-if="failed">
        <XhEmptyStateIndicator>
          <Icon icon="lucide:alert-circle" />
        </XhEmptyStateIndicator>
        <XhEmptyStateTitle>{{ t('common.messages.load_failed') }}</XhEmptyStateTitle>
        <XhEmptyStateDescription>{{ t('tenant.subscription.load_failed') }}</XhEmptyStateDescription>
        <XhEmptyStateAction>
          <XhButton variant="subtle" size="sm" @click="load">
            {{ t('tenant.subscription.retry') }}
          </XhButton>
        </XhEmptyStateAction>
      </XhEmptyStateRoot>

      <div v-else-if="subscription" class="xh-subscription__body">
        <section class="xh-subscription__plan">
          <span class="xh-subscription__plan-icon" aria-hidden="true">
            <Icon icon="lucide:package" width="20" height="20" />
          </span>
          <div class="xh-subscription__plan-main">
            <span class="xh-subscription__caption">{{ t('tenant.subscription.current_edition') }}</span>
            <span class="xh-subscription__plan-name">{{ subscription.editionName ?? t('tenant.subscription.no_edition') }}</span>
            <span v-if="subscription.editionDescription" class="xh-subscription__plan-desc">{{ subscription.editionDescription }}</span>
          </div>
          <XhTagRoot variant="subtle" :tone="tenantStatusTone(subscription.tenantStatus)">
            <XhTagLabel>{{ getOptionLabel(tenantStatusOptions, subscription.tenantStatus) }}</XhTagLabel>
          </XhTagRoot>
        </section>

        <div class="xh-subscription__grid">
          <section class="xh-subscription__tile">
            <span class="xh-subscription__caption">
              <Icon icon="lucide:building-2" width="14" height="14" />
              {{ t('tenant.subscription.tenant') }}
            </span>
            <span class="xh-subscription__value">{{ subscription.tenantName }}</span>
            <span class="xh-subscription__hint">{{ subscription.tenantCode }}</span>
          </section>

          <section class="xh-subscription__tile">
            <span class="xh-subscription__caption">
              <Icon icon="lucide:calendar-clock" width="14" height="14" />
              {{ t('tenant.subscription.expiration') }}
            </span>
            <span class="xh-subscription__value">{{ expiration.text }}</span>
            <span v-if="expiration.hint" class="xh-subscription__hint" :class="`is-${expiration.hintTone}`">{{ expiration.hint }}</span>
          </section>

          <section v-for="quota in quotas" :key="quota.key" class="xh-subscription__tile">
            <span class="xh-subscription__caption">
              <Icon :icon="quota.icon" width="14" height="14" />
              {{ quota.label }}
            </span>
            <span class="xh-subscription__value">
              {{ quota.usedText }}
              <span v-if="quota.limitText" class="xh-subscription__limit">/ {{ quota.limitText }}</span>
            </span>
            <XhProgress
              v-if="quota.ratio != null"
              semantics="meter"
              size="sm"
              :value="Math.min(quota.ratio, 1) * 100"
              :tone="quota.hintTone === 'muted' ? undefined : quota.hintTone"
              :value-text="t('tenant.subscription.usage', { used: quota.usedText, limit: quota.limitText })"
              :aria-label="quota.label"
            />
            <span class="xh-subscription__hint" :class="`is-${quota.hintTone}`">{{ quota.hint }}</span>
          </section>
        </div>

        <p class="xh-subscription__note">
          {{ t('tenant.subscription.contact_platform') }}
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.xh-subscription {
  padding: var(--xh-space-3);
}

.xh-subscription__body {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.xh-subscription__plan,
.xh-subscription__tile {
  background: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.xh-subscription__plan {
  display: flex;
  gap: var(--xh-space-3);
  align-items: center;
  padding: var(--xh-space-4);
}

.xh-subscription__plan-icon {
  display: inline-flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  inline-size: 2.5rem;
  block-size: 2.5rem;
  color: var(--xh-fg-brand);
  background: var(--xh-bg-brand-subtle);
  border-radius: var(--xh-radius-md);
}

.xh-subscription__plan-main {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--xh-space-1);
  min-inline-size: 0;
}

.xh-subscription__plan-name {
  font-size: var(--xh-text-heading-3-size);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.xh-subscription__plan-desc {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}

.xh-subscription__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
  gap: var(--xh-space-3);
}

.xh-subscription__tile {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-inline-size: 0;
  padding: var(--xh-space-4);
}

.xh-subscription__caption {
  display: inline-flex;
  gap: var(--xh-space-1);
  align-items: center;
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}

.xh-subscription__value {
  overflow: hidden;
  font-size: var(--xh-font-size-xl);
  font-weight: var(--xh-font-weight-semibold);
  font-variant-numeric: tabular-nums;
  color: var(--xh-fg-default);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.xh-subscription__limit {
  font-size: var(--xh-text-secondary-size);
  font-weight: var(--xh-font-weight-regular);
  color: var(--xh-fg-muted);
}

.xh-subscription__hint {
  margin-block-start: auto;
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}

.xh-subscription__hint.is-warning {
  color: var(--xh-fg-warning);
}

.xh-subscription__hint.is-danger {
  color: var(--xh-fg-danger);
}

.xh-subscription__note {
  margin: 0;
  font-size: var(--xh-text-caption-size);
  line-height: 1.5;
  color: var(--xh-fg-muted);
}
</style>
