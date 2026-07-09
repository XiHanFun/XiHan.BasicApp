<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { PageResult, PermissionChangeLogDetailDto, PermissionChangeLogListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { createPageRequest, permissionChangeLogApi, PermissionChangeType, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import { permissionChangeLogDetailFields } from '../_components/log-detail-fields'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../_components/trace-nav'

defineOptions({ name: 'LogPermissionChangePage' })

const { t } = useI18n()
const message = useMessage()
const router = useRouter()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<PermissionChangeLogDetailDto | null>(null)

const changeTypeOptions = computed(() => [
  { label: t('log.permission_change.type_role_grant'), value: PermissionChangeType.RoleGrantPermission },
  { label: t('log.permission_change.type_role_revoke'), value: PermissionChangeType.RoleRevokePermission },
  { label: t('log.permission_change.type_user_grant'), value: PermissionChangeType.UserGrantPermission },
  { label: t('log.permission_change.type_user_revoke'), value: PermissionChangeType.UserRevokePermission },
  { label: t('log.permission_change.type_user_assign_role'), value: PermissionChangeType.UserAssignRole },
  { label: t('log.permission_change.type_user_remove_role'), value: PermissionChangeType.UserRemoveRole },
  { label: t('log.permission_change.type_user_deny'), value: PermissionChangeType.UserDenyPermission },
  { label: t('log.permission_change.type_role_deny'), value: PermissionChangeType.RoleDenyPermission },
  { label: t('log.permission_change.type_user_delegate_grant'), value: PermissionChangeType.UserDelegateGrant },
  { label: t('log.permission_change.type_user_delegate_revoke'), value: PermissionChangeType.UserDelegateRevoke },
])

/** 变更类型 → 标签类型：授权绿、撤权橙、拒绝红 */
function changeTypeTagType(type: PermissionChangeType) {
  switch (type) {
    case PermissionChangeType.UserDenyPermission:
    case PermissionChangeType.RoleDenyPermission:
      return 'error'
    case PermissionChangeType.RoleRevokePermission:
    case PermissionChangeType.UserRevokePermission:
    case PermissionChangeType.UserRemoveRole:
      return 'warning'
    default:
      return 'success'
  }
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.permission_change.keyword_placeholder'), order: 0 },
  {
    key: 'changeType',
    title: t('log.permission_change.change_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: changeTypeOptions.value,
    searchPlaceholder: t('log.permission_change.change_type_placeholder'),
    width: 120,
    order: 10,
    render: (row) => {
      const type = (row as unknown as PermissionChangeLogListItemDto).changeType
      return h(NTag, { size: 'small', round: true, bordered: false, type: changeTypeTagType(type) }, () => getOptionLabel(changeTypeOptions.value, type))
    },
  },
  { key: 'operatorUserName', title: t('log.permission_change.operator_user_name'), dataType: 'string', minWidth: 130, order: 11 },
  { key: 'operatorUserId', title: t('log.permission_change.operator_user_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 12 },
  { key: 'targetUserName', title: t('log.permission_change.target_user_name'), dataType: 'string', minWidth: 130, order: 13 },
  { key: 'targetUserId', title: t('log.permission_change.target_user_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 14 },
  { key: 'targetRoleName', title: t('log.permission_change.target_role_name'), dataType: 'string', minWidth: 130, order: 15 },
  { key: 'targetRoleId', title: t('log.permission_change.target_role_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 16 },
  { key: 'permissionName', title: t('log.permission_change.permission_name'), dataType: 'string', minWidth: 150, order: 17 },
  { key: 'permissionId', title: t('log.permission_change.permission_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 18 },
  { key: 'changeReason', title: t('log.permission_change.change_reason'), dataType: 'string', minWidth: 180, order: 19 },
  { key: 'description', title: t('log.permission_change.description'), dataType: 'string', minWidth: 220, order: 20 },
  { key: 'operationIp', title: t('log.permission_change.operation_ip'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.permission_change.operation_ip_placeholder'), minWidth: 130, order: 21 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 160, order: 22 },
  { key: 'changeTime', title: t('log.permission_change.change_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 24 },
  // 仅高级搜索：变更时间区间
  { key: 'changeTime', title: t('log.permission_change.change_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchRange: true, searchPlaceholder: t('log.permission_change.change_time'), order: 40 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

/** 查询构建。changeType 多选与 changeTime 区间走 conditions.filters，由框架统一门控/应用 */
function buildQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    operatorUserId: toStr(f.operatorUserId),
    targetUserId: toStr(f.targetUserId),
    targetRoleId: toStr(f.targetRoleId),
    permissionId: toStr(f.permissionId),
    operationIp: toStr(f.operationIp),
    traceId: toStr(f.traceId),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.permission-change',
  pageName: t('log.permission_change.page_name'),
  rowKey: 'basicId',
  scrollX: 2600,
  fields: decorateTraceFields(fields.value, router, {
    timeField: 'changeTime',
    ipKey: 'operationIp',
    // 操作人 / 目标用户（名称按用户名维度、主键按用户ID维度）均可点击深链追踪
    extraDimensions: {
      operatorUserName: 'UserName',
      operatorUserId: 'UserId',
      targetUserName: 'UserName',
      targetUserId: 'UserId',
    },
  }),
  resource: {
    page: params => permissionChangeLogApi.page(buildQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('log.trace.action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => permissionChangeLogDetailFields(t))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as PermissionChangeLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
  else if (payload.key === 'trace' && row) {
    if (!gotoTrace(router, row, row.changeTime)) {
      message.warning(t('log.trace.value_required'))
    }
  }
}

async function handleDetail(row: PermissionChangeLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await permissionChangeLogApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error(t('log.permission_change.detail_load_failed'))
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      :title="t('log.permission_change.detail_title')"
    />
  </SchemaPage>
</template>
