<script setup lang="ts">
import type { TenantSubscriptionDto } from '@/api'
import { XhButton, XhCardContent, XhCardHeader, XhCardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhProgress, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
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

interface QuotaView {
  key: string
  icon: string
  label: string
  usedText: string
  limitText: string
  /** 有上限时的占用比例（0~1，可超过 1 表示已超限） */
  ratio: number | null
}

function quotaTone(ratio: number | null) {
  if (ratio == null) {
    return undefined
  }
  if (ratio >= 1) {
    return 'danger' as const
  }
  return ratio >= QUOTA_WARNING_RATIO ? 'warning' as const : undefined
}

const quotas = computed<QuotaView[]>(() => {
  const value = subscription.value
  if (!value) {
    return []
  }
  const storageLimitBytes = value.effectiveStorageLimit == null ? null : value.effectiveStorageLimit * BYTES_PER_MB
  return [
    {
      key: 'seats',
      icon: 'lucide:users',
      label: t('tenant.subscription.seats'),
      usedText: String(value.usedUserCount),
      limitText: value.effectiveUserLimit == null ? t('tenant.subscription.unlimited') : String(value.effectiveUserLimit),
      ratio: value.effectiveUserLimit == null ? null : ratioOf(value.usedUserCount, value.effectiveUserLimit),
    },
    {
      key: 'storage',
      icon: 'lucide:hard-drive',
      label: t('tenant.subscription.storage'),
      usedText: formatFileSize(value.usedStorageBytes),
      limitText: storageLimitBytes == null ? t('tenant.subscription.unlimited') : formatFileSize(storageLimitBytes),
      ratio: storageLimitBytes == null ? null : ratioOf(value.usedStorageBytes, storageLimitBytes),
    },
  ]
})

function ratioOf(used: number, limit: number) {
  return limit <= 0 ? 1 : used / limit
}

/** 到期说明：长期有效 / 已到期 / 剩余天数 */
const expirationHint = computed(() => {
  const value = subscription.value
  if (!value?.expirationTime) {
    return t('tenant.subscription.never_expires')
  }
  if (value.isExpired) {
    return t('tenant.subscription.expired')
  }
  const days = Math.ceil((new Date(value.expirationTime).getTime() - Date.now()) / MS_PER_DAY)
  return t('tenant.subscription.days_left', { days })
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
        <XhCardRoot variant="ghost">
          <XhCardHeader>
            <div class="xh-subscription__title">
              <Icon icon="lucide:package" width="16" height="16" />
              <span>{{ subscription.editionName ?? t('tenant.subscription.no_edition') }}</span>
              <XhTagRoot v-if="subscription.isFreeEdition" variant="subtle" tone="info" size="sm">
                <XhTagLabel>{{ t('tenant.subscription.free') }}</XhTagLabel>
              </XhTagRoot>
            </div>
            <p v-if="subscription.editionDescription" class="xh-subscription__description">
              {{ subscription.editionDescription }}
            </p>
          </XhCardHeader>
          <XhCardContent>
            <XhDescriptionsRoot>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('tenant.subscription.tenant') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ subscription.tenantName }}
                  <span class="xh-subscription__hint">{{ subscription.tenantCode }}</span>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('tenant.subscription.tenant_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot variant="subtle" :tone="tenantStatusTone(subscription.tenantStatus)" size="sm">
                    <XhTagLabel>{{ getOptionLabel(tenantStatusOptions, subscription.tenantStatus) }}</XhTagLabel>
                  </XhTagRoot>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('tenant.subscription.expiration') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <span v-if="subscription.expirationTime">{{ formatDate(subscription.expirationTime) }}</span>
                  <span class="xh-subscription__hint" :class="{ 'is-danger': subscription.isExpired }">{{ expirationHint }}</span>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </XhCardContent>
        </XhCardRoot>

        <div class="xh-subscription__quotas">
          <XhCardRoot v-for="quota in quotas" :key="quota.key" variant="ghost">
            <XhCardContent>
              <div class="xh-subscription__quota-head">
                <span class="xh-subscription__quota-label">
                  <Icon :icon="quota.icon" width="16" height="16" />
                  {{ quota.label }}
                </span>
                <span class="xh-subscription__quota-value">
                  {{ t('tenant.subscription.usage', { used: quota.usedText, limit: quota.limitText }) }}
                </span>
              </div>
              <XhProgress
                v-if="quota.ratio != null"
                semantics="meter"
                :value="Math.min(quota.ratio, 1) * 100"
                :tone="quotaTone(quota.ratio)"
                :value-text="t('tenant.subscription.usage', { used: quota.usedText, limit: quota.limitText })"
                :aria-label="quota.label"
              />
            </XhCardContent>
          </XhCardRoot>
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

.xh-subscription__title {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.xh-subscription__description {
  margin: var(--xh-space-1) 0 0;
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}

.xh-subscription__hint {
  margin-inline-start: var(--xh-space-2);
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}

.xh-subscription__hint.is-danger {
  color: var(--xh-fg-danger);
}

.xh-subscription__quotas {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 18rem), 1fr));
  gap: var(--xh-space-3);
}

.xh-subscription__quota-head {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  justify-content: space-between;
  margin-block-end: var(--xh-space-2);
}

.xh-subscription__quota-label {
  display: inline-flex;
  gap: var(--xh-space-2);
  align-items: center;
  color: var(--xh-fg-muted);
}

.xh-subscription__quota-value {
  font-variant-numeric: tabular-nums;
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-default);
}

.xh-subscription__note {
  margin: 0;
  font-size: var(--xh-text-caption-size);
  line-height: 1.5;
  color: var(--xh-fg-muted);
}
</style>
