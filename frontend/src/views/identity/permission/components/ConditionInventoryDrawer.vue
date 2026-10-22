<script setup lang="ts">
import type { PermissionConditionInventoryDto, PermissionConditionInventoryItemDto } from '@/api'
import type { XDataTableColumn } from '~/components'
import {
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  PermissionAction,
  PermissionConditionBindingKind,
  PermissionConditionDiagnostic,
  PermissionConditionGroupConclusion,
  permissionConditionInsightApi,
  ValidityStatus,
} from '@/api'
import { CONDITION_OPERATOR_OPTIONS } from '@/constants'
import { Icon, XDataTable } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { downloadBlob, formatDate, getOptionLabel } from '~/utils'
import { conclusionTone, inventoryToCsv } from '../../components/permission-condition'

/**
 * 条件盘点：逐条诊断存量条件（属性是否登记、值能否解析、所在授权是否有效），给出每组生效后的结论，可导出
 */
defineOptions({ name: 'ConditionInventoryDrawer' })

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const operatorOptions = useEnumOptions('ConditionOperator', CONDITION_OPERATOR_OPTIONS)
const inventory = ref<PermissionConditionInventoryDto | null>(null)
const loading = ref(false)

const conclusions = computed(() => Object.values(PermissionConditionGroupConclusion)
  .map(conclusion => ({ conclusion, count: inventory.value?.groupConclusions[conclusion] ?? 0 }))
  .filter(item => item.count > 0))
const diagnostics = computed(() => Object.values(PermissionConditionDiagnostic)
  .map(diagnostic => ({ diagnostic, count: inventory.value?.diagnostics[diagnostic] ?? 0 }))
  .filter(item => item.count > 0))

function bindingText(item: PermissionConditionInventoryItemDto) {
  switch (item.bindingKind) {
    case PermissionConditionBindingKind.Role:
      return t('identity.condition.binding_role', { name: item.roleName || item.roleCode || String(item.rolePermissionId) })
    case PermissionConditionBindingKind.User:
      return t('identity.condition.binding_user', { name: item.userName || String(item.userId ?? item.userPermissionId) })
    default:
      return t('identity.condition.binding_ambiguous')
  }
}

function clauseText(item: PermissionConditionInventoryItemDto) {
  const operator = `${item.isNegated ? t('identity.condition.negated_prefix') : ''}${getOptionLabel(operatorOptions.value, item.operator)}`
  return `${item.attributeName} ${operator} ${item.conditionValue}`.trim()
}

function actionText(item: PermissionConditionInventoryItemDto) {
  if (!item.permissionAction) {
    return ''
  }
  return item.permissionAction === PermissionAction.Deny ? t('identity.condition.action_deny') : t('identity.condition.action_grant')
}

const columns = computed<XDataTableColumn<PermissionConditionInventoryItemDto>[]>(() => [
  { key: 'tenant', title: t('identity.condition.th_tenant'), width: 110, render: row => row.tenantCode || t('identity.condition.platform') },
  { key: 'binding', title: t('identity.condition.th_binding'), minWidth: 140, render: row => bindingText(row) },
  { key: 'permission', title: t('identity.condition.th_permission'), minWidth: 180, render: row => `${row.permissionName ?? ''} ${row.permissionCode ?? ''}`.trim() },
  { key: 'action', title: t('identity.condition.th_action'), width: 80, render: row => actionText(row) },
  { key: 'group', title: t('identity.condition.th_group'), width: 70, render: row => String(row.conditionGroup) },
  { key: 'clause', title: t('identity.condition.th_clause'), minWidth: 220, render: row => clauseText(row) },
  {
    key: 'status',
    title: t('identity.condition.th_status'),
    width: 80,
    render: row => row.status === ValidityStatus.Valid ? t('identity.condition.status_on') : t('identity.condition.status_off'),
  },
  {
    key: 'diagnostics',
    title: t('identity.condition.th_diagnostics'),
    minWidth: 180,
    render: row => h('span', { style: 'display: flex; flex-wrap: wrap; gap: var(--xh-space-1)', title: row.message ?? undefined }, row.diagnostics.map(diagnostic =>
      h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: diagnostic === PermissionConditionDiagnostic.LegacyName || diagnostic === PermissionConditionDiagnostic.ValueTypeMismatch ? 'warning' : 'danger' }, () =>
        h(XhTagLabel, () => t(`identity.condition.diagnostic.${diagnostic}`))))),
  },
  {
    key: 'conclusion',
    title: t('identity.condition.th_conclusion'),
    width: 120,
    render: row => h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: conclusionTone(row.groupConclusion) }, () =>
      h(XhTagLabel, () => t(`identity.condition.conclusion.${row.groupConclusion}`))),
  },
])

async function load() {
  loading.value = true
  try {
    inventory.value = await permissionConditionInsightApi.inventory()
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_inventory_failed'))
  }
  finally {
    loading.value = false
  }
}

function exportCsv() {
  if (!inventory.value) {
    return
  }
  const headers = ['tenant', 'binding', 'permission', 'action', 'group', 'clause', 'status', 'diagnostics', 'message', 'conclusion']
    .map(key => t(`identity.condition.th_${key}`))
  const csv = inventoryToCsv(inventory.value.items, headers, item => [
    item.tenantCode || t('identity.condition.platform'),
    bindingText(item),
    `${item.permissionName ?? ''} ${item.permissionCode ?? ''}`.trim(),
    actionText(item),
    String(item.conditionGroup),
    clauseText(item),
    item.status === ValidityStatus.Valid ? t('identity.condition.status_on') : t('identity.condition.status_off'),
    item.diagnostics.map(diagnostic => t(`identity.condition.diagnostic.${diagnostic}`)).join('、'),
    item.message ?? '',
    t(`identity.condition.conclusion.${item.groupConclusion}`),
  ])
  downloadBlob(new Blob([csv], { type: 'text/csv;charset=utf-8' }), t('identity.condition.inventory_file', { time: formatDate(inventory.value.generatedTime, 'YYYYMMDDHHmmss') }))
}

watch(open, (value) => {
  if (value) {
    void load()
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 1200px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('identity.condition.inventory_title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body inventory">
        <p class="inventory__tip">
          {{ t('identity.condition.inventory_tip') }}
        </p>
        <div class="inventory__toolbar">
          <XhButton variant="subtle" size="sm" :loading="loading" @click="load">
            <XhButtonIndicator>
              <Icon icon="lucide:refresh-cw" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('identity.condition.rerun') }}</XhButtonLabel>
          </XhButton>
          <XhButton variant="subtle" size="sm" :disabled="!inventory?.items.length" @click="exportCsv">
            <XhButtonIndicator>
              <Icon icon="lucide:download" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('identity.condition.export_csv') }}</XhButtonLabel>
          </XhButton>
          <span v-if="inventory" class="inventory__meta">
            {{ t('identity.condition.inventory_summary', { conditions: inventory.conditionCount, bindings: inventory.bindingCount }) }}
            · {{ t('identity.condition.generated_at', { time: formatDate(inventory.generatedTime) }) }}
          </span>
        </div>
        <div v-if="conclusions.length || diagnostics.length" class="inventory-tags">
          <XhTagRoot v-for="item in conclusions" :key="item.conclusion" variant="subtle" size="sm" :tone="conclusionTone(item.conclusion)">
            <XhTagLabel>{{ t(`identity.condition.conclusion.${item.conclusion}`) }} {{ item.count }}</XhTagLabel>
          </XhTagRoot>
          <XhTagRoot v-for="item in diagnostics" :key="item.diagnostic" variant="outline" size="sm" tone="neutral">
            <XhTagLabel>{{ t(`identity.condition.diagnostic.${item.diagnostic}`) }} {{ item.count }}</XhTagLabel>
          </XhTagRoot>
        </div>
        <XDataTable
          :columns="columns"
          :data="inventory?.items ?? []"
          row-key="conditionId"
          size="sm"
          :loading="loading"
          :max-height="560"
        />
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

.inventory__tip,
.inventory__meta {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.inventory__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.inventory-tags {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1);
}
</style>
