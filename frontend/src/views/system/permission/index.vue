<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  OperationSelectItemDto,
  PermissionCreateDto,
  PermissionListItemDto,
  PermissionUpdateDto,
  ResourceSelectItemDto,
} from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref, watch } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  operationApi,
  permissionApi,
  PermissionType,
  resourceApi,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemPermissionPage' })

interface PermissionGridResult {
  items: PermissionListItemDto[]
  total: number
}

interface PermissionFormModel extends PermissionCreateDto {
  basicId?: ApiId
}

interface NumericSelectOption {
  label: string
  value: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<PermissionListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const resourceLoading = ref(false)
const operationLoading = ref(false)
const permissionForm = ref<PermissionFormModel>(createDefaultForm())
const resourceOptions = ref<NumericSelectOption[]>([])
const operationOptions = ref<NumericSelectOption[]>([])

const queryParams = reactive({
  isGlobal: undefined as number | undefined,
  isRequireAudit: undefined as number | undefined,
  keyword: '',
  moduleCode: '',
  permissionType: undefined as PermissionType | undefined,
  status: undefined as EnableStatus | undefined,
})

const permissionTypeOptions = [
  { label: '资源操作', value: PermissionType.ResourceBased },
  { label: '功能', value: PermissionType.Functional },
  { label: '数据范围', value: PermissionType.DataScope },
]

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

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function canMaintainPermission(row: PermissionListItemDto) {
  return !row.isGlobal
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<PermissionGridResult> {
  return permissionApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isGlobal: toOptionalBoolean(queryParams.isGlobal),
      isRequireAudit: toOptionalBoolean(queryParams.isRequireAudit),
      keyword: normalizeNullable(queryParams.keyword),
      moduleCode: normalizeNullable(queryParams.moduleCode),
      permissionType: queryParams.permissionType,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询权限失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<PermissionListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'permissionName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '权限名称' },
      { field: 'permissionCode', minWidth: 220, showOverflow: 'tooltip', title: '权限编码' },
      { field: 'moduleCode', minWidth: 110, showOverflow: 'tooltip', title: '模块' },
      {
        field: 'permissionType',
        formatter: ({ cellValue }) => getOptionLabel(permissionTypeOptions, cellValue),
        minWidth: 110,
        title: '权限类型',
      },
      { field: 'resourceName', minWidth: 150, showOverflow: 'tooltip', title: '资源' },
      { field: 'operationName', minWidth: 130, showOverflow: 'tooltip', title: '操作' },
      {
        field: 'isGlobal',
        slots: { default: 'col_global' },
        title: '全局',
        width: 82,
      },
      {
        field: 'isRequireAudit',
        slots: { default: 'col_audit' },
        title: '审计',
        width: 82,
      },
      { field: 'priority', minWidth: 80, sortable: true, title: '优先级' },
      { field: 'sort', minWidth: 80, sortable: true, title: '排序' },
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
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 180,
      },
    ],
    id: 'sys_permission',
    name: '权限管理',
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
  queryParams.moduleCode = ''
  queryParams.permissionType = undefined
  queryParams.isGlobal = undefined
  queryParams.isRequireAudit = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
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

async function loadResourceOptions(keyword = '') {
  resourceLoading.value = true
  try {
    const items = await resourceApi.availableGlobal({
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
    const items = await operationApi.availableGlobal({
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

      await permissionApi.update(updateInput)
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

      await permissionApi.create(createInput)
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

async function handleDelete(row: PermissionListItemDto) {
  await permissionApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: PermissionListItemDto) {
  await permissionApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用权限' : '前端启用权限',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索权限名称/编码"
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <vxe-input
          v-model="queryParams.moduleCode"
          clearable
          placeholder="模块编码"
          style="width: 140px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.permissionType"
          :options="permissionTypeOptions"
          clearable
          placeholder="权限类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.isRequireAudit"
          :options="auditOptions"
          clearable
          placeholder="审计"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.isGlobal"
          :options="globalOptions"
          clearable
          placeholder="全局"
          style="width: 110px"
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
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增权限
          </NButton>
        </template>

        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'warning' : 'default'" round size="small">
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_audit="{ row }">
          <NTag :type="row.isRequireAudit ? 'warning' : 'default'" round size="small">
            {{ row.isRequireAudit ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ row.status === EnableStatus.Enabled ? '启用' : '禁用' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton
              :disabled="!canMaintainPermission(row)"
              size="small"
              text
              type="primary"
              @click="handleEdit(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
              编辑
            </NButton>

            <NPopconfirm
              :disabled="!canMaintainPermission(row)"
              @positive-click="handleToggleStatus(row)"
            >
              <template #trigger>
                <NButton :disabled="!canMaintainPermission(row)" size="small" text type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === EnableStatus.Enabled ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新权限状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainPermission(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainPermission(row)" size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确认删除该权限？
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
      style="width: 760px; max-width: 92vw"
    >
      <NForm :model="permissionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="权限名称" path="permissionName">
          <NInput v-model:value="permissionForm.permissionName" clearable placeholder="请输入权限名称" />
        </NFormItem>
        <NFormItem label="权限编码" path="permissionCode">
          <NInput
            v-model:value="permissionForm.permissionCode"
            :disabled="Boolean(permissionForm.basicId)"
            clearable
            placeholder="如: saas:user:read"
          />
        </NFormItem>
        <NFormItem label="模块编码" path="moduleCode">
          <NInput
            v-model:value="permissionForm.moduleCode"
            :disabled="Boolean(permissionForm.basicId)"
            clearable
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
            clearable
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
            clearable
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
            clearable
            placeholder="[&quot;admin&quot;]"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="permissionForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="描述" path="permissionDescription">
          <NInput
            v-model:value="permissionForm.permissionDescription"
            clearable
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
  </div>
</template>
