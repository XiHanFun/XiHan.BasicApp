<script lang="ts" setup>
import { ref, h, reactive, onMounted } from 'vue'
import {
  NCard,
  NDataTable,
  NButton,
  NSpace,
  NInput,
  NSelect,
  NTag,
  NAvatar,
  NPopconfirm,
  NModal,
  NForm,
  NFormItem,
  NIcon,
  NPagination,
  useMessage,
  useDialog,
} from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'
import { Icon } from '@iconify/vue'
import type { SysUser } from '~/types'
import { STATUS_OPTIONS, GENDER_OPTIONS, DEFAULT_PAGE_SIZE } from '~/constants'
import { formatDate, getStatusType } from '~/utils'
import {
  getUserPageApi,
  createUserApi,
  updateUserApi,
  deleteUserApi,
  updateUserStatusApi,
} from '@/api'

defineOptions({ name: 'SystemUserPage' })

const message = useMessage()
const dialog = useDialog()

const loading = ref(false)
const tableData = ref<SysUser[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  status: undefined as number | undefined,
})

const selectedRowKeys = ref<string[]>([])
const modalVisible = ref(false)
const modalTitle = ref('新增用户')
const submitLoading = ref(false)

const formRef = ref(null)
const formData = ref<Partial<SysUser & { password?: string }>>({
  username: '',
  nickname: '',
  email: '',
  phone: '',
  gender: 0,
  status: 1,
  roles: [],
  password: '',
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getUserPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取用户列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增用户'
  formData.value = { username: '', nickname: '', email: '', phone: '', gender: 0, status: 1, roles: [], password: '' }
  modalVisible.value = true
}

function handleEdit(row: SysUser) {
  modalTitle.value = '编辑用户'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteUserApi(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleStatusChange(row: SysUser) {
  const newStatus = row.status === 1 ? 0 : 1
  try {
    await updateUserStatusApi(row.id, newStatus)
    row.status = newStatus
    message.success('状态更新成功')
  } catch {
    message.error('状态更新失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.id) {
      await updateUserApi(formData.value.id, formData.value)
    } else {
      await createUserApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  } catch {
    message.error('操作失败')
  } finally {
    submitLoading.value = false
  }
}

function handleSearch() {
  queryParams.page = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  queryParams.page = 1
  fetchData()
}

const columns: DataTableColumns<SysUser> = [
  {
    type: 'selection',
    fixed: 'left',
  },
  {
    title: '用户',
    key: 'username',
    width: 180,
    render: (row) =>
      h('div', { class: 'flex items-center gap-2' }, [
        h(NAvatar, {
          round: true,
          size: 32,
          fallbackSrc: `https://api.dicebear.com/9.x/initials/svg?seed=${row.nickname}`,
        }),
        h('div', null, [
          h('div', { class: 'text-sm font-medium' }, row.nickname),
          h('div', { class: 'text-xs text-gray-400' }, `@${row.username}`),
        ]),
      ]),
  },
  {
    title: '邮箱',
    key: 'email',
    width: 200,
    ellipsis: { tooltip: true },
  },
  {
    title: '手机号',
    key: 'phone',
    width: 130,
  },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render: (row) =>
      h(
        NTag,
        { type: getStatusType(row.status), size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: (row) => formatDate(row.createTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    fixed: 'right',
    render: (row) =>
      h(NSpace, { size: 'small' }, {
        default: () => [
          h(NButton, {
            size: 'small',
            type: 'primary',
            ghost: true,
            onClick: () => handleEdit(row),
          }, { default: () => '编辑' }),
          h(NPopconfirm, {
            onPositiveClick: () => handleDelete(row.id),
          }, {
            default: () => '确认删除该用户？',
            trigger: () =>
              h(NButton, { size: 'small', type: 'error', ghost: true }, { default: () => '删除' }),
          }),
        ],
      }),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <!-- 搜索栏 -->
    <NCard :bordered="false">
      <div class="flex flex-wrap items-center gap-3">
        <NInput
          v-model:value="queryParams.keyword"
          placeholder="搜索用户名/昵称/邮箱"
          style="width: 220px"
          clearable
          @keydown.enter="handleSearch"
        >
          <template #prefix>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
        </NInput>
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="全部状态"
          style="width: 120px"
          clearable
        />
        <NButton type="primary" @click="handleSearch">
          <template #icon><NIcon><Icon icon="lucide:search" width="14" /></NIcon></template>
          搜索
        </NButton>
        <NButton @click="handleReset">
          <template #icon><NIcon><Icon icon="lucide:rotate-ccw" width="14" /></NIcon></template>
          重置
        </NButton>
        <div class="ml-auto flex gap-2">
          <NButton type="primary" @click="handleAdd">
            <template #icon><NIcon><Icon icon="lucide:plus" width="14" /></NIcon></template>
            新增用户
          </NButton>
        </div>
      </div>
    </NCard>

    <!-- 数据表格 -->
    <NCard :bordered="false">
      <NDataTable
        v-model:checked-row-keys="selectedRowKeys"
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.id"
        :scroll-x="900"
        :pagination="false"
        size="small"
        striped
      />
      <div class="mt-4 flex justify-end">
        <NPagination
          v-model:page="queryParams.page"
          v-model:page-size="queryParams.pageSize"
          :item-count="total"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          show-quick-jumper
          @update:page="fetchData"
          @update:page-size="() => { queryParams.page = 1; fetchData() }"
        />
      </div>
    </NCard>

    <!-- 新增/编辑弹窗 -->
    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 520px"
      :auto-focus="false"
    >
      <NForm
        ref="formRef"
        :model="formData"
        label-placement="left"
        label-width="80px"
      >
        <NFormItem label="用户名" path="username">
          <NInput v-model:value="formData.username" :disabled="!!formData.id" placeholder="请输入用户名" />
        </NFormItem>
        <NFormItem v-if="!formData.id" label="密码" path="password">
          <NInput v-model:value="formData.password" type="password" show-password-on="click" placeholder="请输入初始密码" />
        </NFormItem>
        <NFormItem label="昵称" path="nickname">
          <NInput v-model:value="formData.nickname" placeholder="请输入昵称" />
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
  </div>
</template>
