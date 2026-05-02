<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  DateTimeString,
  DepartmentTreeNodeDto,
  RoleDataScopeGrantDto,
  RoleDataScopeListItemDto,
  RoleDataScopeUpdateDto,
  RoleListItemDto,
} from '@/api'
import {
  NButton,
  NCheckbox,
  NDatePicker,
  NForm,
  NFormItem,
  NIcon,
  NInput,
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
  DataPermissionScope,
  departmentApi,
  DepartmentType,
  EnableStatus,
  roleApi,
  roleDataScopeApi,
  RoleType,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemRoleDataScopePage' })

interface RoleDataScopeGridResult {
  items: RoleDataScopeListItemDto[]
  total: number
}

interface NumericSelectOption {
  label: string
  value: ApiId
}

interface RoleOption extends NumericSelectOption {
  dataScope: DataPermissionScope
  isGlobal: boolean
  roleType: RoleType
}

interface RoleDataScopeFormModel {
  basicId?: ApiId
  departmentId: ApiId | null
  effectiveTime: DateTimeString | null
  expirationTime: DateTimeString | null
  includeChildren: boolean
  remark: string | null
  roleId: ApiId | null
  status: ValidityStatus
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<RoleDataScopeListItemDto>>()
const selectedRoleId = ref<ApiId | null>(null)
const roleOptions = ref<RoleOption[]>([])
const departmentOptions = ref<NumericSelectOption[]>([])
const roleLoading = ref(false)
const departmentLoading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<ValidityStatus | null>(null)
const dataScopeForm = ref<RoleDataScopeFormModel>(createDefaultForm())

const queryParams = reactive({
  keyword: '',
  onlyValid: 0,
  status: null as ValidityStatus | null,
})

const roleFilter = reactive({
  keyword: '',
})

const departmentFilter = reactive({
  keyword: '',
})

const onlyValidOptions = [
  { label: '全部范围', value: 0 },
  { label: '仅有效', value: 1 },
]

const departmentTypeOptions = [
  { label: '集团', value: DepartmentType.Corporation },
  { label: '总部', value: DepartmentType.Headquarters },
  { label: '公司', value: DepartmentType.Company },
  { label: '分公司', value: DepartmentType.Branch },
  { label: '事业部', value: DepartmentType.Division },
  { label: '中心', value: DepartmentType.Center },
  { label: '部门', value: DepartmentType.Department },
  { label: '科室', value: DepartmentType.Section },
  { label: '团队', value: DepartmentType.Team },
  { label: '小组', value: DepartmentType.Group },
  { label: '项目组', value: DepartmentType.Project },
  { label: '工作组', value: DepartmentType.Workgroup },
  { label: '虚拟组织', value: DepartmentType.Virtual },
  { label: '办事处', value: DepartmentType.Office },
  { label: '子公司', value: DepartmentType.Subsidiary },
  { label: '其他', value: DepartmentType.Other },
]

const validityStatusOptions = [
  { label: '有效', value: ValidityStatus.Valid },
  { label: '无效', value: ValidityStatus.Invalid },
]

const modalTitle = computed(() => (dataScopeForm.value.basicId ? '编辑角色数据范围' : '授予角色数据范围'))

function createDefaultForm(): RoleDataScopeFormModel {
  return {
    departmentId: null,
    effectiveTime: null,
    expirationTime: null,
    includeChildren: false,
    remark: null,
    roleId: selectedRoleId.value,
    status: ValidityStatus.Valid,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function toDateInputValue(value?: DateTimeString | null) {
  return value ? value.slice(0, 10) : null
}

function toRoleOption(item: RoleListItemDto): RoleOption {
  const scopeName = item.isGlobal ? '全局' : '租户'

  return {
    dataScope: item.dataScope,
    isGlobal: item.isGlobal,
    label: `[${scopeName}] ${item.roleName} (${item.roleCode})`,
    roleType: item.roleType,
    value: item.basicId,
  }
}

function flattenDepartmentOptions(nodes: DepartmentTreeNodeDto[], level = 0): NumericSelectOption[] {
  return nodes.flatMap((node) => {
    const prefix = level > 0 ? `${'  '.repeat(level)}- ` : ''
    const current = {
      label: `${prefix}${node.departmentName} (${node.departmentCode})`,
      value: node.basicId,
    }

    return [current, ...flattenDepartmentOptions(node.children ?? [], level + 1)]
  })
}

function mergeOptions<TOption extends NumericSelectOption>(current: TOption[], next: TOption[]) {
  const optionMap = new Map<ApiId, TOption>()

  for (const option of current) {
    optionMap.set(option.value, option)
  }

  for (const option of next) {
    optionMap.set(option.value, option)
  }

  return [...optionMap.values()]
}

async function loadRoleOptions(keyword = roleFilter.keyword) {
  roleLoading.value = true
  roleFilter.keyword = keyword
  try {
    const result = await roleApi.page({
      ...createPageRequest({
        page: {
          pageIndex: 1,
          pageSize: 50,
        },
      }),
      dataScope: DataPermissionScope.Custom,
      keyword: normalizeNullable(keyword),
      status: EnableStatus.Enabled,
    })
    const options = result.items
      .filter(item => !item.isGlobal && item.roleType !== RoleType.System)
      .map(toRoleOption)

    roleOptions.value = mergeOptions(roleOptions.value, options)

    const firstRole = options[0]
    if (selectedRoleId.value === null && firstRole) {
      selectedRoleId.value = firstRole.value
      xGrid.value?.commitProxy('reload')
    }
  }
  catch {
    message.error('加载角色选项失败')
  }
  finally {
    roleLoading.value = false
  }
}

async function loadDepartmentOptions(keyword = departmentFilter.keyword) {
  departmentLoading.value = true
  departmentFilter.keyword = keyword
  try {
    const tree = await departmentApi.tree({
      keyword: normalizeNullable(keyword),
      limit: 2000,
      onlyEnabled: true,
    })
    departmentOptions.value = mergeOptions(departmentOptions.value, flattenDepartmentOptions(tree))
  }
  catch {
    message.error('加载部门选项失败')
  }
  finally {
    departmentLoading.value = false
  }
}

function includesKeyword(row: RoleDataScopeListItemDto, keyword: string) {
  const text = [
    row.departmentName,
    row.departmentCode,
    row.remark,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: RoleDataScopeListItemDto[]) {
  const keyword = normalizeNullable(queryParams.keyword)?.toLowerCase()

  return rows.filter((row) => {
    if (keyword && !includesKeyword(row, keyword)) {
      return false
    }

    if (queryParams.status !== null && row.status !== queryParams.status) {
      return false
    }

    return true
  })
}

function pageRows(rows: RoleDataScopeListItemDto[], page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<RoleDataScopeGridResult> {
  if (selectedRoleId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = await roleDataScopeApi.list(selectedRoleId.value, queryParams.onlyValid === 1)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询角色数据范围失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<RoleDataScopeListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'departmentName', minWidth: 160, showOverflow: 'tooltip', title: '部门名称' },
      { field: 'departmentCode', minWidth: 160, showOverflow: 'tooltip', title: '部门编码' },
      {
        field: 'departmentType',
        formatter: ({ cellValue }) => getOptionLabel(departmentTypeOptions, cellValue),
        minWidth: 110,
        title: '部门类型',
      },
      {
        field: 'departmentStatus',
        slots: { default: 'col_department_status' },
        title: '部门状态',
        width: 100,
      },
      {
        field: 'includeChildren',
        slots: { default: 'col_include_children' },
        title: '含下级',
        width: 82,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '绑定状态',
        width: 100,
      },
      {
        field: 'effectiveTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '生效时间',
      },
      {
        field: 'expirationTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '失效时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
      { field: 'remark', minWidth: 180, showOverflow: 'tooltip', title: '备注' },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 220,
      },
    ],
    id: 'sys_role_data_scope',
    name: '角色数据范围',
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

function handleRoleChanged() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.onlyValid = 0
  queryParams.status = null
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  if (selectedRoleId.value === null) {
    message.warning('请先选择自定义数据范围角色')
    return
  }

  dataScopeForm.value = createDefaultForm()
  editingStatus.value = null
  modalVisible.value = true
  void loadDepartmentOptions()
}

function handleEdit(row: RoleDataScopeListItemDto) {
  dataScopeForm.value = {
    basicId: row.basicId,
    departmentId: row.departmentId,
    effectiveTime: toDateInputValue(row.effectiveTime),
    expirationTime: toDateInputValue(row.expirationTime),
    includeChildren: row.includeChildren,
    remark: row.remark ?? null,
    roleId: row.roleId,
    status: row.status,
  }
  editingStatus.value = row.status
  departmentOptions.value = mergeOptions(departmentOptions.value, [
    {
      label: row.departmentCode
        ? `${row.departmentName || row.departmentCode} (${row.departmentCode})`
        : String(row.departmentId),
      value: row.departmentId,
    },
  ])
  modalVisible.value = true
}

function handleRoleSearch(keyword: string) {
  void loadRoleOptions(keyword)
}

function handleDepartmentSearch(keyword: string) {
  void loadDepartmentOptions(keyword)
}

function validateForm() {
  if (dataScopeForm.value.roleId === null) {
    message.warning('请选择自定义数据范围角色')
    return false
  }

  const selectedRole = roleOptions.value.find(option => option.value === dataScopeForm.value.roleId)
  if (selectedRole?.isGlobal || selectedRole?.roleType === RoleType.System) {
    message.warning('平台全局角色或系统角色必须通过平台运维流程维护')
    return false
  }

  if (selectedRole && selectedRole.dataScope !== DataPermissionScope.Custom) {
    message.warning('只有自定义数据权限范围角色才能维护部门范围')
    return false
  }

  if (dataScopeForm.value.departmentId === null) {
    message.warning('请选择部门')
    return false
  }

  if (dataScopeForm.value.expirationTime && dataScopeForm.value.effectiveTime
    && dataScopeForm.value.expirationTime <= dataScopeForm.value.effectiveTime) {
    message.warning('失效时间必须晚于生效时间')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm() || dataScopeForm.value.roleId === null || dataScopeForm.value.departmentId === null) {
    return
  }

  submitLoading.value = true
  try {
    if (dataScopeForm.value.basicId) {
      const updateInput: RoleDataScopeUpdateDto = {
        basicId: dataScopeForm.value.basicId,
        effectiveTime: dataScopeForm.value.effectiveTime,
        expirationTime: dataScopeForm.value.expirationTime,
        includeChildren: dataScopeForm.value.includeChildren,
        remark: normalizeNullable(dataScopeForm.value.remark),
      }

      await roleDataScopeApi.update(updateInput)

      if (editingStatus.value !== dataScopeForm.value.status) {
        await roleDataScopeApi.updateStatus({
          basicId: dataScopeForm.value.basicId,
          remark: normalizeNullable(dataScopeForm.value.remark),
          status: dataScopeForm.value.status,
        })
      }
    }
    else {
      const grantInput: RoleDataScopeGrantDto = {
        departmentId: dataScopeForm.value.departmentId,
        effectiveTime: dataScopeForm.value.effectiveTime,
        expirationTime: dataScopeForm.value.expirationTime,
        includeChildren: dataScopeForm.value.includeChildren,
        remark: normalizeNullable(dataScopeForm.value.remark),
        roleId: dataScopeForm.value.roleId,
      }

      await roleDataScopeApi.grant(grantInput)
      selectedRoleId.value = grantInput.roleId
    }

    message.success('保存成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleToggleStatus(row: RoleDataScopeListItemDto) {
  if (row.status !== ValidityStatus.Valid && row.departmentStatus === EnableStatus.Disabled) {
    message.warning('已禁用的部门不能设为有效数据范围')
    return
  }

  await roleDataScopeApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === ValidityStatus.Valid ? '前端停用角色数据范围' : '前端启用角色数据范围',
    status: row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid,
  })
  message.success('角色数据范围状态已更新')
  xGrid.value?.commitProxy('reload')
}

async function handleRevoke(row: RoleDataScopeListItemDto) {
  await roleDataScopeApi.revoke(row.basicId)
  message.success('角色数据范围已撤销')
  xGrid.value?.commitProxy('reload')
}

void loadRoleOptions()
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="selectedRoleId"
          :loading="roleLoading"
          :options="roleOptions"
          filterable
          placeholder="选择自定义范围角色"
          remote
          style="width: 280px"
          @search="loadRoleOptions"
          @update:value="handleRoleChanged"
        />
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索部门/备注"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="validityStatusOptions"
          clearable
          placeholder="绑定状态"
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
              <NIcon><Icon icon="lucide:building-2" /></NIcon>
            </template>
            授予范围
          </NButton>
        </template>

        <template #col_department_status="{ row }">
          <NTag
            :type="
              row.departmentStatus === EnableStatus.Enabled
                ? 'success'
                : row.departmentStatus === EnableStatus.Disabled
                  ? 'error'
                  : 'default'
            "
            round
            size="small"
          >
            {{
              row.departmentStatus === EnableStatus.Enabled
                ? '启用'
                : row.departmentStatus === EnableStatus.Disabled
                  ? '禁用'
                  : '-'
            }}
          </NTag>
        </template>

        <template #col_include_children="{ row }">
          <NTag :type="row.includeChildren ? 'success' : 'default'" round size="small">
            {{ row.includeChildren ? '是' : '否' }}
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
              确认更新角色数据范围状态？
            </NPopconfirm>

            <NPopconfirm @positive-click="handleRevoke(row)">
              <template #trigger>
                <NButton size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  撤销
                </NButton>
              </template>
              确认撤销该角色数据范围？
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
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="dataScopeForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="角色" path="roleId">
          <NSelect
            v-model:value="dataScopeForm.roleId"
            :disabled="Boolean(dataScopeForm.basicId)"
            :loading="roleLoading"
            :options="roleOptions"
            filterable
            placeholder="搜索并选择自定义范围角色"
            remote
            @focus="loadRoleOptions()"
            @search="handleRoleSearch"
          />
        </NFormItem>
        <NFormItem label="部门" path="departmentId">
          <NSelect
            v-model:value="dataScopeForm.departmentId"
            :disabled="Boolean(dataScopeForm.basicId)"
            :loading="departmentLoading"
            :options="departmentOptions"
            clearable
            filterable
            placeholder="搜索并选择部门"
            remote
            @focus="loadDepartmentOptions()"
            @search="handleDepartmentSearch"
          />
        </NFormItem>
        <NFormItem label="绑定状态" path="status">
          <NSelect v-model:value="dataScopeForm.status" :options="validityStatusOptions" />
        </NFormItem>
        <NFormItem label="包含子部门" path="includeChildren">
          <NCheckbox v-model:checked="dataScopeForm.includeChildren">
            包含
          </NCheckbox>
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="dataScopeForm.effectiveTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="失效时间" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="dataScopeForm.expirationTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="dataScopeForm.remark" clearable placeholder="请输入备注" />
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
