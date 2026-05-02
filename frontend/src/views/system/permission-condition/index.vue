<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  PermissionConditionCreateDto,
  PermissionConditionListItemDto,
  PermissionConditionUpdateDto,
  RolePermissionListItemDto,
  RoleSelectItemDto,
  TenantMemberListItemDto,
  UserPermissionListItemDto,
} from '@/api'
import {
  NButton,
  NCheckbox,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  ConditionOperator,
  ConfigDataType,
  createPageRequest,
  EnableStatus,
  PermissionAction,
  permissionConditionApi,
  roleApi,
  rolePermissionApi,
  RoleType,
  tenantMemberApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  userPermissionApi,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemPermissionConditionPage' })

type BindingMode = 'rolePermission' | 'userPermission'

interface PermissionConditionGridResult {
  items: PermissionConditionListItemDto[]
  total: number
}

interface SelectOption {
  label: string
  value: ApiId
}

interface PermissionConditionFormModel {
  attributeName: string
  basicId?: ApiId
  conditionGroup: number
  conditionValue: string
  description?: string | null
  isNegated: boolean
  operator: ConditionOperator
  remark?: string | null
  rolePermissionId?: ApiId | null
  status: ValidityStatus
  userPermissionId?: ApiId | null
  valueType: ConfigDataType
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<PermissionConditionListItemDto>>()
const bindingMode = ref<BindingMode>('rolePermission')
const selectedRoleId = ref<ApiId | null>(null)
const selectedUserId = ref<ApiId | null>(null)
const selectedBindingId = ref<ApiId | null>(null)
const roleOptions = ref<SelectOption[]>([])
const userOptions = ref<SelectOption[]>([])
const bindingOptions = ref<SelectOption[]>([])
const roleLoading = ref(false)
const userLoading = ref(false)
const bindingLoading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<ValidityStatus | null>(null)
const bindingSummary = ref('')
const conditionForm = ref<PermissionConditionFormModel>(createDefaultForm())

const queryParams = reactive({
  keyword: '',
  onlyValid: false,
})

const roleFilter = reactive({
  keyword: '',
})

const userFilter = reactive({
  keyword: '',
})

const bindingModeOptions = [
  { label: '角色权限', value: 'rolePermission' },
  { label: '用户直授权限', value: 'userPermission' },
]

const operatorOptions = [
  { label: '等于', value: ConditionOperator.Equals },
  { label: '不等于', value: ConditionOperator.NotEquals },
  { label: '大于', value: ConditionOperator.GreaterThan },
  { label: '大于等于', value: ConditionOperator.GreaterThanOrEquals },
  { label: '小于', value: ConditionOperator.LessThan },
  { label: '小于等于', value: ConditionOperator.LessThanOrEquals },
  { label: '包含', value: ConditionOperator.Contains },
  { label: '不包含', value: ConditionOperator.NotContains },
  { label: '在集合中', value: ConditionOperator.In },
  { label: '不在集合中', value: ConditionOperator.NotIn },
  { label: '在范围内', value: ConditionOperator.Between },
  { label: '以...开头', value: ConditionOperator.StartsWith },
  { label: '以...结尾', value: ConditionOperator.EndsWith },
  { label: '为空', value: ConditionOperator.IsNull },
  { label: '不为空', value: ConditionOperator.IsNotNull },
]

const valueTypeOptions = [
  { label: '字符串', value: ConfigDataType.String },
  { label: '数字', value: ConfigDataType.Number },
  { label: '布尔值', value: ConfigDataType.Boolean },
  { label: 'JSON对象', value: ConfigDataType.Json },
  { label: '数组', value: ConfigDataType.Array },
]

const validityStatusOptions = [
  { label: '有效', value: ValidityStatus.Valid },
  { label: '无效', value: ValidityStatus.Invalid },
]

const permissionActionOptions = [
  { label: '授予', value: PermissionAction.Grant },
  { label: '拒绝', value: PermissionAction.Deny },
]

const modalTitle = computed(() => (conditionForm.value.basicId ? '编辑 ABAC 条件' : '新增 ABAC 条件'))
const selectedBindingLabel = computed(() => {
  const option = bindingOptions.value.find(item => item.value === selectedBindingId.value)
  return option?.label ?? ''
})

function createDefaultForm(): PermissionConditionFormModel {
  return {
    attributeName: '',
    conditionGroup: 0,
    conditionValue: '',
    description: null,
    isNegated: false,
    operator: ConditionOperator.Equals,
    remark: null,
    rolePermissionId: null,
    status: ValidityStatus.Valid,
    userPermissionId: null,
    valueType: ConfigDataType.String,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function mergeOptions(current: SelectOption[], next: SelectOption[]) {
  const optionMap = new Map<ApiId, SelectOption>()

  for (const option of current) {
    optionMap.set(option.value, option)
  }

  for (const option of next) {
    optionMap.set(option.value, option)
  }

  return [...optionMap.values()]
}

function toRoleOption(item: RoleSelectItemDto): SelectOption {
  return {
    label: `${item.roleName} (${item.roleCode})`,
    value: item.basicId,
  }
}

function toUserOption(item: TenantMemberListItemDto): SelectOption {
  return {
    label: `${item.displayName || `用户 ${item.userId}`} (${item.userId})`,
    value: item.userId,
  }
}

function toRolePermissionOption(item: RolePermissionListItemDto): SelectOption {
  const actionName = getOptionLabel(permissionActionOptions, item.permissionAction)
  return {
    label: `${item.permissionName || item.permissionCode || item.permissionId} [${actionName}]`,
    value: item.basicId,
  }
}

function toUserPermissionOption(item: UserPermissionListItemDto): SelectOption {
  const actionName = getOptionLabel(permissionActionOptions, item.permissionAction)
  return {
    label: `${item.permissionName || item.permissionCode || item.permissionId} [${actionName}]`,
    value: item.basicId,
  }
}

async function loadRoleOptions(keyword = roleFilter.keyword) {
  roleLoading.value = true
  roleFilter.keyword = keyword

  try {
    const items = await roleApi.enabledList({
      keyword: normalizeNullable(keyword),
      limit: 80,
    })
    const options = items
      .filter(item => !item.isGlobal && item.roleType !== RoleType.System)
      .map(toRoleOption)
    roleOptions.value = mergeOptions(roleOptions.value, options)

    if (selectedRoleId.value === null && options[0]) {
      selectedRoleId.value = options[0].value
      await loadBindingOptions()
    }
  }
  catch {
    message.error('加载角色选项失败')
  }
  finally {
    roleLoading.value = false
  }
}

async function loadUserOptions(keyword = userFilter.keyword) {
  userLoading.value = true
  userFilter.keyword = keyword

  try {
    const result = await tenantMemberApi.page({
      ...createPageRequest({
        page: {
          pageIndex: 1,
          pageSize: 80,
        },
      }),
      inviteStatus: TenantMemberInviteStatus.Accepted,
      keyword: normalizeNullable(keyword),
      status: ValidityStatus.Valid,
    })
    const options = result.items
      .filter(item => item.memberType !== TenantMemberType.PlatformAdmin)
      .map(toUserOption)
    userOptions.value = mergeOptions(userOptions.value, options)

    if (selectedUserId.value === null && options[0]) {
      selectedUserId.value = options[0].value
      await loadBindingOptions()
    }
  }
  catch {
    message.error('加载租户成员选项失败')
  }
  finally {
    userLoading.value = false
  }
}

async function loadBindingOptions() {
  bindingLoading.value = true

  try {
    const options = bindingMode.value === 'rolePermission'
      ? await loadRolePermissionBindingOptions()
      : await loadUserPermissionBindingOptions()
    bindingOptions.value = options

    if (!options.some(option => option.value === selectedBindingId.value)) {
      selectedBindingId.value = options[0]?.value ?? null
    }

    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('加载授权绑定失败')
  }
  finally {
    bindingLoading.value = false
  }
}

async function loadRolePermissionBindingOptions() {
  if (selectedRoleId.value === null) {
    return []
  }

  const items = await rolePermissionApi.list(selectedRoleId.value, true)
  return items
    .filter(item => item.status === ValidityStatus.Valid && item.permissionStatus === EnableStatus.Enabled)
    .map(toRolePermissionOption)
}

async function loadUserPermissionBindingOptions() {
  if (selectedUserId.value === null) {
    return []
  }

  const items = await userPermissionApi.list(selectedUserId.value, true)
  return items
    .filter(item => item.status === ValidityStatus.Valid && item.permissionStatus === EnableStatus.Enabled)
    .map(toUserPermissionOption)
}

function includesKeyword(row: PermissionConditionListItemDto, keyword: string) {
  const text = [
    row.attributeName,
    row.conditionValue,
    row.description,
    row.permissionCode,
    row.permissionName,
    row.remark,
    row.roleCode,
    row.roleName,
    row.userDisplayName,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: PermissionConditionListItemDto[]) {
  const keyword = normalizeNullable(queryParams.keyword)?.toLowerCase()

  return rows.filter((row) => {
    if (keyword && !includesKeyword(row, keyword)) {
      return false
    }

    return true
  })
}

function pageRows(rows: PermissionConditionListItemDto[], page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<PermissionConditionGridResult> {
  if (selectedBindingId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = bindingMode.value === 'rolePermission'
      ? await permissionConditionApi.rolePermissionConditions(selectedBindingId.value, queryParams.onlyValid)
      : await permissionConditionApi.userPermissionConditions(selectedBindingId.value, queryParams.onlyValid)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询 ABAC 条件失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<PermissionConditionListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'conditionGroup', minWidth: 90, sortable: true, title: '分组' },
      { field: 'attributeName', minWidth: 220, showOverflow: 'tooltip', title: '属性名称' },
      {
        field: 'operator',
        formatter: ({ cellValue }) => getOptionLabel(operatorOptions, cellValue),
        minWidth: 120,
        title: '操作符',
      },
      {
        field: 'isNegated',
        slots: { default: 'col_negated' },
        title: '取反',
        width: 82,
      },
      {
        field: 'valueType',
        formatter: ({ cellValue }) => getOptionLabel(valueTypeOptions, cellValue),
        minWidth: 110,
        title: '值类型',
      },
      { field: 'conditionValue', minWidth: 220, showOverflow: 'tooltip', title: '条件值' },
      { field: 'permissionName', minWidth: 160, showOverflow: 'tooltip', title: '权限名称' },
      { field: 'permissionCode', minWidth: 180, showOverflow: 'tooltip', title: '权限编码' },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 82,
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 190,
      },
    ],
    id: 'sys_permission_condition',
    name: '权限 ABAC 条件',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function handleModeChanged() {
  selectedBindingId.value = null
  bindingOptions.value = []
  xGrid.value?.commitProxy('reload')

  if (bindingMode.value === 'rolePermission') {
    void loadRoleOptions()
  }
  else {
    void loadUserOptions()
  }
}

function handleRoleChanged() {
  selectedBindingId.value = null
  void loadBindingOptions()
}

function handleUserChanged() {
  selectedBindingId.value = null
  void loadBindingOptions()
}

function handleBindingChanged() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.onlyValid = false
  xGrid.value?.commitProxy('reload')
}

function fillBindingFields(form: PermissionConditionFormModel) {
  form.rolePermissionId = bindingMode.value === 'rolePermission' ? selectedBindingId.value : null
  form.userPermissionId = bindingMode.value === 'userPermission' ? selectedBindingId.value : null
}

function handleAdd() {
  if (selectedBindingId.value === null) {
    message.warning('请先选择授权绑定')
    return
  }

  editingStatus.value = null
  const form = createDefaultForm()
  fillBindingFields(form)
  conditionForm.value = form
  bindingSummary.value = selectedBindingLabel.value
  modalVisible.value = true
}

async function handleEdit(row: PermissionConditionListItemDto) {
  const detail = await permissionConditionApi.detail(row.basicId)
  if (!detail) {
    message.error('ABAC 条件不存在')
    return
  }

  editingStatus.value = detail.status
  conditionForm.value = {
    attributeName: detail.attributeName,
    basicId: detail.basicId,
    conditionGroup: detail.conditionGroup,
    conditionValue: detail.conditionValue,
    description: detail.description,
    isNegated: detail.isNegated,
    operator: detail.operator,
    remark: detail.remark,
    rolePermissionId: detail.rolePermissionId,
    status: detail.status,
    userPermissionId: detail.userPermissionId,
    valueType: detail.valueType,
  }
  bindingSummary.value = detail.rolePermissionId
    ? `${detail.roleName || detail.roleId} / ${detail.permissionName || detail.permissionCode || detail.permissionId}`
    : `${detail.userDisplayName || detail.userId} / ${detail.permissionName || detail.permissionCode || detail.permissionId}`
  modalVisible.value = true
}

function hasKnownAttributePrefix(attributeName: string) {
  const normalized = attributeName.trim().toLowerCase()
  return normalized.startsWith('subject.')
    || normalized.startsWith('resource.')
    || normalized.startsWith('environment.')
}

function validateForm() {
  const hasRolePermission = Boolean(conditionForm.value.rolePermissionId)
  const hasUserPermission = Boolean(conditionForm.value.userPermissionId)
  if (hasRolePermission === hasUserPermission) {
    message.warning('ABAC 条件必须且只能绑定一种授权')
    return false
  }

  if (conditionForm.value.conditionGroup < 0) {
    message.warning('条件分组不能小于 0')
    return false
  }

  if (!conditionForm.value.attributeName.trim()) {
    message.warning('请输入属性名称')
    return false
  }

  if (!hasKnownAttributePrefix(conditionForm.value.attributeName)) {
    message.warning('属性名称必须使用 subject./resource./environment. 前缀')
    return false
  }

  if (!conditionForm.value.conditionValue.trim()) {
    message.warning('请输入条件值')
    return false
  }

  return true
}

function buildCommonInput() {
  return {
    attributeName: conditionForm.value.attributeName.trim(),
    conditionGroup: conditionForm.value.conditionGroup,
    conditionValue: conditionForm.value.conditionValue.trim(),
    description: normalizeNullable(conditionForm.value.description),
    isNegated: conditionForm.value.isNegated,
    operator: conditionForm.value.operator,
    remark: normalizeNullable(conditionForm.value.remark),
    rolePermissionId: conditionForm.value.rolePermissionId ?? null,
    userPermissionId: conditionForm.value.userPermissionId ?? null,
    valueType: conditionForm.value.valueType,
  }
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true
  try {
    if (conditionForm.value.basicId) {
      const updateInput: PermissionConditionUpdateDto = {
        basicId: conditionForm.value.basicId,
        ...buildCommonInput(),
      }

      await permissionConditionApi.update(updateInput)
      if (editingStatus.value !== conditionForm.value.status) {
        await permissionConditionApi.updateStatus({
          basicId: conditionForm.value.basicId,
          remark: normalizeNullable(conditionForm.value.remark),
          status: conditionForm.value.status,
        })
      }
    }
    else {
      const createInput: PermissionConditionCreateDto = {
        ...buildCommonInput(),
        status: conditionForm.value.status,
      }

      await permissionConditionApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleToggleStatus(row: PermissionConditionListItemDto) {
  await permissionConditionApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === ValidityStatus.Valid ? '前端停用 ABAC 条件' : '前端启用 ABAC 条件',
    status: row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
}

async function handleDelete(row: PermissionConditionListItemDto) {
  await permissionConditionApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

void loadRoleOptions()
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="bindingMode"
          :options="bindingModeOptions"
          placeholder="绑定类型"
          style="width: 130px"
          @update:value="handleModeChanged"
        />
        <NSelect
          v-if="bindingMode === 'rolePermission'"
          v-model:value="selectedRoleId"
          :loading="roleLoading"
          :options="roleOptions"
          filterable
          placeholder="选择角色"
          remote
          style="width: 220px"
          @search="loadRoleOptions"
          @update:value="handleRoleChanged"
        />
        <NSelect
          v-else
          v-model:value="selectedUserId"
          :loading="userLoading"
          :options="userOptions"
          filterable
          placeholder="选择租户成员"
          remote
          style="width: 220px"
          @search="loadUserOptions"
          @update:value="handleUserChanged"
        />
        <NSelect
          v-model:value="selectedBindingId"
          :loading="bindingLoading"
          :options="bindingOptions"
          clearable
          filterable
          placeholder="选择权限绑定"
          style="width: 260px"
          @update:value="handleBindingChanged"
        />
        <NCheckbox v-model:checked="queryParams.onlyValid" @update:checked="handleSearch">
          仅有效
        </NCheckbox>
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索属性/权限/条件值"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增条件
          </NButton>
        </template>

        <template #col_negated="{ row }">
          <NTag :type="row.isNegated ? 'warning' : 'default'" round size="small">
            {{ row.isNegated ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === ValidityStatus.Valid ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(validityStatusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" text type="primary" @click="handleEdit(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
              编辑
            </NButton>

            <NPopconfirm @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton size="small" text type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === ValidityStatus.Valid ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === ValidityStatus.Valid ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新 ABAC 条件状态？
            </NPopconfirm>

            <NPopconfirm @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确认删除该 ABAC 条件？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 820px; max-width: 94vw"
    >
      <NForm :model="conditionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="授权绑定">
          <NInput :value="bindingSummary" disabled />
        </NFormItem>
        <NFormItem label="条件分组" path="conditionGroup">
          <NInputNumber v-model:value="conditionForm.conditionGroup" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="属性名称" path="attributeName">
          <NInput
            v-model:value="conditionForm.attributeName"
            clearable
            placeholder="如: subject.department.id"
          />
        </NFormItem>
        <NFormItem label="操作符" path="operator">
          <NSelect v-model:value="conditionForm.operator" :options="operatorOptions" />
        </NFormItem>
        <NFormItem label="取反" path="isNegated">
          <NCheckbox v-model:checked="conditionForm.isNegated">
            取反
          </NCheckbox>
        </NFormItem>
        <NFormItem label="值类型" path="valueType">
          <NSelect v-model:value="conditionForm.valueType" :options="valueTypeOptions" />
        </NFormItem>
        <NFormItem label="条件值" path="conditionValue">
          <NInput
            v-model:value="conditionForm.conditionValue"
            clearable
            placeholder="请输入条件值"
          />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="conditionForm.status" :options="validityStatusOptions" />
        </NFormItem>
        <NFormItem label="说明" path="description">
          <NInput
            v-model:value="conditionForm.description"
            clearable
            placeholder="请输入条件说明"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="conditionForm.remark" clearable placeholder="请输入备注" />
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
  </div>
</template>
