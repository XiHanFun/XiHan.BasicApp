<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysNotification } from '~/types'
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
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import {
  createNotificationApi,
  deleteNotificationApi,
  getNotificationPageApi,
  getUnreadNotificationCountApi,
  markAllNotificationsReadApi,
  markNotificationReadApi,
  updateNotificationApi,
} from '~/api'
import {
  DEFAULT_PAGE_SIZE,
  NOTIFICATION_STATUS_OPTIONS,
  NOTIFICATION_TYPE_OPTIONS,
  STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemNotificationPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysNotification[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  notificationType: undefined as number | undefined,
  notificationStatus: undefined as number | undefined,
})

const actionUserId = ref<number | undefined>(undefined)
const unreadCount = ref(0)

const modalVisible = ref(false)
const modalTitle = ref('新增通知')
const submitLoading = ref(false)

const formData = ref<Partial<SysNotification>>({
  title: '',
  content: '',
  notificationType: 0,
  recipientUserId: undefined,
  sendUserId: undefined,
  notificationStatus: 0,
  isGlobal: false,
  needConfirm: false,
  status: 1,
  remark: '',
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getNotificationPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取通知列表失败')
  } finally {
    loading.value = false
  }
}

async function handleLoadUnreadCount() {
  if (!actionUserId.value) {
    message.warning('请先输入用户ID')
    return
  }

  try {
    unreadCount.value = await getUnreadNotificationCountApi(actionUserId.value)
    message.success(`未读通知：${unreadCount.value}`)
  } catch {
    message.error('获取未读数量失败')
  }
}

async function handleMarkAllRead() {
  if (!actionUserId.value) {
    message.warning('请先输入用户ID')
    return
  }

  try {
    const count = await markAllNotificationsReadApi(actionUserId.value)
    message.success(`已标记 ${count} 条通知为已读`)
    fetchData()
  } catch {
    message.error('标记全部已读失败')
  }
}

function handleAdd() {
  modalTitle.value = '新增通知'
  formData.value = {
    title: '',
    content: '',
    notificationType: 0,
    recipientUserId: undefined,
    sendUserId: undefined,
    notificationStatus: 0,
    isGlobal: false,
    needConfirm: false,
    status: 1,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysNotification) {
  modalTitle.value = '编辑通知'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteNotificationApi(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleMarkRead(row: SysNotification) {
  if (!row.recipientUserId) {
    message.warning('该通知未绑定接收用户')
    return
  }

  try {
    const ok = await markNotificationReadApi(Number(row.basicId), row.recipientUserId)
    if (ok) {
      message.success('已标记为已读')
      fetchData()
    } else {
      message.warning('标记失败')
    }
  } catch {
    message.error('标记已读失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateNotificationApi(formData.value.basicId, formData.value)
    } else {
      await createNotificationApi(formData.value)
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

const columns: DataTableColumns<SysNotification> = [
  {
    title: '通知标题',
    key: 'title',
    minWidth: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '通知类型',
    key: 'notificationType',
    width: 120,
    render: (row) => getOptionLabel(NOTIFICATION_TYPE_OPTIONS, row.notificationType),
  },
  {
    title: '通知状态',
    key: 'notificationStatus',
    width: 110,
    render: (row) =>
      h(
        NTag,
        {
          type:
            row.notificationStatus === 0
              ? 'warning'
              : row.notificationStatus === 1
                ? 'success'
                : 'default',
          size: 'small',
          round: true,
        },
        { default: () => getOptionLabel(NOTIFICATION_STATUS_OPTIONS, row.notificationStatus) },
      ),
  },
  {
    title: '接收用户ID',
    key: 'recipientUserId',
    width: 110,
  },
  {
    title: '发送时间',
    key: 'sendTime',
    width: 170,
    render: (row) => formatDate(row.sendTime),
  },
  {
    title: '操作',
    key: 'actions',
    width: 260,
    fixed: 'right',
    render: (row) =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () =>
            [
              row.notificationStatus === 0 &&
                h(
                  NButton,
                  {
                    size: 'small',
                    type: 'warning',
                    ghost: true,
                    onClick: () => handleMarkRead(row),
                  },
                  { default: () => '已读' },
                ),
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
              h(
                NPopconfirm,
                {
                  onPositiveClick: () => handleDelete(row.basicId),
                },
                {
                  default: () => '确认删除该通知？',
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
          placeholder="搜索通知标题/内容"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.notificationType"
          :options="NOTIFICATION_TYPE_OPTIONS"
          placeholder="通知类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.notificationStatus"
          :options="NOTIFICATION_STATUS_OPTIONS"
          placeholder="通知状态"
          style="width: 130px"
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
              queryParams.notificationType = undefined
              queryParams.notificationStatus = undefined
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
          新增通知
        </NButton>
      </div>
      <div class="mt-3 flex flex-wrap items-center gap-2">
        <NInputNumber v-model:value="actionUserId" :min="1" placeholder="用户ID" class="w-36" />
        <NButton @click="handleLoadUnreadCount">查询未读数</NButton>
        <NButton @click="handleMarkAllRead">全部已读</NButton>
        <NTag size="small" type="info">未读：{{ unreadCount }}</NTag>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.basicId"
        :pagination="false"
        :scroll-x="1280"
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
      style="width: 640px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="96px">
        <NFormItem label="通知标题" path="title">
          <NInput v-model:value="formData.title" placeholder="请输入通知标题" />
        </NFormItem>
        <NFormItem label="通知内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="3"
            placeholder="请输入通知内容"
          />
        </NFormItem>
        <NFormItem label="通知类型" path="notificationType">
          <NSelect v-model:value="formData.notificationType" :options="NOTIFICATION_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="接收用户ID" path="recipientUserId">
          <NInputNumber
            v-model:value="formData.recipientUserId"
            :disabled="formData.isGlobal"
            :min="1"
            class="w-full"
          />
        </NFormItem>
        <NFormItem label="发送用户ID" path="sendUserId">
          <NInputNumber v-model:value="formData.sendUserId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="通知状态" path="notificationStatus">
          <NSelect v-model:value="formData.notificationStatus" :options="NOTIFICATION_STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="系统状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="全员通知" path="isGlobal">
          <NSwitch v-model:value="formData.isGlobal" />
        </NFormItem>
        <NFormItem label="需要确认" path="needConfirm">
          <NSwitch v-model:value="formData.needConfirm" />
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
