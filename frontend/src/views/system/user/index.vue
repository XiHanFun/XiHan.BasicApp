<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysUser } from '@/api/modules/user'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { userApi } from '@/api'
import { GENDER_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemUserPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return userApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysUser>(
  {
    id: 'sys_user',
    name: '用户管理',
    columns: [
      { type: 'checkbox', width: 40, fixed: 'left' },
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'userName',
        title: '用户名',
        minWidth: 130,
        showOverflow: 'tooltip',
        sortable: true,
      },
      { field: 'nickName', title: '昵称', minWidth: 130, showOverflow: 'tooltip' },
      { field: 'realName', title: '真实姓名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'email', title: '邮箱', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'phone', title: '手机号', minWidth: 130, showOverflow: 'tooltip' },
      {
        field: 'lastLoginTime',
        title: '最后登录',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      { field: 'lastLoginIp', title: '最后登录IP', minWidth: 130, showOverflow: 'tooltip' },
      {
        field: 'gender',
        title: '性别',
        width: 70,
        formatter: ({ cellValue }) => {
          const map: Record<number, string> = { 0: '未知', 1: '男', 2: '女' }
          return map[cellValue] ?? '未知'
        },
      },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'createTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'actions',
        title: '操作',
        width: 220,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
    checkboxConfig: { range: true, reserve: true },
    rowConfig: { keyField: 'basicId' },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

// ==================== CRUD ====================

const modalVisible = ref(false)
const modalTitle = ref('新增用户')
const submitLoading = ref(false)
const formData = ref<Partial<SysUser & { password?: string }>>({})

function resetForm() {
  formData.value = {
    userName: '',
    nickName: '',
    email: '',
    phone: '',
    gender: 0,
    status: 1,
    password: '',
  }
}

function handleAdd() {
  modalTitle.value = '新增用户'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysUser) {
  modalTitle.value = '编辑用户'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await userApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  } catch {
    message.error('删除失败')
  }
}

async function handleToggleStatus(row: any) {
  const newStatus = row.status === 1 ? 0 : 1
  try {
    await userApi.changeStatus(row.basicId, newStatus)
    message.success('状态更新成功')
    xGrid.value?.commitProxy('query')
  } catch {
    message.error('状态更新失败')
  }
}

const resetPwdVisible = ref(false)
const resetPwdUserId = ref('')
const resetPwdValue = ref('')

function handleResetPwd(row: any) {
  resetPwdUserId.value = row.basicId
  resetPwdValue.value = ''
  resetPwdVisible.value = true
}

async function confirmResetPwd() {
  if (!resetPwdValue.value) {
    message.warning('请输入新密码')
    return
  }
  try {
    await userApi.resetPassword(resetPwdUserId.value, resetPwdValue.value)
    message.success('密码重置成功')
    resetPwdVisible.value = false
  } catch {
    message.error('密码重置失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await userApi.update(formData.value.basicId, formData.value)
    } else {
      await userApi.create(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  } catch {
    message.error('操作失败')
  } finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <vxe-card class="mb-2" style="padding: 10px 16px">
      <div class="flex flex-wrap gap-3 items-center">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索用户名/昵称/邮箱/手机"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          clearable
          style="width: 120px"
        />
        <NButton type="primary" size="small" @click="handleSearch">查询</NButton>
        <NButton size="small" @click="handleReset">重置</NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">新增用户</NButton>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleEdit(row)">编辑</NButton>
            <NButton size="small" type="warning" text @click="handleToggleStatus(row)">
              {{ row.status === 1 ? '禁用' : '启用' }}
            </NButton>
            <NButton size="small" type="info" text @click="handleResetPwd(row)">重置密码</NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>删除</NButton>
              </template>
              确认删除该用户？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 520px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="80px">
        <NFormItem label="用户名" path="userName">
          <NInput
            v-model:value="formData.userName"
            :disabled="!!formData.basicId"
            placeholder="请输入用户名"
          />
        </NFormItem>
        <NFormItem v-if="!formData.basicId" label="密码" path="password">
          <NInput
            v-model:value="formData.password"
            type="password"
            show-password-on="click"
            placeholder="请输入初始密码"
          />
        </NFormItem>
        <NFormItem label="昵称" path="nickName">
          <NInput v-model:value="formData.nickName" placeholder="请输入昵称" />
        </NFormItem>
        <NFormItem label="邮箱" path="email">
          <NInput v-model:value="formData.email" placeholder="请输入邮箱" />
        </NFormItem>
        <NFormItem label="手机号" path="phone">
          <NInput v-model:value="formData.phone" placeholder="请输入手机号" />
        </NFormItem>
        <NFormItem label="性别" path="gender">
          <NSelect v-model:value="formData.gender" :options="GENDER_OPTIONS" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">确认</NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="resetPwdVisible"
      title="重置密码"
      preset="card"
      style="width: 400px"
      :auto-focus="false"
    >
      <NInput
        v-model:value="resetPwdValue"
        type="password"
        show-password-on="click"
        placeholder="请输入新密码"
      />
      <template #footer>
        <NSpace justify="end">
          <NButton @click="resetPwdVisible = false">取消</NButton>
          <NButton type="primary" @click="confirmResetPwd">确认重置</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
