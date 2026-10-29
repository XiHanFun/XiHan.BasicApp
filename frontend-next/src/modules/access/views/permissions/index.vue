<script lang="ts" setup>
import type { PermissionRow, PermissionScope } from './use-permissions'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage } from '~/components'
import { permissions as permissionsApi } from '~/contracts'
import ConditionInventoryDrawer from './components/ConditionInventoryDrawer.vue'
import ConditionTrialDrawer from './components/ConditionTrialDrawer.vue'
import { packagesOf, PERMISSION_SCOPES, permissionsResource } from './use-permissions'

defineOptions({ name: 'AccessPermissionsView' })

const { t } = useI18n()
const resource = permissionsResource()

// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as PermissionRow

function scopeLabel(scope: PermissionScope): string {
  switch (scope) {
    case 'Platform': return t('access.permissions.scopes.platform')
    case 'Tenant': return t('access.permissions.scopes.tenant')
    case 'Both': return t('access.permissions.scopes.both')
  }
}

function scopeTone(scope: PermissionScope): 'brand' | 'info' | 'neutral' {
  switch (scope) {
    case 'Platform': return 'brand'
    case 'Tenant': return 'info'
    case 'Both': return 'neutral'
  }
}

// 权限目录是各包在代码里登记的，只读：没有新建、编辑、启停与删除（4.x 平台侧可维护的那几个钮在 6.0 没有对应操作）
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('access.permissions.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('access.permissions.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'package',
    title: t('access.permissions.fields.package'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    optionsLoader: async () => packagesOf((await permissionsApi.list({})).items).map(value => ({ label: value, value })),
    searchPlaceholder: t('access.permissions.fields.package'),
    exportable: true,
    minWidth: 110,
    order: 1,
  },
  { key: 'displayName', title: t('access.permissions.fields.name'), dataType: 'string', exportable: true, minWidth: 160, order: 2 },
  { key: 'code', title: t('access.permissions.fields.code'), dataType: 'string', sortable: true, exportable: true, minWidth: 240, order: 3 },
  { key: 'resource', title: t('access.permissions.fields.resource'), dataType: 'string', exportable: true, minWidth: 130, order: 4 },
  { key: 'action', title: t('access.permissions.fields.action'), dataType: 'string', exportable: true, minWidth: 110, order: 5 },
  {
    key: 'scope',
    title: t('access.permissions.fields.scope'),
    dataType: 'enum',
    searchable: true,
    options: PERMISSION_SCOPES.map(value => ({ label: scopeLabel(value), value })),
    searchPlaceholder: t('access.permissions.fields.scope'),
    exportable: true,
    width: 110,
    order: 6,
    render: row => renderTag(scopeLabel(asRow(row).scope), scopeTone(asRow(row).scope)),
  },
  {
    key: 'audited',
    title: t('access.permissions.fields.audited'),
    dataType: 'enum',
    searchable: true,
    options: [{ label: t('access.permissions.yes'), value: 'true' }, { label: t('access.permissions.no'), value: 'false' }],
    searchPlaceholder: t('access.permissions.fields.audited'),
    exportable: true,
    width: 100,
    order: 7,
    render: row => (asRow(row).audited ? renderTag(t('access.permissions.yes'), 'warning') : '-'),
  },
  { key: 'description', title: t('access.permissions.fields.description'), dataType: 'string', ellipsis: true, exportable: true, minWidth: 260, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.permissions',
  pageName: t('access.permissions.title'),
  exportPermission: 'access.permissions.read',
  rowKey: 'code',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'conditionTrial', title: t('access.conditions.action_trial'), scope: 'page', icon: 'lucide:flask-conical', permission: 'access.permission-conditions.trial' },
    { key: 'conditionInventory', title: t('access.conditions.action_inventory'), scope: 'page', icon: 'lucide:clipboard-check', permission: 'access.permission-conditions.trial' },
  ],
}))

/** 条件试算与盘点两个抽屉（4.x 同挂在权限页） */
const trialVisible = ref(false)
const inventoryVisible = ref(false)

function onAction(payload: SchemaActionPayload) {
  if (payload.key === 'conditionTrial')
    trialVisible.value = true
  else if (payload.key === 'conditionInventory')
    inventoryVisible.value = true
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <ConditionTrialDrawer v-model:open="trialVisible" />
    <ConditionInventoryDrawer v-model:open="inventoryVisible" />
  </SchemaPage>
</template>
