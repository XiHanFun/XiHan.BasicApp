<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysUserSession } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  NSwitch,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import {
  createUserSessionApi,
  deleteUserSessionApi,
  getUserSessionPageApi,
  revokeUserSessionsApi,
  updateUserSessionApi,
} from '~/api'
import { DEFAULT_PAGE_SIZE, DEVICE_TYPE_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserSessionPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysUserSession[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  deviceType: undefined as number | undefined,
  isOnline: undefined as number | undefined,
  isRevoked: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增会话')
const submitLoading = ref(false)

const formData = ref<Partial<SysUserSession & { revokedReason?: string }>>({
  userId: undefined,
  sessionId: '',
  deviceType: 0,
  deviceName: '',
  ipAddress: '',
  isOnline: true,
  isRevoked: false,
  revokedReason: '',
  remark: '',
})

const revokeModalVisible = ref(false)
const revokeLoading = ref(false)
const revokeFormData = ref({
  userId: undefined as number | undefined,
  reason: '管理员手动撤销',
})

const onlineOptions = [
  { label: '在线', value: 1 },
  { label: '离线', value: 0 },
]

const revokedOptions = [
  { label: '已撤销', value: 1 },
  { label: '未撤销', value: 0 },
]

async function fetchData() {
  try {
    loading.value = true
    const result = await getUserSessionPageApi({
      ...queryParams,
      isOnline: queryParams.isOnline === undefined ? undefined : queryParams.isOnline === 1,
      isRevoked: queryParams.isRevoked === undefined ? undefined : queryParams.isRevoked === 1,
    })
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取会话列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增会话'
  formData.value = {
    userId: undefined,
    sessionId: '',
    deviceType: 0,
    deviceName: '',
    ipAddress: '',
    isOnline: true,
    isRevoked: false,
    revokedReason: '',
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysUserSession) {
  modalTitle.value = '编辑会话'
  formData.value = { ...row }
  modalVisible.value = true
}

function handleOpenRevoke(row: SysUserSession) {
  revokeFormData.value = {
    userId: row.userId,
    reason: row.isOnline ? '账号强制下线' : '手动撤销历史会话',
  }
  revokeModalVisible.value = true
}

async function handleConfirmRevoke() {
  if (!revokeFormData.value.userId || !revokeFormData.value.reason.trim()) {
    message.warning('请填写用户ID与撤销原因')
    return
  }

  try {
    revokeLoading.value = true
    const count = await revokeUserSessionsApi(
      revokeFormData.value.userId,
      revokeFormData.value.reason.trim(),
    )
    message.success(`已撤销 ${count} 个会话`)
    revokeModalVisible.value = false
    fetchData()
  } catch {
    message.error('撤销会话失败')
  } finally {
    revokeLoading.value = false
  }
}

async function handleDelete(id: string) {
  try {
    await deleteUserSessionApi(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateUserSessionApi(formData.value.basicId, formData.value)
    } else {
      await createUserSessionApi(formData.value)
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

const columns: DataTableColumns<SysUserSession> = [
  {
    title: '用户ID',
    key: 'userId',
    width: 90,
  },
  {
    title: '会话ID',
    key: 'sessionId',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '设备类型',
    key: 'deviceType',
    width: 120,
    render: (row) => getOptionLabel(DEVICE_TYPE_OPTIONS, row.deviceType),
  },
  {
    title: '设备名称',
    key: 'deviceName',
    width: 150,
    ellipsis: { tooltip: true },
  },
  {
    title: 'IP地址',
    key: 'ipAddress',
    width: 130,
  },
  {
    title: '在线状态',
    key: 'isOnline',
    width: 100,
    render: (row) =>
      h(
        NTag,
        { type: row.isOnline ? 'success' : 'default', size: 'small', round: true },
        { default: () => (row.isOnline ? '在线' : '离线') },
      ),
  },
  {
    title: '撤销状态',
    key: 'isRevoked',
    width: 100,
    render: (row) =>
      h(
        NTag,
        { type: row.isRevoked ? 'error' : 'success', size: 'small', round: true },
        { default: () => (row.isRevoked ? '已撤销' : '正常') },
      ),
  },
  {
    title: '最后活跃',
    key: 'lastActivityTime',
    width: 170,
    render: (row) => formatDate(row.lastActivityTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 240,
    fixed: 'right',
    render: (row) =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () =>
            [
              h(
                NButton,
                {
                  size: 'small',
                  type: 'primary',
                  ghost: true,
                  onClick: () => handleEdit(row),
                },
                { default: () => '编辑' },
              ),
              !row.isRevoked &&
                h(
                  NButton,
                  {
                    size: 'small',
                    type: 'warning',
                    ghost: true,
                    onClick: () => handleOpenRevoke(row),
                  },
                  { default: () => '撤销' },
                ),
              h(
                NPopconfirm,
                {
                  onPositiveClick: () => handleDelete(row.basicId),
                },
                {
                  default: () => '确认删除该会话？',
                  trigger: () =>
                    h(
                      NButton,
                      { size: 'small', type: 'error', ghost: true },
                      { default: () => '删除' },
                    ),
                },
              ),
            ].filter(Boolean),
        },
      ),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <NCard :bordered="false">
      <div class="flex flex-wrap items-center gap-3">
        <NInput
          v-model:value="queryParams.keyword"
          placeholder="搜索会话ID/设备/IP"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.deviceType"
          :options="DEVICE_TYPE_OPTIONS"
          placeholder="设备类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.isOnline"
          :options="onlineOptions"
          placeholder="在线状态"
          style="width: 120px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.isRevoked"
          :options="revokedOptions"
          placeholder="撤销状态"
          style="width: 120px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.deviceType = undefined
              queryParams.isOnline = undefined
              queryParams.isRevoked = undefined
              queryParams.page = 1
              fetchData()
            }
          "
        >
          重置
        </NButton>
        <NButton class="ml-auto" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增会话
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.basicId"
        :pagination="false"
        :scroll-x="1520"
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
          @update:page="fetchData"
          @update:page-size="
            () => {
              queryParams.page = 1
              fetchData()
            }
          "
        />
      </div>
    </NCard>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 600px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="95px">
        <NFormItem label="用户ID" path="userId">
          <NInputNumber v-model:value="formData.userId" :disabled="!!formData.basicId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="会话ID" path="sessionId">
          <NInput
            v-model:value="formData.sessionId"
            :disabled="!!formData.basicId"
            placeholder="请输入会话ID"
          />
        </NFormItem>
        <NFormItem label="设备类型" path="deviceType">
          <NSelect v-model:value="formData.deviceType" :options="DEVICE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="设备名称" path="deviceName">
          <NInput v-model:value="formData.deviceName" placeholder="可选" />
        </NFormItem>
        <NFormItem label="IP地址" path="ipAddress">
          <NInput v-model:value="formData.ipAddress" placeholder="可选" />
        </NFormItem>
        <NFormItem label="在线状态" path="isOnline">
          <NSwitch v-model:value="formData.isOnline" />
        </NFormItem>
        <NFormItem label="撤销状态" path="isRevoked">
          <NSwitch v-model:value="formData.isRevoked" />
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
      v-model:show="revokeModalVisible"
      title="撤销用户会话"
      preset="card"
      style="width: 520px"
      :auto-focus="false"
    >
      <NForm :model="revokeFormData" label-placement="left" label-width="88px">
        <NFormItem label="用户ID" path="userId">
          <NInputNumber v-model:value="revokeFormData.userId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="撤销原因" path="reason">
          <NInput
            v-model:value="revokeFormData.reason"
            type="textarea"
            :rows="3"
            placeholder="请输入撤销原因"
          />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="revokeModalVisible = false">取消</NButton>
          <NButton type="warning" :loading="revokeLoading" @click="handleConfirmRevoke">确认撤销</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
