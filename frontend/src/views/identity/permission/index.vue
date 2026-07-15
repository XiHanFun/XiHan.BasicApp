<script setup lang="ts">
import type {
  ApiId,
  OperationSelectItemDto,
  PageResult,
  PermissionCenterDetailDto,
  PermissionCreateDto,
  PermissionListItemDto,
  PermissionUpdateDto,
  ResourceSelectItemDto,

  ValidityStatus,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTabPane,
  NTabs,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  HttpMethodType,
  permissionCenterApi,
  PermissionType,
  querySortsFromSchema,
} from '@/api'
import { CONDITION_OPERATOR_OPTIONS, CONFIG_DATA_TYPE_OPTIONS, DELEGATION_STATUS_OPTIONS, FIELD_MASK_STRATEGY_OPTIONS, FIELD_SECURITY_TARGET_TYPE_OPTIONS, HTTP_METHOD_OPTIONS, OPERATION_CATEGORY_OPTIONS, OPERATION_TYPE_OPTIONS, PERMISSION_CHANGE_TYPE_OPTIONS, PERMISSION_REQUEST_STATUS_OPTIONS, PERMISSION_TYPE_OPTIONS, RESOURCE_ACCESS_LEVEL_OPTIONS, RESOURCE_TYPE_OPTIONS, STATUS_OPTIONS, VALIDITY_STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage } from '~/components'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemPermissionPage' })

const { t } = useI18n()

interface PermissionFormModel extends PermissionCreateDto {
  basicId?: ApiId
}

interface NumericSelectOption {
  label: string
  value: ApiId
}

const message = useMessage()

const submitLoading = ref(false)
const resourceLoading = ref(false)
const operationLoading = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<PermissionCenterDetailDto | null>(null)
const permissionForm = ref<PermissionFormModel>(createDefaultForm())
const resourceOptions = ref<NumericSelectOption[]>([])
const operationOptions = ref<NumericSelectOption[]>([])

const modalVisible = ref(false)

const permissionTypeOptions = useEnumOptions('PermissionType', PERMISSION_TYPE_OPTIONS)
const validityStatusOptions = useEnumOptions('ValidityStatus', VALIDITY_STATUS_OPTIONS)
const resourceTypeOptions = useEnumOptions('ResourceType', RESOURCE_TYPE_OPTIONS)
const resourceAccessLevelOptions = useEnumOptions('ResourceAccessLevel', RESOURCE_ACCESS_LEVEL_OPTIONS)
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const httpMethodTypeOptions = useEnumOptions('HttpMethodType', HTTP_METHOD_OPTIONS)
// HTTP 谓词为标识符无需翻译；仅合成项「所有方法」(ALL) 走本地 i18n
const httpMethodOptions = computed(() =>
  httpMethodTypeOptions.value.map(o =>
    o.value === HttpMethodType.ALL ? { ...o, label: t('component.http_method.all') } : o,
  ),
)
const operationCategoryOptions = useEnumOptions('OperationCategory', OPERATION_CATEGORY_OPTIONS)
const operationTypeOptions = useEnumOptions('OperationTypeCode', OPERATION_TYPE_OPTIONS)
const conditionOperatorOptions = useEnumOptions('ConditionOperator', CONDITION_OPERATOR_OPTIONS)
const configDataTypeOptions = useEnumOptions('ConfigDataType', CONFIG_DATA_TYPE_OPTIONS)
const delegationStatusOptions = useEnumOptions('DelegationStatus', DELEGATION_STATUS_OPTIONS)
const requestStatusOptions = useEnumOptions('PermissionRequestStatus', PERMISSION_REQUEST_STATUS_OPTIONS)
const fieldMaskStrategyOptions = useEnumOptions('FieldMaskStrategy', FIELD_MASK_STRATEGY_OPTIONS)
const fieldSecurityTargetTypeOptions = useEnumOptions('FieldSecurityTargetType', FIELD_SECURITY_TARGET_TYPE_OPTIONS)
const changeTypeOptions = useEnumOptions('PermissionChangeType', PERMISSION_CHANGE_TYPE_OPTIONS)

const globalOptions = computed(() => [
  { label: t('identity.permission.global_permission'), value: 1 },
  { label: t('identity.permission.tenant_permission'), value: 0 },
])

const auditOptions = computed(() => [
  { label: t('identity.permission.need_audit'), value: 1 },
  { label: t('identity.permission.no_audit'), value: 0 },
])

const modalTitle = computed(() => (permissionForm.value.basicId ? t('identity.permission.form_edit_title') : t('identity.permission.form_create_title')))
const isResourceBasedForm = computed(() => permissionForm.value.permissionType === PermissionType.ResourceBased)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadPermission() {
  void schemaPageRef.value?.reload()
}

watch(
  () => permissionForm.value.permissionType,
  (permissionType) => {
    if (permissionType !== PermissionType.ResourceBased) {
      permissionForm.value.resourceId = null
      permissionForm.value.operationId = null
    }
  },
)

function createDefaultForm(): PermissionFormModel {
  return {
    isRequireAudit: false,
    moduleCode: null,
    operationId: null,
    permissionCode: '',
    permissionDescription: null,
    permissionName: '',
    permissionType: PermissionType.ResourceBased,
    priority: 0,
    remark: null,
    resourceId: null,
    sort: 100,
    status: EnableStatus.Enabled,
    tags: null,
  }
}

function toBool(value: unknown) {
  return value === undefined || value === null ? undefined : Boolean(value)
}

function toStr(value: unknown) {
  return (value as string | undefined)?.trim() || undefined
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }

  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions.value, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions.value, value)
}

function canMaintainPermission(row: PermissionListItemDto) {
  return !row.isGlobal
}

// ── 字段单一事实源 ──────────────────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.permission.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.permission.keyword_placeholder'), width: 240, order: 0 },
  { key: 'moduleCode', title: t('identity.permission.col_module'), dataType: 'string', sortable: true, searchable: true, searchPlaceholder: t('identity.permission.module_placeholder'), minWidth: 110, order: 1 },
  { key: 'permissionName', title: t('identity.permission.col_permission_name'), dataType: 'string', sortable: true, minWidth: 160, order: 2 },
  { key: 'permissionCode', title: t('identity.permission.col_permission_code'), dataType: 'string', sortable: true, minWidth: 220, order: 3 },
  {
    key: 'permissionType',
    title: t('identity.permission.col_permission_type'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    dictionaryCode: 'PermissionType',
    options: permissionTypeOptions.value,
    searchPlaceholder: t('identity.permission.permission_type_placeholder'),
    minWidth: 110,
    order: 4,
  },
  { key: 'resourceName', title: t('identity.permission.col_resource'), dataType: 'string', minWidth: 150, order: 5 },
  { key: 'operationName', title: t('identity.permission.col_operation'), dataType: 'string', minWidth: 130, order: 6 },
  {
    key: 'isGlobal',
    title: t('identity.permission.col_is_global'),
    dataType: 'boolean',
    searchable: true,
    options: globalOptions.value,
    searchPlaceholder: t('identity.permission.is_global_placeholder'),
    width: 82,
    order: 7,
  },
  {
    key: 'isRequireAudit',
    title: t('identity.permission.col_is_audit'),
    dataType: 'boolean',
    searchable: true,
    options: auditOptions.value,
    searchPlaceholder: t('identity.permission.is_audit_placeholder'),
    width: 82,
    order: 8,
  },
  { key: 'priority', title: t('identity.permission.col_priority'), dataType: 'number', sortable: true, width: 90, order: 9 },
  { key: 'sort', title: t('identity.permission.col_sort'), dataType: 'number', sortable: true, width: 80, order: 10 },
  {
    key: 'status',
    title: t('identity.permission.col_status'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    dictionaryCode: 'EnableStatus',
    options: STATUS_OPTIONS,
    searchPlaceholder: t('identity.permission.status_placeholder'),
    width: 90,
    order: 11,
  },
  { key: 'createdTime', title: t('identity.permission.col_create_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, minWidth: 170, order: 12 },
])

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema = computed<PageSchema>(() => ({
  pageCode: 'system.permission',
  exportPermission: 'saas:permission:export',
  pageName: t('identity.permission.page_name'),
  batchRemovable: true,
  removePermission: 'saas:permission:delete',
  statusPermission: 'saas:permission:status',
  rowKey: 'basicId',
  scrollX: 2000,
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword, moduleCode, isGlobal, isRequireAudit } = params.filters
      return permissionCenterApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 区间(createdTime)/多选(permissionType、status) 统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isGlobal: toBool(isGlobal),
        isRequireAudit: toBool(isRequireAudit),
        keyword: toStr(keyword),
        moduleCode: toStr(moduleCode),
        // permissionType、status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => permissionCenterApi.delete(id),
    updateStatus: (id, enabled) => permissionCenterApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? t('identity.permission.batch_enable_remark') : t('identity.permission.batch_disable_remark') }),
  },
  actions: [
    { key: 'create', title: t('identity.permission.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('identity.permission.action_view'), scope: 'row' },
    { key: 'edit', title: t('identity.permission.action_edit'), scope: 'row', visible: row => canMaintainPermission(row as unknown as PermissionListItemDto) },
    { key: 'toggle', title: t('identity.permission.action_toggle'), scope: 'row', visible: row => canMaintainPermission(row as unknown as PermissionListItemDto) },
    { key: 'delete', title: t('identity.permission.action_delete'), scope: 'row', visible: row => canMaintainPermission(row as unknown as PermissionListItemDto) },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as PermissionListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

function handleAdd() {
  permissionForm.value = createDefaultForm()
  modalVisible.value = true
  void loadResourceOptions()
  void loadOperationOptions()
}

function handleEdit(row: PermissionListItemDto) {
  permissionForm.value = {
    basicId: row.basicId,
    isRequireAudit: row.isRequireAudit,
    moduleCode: row.moduleCode ?? null,
    operationId: row.operationId ?? null,
    permissionCode: row.permissionCode,
    permissionDescription: row.permissionDescription ?? null,
    permissionName: row.permissionName,
    permissionType: row.permissionType,
    priority: row.priority,
    remark: null,
    resourceId: row.resourceId ?? null,
    sort: row.sort,
    status: row.status,
    tags: null,
  }
  ensureResourceOption(row)
  ensureOperationOption(row)
  modalVisible.value = true
}

async function handleView(row: PermissionListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await permissionCenterApi.detailView(row.basicId)
    if (!currentDetail.value) {
      message.warning(t('identity.permission.msg_detail_not_found'))
    }
  }
  catch {
    message.error(t('identity.permission.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function loadResourceOptions(keyword = '') {
  resourceLoading.value = true
  try {
    const items = await permissionCenterApi.resources.availableGlobal({
      keyword: normalizeNullable(keyword),
      limit: 50,
    })
    resourceOptions.value = mergeOptions(resourceOptions.value, items.map(toResourceOption))
  }
  catch {
    message.error(t('identity.permission.msg_load_resource_failed'))
  }
  finally {
    resourceLoading.value = false
  }
}

async function loadOperationOptions(keyword = '') {
  operationLoading.value = true
  try {
    const items = await permissionCenterApi.operations.availableGlobal({
      keyword: normalizeNullable(keyword),
      limit: 50,
    })
    operationOptions.value = mergeOptions(operationOptions.value, items.map(toOperationOption))
  }
  catch {
    message.error(t('identity.permission.msg_load_operation_failed'))
  }
  finally {
    operationLoading.value = false
  }
}

function handleResourceSearch(keyword: string) {
  void loadResourceOptions(keyword)
}

function handleOperationSearch(keyword: string) {
  void loadOperationOptions(keyword)
}

function toResourceOption(item: ResourceSelectItemDto): NumericSelectOption {
  return {
    label: `${item.resourceName} (${item.resourceCode})`,
    value: item.basicId,
  }
}

function toOperationOption(item: OperationSelectItemDto): NumericSelectOption {
  return {
    label: `${item.operationName} (${item.operationCode})`,
    value: item.basicId,
  }
}

function mergeOptions(current: NumericSelectOption[], next: NumericSelectOption[]) {
  const optionMap = new Map<ApiId, NumericSelectOption>()

  for (const option of current) {
    optionMap.set(option.value, option)
  }

  for (const option of next) {
    optionMap.set(option.value, option)
  }

  return [...optionMap.values()]
}

function ensureResourceOption(row: PermissionListItemDto) {
  if (!row.resourceId || !row.resourceName) {
    return
  }

  resourceOptions.value = mergeOptions(resourceOptions.value, [
    {
      label: row.resourceCode ? `${row.resourceName} (${row.resourceCode})` : row.resourceName,
      value: row.resourceId,
    },
  ])
}

function ensureOperationOption(row: PermissionListItemDto) {
  if (!row.operationId || !row.operationName) {
    return
  }

  operationOptions.value = mergeOptions(operationOptions.value, [
    {
      label: row.operationCode ? `${row.operationName} (${row.operationCode})` : row.operationName,
      value: row.operationId,
    },
  ])
}

function normalizeTagsInput(value?: string | null) {
  const normalized = normalizeNullable(value)
  if (normalized === null) {
    return null
  }

  try {
    const parsed: unknown = JSON.parse(normalized)
    if (!Array.isArray(parsed)) {
      message.warning(t('identity.permission.msg_tags_must_json_array'))
      return undefined
    }
  }
  catch {
    message.warning(t('identity.permission.msg_tags_invalid_json'))
    return undefined
  }

  return normalized
}

function validateForm() {
  const form = permissionForm.value
  if (!form.permissionName.trim()) {
    message.warning(t('identity.permission.msg_permission_name_required'))
    return false
  }

  if (!form.basicId && !form.permissionCode.trim()) {
    message.warning(t('identity.permission.msg_permission_code_required'))
    return false
  }

  if (!form.basicId && form.permissionType === PermissionType.ResourceBased && (!form.resourceId || !form.operationId)) {
    message.warning(t('identity.permission.msg_resource_operation_required'))
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  const tags = normalizeTagsInput(permissionForm.value.tags)
  if (tags === undefined) {
    return
  }

  submitLoading.value = true
  try {
    if (permissionForm.value.basicId) {
      const updateInput: PermissionUpdateDto = {
        basicId: permissionForm.value.basicId,
        isRequireAudit: permissionForm.value.isRequireAudit,
        permissionDescription: normalizeNullable(permissionForm.value.permissionDescription),
        permissionName: permissionForm.value.permissionName.trim(),
        priority: permissionForm.value.priority,
        remark: normalizeNullable(permissionForm.value.remark),
        sort: permissionForm.value.sort,
        tags,
      }

      await permissionCenterApi.update(updateInput)
    }
    else {
      const createInput: PermissionCreateDto = {
        isRequireAudit: permissionForm.value.isRequireAudit,
        moduleCode: normalizeNullable(permissionForm.value.moduleCode),
        operationId: isResourceBasedForm.value ? permissionForm.value.operationId : null,
        permissionCode: permissionForm.value.permissionCode.trim(),
        permissionDescription: normalizeNullable(permissionForm.value.permissionDescription),
        permissionName: permissionForm.value.permissionName.trim(),
        permissionType: permissionForm.value.permissionType,
        priority: permissionForm.value.priority,
        remark: normalizeNullable(permissionForm.value.remark),
        resourceId: isResourceBasedForm.value ? permissionForm.value.resourceId : null,
        sort: permissionForm.value.sort,
        status: permissionForm.value.status,
        tags,
      }

      await permissionCenterApi.create(createInput)
    }

    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadPermission()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: PermissionListItemDto) {
  await permissionCenterApi.delete(row.basicId)
  message.success(t('common.messages.delete_success'))
  reloadPermission()
}

async function handleToggleStatus(row: PermissionListItemDto) {
  await permissionCenterApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('identity.permission.front_disable_remark') : t('identity.permission.front_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success(t('common.messages.status_updated'))
  reloadPermission()
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NDrawer v-model:show="detailVisible" :width="980">
      <NDrawerContent closable :title="t('identity.permission.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" :description="t('identity.permission.detail_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" :tab="t('identity.permission.tab_overview')">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem :label="t('identity.permission.label_permission_name')">
                    {{ currentDetail.permission.permissionName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_permission_code')">
                    {{ currentDetail.permission.permissionCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_module')">
                    {{ formatNullable(currentDetail.permission.moduleCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_permission_type')">
                    {{ getOptionLabel(permissionTypeOptions, currentDetail.permission.permissionType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_is_global')">
                    {{ formatBoolean(currentDetail.permission.isGlobal) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_need_audit')">
                    {{ formatBoolean(currentDetail.permission.isRequireAudit) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_priority')">
                    {{ currentDetail.permission.priority }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_status')">
                    {{ formatStatus(currentDetail.permission.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_resource')">
                    {{ formatNullable(currentDetail.resource?.resourceName || currentDetail.permission.resourceName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_resource_code')">
                    {{ formatNullable(currentDetail.resource?.resourceCode || currentDetail.permission.resourceCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_resource_type')">
                    {{ getOptionLabel(resourceTypeOptions, currentDetail.resource?.resourceType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_access_level')">
                    {{ getOptionLabel(resourceAccessLevelOptions, currentDetail.resource?.accessLevel) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_operation')">
                    {{ formatNullable(currentDetail.operation?.operationName || currentDetail.permission.operationName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_operation_code')">
                    {{ formatNullable(currentDetail.operation?.operationCode || currentDetail.permission.operationCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_http_method')">
                    {{ getOptionLabel(httpMethodOptions, currentDetail.operation?.httpMethod) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_operation_category')">
                    {{ getOptionLabel(operationCategoryOptions, currentDetail.operation?.category) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_operation_type')">
                    {{ getOptionLabel(operationTypeOptions, currentDetail.operation?.operationTypeCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_dangerous')">
                    {{ formatBoolean(currentDetail.operation?.isDangerous) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_permission_description')">
                    {{ formatNullable(currentDetail.permission.permissionDescription) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_tags')">
                    {{ formatNullable(currentDetail.permission.tags) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_create_time')">
                    {{ formatNullableDate(currentDetail.permission.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.permission.label_generated_time')">
                    {{ formatNullableDate(currentDetail.generatedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="conditions" :tab="t('identity.permission.tab_conditions', { count: currentDetail.conditions.length })">
                <table v-if="currentDetail.conditions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.permission.th_attribute') }}</th>
                      <th>{{ t('identity.permission.th_operator') }}</th>
                      <th>{{ t('identity.permission.th_value') }}</th>
                      <th>{{ t('identity.permission.th_value_type') }}</th>
                      <th>{{ t('identity.permission.th_group') }}</th>
                      <th>{{ t('identity.permission.th_status') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.conditions" :key="item.basicId">
                      <td>{{ item.attributeName }}</td>
                      <td>{{ item.isNegated ? t('identity.permission.negated_prefix') : '' }}{{ getOptionLabel(conditionOperatorOptions, item.operator) }}</td>
                      <td>{{ formatNullable(item.conditionValue) }}</td>
                      <td>{{ getOptionLabel(configDataTypeOptions, item.valueType) }}</td>
                      <td>{{ item.conditionGroup }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.permission.empty_conditions')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="delegations" :tab="t('identity.permission.tab_delegations', { count: currentDetail.delegations.length })">
                <table v-if="currentDetail.delegations.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.permission.th_delegator') }}</th>
                      <th>{{ t('identity.permission.th_delegatee') }}</th>
                      <th>{{ t('identity.permission.th_status') }}</th>
                      <th>{{ t('identity.permission.th_relation') }}</th>
                      <th>{{ t('identity.permission.th_expiration_time') }}</th>
                      <th>{{ t('identity.permission.th_reason') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.delegations" :key="item.basicId">
                      <td>{{ formatNullable(item.delegatorDisplayName || item.delegatorUserId) }}</td>
                      <td>{{ formatNullable(item.delegateeDisplayName || item.delegateeUserId) }}</td>
                      <td>{{ getOptionLabel(delegationStatusOptions, item.delegationStatus) }}</td>
                      <td>{{ formatNullable(item.permissionName || item.roleName) }}</td>
                      <td>{{ formatNullableDate(item.expirationTime) }}</td>
                      <td>{{ formatNullable(item.delegationReason) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.permission.empty_delegations')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="requests" :tab="t('identity.permission.tab_requests', { count: currentDetail.requests.length })">
                <table v-if="currentDetail.requests.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.permission.th_applicant') }}</th>
                      <th>{{ t('identity.permission.th_status') }}</th>
                      <th>{{ t('identity.permission.th_relation') }}</th>
                      <th>{{ t('identity.permission.th_approval_flow') }}</th>
                      <th>{{ t('identity.permission.th_expected_validity') }}</th>
                      <th>{{ t('identity.permission.th_reason') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.requests" :key="item.basicId">
                      <td>{{ formatNullable(item.requestUserDisplayName || item.requestUserId) }}</td>
                      <td>{{ getOptionLabel(requestStatusOptions, item.requestStatus) }}</td>
                      <td>{{ formatNullable(item.permissionName || item.roleName) }}</td>
                      <td>{{ formatNullable(item.reviewTitle || item.reviewCode) }}</td>
                      <td>{{ t('identity.permission.validity_range', { from: formatNullableDate(item.expectedEffectiveTime), to: formatNullableDate(item.expectedExpirationTime) }) }}</td>
                      <td>{{ formatNullable(item.requestReason) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.permission.empty_requests')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="fieldSecurities" :tab="t('identity.permission.tab_field_securities', { count: currentDetail.fieldSecurities.length })">
                <table v-if="currentDetail.fieldSecurities.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.permission.th_field') }}</th>
                      <th>{{ t('identity.permission.th_resource') }}</th>
                      <th>{{ t('identity.permission.th_target') }}</th>
                      <th>{{ t('identity.permission.th_readable') }}</th>
                      <th>{{ t('identity.permission.th_editable') }}</th>
                      <th>{{ t('identity.permission.th_mask') }}</th>
                      <th>{{ t('identity.permission.th_status') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.fieldSecurities" :key="item.basicId">
                      <td>{{ item.fieldName }}</td>
                      <td>{{ formatNullable(item.resourceName || item.resourceCode) }}</td>
                      <td>{{ getOptionLabel(fieldSecurityTargetTypeOptions, item.targetType) }} / {{ formatNullable(item.targetName || item.targetCode) }}</td>
                      <td>{{ formatBoolean(item.isReadable) }}</td>
                      <td>{{ formatBoolean(item.isEditable) }}</td>
                      <td>{{ getOptionLabel(fieldMaskStrategyOptions, item.maskStrategy) }}</td>
                      <td>{{ formatStatus(item.status) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.permission.empty_field_securities')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="changeLogs" :tab="t('identity.permission.tab_change_logs', { count: currentDetail.changeLogs.length })">
                <table v-if="currentDetail.changeLogs.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.permission.th_change_time') }}</th>
                      <th>{{ t('identity.permission.th_type') }}</th>
                      <th>{{ t('identity.permission.th_target_user') }}</th>
                      <th>{{ t('identity.permission.th_target_role') }}</th>
                      <th>{{ t('identity.permission.th_operator_user') }}</th>
                      <th>{{ t('identity.permission.th_reason') }}</th>
                      <th>{{ t('identity.permission.th_trace') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.changeLogs" :key="item.basicId">
                      <td>{{ formatNullableDate(item.changeTime) }}</td>
                      <td>{{ getOptionLabel(changeTypeOptions, item.changeType) }}</td>
                      <td>{{ formatNullable(item.targetUserName || item.targetUserId) }}</td>
                      <td>{{ formatNullable(item.targetRoleName || item.targetRoleId) }}</td>
                      <td>{{ formatNullable(item.operatorUserName || item.operatorUserId) }}</td>
                      <td>{{ formatNullable(item.changeReason || item.description) }}</td>
                      <td>{{ formatNullable(item.traceId) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.permission.empty_change_logs')" style="padding: 40px 0" />
              </NTabPane>
            </NTabs>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 760px; max-width: 92vw"
    >
      <NForm :model="permissionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('identity.permission.label_form_permission_name')" path="permissionName">
          <NInput v-model:value="permissionForm.permissionName" clearable size="small" :placeholder="t('identity.permission.ph_permission_name')" />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_permission_code')" path="permissionCode">
          <NInput
            v-model:value="permissionForm.permissionCode"
            :disabled="Boolean(permissionForm.basicId)"
            clearable size="small"
            :placeholder="t('identity.permission.ph_permission_code')"
          />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_module_code')" path="moduleCode">
          <NInput
            v-model:value="permissionForm.moduleCode"
            :disabled="Boolean(permissionForm.basicId)"
            clearable size="small"
            :placeholder="t('identity.permission.ph_module_code')"
          />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_permission_type')" path="permissionType">
          <NSelect
            v-model:value="permissionForm.permissionType"
            :disabled="Boolean(permissionForm.basicId)"
            :options="permissionTypeOptions"
          />
        </NFormItem>
        <NFormItem v-if="isResourceBasedForm" :label="t('identity.permission.label_form_resource')" path="resourceId">
          <NSelect
            v-model:value="permissionForm.resourceId"
            :disabled="Boolean(permissionForm.basicId)"
            :loading="resourceLoading"
            :options="resourceOptions"
            clearable size="small"
            filterable
            :placeholder="t('identity.permission.ph_resource')"
            remote
            @focus="loadResourceOptions()"
            @search="handleResourceSearch"
          />
        </NFormItem>
        <NFormItem v-if="isResourceBasedForm" :label="t('identity.permission.label_form_operation')" path="operationId">
          <NSelect
            v-model:value="permissionForm.operationId"
            :disabled="Boolean(permissionForm.basicId)"
            :loading="operationLoading"
            :options="operationOptions"
            clearable size="small"
            filterable
            :placeholder="t('identity.permission.ph_operation')"
            remote
            @focus="loadOperationOptions()"
            @search="handleOperationSearch"
          />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_need_audit')" path="isRequireAudit">
          <NSwitch v-model:value="permissionForm.isRequireAudit" />
        </NFormItem>
        <NFormItem v-if="!permissionForm.basicId" :label="t('identity.permission.label_form_status')" path="status">
          <NSelect v-model:value="permissionForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_priority')" path="priority">
          <NInputNumber v-model:value="permissionForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_sort')" path="sort">
          <NInputNumber v-model:value="permissionForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_tags_json')" path="tags">
          <NInput
            v-model:value="permissionForm.tags"
            clearable size="small"
            placeholder="[&quot;admin&quot;]"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_remark')" path="remark">
          <NInput v-model:value="permissionForm.remark" clearable size="small" :placeholder="t('identity.permission.ph_remark')" />
        </NFormItem>
        <NFormItem :label="t('identity.permission.label_form_description')" path="permissionDescription">
          <NInput
            v-model:value="permissionForm.permissionDescription"
            clearable size="small"
            :placeholder="t('identity.permission.ph_description')"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-detail-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.xh-detail-table th,
.xh-detail-table td {
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  text-align: left;
  vertical-align: top;
}

.xh-detail-table th {
  background: var(--n-merged-th-color);
  font-weight: 500;
}
</style>
