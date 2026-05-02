<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  DepartmentTreeNodeDto,
  FieldLevelSecurityCreateDto,
  FieldLevelSecurityListItemDto,
  FieldLevelSecurityUpdateDto,
  PermissionListItemDto,
  ResourceListItemDto,
  RoleSelectItemDto,
  TenantMemberListItemDto,
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
  createPageRequest,
  departmentApi,
  EnableStatus,
  fieldLevelSecurityApi,
  FieldMaskStrategy,
  FieldSecurityTargetType,
  permissionApi,
  resourceApi,
  ResourceType,
  roleApi,
  RoleType,
  tenantMemberApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemFieldLevelSecurityPage' })

interface FieldLevelSecurityGridResult {
  items: FieldLevelSecurityListItemDto[]
  total: number
}

interface SelectOption {
  label: string
  value: ApiId
}

interface FieldLevelSecurityFormModel {
  basicId?: ApiId
  description?: string | null
  fieldName: string
  isEditable: boolean
  isReadable: boolean
  maskPattern?: string | null
  maskStrategy: FieldMaskStrategy
  priority: number
  remark?: string | null
  resourceId: ApiId | null
  status: EnableStatus
  targetId: ApiId | null
  targetType: FieldSecurityTargetType
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<FieldLevelSecurityListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const policyForm = ref<FieldLevelSecurityFormModel>(createDefaultForm())
const resourceOptions = ref<SelectOption[]>([])
const resourceLoading = ref(false)
const targetOptions = ref<SelectOption[]>([])
const targetLoading = ref(false)

const queryParams = reactive({
  keyword: '',
  maskStrategy: undefined as FieldMaskStrategy | undefined,
  resourceId: undefined as ApiId | undefined,
  status: undefined as EnableStatus | undefined,
  targetType: undefined as FieldSecurityTargetType | undefined,
})

const resourceFilter = reactive({
  keyword: '',
})

const targetFilter = reactive({
  keyword: '',
})

const targetTypeOptions = [
  { label: '角色', value: FieldSecurityTargetType.Role },
  { label: '用户', value: FieldSecurityTargetType.User },
  { label: '权限', value: FieldSecurityTargetType.Permission },
  { label: '部门', value: FieldSecurityTargetType.Department },
]

const maskStrategyOptions = [
  { label: '不脱敏', value: FieldMaskStrategy.None },
  { label: '完全隐藏', value: FieldMaskStrategy.Hidden },
  { label: '全部星号', value: FieldMaskStrategy.FullMask },
  { label: '部分脱敏', value: FieldMaskStrategy.PartialMask },
  { label: '哈希', value: FieldMaskStrategy.Hash },
  { label: '固定替换', value: FieldMaskStrategy.Redact },
  { label: '自定义', value: FieldMaskStrategy.Custom },
]

const resourceTypeOptions = [
  { label: 'API', value: ResourceType.Api },
  { label: '文件', value: ResourceType.File },
  { label: '数据表', value: ResourceType.DataTable },
  { label: '业务对象', value: ResourceType.BusinessObject },
  { label: '其他', value: ResourceType.Other },
]

const modalTitle = computed(() => (policyForm.value.basicId ? '编辑字段级安全' : '新增字段级安全'))
const maskPatternDisabled = computed(() => policyForm.value.maskStrategy === FieldMaskStrategy.None)

function createDefaultForm(): FieldLevelSecurityFormModel {
  return {
    description: null,
    fieldName: '',
    isEditable: true,
    isReadable: true,
    maskPattern: null,
    maskStrategy: FieldMaskStrategy.None,
    priority: 0,
    remark: null,
    resourceId: null,
    status: EnableStatus.Enabled,
    targetId: null,
    targetType: FieldSecurityTargetType.Role,
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

function ensureOption(options: SelectOption[], option: SelectOption | null) {
  if (!option) {
    return options
  }

  return mergeOptions(options, [option])
}

function toResourceOption(item: ResourceListItemDto): SelectOption {
  const typeName = getOptionLabel(resourceTypeOptions, item.resourceType)

  return {
    label: `[${typeName}] ${item.resourceName} (${item.resourceCode})`,
    value: item.basicId,
  }
}

function toRoleTargetOption(item: RoleSelectItemDto): SelectOption {
  return {
    label: `${item.roleName} (${item.roleCode})`,
    value: item.basicId,
  }
}

function toPermissionTargetOption(item: PermissionListItemDto): SelectOption {
  return {
    label: `${item.permissionName} (${item.permissionCode})`,
    value: item.basicId,
  }
}

function toTenantMemberTargetOption(item: TenantMemberListItemDto): SelectOption {
  return {
    label: `${item.displayName || `用户 ${item.userId}`} (${item.userId})`,
    value: item.userId,
  }
}

function collectDepartmentTargetOptions(nodes: DepartmentTreeNodeDto[], result: SelectOption[] = []) {
  for (const node of nodes) {
    result.push({
      label: `${node.departmentName} (${node.departmentCode})`,
      value: node.basicId,
    })

    if (node.children.length > 0) {
      collectDepartmentTargetOptions(node.children, result)
    }
  }

  return result
}

async function loadResourceOptions(keyword = resourceFilter.keyword) {
  resourceLoading.value = true
  resourceFilter.keyword = keyword

  try {
    const result = await resourceApi.page({
      ...createPageRequest({
        page: {
          pageIndex: 1,
          pageSize: 80,
        },
      }),
      keyword: normalizeNullable(keyword),
      status: EnableStatus.Enabled,
    })
    resourceOptions.value = mergeOptions(resourceOptions.value, result.items.map(toResourceOption))
  }
  catch {
    message.error('加载资源选项失败')
  }
  finally {
    resourceLoading.value = false
  }
}

async function loadRoleTargetOptions(keyword: string) {
  const items = await roleApi.enabledList({
    keyword: normalizeNullable(keyword),
    limit: 80,
  })

  return items
    .filter(item => !item.isGlobal && item.roleType !== RoleType.System)
    .map(toRoleTargetOption)
}

async function loadPermissionTargetOptions(keyword: string) {
  const result = await permissionApi.page({
    ...createPageRequest({
      page: {
        pageIndex: 1,
        pageSize: 80,
      },
    }),
    keyword: normalizeNullable(keyword),
    status: EnableStatus.Enabled,
  })

  return result.items.map(toPermissionTargetOption)
}

async function loadTenantMemberTargetOptions(keyword: string) {
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

  return result.items
    .filter(item => item.memberType !== TenantMemberType.PlatformAdmin)
    .map(toTenantMemberTargetOption)
}

async function loadDepartmentTargetOptions(keyword: string) {
  const nodes = await departmentApi.tree({
    keyword: normalizeNullable(keyword),
    limit: 120,
    onlyEnabled: true,
  })

  return collectDepartmentTargetOptions(nodes)
}

async function loadTargetOptions(keyword = targetFilter.keyword) {
  targetLoading.value = true
  targetFilter.keyword = keyword

  try {
    const options = policyForm.value.targetType === FieldSecurityTargetType.Role
      ? await loadRoleTargetOptions(keyword)
      : policyForm.value.targetType === FieldSecurityTargetType.Permission
        ? await loadPermissionTargetOptions(keyword)
        : policyForm.value.targetType === FieldSecurityTargetType.Department
          ? await loadDepartmentTargetOptions(keyword)
          : await loadTenantMemberTargetOptions(keyword)

    targetOptions.value = mergeOptions(targetOptions.value, options)
  }
  catch {
    message.error('加载目标选项失败')
  }
  finally {
    targetLoading.value = false
  }
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<FieldLevelSecurityGridResult> {
  return fieldLevelSecurityApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      keyword: normalizeNullable(queryParams.keyword),
      maskStrategy: queryParams.maskStrategy,
      resourceId: queryParams.resourceId,
      status: queryParams.status,
      targetType: queryParams.targetType,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询字段级安全失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<FieldLevelSecurityListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'fieldName', fixed: 'left', minWidth: 150, showOverflow: 'tooltip', title: '字段名' },
      {
        field: 'targetType',
        formatter: ({ cellValue }) => getOptionLabel(targetTypeOptions, cellValue),
        minWidth: 100,
        title: '目标类型',
      },
      { field: 'targetName', minWidth: 160, showOverflow: 'tooltip', title: '目标名称' },
      { field: 'targetCode', minWidth: 180, showOverflow: 'tooltip', title: '目标编码' },
      { field: 'resourceName', minWidth: 160, showOverflow: 'tooltip', title: '资源名称' },
      { field: 'resourceCode', minWidth: 180, showOverflow: 'tooltip', title: '资源编码' },
      {
        field: 'resourceType',
        formatter: ({ cellValue }) => getOptionLabel(resourceTypeOptions, cellValue),
        minWidth: 110,
        title: '资源类型',
      },
      {
        field: 'isReadable',
        slots: { default: 'col_readable' },
        title: '可读',
        width: 82,
      },
      {
        field: 'isEditable',
        slots: { default: 'col_editable' },
        title: '可编辑',
        width: 90,
      },
      {
        field: 'maskStrategy',
        formatter: ({ cellValue }) => getOptionLabel(maskStrategyOptions, cellValue),
        minWidth: 110,
        title: '脱敏策略',
      },
      { field: 'priority', minWidth: 90, sortable: true, title: '优先级' },
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
    id: 'sys_field_level_security',
    name: '字段级安全',
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

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.maskStrategy = undefined
  queryParams.resourceId = undefined
  queryParams.status = undefined
  queryParams.targetType = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  editingStatus.value = null
  policyForm.value = createDefaultForm()
  modalVisible.value = true
  void loadResourceOptions()
  void loadTargetOptions()
}

async function handleEdit(row: FieldLevelSecurityListItemDto) {
  const detail = await fieldLevelSecurityApi.detail(row.basicId)
  if (!detail) {
    message.error('字段级安全策略不存在')
    return
  }

  editingStatus.value = detail.status
  policyForm.value = {
    basicId: detail.basicId,
    description: detail.description,
    fieldName: detail.fieldName,
    isEditable: detail.isEditable,
    isReadable: detail.isReadable,
    maskPattern: detail.maskPattern,
    maskStrategy: detail.maskStrategy,
    priority: detail.priority,
    remark: detail.remark,
    resourceId: detail.resourceId,
    status: detail.status,
    targetId: detail.targetId,
    targetType: detail.targetType,
  }
  resourceOptions.value = ensureOption(resourceOptions.value, {
    label: `${detail.resourceName || detail.resourceCode || detail.resourceId}`,
    value: detail.resourceId,
  })
  targetOptions.value = ensureOption(targetOptions.value, {
    label: `${detail.targetName || detail.targetCode || detail.targetId}`,
    value: detail.targetId,
  })
  modalVisible.value = true
  void loadResourceOptions()
  void loadTargetOptions()
}

function handleTargetTypeChanged() {
  policyForm.value.targetId = null
  targetOptions.value = []
  targetFilter.keyword = ''
  void loadTargetOptions()
}

function handleReadableChanged(value: boolean) {
  if (!value) {
    policyForm.value.isEditable = false
    if (policyForm.value.maskStrategy === FieldMaskStrategy.None) {
      policyForm.value.maskStrategy = FieldMaskStrategy.Hidden
    }
  }
}

function handleMaskStrategyChanged(value: FieldMaskStrategy) {
  if (value === FieldMaskStrategy.None) {
    policyForm.value.maskPattern = null
  }
}

function validateForm() {
  if (policyForm.value.resourceId === null) {
    message.warning('请选择资源')
    return false
  }

  if (policyForm.value.targetId === null) {
    message.warning('请选择目标')
    return false
  }

  if (!policyForm.value.fieldName.trim()) {
    message.warning('请输入字段名')
    return false
  }

  if (!policyForm.value.isReadable && policyForm.value.isEditable) {
    message.warning('不可读字段不能设置为可编辑')
    return false
  }

  if (!policyForm.value.isReadable && policyForm.value.maskStrategy === FieldMaskStrategy.None) {
    message.warning('不可读字段必须指定脱敏策略')
    return false
  }

  if (policyForm.value.priority < 0) {
    message.warning('优先级不能小于 0')
    return false
  }

  return true
}

function buildCommonInput() {
  if (policyForm.value.resourceId === null || policyForm.value.targetId === null) {
    throw new Error('字段级安全策略资源或目标为空')
  }

  return {
    description: normalizeNullable(policyForm.value.description),
    fieldName: policyForm.value.fieldName.trim(),
    isEditable: policyForm.value.isEditable,
    isReadable: policyForm.value.isReadable,
    maskPattern: policyForm.value.maskStrategy === FieldMaskStrategy.None
      ? null
      : normalizeNullable(policyForm.value.maskPattern),
    maskStrategy: policyForm.value.maskStrategy,
    priority: policyForm.value.priority,
    remark: normalizeNullable(policyForm.value.remark),
    resourceId: policyForm.value.resourceId,
    targetId: policyForm.value.targetId,
    targetType: policyForm.value.targetType,
  }
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true
  try {
    if (policyForm.value.basicId) {
      const updateInput: FieldLevelSecurityUpdateDto = {
        basicId: policyForm.value.basicId,
        ...buildCommonInput(),
      }

      await fieldLevelSecurityApi.update(updateInput)
      if (editingStatus.value !== policyForm.value.status) {
        await fieldLevelSecurityApi.updateStatus({
          basicId: policyForm.value.basicId,
          remark: normalizeNullable(policyForm.value.remark),
          status: policyForm.value.status,
        })
      }
    }
    else {
      const createInput: FieldLevelSecurityCreateDto = {
        ...buildCommonInput(),
        status: policyForm.value.status,
      }

      await fieldLevelSecurityApi.create(createInput)
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

async function handleToggleStatus(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用字段级安全策略' : '前端启用字段级安全策略',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
}

async function handleDelete(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

void loadResourceOptions()
void loadTargetOptions()
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索字段/描述/备注"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.targetType"
          :options="targetTypeOptions"
          clearable
          placeholder="目标类型"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.resourceId"
          :loading="resourceLoading"
          :options="resourceOptions"
          clearable
          filterable
          placeholder="资源"
          remote
          style="width: 220px"
          @search="loadResourceOptions"
        />
        <NSelect
          v-model:value="queryParams.maskStrategy"
          :options="maskStrategyOptions"
          clearable
          placeholder="脱敏策略"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          clearable
          placeholder="状态"
          style="width: 110px"
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
              <NIcon><Icon icon="lucide:shield-plus" /></NIcon>
            </template>
            新增策略
          </NButton>
        </template>

        <template #col_readable="{ row }">
          <NTag :type="row.isReadable ? 'success' : 'error'" round size="small">
            {{ row.isReadable ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_editable="{ row }">
          <NTag :type="row.isEditable ? 'success' : 'default'" round size="small">
            {{ row.isEditable ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ row.status === EnableStatus.Enabled ? '启用' : '禁用' }}
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
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === EnableStatus.Enabled ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新字段级安全策略状态？
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
              确认删除该字段级安全策略？
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
      <NForm :model="policyForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="资源" path="resourceId">
          <NSelect
            v-model:value="policyForm.resourceId"
            :loading="resourceLoading"
            :options="resourceOptions"
            filterable
            placeholder="搜索并选择资源"
            remote
            @focus="loadResourceOptions()"
            @search="loadResourceOptions"
          />
        </NFormItem>
        <NFormItem label="目标类型" path="targetType">
          <NSelect
            v-model:value="policyForm.targetType"
            :options="targetTypeOptions"
            @update:value="handleTargetTypeChanged"
          />
        </NFormItem>
        <NFormItem label="目标" path="targetId">
          <NSelect
            v-model:value="policyForm.targetId"
            :loading="targetLoading"
            :options="targetOptions"
            filterable
            placeholder="搜索并选择目标"
            remote
            @focus="loadTargetOptions()"
            @search="loadTargetOptions"
          />
        </NFormItem>
        <NFormItem label="字段名" path="fieldName">
          <NInput v-model:value="policyForm.fieldName" clearable placeholder="如: phoneNumber" />
        </NFormItem>
        <NFormItem label="可读" path="isReadable">
          <NCheckbox v-model:checked="policyForm.isReadable" @update:checked="handleReadableChanged">
            可读
          </NCheckbox>
        </NFormItem>
        <NFormItem label="可编辑" path="isEditable">
          <NCheckbox v-model:checked="policyForm.isEditable" :disabled="!policyForm.isReadable">
            可编辑
          </NCheckbox>
        </NFormItem>
        <NFormItem label="脱敏策略" path="maskStrategy">
          <NSelect
            v-model:value="policyForm.maskStrategy"
            :options="maskStrategyOptions"
            @update:value="handleMaskStrategyChanged"
          />
        </NFormItem>
        <NFormItem label="脱敏模式" path="maskPattern">
          <NInput
            v-model:value="policyForm.maskPattern"
            clearable
            :disabled="maskPatternDisabled"
            placeholder="如: {&quot;keepLeft&quot;:3,&quot;keepRight&quot;:4}"
          />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="policyForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="policyForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="policyForm.description"
            clearable
            placeholder="请输入策略描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="policyForm.remark" clearable placeholder="请输入备注" />
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
