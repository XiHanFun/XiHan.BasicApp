<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, UserCreateDto, UserListItemDto, UserManagementDetailDto, UserUpdateDto } from '@/api'
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
  NModal,
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  TenantMemberType,
  UserGender,
  userManagementApi,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import {
  DATA_SCOPE_OPTIONS,
  DEVICE_TYPE_OPTIONS,
  GENDER_OPTIONS,
  MEMBER_TYPE_OPTIONS,
  PERMISSION_ACTION_OPTIONS,
  ROLE_TYPE_OPTIONS,
  STATISTICS_PERIOD_OPTIONS,
  STATUS_OPTIONS,
  TWO_FACTOR_METHOD_OPTIONS,
  VALIDITY_STATUS_OPTIONS,
} from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserPage' })

interface UserGridResult {
  items: UserListItemDto[]
  total: number
}

interface UserFormModel extends UserCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<UserListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<UserManagementDetailDto | null>(null)
const userForm = ref<UserFormModel>(createDefaultForm())

const queryParams = reactive({
  gender: undefined as UserGender | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const genderOptions = GENDER_OPTIONS

const statusOptions = STATUS_OPTIONS

const memberTypeOptions = MEMBER_TYPE_OPTIONS

const languageOptions = [
  { label: '简体中文', value: 'zh-CN' },
  { label: '繁體中文', value: 'zh-TW' },
  { label: 'English', value: 'en-US' },
  { label: '日本語', value: 'ja-JP' },
  { label: '한국어', value: 'ko-KR' },
]

const validityStatusOptions = VALIDITY_STATUS_OPTIONS

const roleTypeOptions = ROLE_TYPE_OPTIONS

const dataScopeOptions = DATA_SCOPE_OPTIONS

const permissionActionOptions = PERMISSION_ACTION_OPTIONS

const twoFactorMethodOptions = TWO_FACTOR_METHOD_OPTIONS

const deviceTypeOptions = DEVICE_TYPE_OPTIONS

const statisticsPeriodOptions = STATISTICS_PERIOD_OPTIONS

const modalTitle = computed(() => (userForm.value.basicId ? '编辑用户' : '新增用户'))

function createDefaultForm(): UserFormModel {
  return {
    avatar: null,
    birthday: null,
    country: null,
    displayName: null,
    effectiveTime: null,
    email: null,
    expirationTime: null,
    gender: UserGender.Unknown,
    initialPassword: '',
    inviteRemark: null,
    language: 'zh-CN',
    memberType: TenantMemberType.Member,
    nickName: null,
    phone: null,
    realName: null,
    remark: null,
    status: EnableStatus.Enabled,
    timeZone: null,
    userName: '',
  }
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
  return getOptionLabel(statusOptions, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions, value)
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<UserGridResult> {
  return userManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      gender: queryParams.gender,
      keyword: normalizeNullable(queryParams.keyword),
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询用户失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<UserListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'userName', minWidth: 130, showOverflow: 'tooltip', sortable: true, title: '用户名' },
      { field: 'realName', minWidth: 110, showOverflow: 'tooltip', title: '真实姓名' },
      { field: 'nickName', minWidth: 110, showOverflow: 'tooltip', title: '昵称' },
      {
        field: 'gender',
        formatter: ({ cellValue }) => getOptionLabel(genderOptions, cellValue),
        title: '性别',
        width: 80,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      {
        field: 'isSystemAccount',
        slots: { default: 'col_system' },
        title: '系统账号',
        width: 90,
      },
      { field: 'language', minWidth: 80, title: '语言' },
      {
        field: 'lastLoginTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '最后登录',
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
        width: 132,
      },
    ],
    id: 'sys_user',
    name: '用户管理',
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
  queryParams.gender = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  userForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: UserListItemDto) {
  userForm.value = {
    ...createDefaultForm(),
    avatar: row.avatar ?? null,
    basicId: row.basicId,
    gender: row.gender,
    language: row.language ?? 'zh-CN',
    nickName: row.nickName ?? null,
    realName: row.realName ?? null,
    status: row.status,
    timeZone: row.timeZone ?? null,
    userName: row.userName,
  }
  modalVisible.value = true
}

async function handleView(row: UserListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await userManagementApi.detailView(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到用户详情')
    }
  }
  catch {
    message.error('加载用户详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function handleToggleStatus(row: UserListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await userManagementApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('状态更新失败')
  }
}

function validateForm() {
  if (!userForm.value.userName.trim()) {
    message.warning('请输入用户名')
    return false
  }
  if (!userForm.value.basicId && !userForm.value.initialPassword.trim()) {
    message.warning('请输入初始密码')
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm())
    return

  submitLoading.value = true
  try {
    if (userForm.value.basicId) {
      const updateInput: UserUpdateDto = {
        avatar: normalizeNullable(userForm.value.avatar),
        basicId: userForm.value.basicId,
        birthday: userForm.value.birthday,
        country: normalizeNullable(userForm.value.country),
        email: normalizeNullable(userForm.value.email),
        gender: userForm.value.gender,
        language: userForm.value.language,
        nickName: normalizeNullable(userForm.value.nickName),
        phone: normalizeNullable(userForm.value.phone),
        realName: normalizeNullable(userForm.value.realName),
        remark: normalizeNullable(userForm.value.remark),
        timeZone: normalizeNullable(userForm.value.timeZone),
      }
      await userManagementApi.update(updateInput)
    }
    else {
      const createInput: UserCreateDto = {
        avatar: normalizeNullable(userForm.value.avatar),
        gender: userForm.value.gender,
        initialPassword: userForm.value.initialPassword,
        language: userForm.value.language,
        memberType: userForm.value.memberType,
        nickName: normalizeNullable(userForm.value.nickName),
        phone: normalizeNullable(userForm.value.phone),
        realName: normalizeNullable(userForm.value.realName),
        remark: normalizeNullable(userForm.value.remark),
        status: userForm.value.status,
        userName: userForm.value.userName.trim(),
      }
      await userManagementApi.create(createInput)
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
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索用户名/姓名/昵称"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.gender"
          :options="genderOptions"
          clearable
          placeholder="性别"
          style="width: 100px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="状态"
          style="width: 100px"
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
            新增用户
          </NButton>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_system="{ row }">
          <NTag :type="row.isSystemAccount ? 'warning' : 'default'" round size="small">
            {{ row.isSystemAccount ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton aria-label="查看详情" circle quaternary size="small" @click="handleView(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>
            <NPopconfirm @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton
                  :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
                  aria-label="切换状态"
                  circle
                  quaternary
                  size="small"
                >
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:check'" />
                    </NIcon>
                  </template>
                </NButton>
              </template>
              确认{{ row.status === EnableStatus.Enabled ? '禁用' : '启用' }}？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="960">
      <NDrawerContent closable title="用户详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无用户详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="用户名">
                    {{ currentDetail.user.userName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="真实姓名">
                    {{ formatNullable(currentDetail.user.realName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="昵称">
                    {{ formatNullable(currentDetail.user.nickName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="性别">
                    {{ getOptionLabel(genderOptions, currentDetail.user.gender) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="状态">
                    {{ formatStatus(currentDetail.user.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="系统账号">
                    {{ formatBoolean(currentDetail.user.isSystemAccount) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="语言">
                    {{ formatNullable(currentDetail.user.language) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="时区">
                    {{ formatNullable(currentDetail.user.timeZone) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="最后登录">
                    {{ formatNullableDate(currentDetail.user.lastLoginTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.user.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="租户显示名">
                    {{ formatNullable(currentDetail.tenantMembership?.displayName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="成员类型">
                    {{ getOptionLabel(memberTypeOptions, currentDetail.tenantMembership?.memberType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="租户成员状态">
                    {{ formatValidityStatus(currentDetail.tenantMembership?.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="聚合时间">
                    {{ formatNullableDate(currentDetail.generatedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="security" tab="安全设置">
                <NEmpty v-if="!currentDetail.security" description="暂无安全设置" style="padding: 40px 0">
                  <template #icon>
                    <NIcon><Icon icon="lucide:inbox" /></NIcon>
                  </template>
                </NEmpty>
                <NDescriptions v-else :column="2" bordered size="small">
                  <NDescriptionsItem label="允许多端登录">
                    {{ formatBoolean(currentDetail.security.allowMultiLogin) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="最大登录设备">
                    {{ currentDetail.security.maxLoginDevices }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="两步验证">
                    {{ formatBoolean(currentDetail.security.twoFactorEnabled) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="验证方式">
                    {{ getOptionLabel(twoFactorMethodOptions, currentDetail.security.twoFactorMethod) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="邮箱验证">
                    {{ formatBoolean(currentDetail.security.emailVerified) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="手机验证">
                    {{ formatBoolean(currentDetail.security.phoneVerified) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="账号锁定">
                    {{ formatBoolean(currentDetail.security.isLocked) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="密码过期">
                    {{ formatBoolean(currentDetail.security.isPasswordExpired) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="失败次数">
                    {{ currentDetail.security.failedLoginAttempts }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="密码过期时间">
                    {{ formatNullableDate(currentDetail.security.passwordExpiryTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="最后改密">
                    {{ formatNullableDate(currentDetail.security.lastPasswordChangeTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="最后安全检查">
                    {{ formatNullableDate(currentDetail.security.lastSecurityCheckTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="departments" :tab="`部门 (${currentDetail.departments.length})`">
                <table v-if="currentDetail.departments.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>部门</th>
                      <th>编码</th>
                      <th>主部门</th>
                      <th>状态</th>
                      <th>创建时间</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.departments" :key="item.basicId">
                      <td>{{ formatNullable(item.departmentName) }}</td>
                      <td>{{ formatNullable(item.departmentCode) }}</td>
                      <td>{{ formatBoolean(item.isMain) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.createdTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无部门分配" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="roles" :tab="`角色 (${currentDetail.roles.length})`">
                <table v-if="currentDetail.roles.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>角色</th>
                      <th>编码</th>
                      <th>类型</th>
                      <th>授权状态</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.roles" :key="item.basicId">
                      <td>{{ formatNullable(item.roleName) }}</td>
                      <td>{{ formatNullable(item.roleCode) }}</td>
                      <td>{{ getOptionLabel(roleTypeOptions, item.roleType) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无角色分配" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="permissions" :tab="`额外权限 (${currentDetail.permissions.length})`">
                <table v-if="currentDetail.permissions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>权限</th>
                      <th>编码</th>
                      <th>动作</th>
                      <th>授权状态</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.permissions" :key="item.basicId">
                      <td>{{ formatNullable(item.permissionName) }}</td>
                      <td>{{ formatNullable(item.permissionCode) }}</td>
                      <td>{{ getOptionLabel(permissionActionOptions, item.permissionAction) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无额外权限" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="dataScopes" :tab="`数据范围 (${currentDetail.dataScopes.length})`">
                <table v-if="currentDetail.dataScopes.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>范围</th>
                      <th>部门</th>
                      <th>包含子部门</th>
                      <th>状态</th>
                      <th>创建时间</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.dataScopes" :key="item.basicId">
                      <td>{{ getOptionLabel(dataScopeOptions, item.dataScope) }}</td>
                      <td>{{ formatNullable(item.departmentName) }}</td>
                      <td>{{ formatBoolean(item.includeChildren) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.createdTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无自定义数据范围" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="sessions" :tab="`登录会话 (${currentDetail.sessions.length})`">
                <table v-if="currentDetail.sessions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>会话</th>
                      <th>设备</th>
                      <th>IP</th>
                      <th>在线</th>
                      <th>最后活跃</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.sessions" :key="item.basicId">
                      <td>{{ item.userSessionId }}</td>
                      <td>{{ formatNullable(item.deviceName) }} / {{ getOptionLabel(deviceTypeOptions, item.deviceType) }}</td>
                      <td>{{ formatNullable(item.ipAddressMasked) }}</td>
                      <td>{{ formatBoolean(item.isOnline) }}</td>
                      <td>{{ formatNullableDate(item.lastActivityTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无登录会话" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="externalLogins" :tab="`第三方绑定 (${currentDetail.externalLogins.length})`">
                <table v-if="currentDetail.externalLogins.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>提供方</th>
                      <th>账号</th>
                      <th>邮箱</th>
                      <th>最近登录</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.externalLogins" :key="item.basicId">
                      <td>{{ formatNullable(item.providerDisplayName || item.provider) }}</td>
                      <td>{{ formatNullable(item.externalAccountMasked) }}</td>
                      <td>{{ formatNullable(item.externalEmailMasked) }}</td>
                      <td>{{ formatNullableDate(item.lastLoginTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无第三方绑定" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="passwordHistories" :tab="`密码历史 (${currentDetail.passwordHistories.length})`">
                <table v-if="currentDetail.passwordHistories.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>用户</th>
                      <th>变更时间</th>
                      <th>创建时间</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.passwordHistories" :key="item.basicId">
                      <td>{{ formatNullable(item.realName || item.nickName || item.userName) }}</td>
                      <td>{{ formatNullableDate(item.changedTime) }}</td>
                      <td>{{ formatNullableDate(item.createdTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无密码历史" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="statistics" :tab="`统计 (${currentDetail.statistics.length})`">
                <table v-if="currentDetail.statistics.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>周期</th>
                      <th>统计日期</th>
                      <th>登录</th>
                      <th>操作</th>
                      <th>API</th>
                      <th>访问</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.statistics" :key="item.basicId">
                      <td>{{ getOptionLabel(statisticsPeriodOptions, item.period) }}</td>
                      <td>{{ formatNullableDate(item.statisticsDate) }}</td>
                      <td>{{ item.loginCount }}</td>
                      <td>{{ item.operationCount }}</td>
                      <td>{{ item.apiCallCount }}</td>
                      <td>{{ item.accessCount }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无统计数据" style="padding: 40px 0" />
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
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="userForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="用户名" path="userName">
          <NInput
            v-model:value="userForm.userName"
            :disabled="Boolean(userForm.basicId)"
            clearable
            placeholder="请输入用户名"
          />
        </NFormItem>
        <NFormItem v-if="!userForm.basicId" label="初始密码" path="initialPassword">
          <NInput
            v-model:value="userForm.initialPassword"
            clearable
            placeholder="请输入初始密码"
            show-password-on="click"
            type="password"
          />
        </NFormItem>
        <NFormItem label="真实姓名" path="realName">
          <NInput v-model:value="userForm.realName" clearable placeholder="请输入真实姓名" />
        </NFormItem>
        <NFormItem label="昵称" path="nickName">
          <NInput v-model:value="userForm.nickName" clearable placeholder="请输入昵称" />
        </NFormItem>
        <NFormItem label="性别" path="gender">
          <NSelect v-model:value="userForm.gender" :options="genderOptions" />
        </NFormItem>
        <NFormItem label="手机号" path="phone">
          <NInput v-model:value="userForm.phone" clearable placeholder="请输入手机号" />
        </NFormItem>
        <NFormItem label="邮箱" path="email">
          <NInput v-model:value="userForm.email" clearable placeholder="请输入邮箱" />
        </NFormItem>
        <NFormItem label="语言" path="language">
          <NSelect v-model:value="userForm.language" :options="languageOptions" clearable placeholder="选择语言" />
        </NFormItem>
        <NFormItem v-if="!userForm.basicId" label="成员类型" path="memberType">
          <NSelect v-model:value="userForm.memberType" :options="memberTypeOptions" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="userForm.remark"
            clearable
            placeholder="请输入备注"
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
