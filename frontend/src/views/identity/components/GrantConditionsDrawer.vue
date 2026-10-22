<script setup lang="ts">
import type { GrantConditionTarget } from './permission-condition'
import {
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhEmptyStateDescription,
  XhEmptyStateIndicator,
  XhEmptyStateRoot,
  XhEmptyStateTitle,
  XhSpinner,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { PermissionAction } from '@/api'
import { Icon, XInput } from '~/components'
import PermissionConditionPanel from './PermissionConditionPanel.vue'

/**
 * 授权条件抽屉：左边是这个角色（或用户）的有效授权，右边编辑选中那条授权上的条件。
 * 只有在授权上才能挂条件，未授予的权限不在这里出现
 */
defineOptions({ name: 'GrantConditionsDrawer' })

const props = defineProps<{
  title: string
  grants: readonly GrantConditionTarget[]
  loading: boolean
  can: { create: boolean, update: boolean, status: boolean, delete: boolean, trial: boolean }
}>()

const emit = defineEmits<{
  /** 条件有增删改或启停 */
  changed: []
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const keyword = ref('')
const selectedKey = ref<string | null>(null)

const filtered = computed(() => {
  const value = keyword.value.trim().toLowerCase()
  return value
    ? props.grants.filter(grant => grant.permissionName.toLowerCase().includes(value) || grant.permissionCode.toLowerCase().includes(value))
    : props.grants
})
const selected = computed(() => props.grants.find(grant => grant.key === selectedKey.value) ?? null)

watch(open, (value) => {
  if (value) {
    keyword.value = ''
    selectedKey.value = null
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 1100px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ title }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body grant-conditions">
        <aside class="grant-conditions__list" :aria-label="t('identity.condition.grants_label')">
          <XInput v-model:value="keyword" clearable :placeholder="t('identity.condition.search_grant')" />
          <div v-if="loading" class="grant-conditions__loading">
            <XhSpinner />
          </div>
          <ul v-else-if="filtered.length" class="grant-conditions__items">
            <li v-for="grant in filtered" :key="grant.key">
              <button
                type="button"
                class="grant-conditions__item"
                :aria-current="grant.key === selectedKey ? 'true' : undefined"
                @click="selectedKey = grant.key"
              >
                <span class="grant-conditions__name">{{ grant.permissionName }}</span>
                <code class="grant-conditions__code">{{ grant.permissionCode }}</code>
                <span class="grant-conditions__tags">
                  <XhTagRoot v-if="grant.action === PermissionAction.Deny" variant="subtle" size="sm" tone="danger">
                    <XhTagLabel>{{ t('identity.condition.action_deny') }}</XhTagLabel>
                  </XhTagRoot>
                  <XhTagRoot v-if="grant.conditionCount > 0" variant="subtle" size="sm" tone="info">
                    <XhTagLabel>{{ t('identity.condition.count_tag', { count: grant.conditionCount }) }}</XhTagLabel>
                  </XhTagRoot>
                </span>
              </button>
            </li>
          </ul>
          <p v-else class="grant-conditions__hint">
            {{ t('identity.condition.grants_empty') }}
          </p>
        </aside>
        <section class="grant-conditions__detail">
          <template v-if="selected">
            <h3 class="grant-conditions__heading">
              {{ selected.permissionName }}
              <code class="grant-conditions__code">{{ selected.permissionCode }}</code>
            </h3>
            <PermissionConditionPanel :target="selected" :can="can" @changed="emit('changed')" />
          </template>
          <XhEmptyStateRoot v-else class="grant-conditions__empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:mouse-pointer-click" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('identity.condition.pick_grant_title') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('identity.condition.pick_grant_description') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </section>
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
/* 窄屏时列表与编辑区上下叠放 */
.grant-conditions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
  align-items: start;
  gap: var(--xh-space-4);
}

.grant-conditions__list {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-block-size: 0;
}

.grant-conditions__loading {
  display: flex;
  justify-content: center;
  padding-block: var(--xh-space-6);
}

.grant-conditions__items {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  margin: 0;
  padding: 0;
  list-style: none;
}

.grant-conditions__item {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--xh-space-1);
  inline-size: 100%;
  padding: var(--xh-space-2) var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-sm);
  background: transparent;
  color: inherit;
  text-align: start;
  cursor: pointer;
}

.grant-conditions__item:hover {
  background: var(--xh-bg-subtle-hover);
}

.grant-conditions__item:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.grant-conditions__item[aria-current='true'] {
  border-color: var(--xh-border-control-focus);
  background: var(--xh-bg-brand-subtle);
}

.grant-conditions__name {
  font-weight: var(--xh-font-weight-medium);
}

.grant-conditions__code {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.grant-conditions__tags {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1);
}

.grant-conditions__hint {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.grant-conditions__detail {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  min-block-size: 0;
}

.grant-conditions__heading {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: var(--xh-space-2);
  margin: 0;
  font-size: var(--xh-text-body-size);
}

.grant-conditions__empty {
  padding-block: var(--xh-space-8);
}
</style>
