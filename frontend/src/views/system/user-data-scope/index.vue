<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  DepartmentTreeNodeDto,
  TenantMemberListItemDto,
  UserDataScopeGrantDto,
  UserDataScopeListItemDto,
  UserDataScopeUpdateDto,
} from '@/api'
import {
  NButton,
  NCheckbox,
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
  tenantMemberApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  userDataScopeApi,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserDataScopePage' })

interface UserDataScopeGridResult {
  items: UserDataScopeListItemDto[]
  total: number
}

interface NumericSelectOption {
  label: string
  value: ApiId
}

interface UserOption extends NumericSelectOption {
  inviteStatus: TenantMemberInviteStatus
  memberType: TenantMemberType
  status: ValidityStatus
}

interface UserDataScopeFormModel {
  basicId?: ApiId
  dataScope: DataPermissionScope
  departmentId: ApiId | null
  includeChildren: boolean
  remark: string | null
  status: ValidityStatus
  userId: ApiId | null
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<UserDataScopeListItemDto>>()
const selectedUserId = ref<ApiId | null>(null)
const userOptions = ref<UserOption[]>([])
const departmentOptions = ref<NumericSelectOption[]>([])
const userLoading = ref(false)
const departmentLoading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<ValidityStatus | null>(null)
const dataScopeForm = ref<UserDataScopeFormModel>(createDefaultForm())

const queryParams = reactive({
  dataScope: null as DataPermissionScope | null,
  keyword: '',
  onlyValid: 0,
  status: null as ValidityStatus | null,
})

const userFilter = reactive({
  keyword: '',
})

const departmentFilter = reactive({
  keyword: '',
})

const onlyValidOptions = [
  { label: '全部覆盖', value: 0 },
  { label: '仅有效', value: 1 },
]

const dataScopeOptions = [
  { label: '本人', value: DataPermissionScope.SelfOnly },
  { label: '本部门', value: DataPermissionScope.DepartmentOnly },
  { label: '本部门及下级', value: DataPermissionScope.DepartmentAndChildren },
  { label: '全部', value: DataPermissionScope.All },
  { label: '自定义', value: DataPermissionScope.Custom },
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

const memberTypeOptions = [
  { label: '所有者', value: TenantMemberType.Owner },
  { label: '管理员', value: TenantMemberType.Admin },
  { label: '成员', value: TenantMemberType.Member },
  { label: '外部成员', value: TenantMemberType.External },
  { label: '访客', value: TenantMemberType.Guest },
  { label: '顾问', value: TenantMemberType.Consultant },
  { label: '平台管理员', value: TenantMemberType.PlatformAdmin },
]

const validityStatusOptions = [
  { label: '有效', value: ValidityStatus.Valid },
  { label: '无效', value: ValidityStatus.Invalid },
]

const modalTitle = computed(() => (dataScopeForm.value.basicId ? '编辑用户数据范围' : '授予用户数据范围'))

function createDefaultForm(): UserDataScopeFormModel {
  return {
    dataScope: DataPermissionScope.Custom,
    departmentId: null,
    includeChildren: false,
    remark: null,
    status: ValidityStatus.Valid,
    userId: selectedUserId.value,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function toUserOption(item: TenantMemberListItemDto): UserOption {
  const displayName = item.displayName || `用户 ${item.userId}`
  const memberType = getOptionLabel(memberTypeOptions, item.memberType)

  return {
    inviteStatus: item.inviteStatus,
    label: `${displayName} (${memberType} / ${item.userId})`,
    memberType: item.memberType,
    status: item.status,
    value: item.userId,
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

async function loadUserOptions(keyword = userFilter.keyword) {
  userLoading.value = true
  userFilter.keyword = keyword
  try {
    const result = await tenantMemberApi.page({
      ...createPageRequest({
        page: {
          pageIndex: 1,
          pageSize: 50,
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

    const firstUser = options[0]
    if (selectedUserId.value === null && firstUser) {
      selectedUserId.value = firstUser.value
      xGrid.value?.commitProxy('reload')
    }
  }
  catch {
    message.error('加载租户成员选项失败')
  }
  finally {
    userLoading.value = false
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

function includesKeyword(row: UserDataScopeListItemDto, keyword: string) {
  const text = [
    row.tenantMemberDisplayName,
    row.departmentName,
    row.departmentCode,
    row.remark,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: UserDataScopeListItemDto[]) {
  const keyword = normalizeNullable(queryParams.keyword)?.toLowerCase()

  return rows.filter((row) => {
    if (keyword && !includesKeyword(row, keyword)) {
      return false
    }

    if (queryParams.dataScope !== null && row.dataScope !== queryParams.dataScope) {
      return false
    }

    if (queryParams.status !== null && row.status !== queryParams.status) {
      return false
    }

    return true
  })
}

function pageRows(rows: UserDataScopeListItemDto[], page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<UserDataScopeGridResult> {
  if (selectedUserId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = await userDataScopeApi.list(selectedUserId.value, queryParams.onlyValid === 1)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询用户数据范围失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<UserDataScopeListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'tenantMemberDisplayName', minWidth: 150, showOverflow: 'tooltip', title: '成员名称' },
      { field: 'userId', minWidth: 120, title: '用户主键' },
      {
        field: 'dataScope',
        formatter: ({ cellValue }) => getOptionLabel(dataScopeOptions, cellValue),
        minWidth: 130,
        title: '数据范围',
      },
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
    id: 'sys_user_data_scope',
    name: '用户数据范围',
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

function handleUserChanged() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.dataScope = null
  queryParams.keyword = ''
  queryParams.onlyValid = 0
  queryParams.status = null
  xGrid.value?.commitProxy('reload')
}

function handleDataScopeChanged() {
  if (dataScopeForm.value.dataScope !== DataPermissionScope.Custom) {
    dataScopeForm.value.departmentId = null
    dataScopeForm.value.includeChildren = false
    return
  }

  void loadDepartmentOptions()
}

function handleAdd() {
  if (selectedUserId.value === null) {
    message.warning('请先选择租户成员')
    return
  }

  dataScopeForm.value = createDefaultForm()
  editingStatus.value = null
  modalVisible.value = true
  void loadDepartmentOptions()
}

function handleEdit(row: UserDataScopeListItemDto) {
  dataScopeForm.value = {
    basicId: row.basicId,
    dataScope: row.dataScope,
    departmentId: row.departmentId > 0 ? row.departmentId : null,
    includeChildren: row.includeChildren,
    remark: row.remark ?? null,
    status: row.status,
    userId: row.userId,
  }
  editingStatus.value = row.status
  userOptions.value = mergeOptions(userOptions.value, [
    {
      inviteStatus: row.tenantMemberInviteStatus ?? TenantMemberInviteStatus.Accepted,
      label: row.tenantMemberDisplayName
        ? `${row.tenantMemberDisplayName} (${row.userId})`
        : `用户 ${row.userId}`,
      memberType: row.tenantMemberType ?? TenantMemberType.Member,
      status: row.tenantMemberStatus ?? ValidityStatus.Valid,
      value: row.userId,
    },
  ])

  if (row.departmentId > 0) {
    departmentOptions.value = mergeOptions(departmentOptions.value, [
      {
        label: row.departmentCode
          ? `${row.departmentName || row.departmentCode} (${row.departmentCode})`
          : String(row.departmentId),
        value: row.departmentId,
      },
    ])
  }

  modalVisible.value = true
}

function handleUserSearch(keyword: string) {
  void loadUserOptions(keyword)
}

function handleDepartmentSearch(keyword: string) {
  void loadDepartmentOptions(keyword)
}

function validateForm() {
  if (dataScopeForm.value.userId === null) {
    message.warning('请选择租户成员')
    return false
  }

  const selectedUser = userOptions.value.find(option => option.value === dataScopeForm.value.userId)
  if (selectedUser?.memberType === TenantMemberType.PlatformAdmin) {
    message.warning('平台管理员成员数据范围必须通过平台运维流程维护')
    return false
  }

  if (dataScopeForm.value.dataScope === DataPermissionScope.Custom && dataScopeForm.value.departmentId === null) {
    message.warning('自定义数据范围必须选择部门')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm() || dataScopeForm.value.userId === null) {
    return
  }

  const departmentId = dataScopeForm.value.dataScope === DataPermissionScope.Custom
    ? dataScopeForm.value.departmentId
    : null

  submitLoading.value = true
  try {
    if (dataScopeForm.value.basicId) {
      const updateInput: UserDataScopeUpdateDto = {
        basicId: dataScopeForm.value.basicId,
        dataScope: dataScopeForm.value.dataScope,
        departmentId,
        includeChildren: dataScopeForm.value.dataScope === DataPermissionScope.Custom
          && dataScopeForm.value.includeChildren,
        remark: normalizeNullable(dataScopeForm.value.remark),
      }

      await userDataScopeApi.update(updateInput)

      if (editingStatus.value !== dataScopeForm.value.status) {
        await userDataScopeApi.updateStatus({
          basicId: dataScopeForm.value.basicId,
          remark: normalizeNullable(dataScopeForm.value.remark),
          status: dataScopeForm.value.status,
        })
      }
    }
    else {
      const grantInput: UserDataScopeGrantDto = {
        dataScope: dataScopeForm.value.dataScope,
        departmentId,
        includeChildren: dataScopeForm.value.dataScope === DataPermissionScope.Custom
          && dataScopeForm.value.includeChildren,
        remark: normalizeNullable(dataScopeForm.value.remark),
        userId: dataScopeForm.value.userId,
      }

      await userDataScopeApi.grant(grantInput)
      selectedUserId.value = grantInput.userId
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

async function handleToggleStatus(row: UserDataScopeListItemDto) {
  if (
    row.status !== ValidityStatus.Valid
    && row.dataScope === DataPermissionScope.Custom
    && row.departmentStatus === EnableStatus.Disabled
  ) {
    message.warning('已禁用的部门不能设为有效数据范围')
    return
  }

  if (row.status !== ValidityStatus.Valid && row.tenantMemberStatus !== ValidityStatus.Valid) {
    message.warning('无效租户成员不能设为有效数据范围')
    return
  }

  await userDataScopeApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === ValidityStatus.Valid ? '前端停用用户数据范围' : '前端启用用户数据范围',
    status: row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid,
  })
  message.success('用户数据范围状态已更新')
  xGrid.value?.commitProxy('reload')
}

async function handleRevoke(row: UserDataScopeListItemDto) {
  await userDataScopeApi.revoke(row.basicId)
  message.success('用户数据范围已撤销')
  xGrid.value?.commitProxy('reload')
}

void loadUserOptions()
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="selectedUserId"
          :loading="userLoading"
          :options="userOptions"
          filterable
          placeholder="选择租户成员"
          remote
          style="width: 260px"
          @search="loadUserOptions"
          @update:value="handleUserChanged"
        />
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索部门/备注"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.dataScope"
          :options="dataScopeOptions"
          clearable
          placeholder="数据范围"
          style="width: 140px"
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
              确认更新用户数据范围状态？
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
              确认撤销该用户数据范围？
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
        <NFormItem label="租户成员" path="userId">
          <NSelect
            v-model:value="dataScopeForm.userId"
            :disabled="Boolean(dataScopeForm.basicId)"
            :loading="userLoading"
            :options="userOptions"
            filterable
            placeholder="搜索并选择租户成员"
            remote
            @focus="loadUserOptions()"
            @search="handleUserSearch"
          />
        </NFormItem>
        <NFormItem label="数据范围" path="dataScope">
          <NSelect
            v-model:value="dataScopeForm.dataScope"
            :options="dataScopeOptions"
            @update:value="handleDataScopeChanged"
          />
        </NFormItem>
        <NFormItem label="部门" path="departmentId">
          <NSelect
            v-model:value="dataScopeForm.departmentId"
            :disabled="dataScopeForm.dataScope !== DataPermissionScope.Custom"
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
          <NCheckbox
            v-model:checked="dataScopeForm.includeChildren"
            :disabled="dataScopeForm.dataScope !== DataPermissionScope.Custom"
          >
            包含
          </NCheckbox>
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
