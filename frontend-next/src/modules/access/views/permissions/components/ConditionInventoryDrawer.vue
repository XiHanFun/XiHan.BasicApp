<script setup lang="ts">
import type { ConditionInventory, ConditionInventoryItem } from '../../roles/condition-trial'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/components'
import { toast } from '~/composables'
import { permissionConditions as conditionsApi } from '~/contracts'
import { formatDate } from '~/utils'
import { describeCondition } from '../../roles/condition-labels'
import { conclusionLabel, conclusionTone, diagnosticLabel, inventoryToCsv } from '../../roles/condition-trial'

/**
 * 条件盘点：当前一侧每条条件的诊断（属性未登记、旧写法、编译不过、绑定已不在）与所在组的结论，可导出 CSV。
 */
defineOptions({ name: 'ConditionInventoryDrawer' })

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const loading = ref(false)
const inventory = ref<ConditionInventory | null>(null)

const summary = computed(() => {
  const items = inventory.value?.items ?? []
  return {
    never: items.filter(item => item.groupConclusion === 'NeverMatches').length,
    inactive: items.filter(item => item.groupConclusion === 'BindingInactive').length,
    diagnosed: items.filter(item => item.diagnostics.length > 0).length,
  }
})

function subjectOf(item: ConditionInventoryItem): string {
  const kind = item.subjectKind === 'User' ? t('access.conditions.subject_user') : t('access.conditions.subject_role')
  return `${kind} · ${item.subjectName ?? item.subjectId}`
}

function bindingOf(item: ConditionInventoryItem): string {
  return `${item.permission} · ${item.action === 'Deny' ? t('access.conditions.action_deny') : t('access.conditions.action_grant')}`
}

async function load() {
  loading.value = true
  try {
    inventory.value = await conditionsApi.inventory({})
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.conditions.msg_load_failed'))
  }
  finally {
    loading.value = false
  }
}

function exportCsv() {
  const items = inventory.value?.items ?? []
  const csv = inventoryToCsv(
    items,
    [t('access.conditions.th_subject'), t('access.conditions.th_binding'), t('access.conditions.th_group'), t('access.conditions.th_clause'), t('access.conditions.th_status'), t('access.conditions.th_diagnostics'), t('access.conditions.th_conclusion'), t('access.conditions.th_message')],
    item => [
      subjectOf(item),
      bindingOf(item),
      String(item.groupNo),
      describeCondition(t, item),
      item.status === 'Active' ? t('access.conditions.status_on') : t('access.conditions.status_off'),
      item.diagnostics.map(d => diagnosticLabel(t, d)).join('; '),
      conclusionLabel(t, item.groupConclusion),
      item.message ?? '',
    ],
  )
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `permission-conditions-${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  URL.revokeObjectURL(url)
}

watch(open, (value) => {
  if (value)
    void load()
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 1100px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('access.conditions.inventory_title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body inventory">
        <div class="inventory__toolbar">
          <p class="inventory__tip">
            <template v-if="inventory">
              {{ t('access.conditions.inventory_summary', { conditions: inventory.conditionCount, bindings: inventory.bindingCount, diagnosed: summary.diagnosed, never: summary.never, inactive: summary.inactive, time: formatDate(inventory.generatedAt) }) }}
            </template>
            <template v-else>
              {{ t('access.conditions.inventory_tip') }}
            </template>
          </p>
          <XhButton variant="subtle" size="sm" :loading="loading" @click="load">
            <XhButtonIndicator>
              <Icon icon="lucide:refresh-cw" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('access.conditions.action_refresh') }}</XhButtonLabel>
          </XhButton>
          <XhButton variant="subtle" size="sm" :disabled="!inventory?.items.length" @click="exportCsv">
            <XhButtonIndicator>
              <Icon icon="lucide:download" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('access.conditions.action_export') }}</XhButtonLabel>
          </XhButton>
        </div>
        <div v-if="loading && !inventory" class="inventory__loading">
          <XhSpinner />
        </div>
        <table v-else-if="inventory?.items.length" class="xh-detail-table">
          <thead>
            <tr>
              <th>{{ t('access.conditions.th_subject') }}</th>
              <th>{{ t('access.conditions.th_binding') }}</th>
              <th>{{ t('access.conditions.th_group') }}</th>
              <th>{{ t('access.conditions.th_clause') }}</th>
              <th>{{ t('access.conditions.th_status') }}</th>
              <th>{{ t('access.conditions.th_diagnostics') }}</th>
              <th>{{ t('access.conditions.th_conclusion') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in inventory.items" :key="item.conditionId">
              <td>{{ subjectOf(item) }}</td>
              <td><code>{{ bindingOf(item) }}</code></td>
              <td>{{ item.groupNo }}</td>
              <td :title="item.message ?? undefined">
                {{ describeCondition(t, item) }}
              </td>
              <td>{{ item.status === 'Active' ? t('access.conditions.status_on') : t('access.conditions.status_off') }}</td>
              <td>
                <span class="inventory__tags">
                  <XhTagRoot v-for="diagnostic in item.diagnostics" :key="diagnostic" variant="subtle" size="sm" tone="warning">
                    <XhTagLabel>{{ diagnosticLabel(t, diagnostic) }}</XhTagLabel>
                  </XhTagRoot>
                </span>
              </td>
              <td>
                <XhTagRoot variant="subtle" size="sm" :tone="conclusionTone(item.groupConclusion)">
                  <XhTagLabel>{{ conclusionLabel(t, item.groupConclusion) }}</XhTagLabel>
                </XhTagRoot>
              </td>
            </tr>
          </tbody>
        </table>
        <XhEmptyStateRoot v-else class="inventory__empty">
          <XhEmptyStateIndicator>
            <Icon icon="lucide:clipboard-check" width="28" />
          </XhEmptyStateIndicator>
          <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
          <XhEmptyStateDescription>{{ t('access.conditions.inventory_empty') }}</XhEmptyStateDescription>
        </XhEmptyStateRoot>
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.inventory {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.inventory__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.inventory__tip {
  flex: 1;
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.inventory__tags {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1);
}

.inventory__loading {
  display: flex;
  justify-content: center;
  padding-block: var(--xh-space-6);
}

.inventory__empty {
  padding-block: var(--xh-space-8);
}
</style>
