<script setup lang="ts">
import type { ConditionAttribute, ConditionSubjectKind, PermissionConditionsResult } from '../permission-condition'
import { XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XInput } from '~/components'
import { toast } from '~/composables'
import { permissionConditions as conditionsApi } from '~/contracts'
import { useAuthStore } from '~/stores'
import { conditionsOf, toGrantConditionTargets } from '../permission-condition'
import PermissionConditionPanel from './PermissionConditionPanel.vue'

/**
 * 授权条件抽屉：左边是这个角色（或用户）此刻的授予与拒绝，右边编辑选中那条上的条件。
 * 只有在授权上才能挂条件，未授予的权限不在这里出现。
 */
defineOptions({ name: 'GrantConditionsDrawer' })

const props = defineProps<{
  title: string
  subjectKind: ConditionSubjectKind
  subjectId: null | string
}>()

const emit = defineEmits<{
  /** 条件有增删改或启停 */
  changed: []
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const auth = useAuthStore()
const can = computed(() => ({
  create: auth.can('access.permission-conditions.create'),
  update: auth.can('access.permission-conditions.update'),
  delete: auth.can('access.permission-conditions.delete'),
  trial: auth.can('access.permission-conditions.trial'),
}))

const loading = ref(false)
const result = ref<PermissionConditionsResult | null>(null)
const catalog = ref<ConditionAttribute[]>([])
const keyword = ref('')
const selectedKey = ref<null | string>(null)

const grants = computed(() => result.value ? toGrantConditionTargets(result.value) : [])
const filtered = computed(() => {
  const value = keyword.value.trim().toLowerCase()
  return value
    ? grants.value.filter(grant => grant.permissionName.toLowerCase().includes(value) || grant.permission.toLowerCase().includes(value))
    : grants.value
})
const selected = computed(() => grants.value.find(grant => grant.key === selectedKey.value) ?? null)
const selectedConditions = computed(() => selected.value ? conditionsOf(result.value, selected.value) : [])

async function load() {
  if (!props.subjectId)
    return
  loading.value = true
  try {
    const [list, attributes] = await Promise.all([
      conditionsApi.list({ subjectKind: props.subjectKind, subjectId: props.subjectId }),
      catalog.value.length > 0 ? Promise.resolve(null) : conditionsApi.attributes({}),
    ])
    result.value = list
    if (attributes)
      catalog.value = attributes.items
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.conditions.msg_load_failed'))
  }
  finally {
    loading.value = false
  }
}

async function onChanged() {
  await load()
  emit('changed')
}

watch(open, (value) => {
  if (value) {
    keyword.value = ''
    selectedKey.value = null
    result.value = null
    void load()
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
        <aside class="grant-conditions__list" :aria-label="t('access.conditions.grants_label')">
          <XInput v-model:value="keyword" clearable :placeholder="t('access.conditions.search_grant')" />
          <div v-if="loading && !result" class="grant-conditions__loading">
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
                <code class="grant-conditions__code">{{ grant.permission }}</code>
                <span class="grant-conditions__tags">
                  <XhTagRoot v-if="grant.action === 'Deny'" variant="subtle" size="sm" tone="danger">
                    <XhTagLabel>{{ t('access.conditions.action_deny') }}</XhTagLabel>
                  </XhTagRoot>
                  <XhTagRoot v-if="grant.conditionCount > 0" variant="subtle" size="sm" tone="info">
                    <XhTagLabel>{{ t('access.conditions.count_tag', { count: grant.conditionCount }) }}</XhTagLabel>
                  </XhTagRoot>
                </span>
              </button>
            </li>
          </ul>
          <p v-else class="grant-conditions__hint">
            {{ t('access.conditions.grants_empty') }}
          </p>
        </aside>
        <section class="grant-conditions__detail">
          <template v-if="selected">
            <h3 class="grant-conditions__heading">
              {{ selected.permissionName }}
              <code class="grant-conditions__code">{{ selected.permission }}</code>
            </h3>
            <PermissionConditionPanel :target="selected" :conditions="selectedConditions" :catalog="catalog" :can="can" @changed="onChanged" />
          </template>
          <XhEmptyStateRoot v-else class="grant-conditions__empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:mouse-pointer-click" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('access.conditions.pick_grant_title') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('access.conditions.pick_grant_description') }}</XhEmptyStateDescription>
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
