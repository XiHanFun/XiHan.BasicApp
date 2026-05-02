<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  DateTimeString,
  PermissionListItemDto,
  TenantMemberListItemDto,
  UserPermissionGrantDto,
  UserPermissionListItemDto,
  UserPermissionUpdateDto,
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
  EnableStatus,
  PermissionAction,
  permissionApi,
  PermissionType,
  tenantMemberApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  userPermissionApi,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserPermissionPage' })

interface UserPermissionGridResult {
  items: UserPermissionListItemDto[]
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

interface UserPermissionFormModel {
  basicId?: ApiId
  effectiveTime: DateTimeString | null
  expirationTime: DateTimeString | null
  grantReason: string | null
  permissionAction: PermissionAction
  permissionId: ApiId | null
  remark: string | null
  status: ValidityStatus
  userId: ApiId | null
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<UserPermissionListItemDto>>()
const selectedUserId = ref<ApiId | null>(null)
const userOptions = ref<UserOption[]>([])
const permissionOptions = ref<NumericSelectOption[]>([])
const userLoading = ref(false)
const permissionLoading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<ValidityStatus | null>(null)
const userPermissionForm = ref<UserPermissionFormModel>(createDefaultForm())

const queryParams = reactive({
  keyword: '',
  moduleCode: '',
  onlyValid: 0,
  permissionAction: null as PermissionAction | null,
  permissionType: null as PermissionType | null,
  status: null as ValidityStatus | null,
})

const userFilter = reactive({
  keyword: '',
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

const permissionActionOptions = [
  { label: '允许', value: PermissionAction.Grant },
  { label: '拒绝', value: PermissionAction.Deny },
]

const permissionTypeOptions = [
  { label: '资源操作', value: PermissionType.ResourceBased },
  { label: '功能', value: PermissionType.Functional },
  { label: '数据范围', value: PermissionType.DataScope },
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

const modalTitle = computed(() => (userPermissionForm.value.basicId ? '编辑用户权限' : '直授用户权限'))

function createDefaultForm(): UserPermissionFormModel {
  return {
    effectiveTime: null,
    expirationTime: null,
    grantReason: null,
    permissionAction: PermissionAction.Grant,
    permissionId: null,
    remark: null,
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

function toPermissionOption(item: PermissionListItemDto): NumericSelectOption {
  const moduleName = item.moduleCode ? `[${item.moduleCode}] ` : ''

  return {
    label: `${moduleName}${item.permissionName} (${item.permissionCode})`,
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

async function loadPermissionOptions(keyword = permissionFilter.keyword) {
  permissionLoading.value = true
  permissionFilter.keyword = keyword
  try {
    const result = await permissionApi.page({
      ...createPageRequest({
        page: {
          pageIndex: 1,
          pageSize: 50,
        },
      }),
      keyword: normalizeNullable(keyword),
      moduleCode: normalizeNullable(permissionFilter.moduleCode),
      permissionType: permissionFilter.permissionType,
      status: EnableStatus.Enabled,
    })
    permissionOptions.value = mergeOptions(permissionOptions.value, result.items.map(toPermissionOption))
  }
  catch {
    message.error('加载权限选项失败')
  }
  finally {
    permissionLoading.value = false
  }
}

function includesKeyword(row: UserPermissionListItemDto, keyword: string) {
  const text = [
    row.tenantMemberDisplayName,
    row.permissionName,
    row.permissionCode,
    row.moduleCode,
    row.grantReason,
    row.remark,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: UserPermissionListItemDto[]) {
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

    if (queryParams.permissionAction !== null && row.permissionAction !== queryParams.permissionAction) {
      return false
    }

    if (queryParams.status !== null && row.status !== queryParams.status) {
      return false
    }

    return true
  })
}

function pageRows(rows: UserPermissionListItemDto[], page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<UserPermissionGridResult> {
  if (selectedUserId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = await userPermissionApi.list(selectedUserId.value, queryParams.onlyValid === 1)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询用户直授权限失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<UserPermissionListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'tenantMemberDisplayName', minWidth: 150, showOverflow: 'tooltip', title: '成员名称' },
      { field: 'userId', minWidth: 120, title: '用户主键' },
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
        field: 'permissionAction',
        slots: { default: 'col_action' },
        title: '授权动作',
        width: 100,
      },
      {
        field: 'permissionStatus',
        slots: { default: 'col_permission_status' },
        title: '权限状态',
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
    id: 'sys_user_permission',
    name: '用户直授权限',
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
  queryParams.moduleCode = ''
  queryParams.onlyValid = 0
  queryParams.permissionAction = null
  queryParams.permissionType = null
  queryParams.status = null
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  if (selectedUserId.value === null) {
    message.warning('请先选择租户成员')
    return
  }

  userPermissionForm.value = createDefaultForm()
  editingStatus.value = null
  modalVisible.value = true
  void loadPermissionOptions()
}

function handleEdit(row: UserPermissionListItemDto) {
  userPermissionForm.value = {
    basicId: row.basicId,
    effectiveTime: toDateInputValue(row.effectiveTime),
    expirationTime: toDateInputValue(row.expirationTime),
    grantReason: row.grantReason ?? null,
    permissionAction: row.permissionAction,
    permissionId: row.permissionId,
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
  permissionOptions.value = mergeOptions(permissionOptions.value, [
    {
      label: row.permissionCode
        ? `${row.permissionName || row.permissionCode} (${row.permissionCode})`
        : String(row.permissionId),
      value: row.permissionId,
    },
  ])
  modalVisible.value = true
}

function handleUserSearch(keyword: string) {
  void loadUserOptions(keyword)
}

function handlePermissionSearch(keyword: string) {
  void loadPermissionOptions(keyword)
}

function handleRefreshPermissionOptions() {
  permissionOptions.value = []
  void loadPermissionOptions()
}

function validateForm() {
  if (userPermissionForm.value.userId === null) {
    message.warning('请选择租户成员')
    return false
  }

  if (userPermissionForm.value.permissionId === null) {
    message.warning('请选择权限')
    return false
  }

  const selectedUser = userOptions.value.find(option => option.value === userPermissionForm.value.userId)
  if (selectedUser?.memberType === TenantMemberType.PlatformAdmin) {
    message.warning('平台管理员成员权限必须通过平台运维流程维护')
    return false
  }

  if (userPermissionForm.value.expirationTime && userPermissionForm.value.effectiveTime
    && userPermissionForm.value.expirationTime <= userPermissionForm.value.effectiveTime) {
    message.warning('失效时间必须晚于生效时间')
    return false
  }

  return true
}

async function handleSubmit() {
  if (
    !validateForm()
    || userPermissionForm.value.userId === null
    || userPermissionForm.value.permissionId === null
  ) {
    return
  }

  submitLoading.value = true
  try {
    if (userPermissionForm.value.basicId) {
      const updateInput: UserPermissionUpdateDto = {
        basicId: userPermissionForm.value.basicId,
        effectiveTime: userPermissionForm.value.effectiveTime,
        expirationTime: userPermissionForm.value.expirationTime,
        grantReason: normalizeNullable(userPermissionForm.value.grantReason),
        permissionAction: userPermissionForm.value.permissionAction,
        remark: normalizeNullable(userPermissionForm.value.remark),
      }

      await userPermissionApi.update(updateInput)

      if (editingStatus.value !== userPermissionForm.value.status) {
        await userPermissionApi.updateStatus({
          basicId: userPermissionForm.value.basicId,
          remark: normalizeNullable(userPermissionForm.value.remark),
          status: userPermissionForm.value.status,
        })
      }
    }
    else {
      const grantInput: UserPermissionGrantDto = {
        effectiveTime: userPermissionForm.value.effectiveTime,
        expirationTime: userPermissionForm.value.expirationTime,
        grantReason: normalizeNullable(userPermissionForm.value.grantReason),
        permissionAction: userPermissionForm.value.permissionAction,
        permissionId: userPermissionForm.value.permissionId,
        remark: normalizeNullable(userPermissionForm.value.remark),
        userId: userPermissionForm.value.userId,
      }

      await userPermissionApi.grant(grantInput)
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

async function handleToggleStatus(row: UserPermissionListItemDto) {
  if (row.status !== ValidityStatus.Valid && row.permissionStatus === EnableStatus.Disabled) {
    message.warning('已禁用的权限不能设为有效绑定')
    return
  }

  if (row.status !== ValidityStatus.Valid && row.tenantMemberStatus !== ValidityStatus.Valid) {
    message.warning('无效租户成员不能设为有效绑定')
    return
  }

  await userPermissionApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === ValidityStatus.Valid ? '前端停用用户直授权限' : '前端启用用户直授权限',
    status: row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid,
  })
  message.success('用户直授权限状态已更新')
  xGrid.value?.commitProxy('reload')
}

async function handleRevoke(row: UserPermissionListItemDto) {
  await userPermissionApi.revoke(row.basicId)
  message.success('用户直授权限已撤销')
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
          v-model:value="queryParams.permissionAction"
          :options="permissionActionOptions"
          clearable
          placeholder="授权动作"
          style="width: 110px"
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
              <NIcon><Icon icon="lucide:key-round" /></NIcon>
            </template>
            直授权限
          </NButton>
        </template>

        <template #col_action="{ row }">
          <NTag :type="row.permissionAction === PermissionAction.Grant ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(permissionActionOptions, row.permissionAction) }}
          </NTag>
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
              确认更新用户直授权限状态？
            </NPopconfirm>

            <NPopconfirm @positive-click="handleRevoke(row)">
              <template #trigger>
                <NButton aria-label="撤销" circle quaternary size="small" type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认撤销该用户直授权限？
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
      <NForm :model="userPermissionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="租户成员" path="userId">
          <NSelect
            v-model:value="userPermissionForm.userId"
            :disabled="Boolean(userPermissionForm.basicId)"
            :loading="userLoading"
            :options="userOptions"
            filterable
            placeholder="搜索并选择租户成员"
            remote
            @focus="loadUserOptions()"
            @search="handleUserSearch"
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
        <NFormItem label="权限" path="permissionId">
          <NSelect
            v-model:value="userPermissionForm.permissionId"
            :disabled="Boolean(userPermissionForm.basicId)"
            :loading="permissionLoading"
            :options="permissionOptions"
            clearable
            filterable
            placeholder="搜索并选择权限"
            remote
            @focus="loadPermissionOptions()"
            @search="handlePermissionSearch"
          />
        </NFormItem>
        <NFormItem label="授权动作" path="permissionAction">
          <NSelect v-model:value="userPermissionForm.permissionAction" :options="permissionActionOptions" />
        </NFormItem>
        <NFormItem label="绑定状态" path="status">
          <NSelect v-model:value="userPermissionForm.status" :options="validityStatusOptions" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="userPermissionForm.effectiveTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="失效时间" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="userPermissionForm.expirationTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="授权原因" path="grantReason">
          <NInput v-model:value="userPermissionForm.grantReason" clearable placeholder="请输入授权原因" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="userPermissionForm.remark" clearable placeholder="请输入备注" />
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
