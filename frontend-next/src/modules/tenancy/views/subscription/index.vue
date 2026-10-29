<script lang="ts" setup>
import type { SubscriptionStatus } from './subscription'
import { XhButton, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhProgress, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/components'
import { statusTone, subscriptionExpiry, subscriptionPricing, subscriptionQuotas, useSubscription } from './subscription'

defineOptions({ name: 'TenancySubscriptionView' })

const { t } = useI18n()
const { subscription, loading, error, load } = useSubscription()

onMounted(load)

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: SubscriptionStatus): string {
  return status === 'Suspended' ? t('tenancy.tenants.status.suspended') : t('tenancy.tenants.status.active')
}

const quotas = computed(() => (subscription.value ? subscriptionQuotas(t, subscription.value) : []))
const expiry = computed(() => (subscription.value ? subscriptionExpiry(t, subscription.value) : null))
const pricing = computed(() => (subscription.value ? subscriptionPricing(t, subscription.value) : null))
</script>

<template>
  <div class="tenant-subscription">
    <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
      <div class="xh-loading-stage__veil">
        <XhSpinner />
      </div>

      <XhEmptyStateRoot v-if="error" class="tenant-subscription__empty">
        <XhEmptyStateIndicator>
          <Icon icon="lucide:alert-circle" />
        </XhEmptyStateIndicator>
        <XhEmptyStateTitle>{{ t('tenancy.subscription.load_failed') }}</XhEmptyStateTitle>
        <XhEmptyStateDescription>{{ error }}</XhEmptyStateDescription>
        <XhEmptyStateAction>
          <XhButton variant="subtle" size="sm" @click="load">
            {{ t('tenancy.subscription.retry') }}
          </XhButton>
        </XhEmptyStateAction>
      </XhEmptyStateRoot>

      <div v-else-if="subscription" class="tenant-subscription__body" data-testid="tenant-subscription">
        <section class="tenant-subscription__plan">
          <span class="tenant-subscription__plan-icon" aria-hidden="true">
            <Icon icon="lucide:package" width="20" height="20" />
          </span>
          <div class="tenant-subscription__plan-main">
            <span class="tenant-subscription__caption">{{ t('tenancy.subscription.current_edition') }}</span>
            <span class="tenant-subscription__plan-name">{{ subscription.editionName ?? t('tenancy.subscription.no_edition') }}</span>
            <span v-if="subscription.editionDescription" class="tenant-subscription__plan-desc">{{ subscription.editionDescription }}</span>
          </div>
          <!-- 价格与计费周期只作展示：6.0 没有续费与付款流程 -->
          <div v-if="pricing" class="tenant-subscription__price">
            <span class="tenant-subscription__caption">{{ t('tenancy.subscription.price') }}</span>
            <span class="tenant-subscription__price-value">{{ pricing.value }}</span>
            <span v-if="pricing.period" class="tenant-subscription__hint">{{ pricing.period }}</span>
          </div>
          <XhTagRoot variant="subtle" :tone="statusTone(subscription.status)">
            <XhTagLabel>{{ statusLabel(subscription.status) }}</XhTagLabel>
          </XhTagRoot>
        </section>

        <div class="tenant-subscription__grid">
          <section class="tenant-subscription__tile">
            <span class="tenant-subscription__caption">
              <Icon icon="lucide:building-2" width="14" height="14" />
              {{ t('tenancy.subscription.tenant') }}
            </span>
            <span class="tenant-subscription__value">{{ subscription.tenantName }}</span>
            <span class="tenant-subscription__hint">{{ subscription.tenantId }}</span>
          </section>

          <section v-if="expiry" class="tenant-subscription__tile">
            <span class="tenant-subscription__caption">
              <Icon icon="lucide:calendar-clock" width="14" height="14" />
              {{ t('tenancy.subscription.expiration') }}
            </span>
            <span class="tenant-subscription__value">{{ expiry.text }}</span>
            <span v-if="expiry.hint" class="tenant-subscription__hint" :class="`is-${expiry.tone}`">{{ expiry.hint }}</span>
          </section>

          <section v-for="quota in quotas" :key="quota.key" class="tenant-subscription__tile">
            <span class="tenant-subscription__caption">
              <Icon :icon="quota.icon" width="14" height="14" />
              {{ quota.label }}
            </span>
            <span class="tenant-subscription__value">
              {{ quota.usedText }}
              <span v-if="quota.limitText" class="tenant-subscription__limit">/ {{ quota.limitText }}</span>
            </span>
            <XhProgress
              v-if="quota.ratio !== null"
              semantics="meter"
              size="sm"
              :value="Math.min(quota.ratio, 1) * 100"
              :tone="quota.tone === 'muted' ? undefined : quota.tone"
              :value-text="t('tenancy.subscription.usage', { used: quota.usedText, limit: quota.limitText })"
              :aria-label="quota.label"
            />
            <span class="tenant-subscription__hint" :class="`is-${quota.tone}`">{{ quota.hint }}</span>
          </section>
        </div>

        <p class="tenant-subscription__note">
          {{ t('tenancy.subscription.contact_platform') }}
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tenant-subscription {
  padding: var(--xh-space-3);
}

.tenant-subscription__empty {
  padding-block: var(--xh-space-8);
}

.tenant-subscription__body {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.tenant-subscription__plan,
.tenant-subscription__tile {
  background: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.tenant-subscription__plan {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-3);
  align-items: center;
  padding: var(--xh-space-4);
}

.tenant-subscription__plan-icon {
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

.tenant-subscription__plan-main {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--xh-space-1);
  min-inline-size: 0;
}

.tenant-subscription__plan-name {
  font-size: var(--xh-text-heading-3-size);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.tenant-subscription__plan-desc {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}

.tenant-subscription__price {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  align-items: flex-end;
  text-align: end;
}

.tenant-subscription__price-value {
  font-size: var(--xh-font-size-xl);
  font-weight: var(--xh-font-weight-semibold);
  font-variant-numeric: tabular-nums;
  color: var(--xh-fg-default);
  white-space: nowrap;
}

.tenant-subscription__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
  gap: var(--xh-space-3);
}

.tenant-subscription__tile {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-inline-size: 0;
  padding: var(--xh-space-4);
}

.tenant-subscription__caption {
  display: inline-flex;
  gap: var(--xh-space-1);
  align-items: center;
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}

.tenant-subscription__value {
  overflow: hidden;
  font-size: var(--xh-font-size-xl);
  font-weight: var(--xh-font-weight-semibold);
  font-variant-numeric: tabular-nums;
  color: var(--xh-fg-default);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tenant-subscription__limit {
  font-size: var(--xh-text-secondary-size);
  font-weight: var(--xh-font-weight-regular);
  color: var(--xh-fg-muted);
}

.tenant-subscription__hint {
  margin-block-start: auto;
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}

.tenant-subscription__hint.is-warning {
  color: var(--xh-fg-warning);
}

.tenant-subscription__hint.is-danger {
  color: var(--xh-fg-danger);
}

.tenant-subscription__note {
  margin: 0;
  font-size: var(--xh-text-caption-size);
  line-height: 1.5;
  color: var(--xh-fg-muted);
}
</style>
