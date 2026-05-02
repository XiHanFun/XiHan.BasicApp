<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, UserCreateDto, UserListItemDto, UserUpdateDto } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  TenantMemberType,
  userApi,
  UserGender,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
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
const userForm = ref<UserFormModel>(createDefaultForm())

const queryParams = reactive({
  gender: undefined as UserGender | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const genderOptions = [
  { label: '未知', value: UserGender.Unknown },
  { label: '男', value: UserGender.Male },
  { label: '女', value: UserGender.Female },
]

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

const memberTypeOptions = [
  { label: '负责人', value: TenantMemberType.Owner },
  { label: '管理员', value: TenantMemberType.Admin },
  { label: '成员', value: TenantMemberType.Member },
  { label: '外部成员', value: TenantMemberType.External },
  { label: '访客', value: TenantMemberType.Guest },
]

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

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<UserGridResult> {
  return userApi
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
        width: 100,
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

async function handleToggleStatus(row: UserListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await userApi.updateStatus({ basicId: row.basicId, status: nextStatus })
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
      await userApi.update(updateInput)
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
      await userApi.create(createInput)
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
          <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:pencil" /></NIcon>
            </template>
          </NButton>
          <NButton
            :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
            aria-label="切换状态"
            circle
            quaternary
            size="small"
            @click="handleToggleStatus(row)"
          >
            <template #icon>
              <NIcon>
                <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:check'" />
              </NIcon>
            </template>
          </NButton>
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
          <NInput v-model:value="userForm.language" clearable placeholder="如: zh-CN" />
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
