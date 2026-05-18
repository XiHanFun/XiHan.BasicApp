<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { ApiId, UserCreateDto, UserListItemDto, UserManagementDetailDto, UserUpdateDto } from '@/api'
import type { DepartmentTreeNodeDto } from '@/api/modules/organization/department.types'
import {
  NButton,
  NCard,
  NDataTable,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NPagination,
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NStatistic,
  NTabPane,
  NTabs,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import {
  createPageRequest,
  departmentApi,
  EnableStatus,
  TenantMemberType,
  UserGender,
  userManagementApi,
} from '@/api'
import { Icon } from '~/components'
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
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserPage' })

interface UserFormModel extends UserCreateDto {
  basicId?: ApiId
}

const message = useMessage()

const tableLoading = ref(false)
const dataList = ref<UserListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)
const selectedRowKeys = ref<ApiId[]>([])

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

const departmentTree = ref<DepartmentTreeNodeDto[]>([])
const selectedDeptId = ref<ApiId | null>(null)
const deptExpandedKeys = ref<ApiId[]>([])

const genderOptions = GENDER_OPTIONS
const statusOptions = STATUS_OPTIONS
const memberTypeOptions = MEMBER_TYPE_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS
const roleTypeOptions = ROLE_TYPE_OPTIONS
const dataScopeOptions = DATA_SCOPE_OPTIONS
const permissionActionOptions = PERMISSION_ACTION_OPTIONS
const twoFactorMethodOptions = TWO_FACTOR_METHOD_OPTIONS
const deviceTypeOptions = DEVICE_TYPE_OPTIONS
const statisticsPeriodOptions = STATISTICS_PERIOD_OPTIONS

const languageOptions = [
  { label: '简体中文', value: 'zh-CN' },
  { label: '繁體中文', value: 'zh-TW' },
  { label: 'English', value: 'en-US' },
  { label: '日本語', value: 'ja-JP' },
  { label: '한국어', value: 'ko-KR' },
]

const modalTitle = computed(() => (userForm.value.basicId ? '编辑用户' : '新增用户'))
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const statsTotal = computed(() => totalCount.value)
const statsEnabled = computed(() => dataList.value.filter(u => u.status === EnableStatus.Enabled).length)
const statsDisabled = computed(() => dataList.value.filter(u => u.status === EnableStatus.Disabled).length)
const statsSystem = computed(() => dataList.value.filter(u => u.isSystemAccount).length)

const avatarColors = ['#6366f1', '#8b5cf6', '#ec4899', '#f43f5e', '#f97316', '#eab308', '#22c55e', '#14b8a6', '#06b6d4', '#3b82f6']

function getAvatarColor(name: string) {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash)
  }
  return avatarColors[Math.abs(hash) % avatarColors.length]
}

function getAvatarText(row: UserListItemDto) {
  const name = row.realName || row.nickName || row.userName
  return name ? name.charAt(0).toUpperCase() : '?'
}

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
  if (value === undefined || value === null) return '-'
  return value ? '是' : '否'
}

const tableColumns = computed<DataTableColumns<UserListItemDto>>(() => [
  {
    key: 'userName',
    title: '用户信息',
    minWidth: 220,
    sorter: true,
    render(row) {
      return h('div', { style: 'display:flex;align-items:center;gap:12px;cursor:pointer;', onClick: () => handleView(row) }, [
        h('div', {
          style: `width:38px;height:38px;border-radius:50%;display:flex;align-items:center;justify-content:center;font-weight:600;font-size:14px;color:#fff;flex-shrink:0;background:${getAvatarColor(row.userName)}`,
        }, getAvatarText(row)),
        h('div', { style: 'min-width:0;' }, [
          h('div', { style: 'font-size:14px;font-weight:600;color:var(--n-text-color);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;' }, row.userName),
          h('div', { style: 'font-size:12px;color:var(--n-text-color-3);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;' }, row.realName || row.nickName || '-'),
        ]),
      ])
    },
  },
  {
    key: 'gender',
    title: '性别',
    width: 80,
    render(row) {
      const label = getOptionLabel(genderOptions, row.gender)
      const typeMap: Record<number, 'default' | 'info' | 'success'> = { 0: 'default', 1: 'info', 2: 'success' }
      return h(NTag, { size: 'small', round: true, type: typeMap[row.gender] ?? 'default', bordered: false }, () => label)
    },
  },
  {
    key: 'status',
    title: '状态',
    width: 80,
    render(row) {
      return h(NTag, {
        size: 'small',
        round: true,
        type: row.status === EnableStatus.Enabled ? 'success' : 'error',
        bordered: false,
      }, () => row.status === EnableStatus.Enabled ? '启用' : '禁用')
    },
  },
  {
    key: 'isSystemAccount',
    title: '系统账号',
    width: 90,
    render(row) {
      return row.isSystemAccount
        ? h(NTag, { size: 'small', round: true, type: 'warning', bordered: false }, () => '是')
        : h('span', { style: 'color:var(--n-text-color-3)' }, '否')
    },
  },
  {
    key: 'language',
    title: '语言',
    width: 90,
    render(row) {
      return h('span', {}, row.language || '-')
    },
  },
  {
    key: 'lastLoginTime',
    title: '最后登录',
    minWidth: 160,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatNullableDate(row.lastLoginTime))
    },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 160,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 130,
    fixed: 'right',
    render(row) {
      return h('div', { style: 'display:flex;align-items:center;gap:2px;' }, [
        h(NTooltip, {}, {
          trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, onClick: () => handleView(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '查看详情',
        }),
        h(NTooltip, {}, {
          trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, type: 'primary', onClick: () => handleEdit(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
          default: () => '编辑',
        }),
        h(NPopconfirm, { onPositiveClick: () => handleToggleStatus(row) }, {
          trigger: () => h(NTooltip, {}, {
            trigger: () => h(NButton, {
              size: 'small',
              quaternary: true,
              circle: true,
              type: row.status === EnableStatus.Enabled ? 'warning' : 'success',
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:check' })) }),
            default: () => row.status === EnableStatus.Enabled ? '禁用' : '启用',
          }),
          default: () => `确认${row.status === EnableStatus.Enabled ? '禁用' : '启用'}该用户？`,
        }),
      ])
    },
  },
])

function buildPageRequest() {
  return createPageRequest({
    page: { pageIndex: currentPage.value, pageSize: pageSize.value },
  })
}

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await userManagementApi.page({
      ...buildPageRequest(),
      gender: queryParams.gender,
      keyword: normalizeNullable(queryParams.keyword),
      status: queryParams.status,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  } catch {
    message.error('查询用户失败')
  } finally {
    tableLoading.value = false
  }
}

async function fetchDepartmentTree() {
  try {
    departmentTree.value = await departmentApi.tree({ limit: 500, onlyEnabled: true })
    const allIds: ApiId[] = []
    function collectExpanded(nodes: DepartmentTreeNodeDto[]) {
      for (const node of nodes) {
        if (node.children?.length) {
          allIds.push(node.basicId)
          collectExpanded(node.children)
        }
      }
    }
    collectExpanded(departmentTree.value)
    deptExpandedKeys.value = allIds
  } catch {
    // department tree is optional, ignore failure
  }
}

onMounted(() => {
  fetchData()
  fetchDepartmentTree()
})

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.gender = undefined
  queryParams.status = undefined
  selectedDeptId.value = null
  currentPage.value = 1
  fetchData()
}

function handlePageChange(page: number) {
  currentPage.value = page
  fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  fetchData()
}

function handleSorterChange(params: { columnKey: string | number | undefined, order: 'ascend' | 'descend' | false }) {
  // Remote sorting is handled by the backend default sort
  // This hook is available for future enhancement
  void params
}

function handleSelectionChange(keys: Array<string | number>) {
  selectedRowKeys.value = keys as ApiId[]
}

function handleDeptSelect(deptId: ApiId | null) {
  selectedDeptId.value = deptId
  currentPage.value = 1
  fetchData()
}

function getDeptUserCount(_deptId: ApiId): number {
  // Backend does not return per-department user count in tree API
  // This is a placeholder; actual count requires a dedicated API
  return 0
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
  } catch {
    message.error('加载用户详情失败')
  } finally {
    detailLoading.value = false
  }
}

async function handleToggleStatus(row: UserListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await userManagementApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    fetchData()
  } catch {
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
  if (!validateForm()) return
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
    } else {
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
    fetchData()
  } catch {
    message.error('保存失败')
  } finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="xh-user-page">
    <!-- 页头 -->
    <div class="xh-page-header">
      <div class="xh-page-header__content">
        <div>
          <h1 class="xh-page-header__title">用户管理</h1>
          <p class="xh-page-header__desc">管理系统用户信息、角色分配与账户状态</p>
        </div>
        <NButton type="primary" size="large" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:user-plus" /></NIcon>
          </template>
          新增用户
        </NButton>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="xh-stats-grid">
      <div class="xh-stat-card xh-stat-card--total">
        <div class="xh-stat-card__icon">
          <Icon icon="lucide:users" />
        </div>
        <NStatistic label="总用户" :value="statsTotal" />
      </div>
      <div class="xh-stat-card xh-stat-card--enabled">
        <div class="xh-stat-card__icon xh-stat-card__icon--green">
          <Icon icon="lucide:user-check" />
        </div>
        <NStatistic label="已启用" :value="statsEnabled" />
      </div>
      <div class="xh-stat-card xh-stat-card--disabled">
        <div class="xh-stat-card__icon xh-stat-card__icon--red">
          <Icon icon="lucide:user-x" />
        </div>
        <NStatistic label="已禁用" :value="statsDisabled" />
      </div>
      <div class="xh-stat-card xh-stat-card--system">
        <div class="xh-stat-card__icon xh-stat-card__icon--amber">
          <Icon icon="lucide:shield" />
        </div>
        <NStatistic label="系统账号" :value="statsSystem" />
      </div>
    </div>

    <!-- 主体区域 -->
    <div class="xh-main-layout">
      <!-- 部门树侧边栏 -->
      <div class="xh-dept-sidebar">
        <div class="xh-dept-sidebar__header">
          <span class="xh-dept-sidebar__title">组织架构</span>
        </div>
        <NScrollbar style="max-height: calc(100vh - 380px)">
          <div
            :class="['xh-dept-node', { 'xh-dept-node--active': selectedDeptId === null }]"
            @click="handleDeptSelect(null)"
          >
            <Icon icon="lucide:building-2" style="font-size:14px;" />
            <span>全部人员</span>
          </div>
          <template v-for="dept in departmentTree" :key="dept.basicId">
            <div
              :class="['xh-dept-node', { 'xh-dept-node--active': selectedDeptId === dept.basicId }]"
              @click="handleDeptSelect(dept.basicId)"
            >
              <span
                class="xh-dept-node__arrow"
                :class="{ 'xh-dept-node__arrow--expanded': deptExpandedKeys.includes(dept.basicId) }"
                @click.stop="deptExpandedKeys.includes(dept.basicId) ? deptExpandedKeys.splice(deptExpandedKeys.indexOf(dept.basicId), 1) : deptExpandedKeys.push(dept.basicId)"
              >
                <Icon icon="lucide:chevron-right" style="font-size:12px;" />
              </span>
              <Icon icon="lucide:folder" style="font-size:14px;" />
              <span>{{ dept.departmentName }}</span>
              <span class="xh-dept-node__count">{{ getDeptUserCount(dept.basicId) }}</span>
            </div>
            <div v-if="dept.children?.length && deptExpandedKeys.includes(dept.basicId)" class="xh-dept-children">
              <div
                v-for="child in dept.children"
                :key="child.basicId"
                :class="['xh-dept-node xh-dept-node--small', { 'xh-dept-node--active': selectedDeptId === child.basicId }]"
                @click="handleDeptSelect(child.basicId)"
              >
                <Icon icon="lucide:folder-open" style="font-size:12px;" />
                <span>{{ child.departmentName }}</span>
              </div>
            </div>
          </template>
        </NScrollbar>
      </div>

      <!-- 右侧主内容 -->
      <div class="xh-main-content">
        <NCard content-style="padding:0;" :bordered="false" class="xh-content-card">
          <!-- 筛选栏 -->
          <div class="xh-filter-bar">
            <div class="xh-filter-bar__fields">
              <NInput
                v-model:value="queryParams.keyword"
                clearable
                placeholder="搜索用户名、姓名、昵称"
                style="flex:1;min-width:200px;"
                @keyup.enter="handleSearch"
              >
                <template #prefix>
                  <NIcon><Icon icon="lucide:search" /></NIcon>
                </template>
              </NInput>
              <NSelect
                v-model:value="queryParams.gender"
                :options="genderOptions"
                clearable
                placeholder="性别"
                style="width:110px;"
              />
              <NSelect
                v-model:value="queryParams.status"
                :options="statusOptions"
                clearable
                placeholder="状态"
                style="width:110px;"
              />
              <NButton type="primary" @click="handleSearch">
                <template #icon>
                  <NIcon><Icon icon="lucide:search" /></NIcon>
                </template>
                查询
              </NButton>
              <NButton @click="handleReset">
                <template #icon>
                  <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
                </template>
                重置
              </NButton>
            </div>
          </div>

          <!-- 批量操作 -->
          <div v-if="selectedRowKeys.length" class="xh-batch-bar">
            <span class="xh-batch-bar__info">已选 {{ selectedRowKeys.length }} 项</span>
            <NButton size="small" type="error" quaternary @click="selectedRowKeys = []">
              取消选择
            </NButton>
          </div>

          <!-- 数据表格 -->
          <NDataTable
            :columns="tableColumns"
            :data="dataList"
            :loading="tableLoading"
            :bordered="false"
            :single-line="false"
            :row-key="(row: UserListItemDto) => row.basicId"
            :checked-row-keys="selectedRowKeys"
            :scroll-x="1100"
            size="small"
            striped
            flex-height
            style="flex:1;"
            @update:checked-row-keys="handleSelectionChange"
            @update:sorter="handleSorterChange"
          />

          <!-- 分页 -->
          <div class="xh-pagination-bar">
            <div class="xh-pagination-bar__info">
              共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页
            </div>
            <div class="xh-pagination-bar__controls">
              <NPagination
                :page="currentPage"
                :page-count="totalPages"
                :page-slot="7"
                :page-sizes="[10, 20, 50, 100]"
                :page-size="pageSize"
                show-size-picker
                @update:page="handlePageChange"
                @update:page-size="handlePageSizeChange"
              />
            </div>
          </div>
        </NCard>
      </div>
    </div>

    <!-- 用户详情抽屉 -->
    <NDrawer v-model:show="detailVisible" :width="720">
      <NDrawerContent closable>
        <template #header>
          <div style="display:flex;align-items:center;gap:12px;">
            <div
              v-if="currentDetail"
              class="xh-detail-avatar"
              :style="{ background: getAvatarColor(currentDetail.user.userName) }"
            >
              {{ getAvatarText(currentDetail.user) }}
            </div>
            <div>
              <div style="font-size:16px;font-weight:600;">用户详情</div>
              <div v-if="currentDetail" style="font-size:12px;color:var(--n-text-color-3);">
                {{ currentDetail.user.userName }}
              </div>
            </div>
          </div>
        </template>
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" description="暂无用户详情" style="padding:48px 0;">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height:calc(100vh - 120px);">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <div class="xh-detail-section">
                  <div class="xh-detail-section__title">基本信息</div>
                  <div class="xh-detail-info-grid">
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">用户名</span>
                      <span class="xh-detail-info-value">{{ currentDetail.user.userName }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">真实姓名</span>
                      <span class="xh-detail-info-value">{{ formatNullable(currentDetail.user.realName) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">昵称</span>
                      <span class="xh-detail-info-value">{{ formatNullable(currentDetail.user.nickName) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">性别</span>
                      <span class="xh-detail-info-value">{{ getOptionLabel(genderOptions, currentDetail.user.gender) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">状态</span>
                      <NTag :type="currentDetail.user.status === EnableStatus.Enabled ? 'success' : 'error'" size="small" round>
                        {{ currentDetail.user.status === EnableStatus.Enabled ? '启用' : '禁用' }}
                      </NTag>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">系统账号</span>
                      <NTag :type="currentDetail.user.isSystemAccount ? 'warning' : 'default'" size="small" round>
                        {{ currentDetail.user.isSystemAccount ? '是' : '否' }}
                      </NTag>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">语言</span>
                      <span class="xh-detail-info-value">{{ formatNullable(currentDetail.user.language) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">时区</span>
                      <span class="xh-detail-info-value">{{ formatNullable(currentDetail.user.timeZone) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">最后登录</span>
                      <span class="xh-detail-info-value">{{ formatNullableDate(currentDetail.user.lastLoginTime) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">创建时间</span>
                      <span class="xh-detail-info-value">{{ formatNullableDate(currentDetail.user.createdTime) }}</span>
                    </div>
                  </div>
                </div>
                <div class="xh-detail-section">
                  <div class="xh-detail-section__title">租户信息</div>
                  <div class="xh-detail-info-grid">
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">租户显示名</span>
                      <span class="xh-detail-info-value">{{ formatNullable(currentDetail.tenantMembership?.displayName) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">成员类型</span>
                      <span class="xh-detail-info-value">{{ getOptionLabel(memberTypeOptions, currentDetail.tenantMembership?.memberType) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">租户成员状态</span>
                      <span class="xh-detail-info-value">{{ getOptionLabel(validityStatusOptions, currentDetail.tenantMembership?.status) }}</span>
                    </div>
                    <div class="xh-detail-info-item">
                      <span class="xh-detail-info-label">聚合时间</span>
                      <span class="xh-detail-info-value">{{ formatNullableDate(currentDetail.generatedTime) }}</span>
                    </div>
                  </div>
                </div>
              </NTabPane>

              <NTabPane name="security" tab="安全设置">
                <NEmpty v-if="!currentDetail.security" description="暂无安全设置" style="padding:40px 0;" />
                <template v-else>
                  <div class="xh-detail-section">
                    <div class="xh-detail-section__title">登录安全</div>
                    <div class="xh-detail-info-grid">
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">允许多端登录</span>
                        <NTag :type="currentDetail.security.allowMultiLogin ? 'success' : 'default'" size="small" round>
                          {{ formatBoolean(currentDetail.security.allowMultiLogin) }}
                        </NTag>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">最大登录设备</span>
                        <span class="xh-detail-info-value">{{ currentDetail.security.maxLoginDevices || '不限' }}</span>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">两步验证</span>
                        <NTag :type="currentDetail.security.twoFactorEnabled ? 'success' : 'default'" size="small" round>
                          {{ formatBoolean(currentDetail.security.twoFactorEnabled) }}
                        </NTag>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">验证方式</span>
                        <span class="xh-detail-info-value">{{ getOptionLabel(twoFactorMethodOptions, currentDetail.security.twoFactorMethod) }}</span>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">邮箱验证</span>
                        <NTag :type="currentDetail.security.emailVerified ? 'success' : 'warning'" size="small" round>
                          {{ currentDetail.security.emailVerified ? '已验证' : '未验证' }}
                        </NTag>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">手机验证</span>
                        <NTag :type="currentDetail.security.phoneVerified ? 'success' : 'warning'" size="small" round>
                          {{ currentDetail.security.phoneVerified ? '已验证' : '未验证' }}
                        </NTag>
                      </div>
                    </div>
                  </div>
                  <div class="xh-detail-section">
                    <div class="xh-detail-section__title">锁定与密码</div>
                    <div class="xh-detail-info-grid">
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">账号锁定</span>
                        <NTag :type="currentDetail.security.isLocked ? 'error' : 'success'" size="small" round>
                          {{ currentDetail.security.isLocked ? '已锁定' : '正常' }}
                        </NTag>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">密码过期</span>
                        <NTag :type="currentDetail.security.isPasswordExpired ? 'error' : 'success'" size="small" round>
                          {{ currentDetail.security.isPasswordExpired ? '已过期' : '正常' }}
                        </NTag>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">失败次数</span>
                        <span class="xh-detail-info-value">{{ currentDetail.security.failedLoginAttempts }}</span>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">密码过期时间</span>
                        <span class="xh-detail-info-value">{{ formatNullableDate(currentDetail.security.passwordExpiryTime) }}</span>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">最后改密</span>
                        <span class="xh-detail-info-value">{{ formatNullableDate(currentDetail.security.lastPasswordChangeTime) }}</span>
                      </div>
                      <div class="xh-detail-info-item">
                        <span class="xh-detail-info-label">最后安全检查</span>
                        <span class="xh-detail-info-value">{{ formatNullableDate(currentDetail.security.lastSecurityCheckTime) }}</span>
                      </div>
                    </div>
                  </div>
                </template>
              </NTabPane>

              <NTabPane name="departments" :tab="`部门 (${currentDetail.departments.length})`">
                <NDataTable
                  v-if="currentDetail.departments.length"
                  :columns="[
                    { title: '部门', key: 'departmentName', render: (row) => formatNullable(row.departmentName) },
                    { title: '编码', key: 'departmentCode', render: (row) => formatNullable(row.departmentCode) },
                    { title: '主部门', key: 'isMain', width: 80, render: (row) => formatBoolean(row.isMain) },
                    { title: '状态', key: 'status', width: 80, render: (row) => getOptionLabel(validityStatusOptions, row.status) },
                    { title: '创建时间', key: 'createdTime', render: (row) => formatNullableDate(row.createdTime) },
                  ]"
                  :data="currentDetail.departments"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无部门分配" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="roles" :tab="`角色 (${currentDetail.roles.length})`">
                <NDataTable
                  v-if="currentDetail.roles.length"
                  :columns="[
                    { title: '角色', key: 'roleName', render: (row) => formatNullable(row.roleName) },
                    { title: '编码', key: 'roleCode', render: (row) => formatNullable(row.roleCode) },
                    { title: '类型', key: 'roleType', width: 80, render: (row) => getOptionLabel(roleTypeOptions, row.roleType) },
                    { title: '授权状态', key: 'status', width: 80, render: (row) => getOptionLabel(validityStatusOptions, row.status) },
                    { title: '有效期', key: 'period', render: (row) => `${formatNullableDate(row.effectiveTime)} 至 ${formatNullableDate(row.expirationTime)}` },
                  ]"
                  :data="currentDetail.roles"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无角色分配" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="permissions" :tab="`额外权限 (${currentDetail.permissions.length})`">
                <NDataTable
                  v-if="currentDetail.permissions.length"
                  :columns="[
                    { title: '权限', key: 'permissionName', render: (row) => formatNullable(row.permissionName) },
                    { title: '编码', key: 'permissionCode', render: (row) => formatNullable(row.permissionCode) },
                    { title: '动作', key: 'permissionAction', width: 80, render: (row) => getOptionLabel(permissionActionOptions, row.permissionAction) },
                    { title: '授权状态', key: 'status', width: 80, render: (row) => getOptionLabel(validityStatusOptions, row.status) },
                    { title: '有效期', key: 'period', render: (row) => `${formatNullableDate(row.effectiveTime)} 至 ${formatNullableDate(row.expirationTime)}` },
                  ]"
                  :data="currentDetail.permissions"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无额外权限" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="dataScopes" :tab="`数据范围 (${currentDetail.dataScopes.length})`">
                <NDataTable
                  v-if="currentDetail.dataScopes.length"
                  :columns="[
                    { title: '范围', key: 'dataScope', render: (row) => getOptionLabel(dataScopeOptions, row.dataScope) },
                    { title: '部门', key: 'departmentName', render: (row) => formatNullable(row.departmentName) },
                    { title: '包含子部门', key: 'includeChildren', width: 100, render: (row) => formatBoolean(row.includeChildren) },
                    { title: '状态', key: 'status', width: 80, render: (row) => getOptionLabel(validityStatusOptions, row.status) },
                    { title: '创建时间', key: 'createdTime', render: (row) => formatNullableDate(row.createdTime) },
                  ]"
                  :data="currentDetail.dataScopes"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无自定义数据范围" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="sessions" :tab="`登录会话 (${currentDetail.sessions.length})`">
                <NDataTable
                  v-if="currentDetail.sessions.length"
                  :columns="[
                    { title: '会话', key: 'userSessionId', ellipsis: { tooltip: true } },
                    { title: '设备', key: 'device', render: (row) => `${formatNullable(row.deviceName)} / ${getOptionLabel(deviceTypeOptions, row.deviceType)}` },
                    { title: 'IP', key: 'ipAddressMasked', render: (row) => formatNullable(row.ipAddressMasked) },
                    { title: '在线', key: 'isOnline', width: 70, render: (row) => formatBoolean(row.isOnline) },
                    { title: '最后活跃', key: 'lastActivityTime', render: (row) => formatNullableDate(row.lastActivityTime) },
                  ]"
                  :data="currentDetail.sessions"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无登录会话" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="externalLogins" :tab="`第三方绑定 (${currentDetail.externalLogins.length})`">
                <NDataTable
                  v-if="currentDetail.externalLogins.length"
                  :columns="[
                    { title: '提供方', key: 'provider', render: (row) => formatNullable(row.providerDisplayName || row.provider) },
                    { title: '账号', key: 'externalAccountMasked', render: (row) => formatNullable(row.externalAccountMasked) },
                    { title: '邮箱', key: 'externalEmailMasked', render: (row) => formatNullable(row.externalEmailMasked) },
                    { title: '最近登录', key: 'lastLoginTime', render: (row) => formatNullableDate(row.lastLoginTime) },
                  ]"
                  :data="currentDetail.externalLogins"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无第三方绑定" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="passwordHistories" :tab="`密码历史 (${currentDetail.passwordHistories.length})`">
                <NDataTable
                  v-if="currentDetail.passwordHistories.length"
                  :columns="[
                    { title: '用户', key: 'user', render: (row) => formatNullable(row.realName || row.nickName || row.userName) },
                    { title: '变更时间', key: 'changedTime', render: (row) => formatNullableDate(row.changedTime) },
                    { title: '创建时间', key: 'createdTime', render: (row) => formatNullableDate(row.createdTime) },
                  ]"
                  :data="currentDetail.passwordHistories"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无密码历史" style="padding:40px 0;" />
              </NTabPane>

              <NTabPane name="statistics" :tab="`统计 (${currentDetail.statistics.length})`">
                <NDataTable
                  v-if="currentDetail.statistics.length"
                  :columns="[
                    { title: '周期', key: 'period', render: (row) => getOptionLabel(statisticsPeriodOptions, row.period) },
                    { title: '统计日期', key: 'statisticsDate', render: (row) => formatNullableDate(row.statisticsDate) },
                    { title: '登录', key: 'loginCount', width: 70 },
                    { title: '操作', key: 'operationCount', width: 70 },
                    { title: 'API', key: 'apiCallCount', width: 70 },
                    { title: '访问', key: 'accessCount', width: 70 },
                  ]"
                  :data="currentDetail.statistics"
                  :bordered="true"
                  size="small"
                  :pagination="false"
                />
                <NEmpty v-else description="暂无统计数据" style="padding:40px 0;" />
              </NTabPane>
            </NTabs>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- 新增/编辑弹窗 -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width:720px;max-width:92vw;"
    >
      <NForm :model="userForm" label-placement="top">
        <div class="xh-form-grid">
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
          <NFormItem label="备注" path="remark" :span="2">
            <NInput
              v-model:value="userForm.remark"
              clearable
              placeholder="请输入备注"
              :rows="3"
              type="textarea"
            />
          </NFormItem>
        </div>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">取消</NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">保存</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.xh-user-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px 24px;
  height: 100%;
  overflow: hidden;
}

.xh-page-header {
  background: linear-gradient(135deg, #312e81 0%, #4338ca 50%, #6366f1 100%);
  border-radius: 14px;
  padding: 24px 28px;
  color: #fff;
  position: relative;
  overflow: hidden;
}

.xh-page-header::before {
  content: '';
  position: absolute;
  top: -50%;
  right: -10%;
  width: 260px;
  height: 260px;
  background: radial-gradient(circle, rgba(255,255,255,.08) 0%, transparent 70%);
  border-radius: 50%;
}

.xh-page-header::after {
  content: '';
  position: absolute;
  bottom: -30%;
  left: 20%;
  width: 180px;
  height: 180px;
  background: radial-gradient(circle, rgba(255,255,255,.05) 0%, transparent 70%);
  border-radius: 50%;
}

.xh-page-header__content {
  position: relative;
  z-index: 1;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.xh-page-header__title {
  font-size: 22px;
  font-weight: 700;
  margin: 0 0 4px;
}

.xh-page-header__desc {
  font-size: 13px;
  opacity: .75;
  margin: 0;
}

.xh-stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 14px;
}

.xh-stat-card {
  background: #fff;
  border-radius: 12px;
  padding: 18px 20px;
  box-shadow: 0 1px 3px rgba(0,0,0,.04);
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all .25s cubic-bezier(.16,1,.3,1);
}

.xh-stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 24px rgba(0,0,0,.07);
}

.xh-stat-card__icon {
  width: 46px;
  height: 46px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
  background: #eef2ff;
  color: #6366f1;
}

.xh-stat-card__icon--green {
  background: #ecfdf5;
  color: #059669;
}

.xh-stat-card__icon--red {
  background: #fef2f2;
  color: #ef4444;
}

.xh-stat-card__icon--amber {
  background: #fffbeb;
  color: #d97706;
}

.xh-main-layout {
  display: flex;
  gap: 16px;
  flex: 1;
  min-height: 0;
  align-items: flex-start;
}

.xh-dept-sidebar {
  width: 240px;
  flex-shrink: 0;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0,0,0,.04);
  overflow: hidden;
  position: sticky;
  top: 20px;
}

.xh-dept-sidebar__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 16px 12px;
  border-bottom: 1px solid var(--n-border-color);
}

.xh-dept-sidebar__title {
  font-size: 14px;
  font-weight: 700;
  color: var(--n-text-color);
}

.xh-dept-node {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  font-size: 13px;
  color: var(--n-text-color-2);
  cursor: pointer;
  transition: all .15s ease;
  border-radius: 0;
}

.xh-dept-node:hover {
  background: var(--n-hover-color);
  color: var(--n-text-color);
}

.xh-dept-node--active {
  background: #eef2ff;
  color: #4f46e5;
  font-weight: 600;
}

.xh-dept-node--small {
  padding: 6px 16px 6px 24px;
  font-size: 12px;
}

.xh-dept-node__arrow {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  border-radius: 4px;
  transition: transform .2s ease;
}

.xh-dept-node__arrow--expanded {
  transform: rotate(90deg);
}

.xh-dept-node__count {
  margin-left: auto;
  font-size: 11px;
  color: var(--n-text-color-3);
}

.xh-dept-children {
  border-left: 2px solid var(--n-border-color);
  margin-left: 24px;
}

.xh-main-content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  height: calc(100vh - 310px);
}

.xh-content-card {
  display: flex;
  flex-direction: column;
  height: 100%;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0,0,0,.04);
}

.xh-filter-bar {
  padding: 16px 20px;
  border-bottom: 1px solid var(--n-border-color);
}

.xh-filter-bar__fields {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
}

.xh-batch-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 20px;
  background: #eef2ff;
  border-bottom: 1px solid #c7d2fe;
}

.xh-batch-bar__info {
  font-size: 13px;
  font-weight: 600;
  color: #4f46e5;
}

.xh-pagination-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 20px;
  border-top: 1px solid var(--n-border-color);
  flex-shrink: 0;
}

.xh-pagination-bar__info {
  font-size: 13px;
  color: var(--n-text-color-3);
}

.xh-pagination-bar__info strong {
  color: var(--n-text-color-2);
  font-weight: 600;
}

.xh-detail-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 14px;
  color: #fff;
  flex-shrink: 0;
}

.xh-detail-section {
  margin-bottom: 24px;
}

.xh-detail-section__title {
  font-size: 13px;
  font-weight: 700;
  color: var(--n-text-color-3);
  text-transform: uppercase;
  letter-spacing: .6px;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--n-border-color);
}

.xh-detail-info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.xh-detail-info-item {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.xh-detail-info-label {
  font-size: 12px;
  color: var(--n-text-color-3);
  font-weight: 500;
}

.xh-detail-info-value {
  font-size: 14px;
  color: var(--n-text-color);
  font-weight: 500;
}

.xh-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0 20px;
}

.xh-form-grid [span="2"] {
  grid-column: 1 / -1;
}
</style>
