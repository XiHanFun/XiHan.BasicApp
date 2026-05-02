<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  PermissionSelectItemDto,
  TenantEditionListItemDto,
  TenantEditionPermissionGrantDto,
  TenantEditionPermissionListItemDto,
} from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { onMounted, reactive, ref } from 'vue'
import {
  EnableStatus,
  permissionApi,
  PermissionType,
  tenantEditionApi,
  tenantEditionPermissionApi,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemTenantEditionPermissionPage' })

interface TenantEditionPermissionGridResult {
  items: TenantEditionPermissionListItemDto[]
  total: number
}

interface NumericSelectOption {
  label: string
  value: ApiId
}

interface GrantFormModel {
  editionId: ApiId | null
  permissionId: ApiId | null
  remark: string | null
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<TenantEditionPermissionListItemDto>>()
const selectedEditionId = ref<ApiId | null>(null)
const editionLoading = ref(false)
const permissionLoading = ref(false)
const submitLoading = ref(false)
const modalVisible = ref(false)
const editionOptions = ref<NumericSelectOption[]>([])
const permissionOptions = ref<NumericSelectOption[]>([])
const grantForm = ref<GrantFormModel>(createDefaultGrantForm())

const queryParams = reactive({
  keyword: '',
  moduleCode: '',
  onlyValid: 0,
  permissionType: null as PermissionType | null,
  status: null as ValidityStatus | null,
})

const permissionFilter = reactive({
  keyword: '',
  moduleCode: '',
  permissionType: null as PermissionType | null,
})

const onlyValidOptions = [
  { label: '全部授权', value: 0 },
  { label: '仅有效', value: 1 },
]

const validityStatusOptions = [
  { label: '有效', value: ValidityStatus.Valid },
  { label: '无效', value: ValidityStatus.Invalid },
]

const permissionTypeOptions = [
  { label: '资源操作', value: PermissionType.ResourceBased },
  { label: '功能', value: PermissionType.Functional },
  { label: '数据范围', value: PermissionType.DataScope },
]

function createDefaultGrantForm(): GrantFormModel {
  return {
    editionId: selectedEditionId.value,
    permissionId: null,
    remark: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function toEditionOption(item: TenantEditionListItemDto): NumericSelectOption {
  return {
    label: `${item.editionName} (${item.editionCode})`,
    value: item.basicId,
  }
}

function toPermissionOption(item: PermissionSelectItemDto): NumericSelectOption {
  const moduleName = item.moduleCode ? `[${item.moduleCode}] ` : ''

  return {
    label: `${moduleName}${item.permissionName} (${item.permissionCode})`,
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

async function loadEditionOptions() {
  editionLoading.value = true
  try {
    const items = await tenantEditionApi.enabledList()
    editionOptions.value = items.map(toEditionOption)

    const firstEdition = items[0]
    if (selectedEditionId.value === null && firstEdition) {
      selectedEditionId.value = firstEdition.basicId
    }

    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('加载租户版本失败')
  }
  finally {
    editionLoading.value = false
  }
}

async function loadPermissionOptions(keyword = permissionFilter.keyword) {
  permissionLoading.value = true
  permissionFilter.keyword = keyword
  try {
    const items = await permissionApi.availableGlobal({
      keyword: normalizeNullable(keyword),
      limit: 50,
      moduleCode: normalizeNullable(permissionFilter.moduleCode),
      permissionType: permissionFilter.permissionType,
    })
    permissionOptions.value = mergeOptions(permissionOptions.value, items.map(toPermissionOption))
  }
  catch {
    message.error('加载权限选项失败')
  }
  finally {
    permissionLoading.value = false
  }
}

function includesKeyword(row: TenantEditionPermissionListItemDto, keyword: string) {
  const text = [
    row.permissionName,
    row.permissionCode,
    row.moduleCode,
    row.remark,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: TenantEditionPermissionListItemDto[]) {
  const keyword = normalizeNullable(queryParams.keyword)?.toLowerCase()
  const moduleCode = normalizeNullable(queryParams.moduleCode)?.toLowerCase()

  return rows.filter((row) => {
    if (keyword && !includesKeyword(row, keyword)) {
      return false
    }

    if (moduleCode && !row.moduleCode?.toLowerCase().includes(moduleCode)) {
      return false
    }

    if (queryParams.permissionType !== null && row.permissionType !== queryParams.permissionType) {
      return false
    }

    if (queryParams.status !== null && row.status !== queryParams.status) {
      return false
    }

    return true
  })
}

function pageRows(
  rows: TenantEditionPermissionListItemDto[],
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
): Promise<TenantEditionPermissionGridResult> {
  if (selectedEditionId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = await tenantEditionPermissionApi.list(selectedEditionId.value, queryParams.onlyValid === 1)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询版本权限失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<TenantEditionPermissionListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'permissionName', minWidth: 160, showOverflow: 'tooltip', title: '权限名称' },
      { field: 'permissionCode', minWidth: 220, showOverflow: 'tooltip', title: '权限编码' },
      { field: 'moduleCode', minWidth: 120, showOverflow: 'tooltip', title: '模块' },
      {
        field: 'permissionType',
        formatter: ({ cellValue }) => getOptionLabel(permissionTypeOptions, cellValue),
        minWidth: 110,
        title: '权限类型',
      },
      {
        field: 'permissionStatus',
        slots: { default: 'col_permission_status' },
        title: '权限状态',
        width: 96,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '授权状态',
        width: 96,
      },
      {
        field: 'isGlobalPermission',
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
      { field: 'remark', minWidth: 180, showOverflow: 'tooltip', title: '备注' },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '授权时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 88,
      },
    ],
    id: 'sys_tenant_edition_permission',
    name: '租户版本权限',
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

function handleEditionChanged() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.moduleCode = ''
  queryParams.onlyValid = 0
  queryParams.permissionType = null
  queryParams.status = null
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  if (selectedEditionId.value === null) {
    message.warning('请先选择租户版本')
    return
  }

  grantForm.value = createDefaultGrantForm()
  modalVisible.value = true
  void loadPermissionOptions()
}

function handlePermissionSearch(keyword: string) {
  void loadPermissionOptions(keyword)
}

function handleRefreshPermissionOptions() {
  permissionOptions.value = []
  void loadPermissionOptions()
}

function validateGrantForm() {
  if (grantForm.value.editionId === null) {
    message.warning('请选择租户版本')
    return false
  }

  if (grantForm.value.permissionId === null) {
    message.warning('请选择全局权限')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateGrantForm() || grantForm.value.editionId === null || grantForm.value.permissionId === null) {
    return
  }

  const input: TenantEditionPermissionGrantDto = {
    editionId: grantForm.value.editionId,
    permissionId: grantForm.value.permissionId,
    remark: normalizeNullable(grantForm.value.remark),
  }

  submitLoading.value = true
  try {
    await tenantEditionPermissionApi.grant(input)
    selectedEditionId.value = input.editionId
    message.success('授权成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('授权失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleToggleStatus(row: TenantEditionPermissionListItemDto) {
  const nextStatus = row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid

  if (nextStatus === ValidityStatus.Valid && row.permissionStatus === EnableStatus.Disabled) {
    message.warning('已禁用的权限不能设为有效授权')
    return
  }

  await tenantEditionPermissionApi.updateStatus({
    basicId: row.basicId,
    remark: nextStatus === ValidityStatus.Valid ? '前端启用版本权限' : '前端停用版本权限',
    status: nextStatus,
  })
  message.success('授权状态已更新')
  xGrid.value?.commitProxy('reload')
}

async function handleRevoke(row: TenantEditionPermissionListItemDto) {
  await tenantEditionPermissionApi.revoke(row.basicId)
  message.success('授权已撤销')
  xGrid.value?.commitProxy('reload')
}

onMounted(() => {
  void loadEditionOptions()
})
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="selectedEditionId"
          :loading="editionLoading"
          :options="editionOptions"
          filterable
          placeholder="选择租户版本"
          style="width: 240px"
          @update:value="handleEditionChanged"
        />
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索权限名称/编码"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <vxe-input
          v-model="queryParams.moduleCode"
          clearable
          placeholder="模块编码"
          style="width: 130px"
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
          v-model:value="queryParams.status"
          :options="validityStatusOptions"
          clearable
          placeholder="授权状态"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.onlyValid"
          :options="onlyValidOptions"
          placeholder="有效过滤"
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
              <NIcon><Icon icon="lucide:key-round" /></NIcon>
            </template>
            授权权限
          </NButton>
        </template>

        <template #col_permission_status="{ row }">
          <NTag
            :type="
              row.permissionStatus === EnableStatus.Enabled
                ? 'success'
                : row.permissionStatus === EnableStatus.Disabled
                  ? 'error'
                  : 'default'
            "
            round
            size="small"
          >
            {{
              row.permissionStatus === EnableStatus.Enabled
                ? '启用'
                : row.permissionStatus === EnableStatus.Disabled
                  ? '禁用'
                  : '-'
            }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === ValidityStatus.Valid ? 'success' : 'error'" round size="small">
            {{ row.status === ValidityStatus.Valid ? '有效' : '无效' }}
          </NTag>
        </template>

        <template #col_global="{ row }">
          <NTag :type="row.isGlobalPermission ? 'warning' : 'default'" round size="small">
            {{ row.isGlobalPermission ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_audit="{ row }">
          <NTag :type="row.isRequireAudit ? 'warning' : 'default'" round size="small">
            {{ row.isRequireAudit ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <!-- 操作列仅图标 -->
            <NPopconfirm @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton aria-label="停用或启用" circle quaternary size="small" type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === ValidityStatus.Valid ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                </NButton>
              </template>
              确认更新版本权限状态？
            </NPopconfirm>

            <NPopconfirm @positive-click="handleRevoke(row)">
              <template #trigger>
                <NButton aria-label="撤销" circle quaternary size="small" type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认撤销该版本权限？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 720px; max-width: 92vw"
      title="授权租户版本权限"
    >
      <NForm :model="grantForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="租户版本" path="editionId">
          <NSelect
            v-model:value="grantForm.editionId"
            :loading="editionLoading"
            :options="editionOptions"
            filterable
            placeholder="选择租户版本"
          />
        </NFormItem>
        <NFormItem label="模块过滤" path="moduleCode">
          <NInput
            v-model:value="permissionFilter.moduleCode"
            clearable
            placeholder="可输入模块编码"
            @keyup.enter="handleRefreshPermissionOptions"
          />
        </NFormItem>
        <NFormItem label="权限类型" path="permissionType">
          <NSelect
            v-model:value="permissionFilter.permissionType"
            :options="permissionTypeOptions"
            clearable
            placeholder="不限类型"
            @update:value="handleRefreshPermissionOptions"
          />
        </NFormItem>
        <NFormItem label="全局权限" path="permissionId">
          <NSelect
            v-model:value="grantForm.permissionId"
            :loading="permissionLoading"
            :options="permissionOptions"
            clearable
            filterable
            placeholder="搜索并选择全局权限"
            remote
            @focus="loadPermissionOptions()"
            @search="handlePermissionSearch"
          />
        </NFormItem>
        <NFormItem label="仅展示有效授权" path="onlyValid">
          <NSwitch v-model:value="queryParams.onlyValid" :checked-value="1" :unchecked-value="0" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="grantForm.remark"
            clearable
            placeholder="请输入授权备注"
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
            授权
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
