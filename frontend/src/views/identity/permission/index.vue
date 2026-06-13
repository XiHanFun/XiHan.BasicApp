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
import {
  createPageRequest,
  EnableStatus,
  permissionCenterApi,
  PermissionType,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import {
  CONDITION_OPERATOR_OPTIONS,
  CONFIG_DATA_TYPE_OPTIONS,
  DELEGATION_STATUS_OPTIONS,
  FIELD_MASK_STRATEGY_OPTIONS,
  FIELD_SECURITY_TARGET_TYPE_OPTIONS,
  HTTP_METHOD_OPTIONS,
  OPERATION_CATEGORY_OPTIONS,
  OPERATION_TYPE_OPTIONS,
  PERMISSION_CHANGE_TYPE_OPTIONS,
  PERMISSION_REQUEST_STATUS_OPTIONS,
  PERMISSION_TYPE_OPTIONS,
  RESOURCE_ACCESS_LEVEL_OPTIONS,
  RESOURCE_TYPE_OPTIONS,
  STATUS_OPTIONS,
  VALIDITY_STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemPermissionPage' })

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

const permissionTypeOptions = PERMISSION_TYPE_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS
const resourceTypeOptions = RESOURCE_TYPE_OPTIONS
const resourceAccessLevelOptions = RESOURCE_ACCESS_LEVEL_OPTIONS
const httpMethodOptions = HTTP_METHOD_OPTIONS
const operationCategoryOptions = OPERATION_CATEGORY_OPTIONS
const operationTypeOptions = OPERATION_TYPE_OPTIONS
const conditionOperatorOptions = CONDITION_OPERATOR_OPTIONS
const configDataTypeOptions = CONFIG_DATA_TYPE_OPTIONS
const delegationStatusOptions = DELEGATION_STATUS_OPTIONS
const requestStatusOptions = PERMISSION_REQUEST_STATUS_OPTIONS
const fieldMaskStrategyOptions = FIELD_MASK_STRATEGY_OPTIONS
const fieldSecurityTargetTypeOptions = FIELD_SECURITY_TARGET_TYPE_OPTIONS
const changeTypeOptions = PERMISSION_CHANGE_TYPE_OPTIONS

const globalOptions = [
  { label: '全局权限', value: 1 },
  { label: '租户权限', value: 0 },
]

const auditOptions = [
  { label: '需要审计', value: 1 },
  { label: '无需审计', value: 0 },
]

const modalTitle = computed(() => (permissionForm.value.basicId ? '编辑权限' : '新增权限'))
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

  return value ? '是' : '否'
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(STATUS_OPTIONS, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions, value)
}

function canMaintainPermission(row: PermissionListItemDto) {
  return !row.isGlobal
}

// ── 字段单一事实源 ──────────────────────────────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索权限名称/编码', width: 240, order: 0 },
  { key: 'moduleCode', title: '模块', dataType: 'string', searchable: true, searchPlaceholder: '模块编码', minWidth: 110, order: 1 },
  { key: 'permissionName', title: '权限名称', dataType: 'string', sortable: true, minWidth: 160, order: 2 },
  { key: 'permissionCode', title: '权限编码', dataType: 'string', minWidth: 220, order: 3 },
  {
    key: 'permissionType',
    title: '权限类型',
    dataType: 'enum',
    searchable: true,
    options: permissionTypeOptions,
    searchPlaceholder: '权限类型',
    minWidth: 110,
    order: 4,
  },
  { key: 'resourceName', title: '资源', dataType: 'string', minWidth: 150, order: 5 },
  { key: 'operationName', title: '操作', dataType: 'string', minWidth: 130, order: 6 },
  {
    key: 'isGlobal',
    title: '全局',
    dataType: 'boolean',
    searchable: true,
    options: globalOptions,
    searchPlaceholder: '全局',
    width: 82,
    order: 7,
  },
  {
    key: 'isRequireAudit',
    title: '审计',
    dataType: 'boolean',
    searchable: true,
    options: auditOptions,
    searchPlaceholder: '审计',
    width: 82,
    order: 8,
  },
  { key: 'priority', title: '优先级', dataType: 'number', sortable: true, width: 90, order: 9 },
  { key: 'sort', title: '排序', dataType: 'number', sortable: true, width: 80, order: 10 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: STATUS_OPTIONS,
    searchPlaceholder: '状态',
    width: 90,
    order: 11,
  },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 12 },
]

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema: PageSchema = {
  pageCode: 'system.permission',
  exportPermission: 'saas:permission:export',
  pageName: '权限管理',
  rowKey: 'basicId',
  scrollX: 2000,
  fields,
  resource: {
    page: (params) => {
      const { keyword, moduleCode, permissionType, isGlobal, isRequireAudit, status } = params.filters
      return permissionCenterApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        isGlobal: toBool(isGlobal),
        isRequireAudit: toBool(isRequireAudit),
        keyword: toStr(keyword),
        moduleCode: toStr(moduleCode),
        permissionType: permissionType as PermissionType | undefined,
        status: status as EnableStatus | undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => permissionCenterApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增权限', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'edit', title: '编辑', scope: 'row', visible: row => canMaintainPermission(row as unknown as PermissionListItemDto) },
    { key: 'toggle', title: '启用/停用', scope: 'row', visible: row => canMaintainPermission(row as unknown as PermissionListItemDto) },
    { key: 'delete', title: '删除', scope: 'row', visible: row => canMaintainPermission(row as unknown as PermissionListItemDto) },
  ],
}

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
      message.warning('未查询到权限详情')
    }
  }
  catch {
    message.error('加载权限详情失败')
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
    message.error('加载资源选项失败')
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
    message.error('加载操作选项失败')
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
      message.warning('权限标签必须是 JSON 数组')
      return undefined
    }
  }
  catch {
    message.warning('权限标签必须是合法 JSON 数组')
    return undefined
  }

  return normalized
}

function validateForm() {
  const form = permissionForm.value
  if (!form.permissionName.trim()) {
    message.warning('请输入权限名称')
    return false
  }

  if (!form.basicId && !form.permissionCode.trim()) {
    message.warning('请输入权限编码')
    return false
  }

  if (!form.basicId && form.permissionType === PermissionType.ResourceBased && (!form.resourceId || !form.operationId)) {
    message.warning('资源操作权限必须选择资源和操作')
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

    message.success('保存成功')
    modalVisible.value = false
    reloadPermission()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: PermissionListItemDto) {
  await permissionCenterApi.delete(row.basicId)
  message.success('删除成功')
  reloadPermission()
}

async function handleToggleStatus(row: PermissionListItemDto) {
  await permissionCenterApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用权限' : '前端启用权限',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
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
      <NDrawerContent closable title="权限详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无权限详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="权限名称">
                    {{ currentDetail.permission.permissionName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="权限编码">
                    {{ currentDetail.permission.permissionCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="模块">
                    {{ formatNullable(currentDetail.permission.moduleCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="权限类型">
                    {{ getOptionLabel(permissionTypeOptions, currentDetail.permission.permissionType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="全局权限">
                    {{ formatBoolean(currentDetail.permission.isGlobal) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="需要审计">
                    {{ formatBoolean(currentDetail.permission.isRequireAudit) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="优先级">
                    {{ currentDetail.permission.priority }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="状态">
                    {{ formatStatus(currentDetail.permission.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="资源">
                    {{ formatNullable(currentDetail.resource?.resourceName || currentDetail.permission.resourceName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="资源编码">
                    {{ formatNullable(currentDetail.resource?.resourceCode || currentDetail.permission.resourceCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="资源类型">
                    {{ getOptionLabel(resourceTypeOptions, currentDetail.resource?.resourceType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="访问级别">
                    {{ getOptionLabel(resourceAccessLevelOptions, currentDetail.resource?.accessLevel) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="操作">
                    {{ formatNullable(currentDetail.operation?.operationName || currentDetail.permission.operationName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="操作编码">
                    {{ formatNullable(currentDetail.operation?.operationCode || currentDetail.permission.operationCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="HTTP 方法">
                    {{ getOptionLabel(httpMethodOptions, currentDetail.operation?.httpMethod) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="操作分类">
                    {{ getOptionLabel(operationCategoryOptions, currentDetail.operation?.category) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="操作类型">
                    {{ getOptionLabel(operationTypeOptions, currentDetail.operation?.operationTypeCode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="危险操作">
                    {{ formatBoolean(currentDetail.operation?.isDangerous) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="权限描述">
                    {{ formatNullable(currentDetail.permission.permissionDescription) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="标签">
                    {{ formatNullable(currentDetail.permission.tags) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.permission.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="聚合时间">
                    {{ formatNullableDate(currentDetail.generatedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="conditions" :tab="`ABAC 条件 (${currentDetail.conditions.length})`">
                <table v-if="currentDetail.conditions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>属性</th>
                      <th>操作符</th>
                      <th>值</th>
                      <th>值类型</th>
                      <th>分组</th>
                      <th>状态</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.conditions" :key="item.basicId">
                      <td>{{ item.attributeName }}</td>
                      <td>{{ item.isNegated ? '非 ' : '' }}{{ getOptionLabel(conditionOperatorOptions, item.operator) }}</td>
                      <td>{{ formatNullable(item.conditionValue) }}</td>
                      <td>{{ getOptionLabel(configDataTypeOptions, item.valueType) }}</td>
                      <td>{{ item.conditionGroup }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无 ABAC 条件" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="delegations" :tab="`委托 (${currentDetail.delegations.length})`">
                <table v-if="currentDetail.delegations.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>委托人</th>
                      <th>接收人</th>
                      <th>状态</th>
                      <th>关联</th>
                      <th>到期时间</th>
                      <th>原因</th>
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
                <NEmpty v-else description="暂无权限委托" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="requests" :tab="`申请 (${currentDetail.requests.length})`">
                <table v-if="currentDetail.requests.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>申请人</th>
                      <th>状态</th>
                      <th>关联</th>
                      <th>审批流</th>
                      <th>期望有效期</th>
                      <th>原因</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.requests" :key="item.basicId">
                      <td>{{ formatNullable(item.requestUserDisplayName || item.requestUserId) }}</td>
                      <td>{{ getOptionLabel(requestStatusOptions, item.requestStatus) }}</td>
                      <td>{{ formatNullable(item.permissionName || item.roleName) }}</td>
                      <td>{{ formatNullable(item.reviewTitle || item.reviewCode) }}</td>
                      <td>{{ formatNullableDate(item.expectedEffectiveTime) }} 至 {{ formatNullableDate(item.expectedExpirationTime) }}</td>
                      <td>{{ formatNullable(item.requestReason) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无权限申请" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="fieldSecurities" :tab="`字段级安全 (${currentDetail.fieldSecurities.length})`">
                <table v-if="currentDetail.fieldSecurities.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>字段</th>
                      <th>资源</th>
                      <th>目标</th>
                      <th>可读</th>
                      <th>可编辑</th>
                      <th>脱敏</th>
                      <th>状态</th>
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
                <NEmpty v-else description="暂无字段级安全策略" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="changeLogs" :tab="`变更历史 (${currentDetail.changeLogs.length})`">
                <table v-if="currentDetail.changeLogs.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>变更时间</th>
                      <th>类型</th>
                      <th>目标用户</th>
                      <th>目标角色</th>
                      <th>操作人</th>
                      <th>原因</th>
                      <th>链路</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.changeLogs" :key="item.basicId">
                      <td>{{ formatNullableDate(item.changeTime) }}</td>
                      <td>{{ getOptionLabel(changeTypeOptions, item.changeType) }}</td>
                      <td>{{ formatNullable(item.targetUserId) }}</td>
                      <td>{{ formatNullable(item.targetRoleId) }}</td>
                      <td>{{ formatNullable(item.operatorUserId) }}</td>
                      <td>{{ formatNullable(item.changeReason || item.description) }}</td>
                      <td>{{ formatNullable(item.traceId) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无权限变更历史" style="padding: 40px 0" />
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
        <NFormItem label="权限名称" path="permissionName">
          <NInput v-model:value="permissionForm.permissionName" clearable size="small" placeholder="请输入权限名称" />
        </NFormItem>
        <NFormItem label="权限编码" path="permissionCode">
          <NInput
            v-model:value="permissionForm.permissionCode"
            :disabled="Boolean(permissionForm.basicId)"
            clearable size="small"
            placeholder="如: saas:user:read"
          />
        </NFormItem>
        <NFormItem label="模块编码" path="moduleCode">
          <NInput
            v-model:value="permissionForm.moduleCode"
            :disabled="Boolean(permissionForm.basicId)"
            clearable size="small"
            placeholder="默认取权限编码第一段"
          />
        </NFormItem>
        <NFormItem label="权限类型" path="permissionType">
          <NSelect
            v-model:value="permissionForm.permissionType"
            :disabled="Boolean(permissionForm.basicId)"
            :options="permissionTypeOptions"
          />
        </NFormItem>
        <NFormItem v-if="isResourceBasedForm" label="资源" path="resourceId">
          <NSelect
            v-model:value="permissionForm.resourceId"
            :disabled="Boolean(permissionForm.basicId)"
            :loading="resourceLoading"
            :options="resourceOptions"
            clearable size="small"
            filterable
            placeholder="搜索并选择资源"
            remote
            @focus="loadResourceOptions()"
            @search="handleResourceSearch"
          />
        </NFormItem>
        <NFormItem v-if="isResourceBasedForm" label="操作" path="operationId">
          <NSelect
            v-model:value="permissionForm.operationId"
            :disabled="Boolean(permissionForm.basicId)"
            :loading="operationLoading"
            :options="operationOptions"
            clearable size="small"
            filterable
            placeholder="搜索并选择操作"
            remote
            @focus="loadOperationOptions()"
            @search="handleOperationSearch"
          />
        </NFormItem>
        <NFormItem label="需要审计" path="isRequireAudit">
          <NSwitch v-model:value="permissionForm.isRequireAudit" />
        </NFormItem>
        <NFormItem v-if="!permissionForm.basicId" label="状态" path="status">
          <NSelect v-model:value="permissionForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="permissionForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="permissionForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="标签 JSON" path="tags">
          <NInput
            v-model:value="permissionForm.tags"
            clearable size="small"
            placeholder="[&quot;admin&quot;]"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="permissionForm.remark" clearable size="small" placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="描述" path="permissionDescription">
          <NInput
            v-model:value="permissionForm.permissionDescription"
            clearable size="small"
            placeholder="请输入权限描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
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
