<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  DateTimeString,
  RoleSelectItemDto,
  TenantMemberListItemDto,
  UserRoleGrantDto,
  UserRoleListItemDto,
  UserRoleUpdateDto,
} from '@/api'
import {
  NButton,
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
  EnableStatus,
  roleApi,
  RoleType,
  tenantMemberApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  userRoleApi,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserRolePage' })

interface UserRoleGridResult {
  items: UserRoleListItemDto[]
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

interface RoleOption extends NumericSelectOption {
  roleType: RoleType
}

interface UserRoleFormModel {
  basicId?: ApiId
  effectiveTime: DateTimeString | null
  expirationTime: DateTimeString | null
  grantReason: string | null
  remark: string | null
  roleId: ApiId | null
  status: ValidityStatus
  userId: ApiId | null
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<UserRoleListItemDto>>()
const selectedUserId = ref<ApiId | null>(null)
const userOptions = ref<UserOption[]>([])
const roleOptions = ref<RoleOption[]>([])
const userLoading = ref(false)
const roleLoading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<ValidityStatus | null>(null)
const userRoleForm = ref<UserRoleFormModel>(createDefaultForm())

const queryParams = reactive({
  keyword: '',
  onlyValid: 0,
  roleDataScope: null as DataPermissionScope | null,
  roleType: null as RoleType | null,
  status: null as ValidityStatus | null,
})

const userFilter = reactive({
  keyword: '',
})

const roleFilter = reactive({
  keyword: '',
  roleType: null as RoleType | null,
})

const onlyValidOptions = [
  { label: '全部授权', value: 0 },
  { label: '仅有效', value: 1 },
]

const roleTypeOptions = [
  { label: '系统', value: RoleType.System },
  { label: '业务', value: RoleType.Business },
  { label: '自定义', value: RoleType.Custom },
]

const assignableRoleTypeOptions = roleTypeOptions.filter(option => option.value !== RoleType.System)

const dataScopeOptions = [
  { label: '本人', value: DataPermissionScope.SelfOnly },
  { label: '本部门', value: DataPermissionScope.DepartmentOnly },
  { label: '本部门及下级', value: DataPermissionScope.DepartmentAndChildren },
  { label: '全部', value: DataPermissionScope.All },
  { label: '自定义', value: DataPermissionScope.Custom },
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

const modalTitle = computed(() => (userRoleForm.value.basicId ? '编辑用户角色' : '授权用户角色'))

function createDefaultForm(): UserRoleFormModel {
  return {
    effectiveTime: null,
    expirationTime: null,
    grantReason: null,
    remark: null,
    roleId: null,
    status: ValidityStatus.Valid,
    userId: selectedUserId.value,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function toDateInputValue(value?: DateTimeString | null) {
  return value ? value.slice(0, 10) : null
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

function toRoleOption(item: RoleSelectItemDto): RoleOption {
  const scopeName = item.isGlobal ? '全局' : '租户'

  return {
    label: `[${scopeName}] ${item.roleName} (${item.roleCode})`,
    roleType: item.roleType,
    value: item.basicId,
  }
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

async function loadRoleOptions(keyword = roleFilter.keyword) {
  roleLoading.value = true
  roleFilter.keyword = keyword
  try {
    const items = await roleApi.enabledList({
      keyword: normalizeNullable(keyword),
      limit: 50,
      roleType: roleFilter.roleType,
    })
    const options = items
      .filter(item => item.roleType !== RoleType.System)
      .map(toRoleOption)

    roleOptions.value = mergeOptions(roleOptions.value, options)
  }
  catch {
    message.error('加载角色选项失败')
  }
  finally {
    roleLoading.value = false
  }
}

function includesKeyword(row: UserRoleListItemDto, keyword: string) {
  const text = [
    row.tenantMemberDisplayName,
    row.roleName,
    row.roleCode,
    row.grantReason,
    row.remark,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: UserRoleListItemDto[]) {
  const keyword = normalizeNullable(queryParams.keyword)?.toLowerCase()

  return rows.filter((row) => {
    if (keyword && !includesKeyword(row, keyword)) {
      return false
    }

    if (queryParams.roleType !== null && row.roleType !== queryParams.roleType) {
      return false
    }

    if (queryParams.roleDataScope !== null && row.roleDataScope !== queryParams.roleDataScope) {
      return false
    }

    if (queryParams.status !== null && row.status !== queryParams.status) {
      return false
    }

    return true
  })
}

function pageRows(rows: UserRoleListItemDto[], page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<UserRoleGridResult> {
  if (selectedUserId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = await userRoleApi.list(selectedUserId.value, queryParams.onlyValid === 1)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询用户角色失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<UserRoleListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'tenantMemberDisplayName', minWidth: 150, showOverflow: 'tooltip', title: '成员名称' },
      { field: 'userId', minWidth: 120, title: '用户主键' },
      { field: 'roleName', minWidth: 160, showOverflow: 'tooltip', title: '角色名称' },
      { field: 'roleCode', minWidth: 180, showOverflow: 'tooltip', title: '角色编码' },
      {
        field: 'roleType',
        formatter: ({ cellValue }) => getOptionLabel(roleTypeOptions, cellValue),
        minWidth: 110,
        title: '角色类型',
      },
      {
        field: 'roleDataScope',
        formatter: ({ cellValue }) => getOptionLabel(dataScopeOptions, cellValue),
        minWidth: 130,
        title: '数据范围',
      },
      {
        field: 'roleStatus',
        slots: { default: 'col_role_status' },
        title: '角色状态',
        width: 100,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '绑定状态',
        width: 100,
      },
      {
        field: 'isExpired',
        slots: { default: 'col_expired' },
        title: '过期',
        width: 82,
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
      { field: 'grantReason', minWidth: 180, showOverflow: 'tooltip', title: '授权原因' },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 120,
      },
    ],
    id: 'sys_user_role',
    name: '用户角色',
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
  queryParams.keyword = ''
  queryParams.onlyValid = 0
  queryParams.roleDataScope = null
  queryParams.roleType = null
  queryParams.status = null
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  if (selectedUserId.value === null) {
    message.warning('请先选择租户成员')
    return
  }

  userRoleForm.value = createDefaultForm()
  editingStatus.value = null
  modalVisible.value = true
  void loadRoleOptions()
}

function handleEdit(row: UserRoleListItemDto) {
  userRoleForm.value = {
    basicId: row.basicId,
    effectiveTime: toDateInputValue(row.effectiveTime),
    expirationTime: toDateInputValue(row.expirationTime),
    grantReason: row.grantReason ?? null,
    remark: row.remark ?? null,
    roleId: row.roleId,
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
  roleOptions.value = mergeOptions(roleOptions.value, [
    {
      label: row.roleCode ? `${row.roleName || row.roleCode} (${row.roleCode})` : String(row.roleId),
      roleType: row.roleType ?? RoleType.Custom,
      value: row.roleId,
    },
  ])
  modalVisible.value = true
}

function handleUserSearch(keyword: string) {
  void loadUserOptions(keyword)
}

function handleRoleSearch(keyword: string) {
  void loadRoleOptions(keyword)
}

function handleRefreshRoleOptions() {
  roleOptions.value = []
  void loadRoleOptions()
}

function validateForm() {
  if (userRoleForm.value.userId === null) {
    message.warning('请选择租户成员')
    return false
  }

  if (userRoleForm.value.roleId === null) {
    message.warning('请选择角色')
    return false
  }

  const selectedUser = userOptions.value.find(option => option.value === userRoleForm.value.userId)
  if (selectedUser?.memberType === TenantMemberType.PlatformAdmin) {
    message.warning('平台管理员成员角色必须通过平台运维流程维护')
    return false
  }

  const selectedRole = roleOptions.value.find(option => option.value === userRoleForm.value.roleId)
  if (selectedRole?.roleType === RoleType.System) {
    message.warning('系统角色必须通过平台运维流程分配')
    return false
  }

  if (userRoleForm.value.expirationTime && userRoleForm.value.effectiveTime
    && userRoleForm.value.expirationTime <= userRoleForm.value.effectiveTime) {
    message.warning('失效时间必须晚于生效时间')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm() || userRoleForm.value.userId === null || userRoleForm.value.roleId === null) {
    return
  }

  submitLoading.value = true
  try {
    if (userRoleForm.value.basicId) {
      const updateInput: UserRoleUpdateDto = {
        basicId: userRoleForm.value.basicId,
        effectiveTime: userRoleForm.value.effectiveTime,
        expirationTime: userRoleForm.value.expirationTime,
        grantReason: normalizeNullable(userRoleForm.value.grantReason),
        remark: normalizeNullable(userRoleForm.value.remark),
      }

      await userRoleApi.update(updateInput)

      if (editingStatus.value !== userRoleForm.value.status) {
        await userRoleApi.updateStatus({
          basicId: userRoleForm.value.basicId,
          remark: normalizeNullable(userRoleForm.value.remark),
          status: userRoleForm.value.status,
        })
      }
    }
    else {
      const grantInput: UserRoleGrantDto = {
        effectiveTime: userRoleForm.value.effectiveTime,
        expirationTime: userRoleForm.value.expirationTime,
        grantReason: normalizeNullable(userRoleForm.value.grantReason),
        remark: normalizeNullable(userRoleForm.value.remark),
        roleId: userRoleForm.value.roleId,
        userId: userRoleForm.value.userId,
      }

      await userRoleApi.grant(grantInput)
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

async function handleToggleStatus(row: UserRoleListItemDto) {
  if (row.status !== ValidityStatus.Valid && row.roleStatus === EnableStatus.Disabled) {
    message.warning('已禁用的角色不能设为有效绑定')
    return
  }

  if (row.status !== ValidityStatus.Valid && row.tenantMemberStatus !== ValidityStatus.Valid) {
    message.warning('无效租户成员不能设为有效绑定')
    return
  }

  await userRoleApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === ValidityStatus.Valid ? '前端停用用户角色' : '前端启用用户角色',
    status: row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid,
  })
  message.success('用户角色状态已更新')
  xGrid.value?.commitProxy('reload')
}

async function handleRevoke(row: UserRoleListItemDto) {
  await userRoleApi.revoke(row.basicId)
  message.success('用户角色已撤销')
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
          placeholder="搜索角色名称/编码"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.roleType"
          :options="roleTypeOptions"
          clearable
          placeholder="角色类型"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.roleDataScope"
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
              <NIcon><Icon icon="lucide:user-plus" /></NIcon>
            </template>
            授权角色
          </NButton>
        </template>

        <template #col_role_status="{ row }">
          <NTag
            :type="
              row.roleStatus === EnableStatus.Enabled
                ? 'success'
                : row.roleStatus === EnableStatus.Disabled
                  ? 'error'
                  : 'default'
            "
            round
            size="small"
          >
            {{
              row.roleStatus === EnableStatus.Enabled
                ? '启用'
                : row.roleStatus === EnableStatus.Disabled
                  ? '禁用'
                  : '-'
            }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === ValidityStatus.Valid ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(validityStatusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_expired="{ row }">
          <NTag :type="row.isExpired ? 'error' : 'default'" round size="small">
            {{ row.isExpired ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <!-- 操作列仅图标 -->
            <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>

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
              确认更新用户角色状态？
            </NPopconfirm>

            <NPopconfirm @positive-click="handleRevoke(row)">
              <template #trigger>
                <NButton aria-label="撤销" circle quaternary size="small" type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:user-x" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认撤销该用户角色？
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
      <NForm :model="userRoleForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="租户成员" path="userId">
          <NSelect
            v-model:value="userRoleForm.userId"
            :disabled="Boolean(userRoleForm.basicId)"
            :loading="userLoading"
            :options="userOptions"
            filterable
            placeholder="搜索并选择租户成员"
            remote
            @focus="loadUserOptions()"
            @search="handleUserSearch"
          />
        </NFormItem>
        <NFormItem label="角色类型" path="roleType">
          <NSelect
            v-model:value="roleFilter.roleType"
            :options="assignableRoleTypeOptions"
            clearable
            placeholder="不限类型"
            @update:value="handleRefreshRoleOptions"
          />
        </NFormItem>
        <NFormItem label="角色" path="roleId">
          <NSelect
            v-model:value="userRoleForm.roleId"
            :disabled="Boolean(userRoleForm.basicId)"
            :loading="roleLoading"
            :options="roleOptions"
            clearable
            filterable
            placeholder="搜索并选择角色"
            remote
            @focus="loadRoleOptions()"
            @search="handleRoleSearch"
          />
        </NFormItem>
        <NFormItem label="绑定状态" path="status">
          <NSelect v-model:value="userRoleForm.status" :options="validityStatusOptions" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="userRoleForm.effectiveTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="失效时间" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="userRoleForm.expirationTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="授权原因" path="grantReason">
          <NInput v-model:value="userRoleForm.grantReason" clearable placeholder="请输入授权原因" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="userRoleForm.remark" clearable placeholder="请输入备注" />
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
