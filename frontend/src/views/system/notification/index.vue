<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysNotification } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { notificationApi } from '@/api'
import { NOTIFICATION_STATUS_OPTIONS, NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import { XSystemQueryPanel } from '~/components'

defineOptions({ name: 'SystemNotificationPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  notificationType: undefined as number | undefined,
  notificationStatus: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return notificationApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    notificationType: queryParams.notificationType,
    notificationStatus: queryParams.notificationStatus,
  })
}

const options = useVxeTable<SysNotification>(
  {
    id: 'sys_notification',
    name: '通知管理',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'title', title: '标题', minWidth: 200, showOverflow: 'tooltip', sortable: true },
      { field: 'content', title: '内容', minWidth: 260, showOverflow: 'tooltip' },
      {
        field: 'notificationType',
        title: '类型',
        width: 100,
        formatter: ({ cellValue }) => getOptionLabel(NOTIFICATION_TYPE_OPTIONS, cellValue),
      },
      {
        field: 'notificationStatus',
        title: '状态',
        width: 80,
        slots: { default: 'col_nStatus' },
      },
      {
        field: 'isGlobal',
        title: '全局',
        width: 70,
        slots: { default: 'col_global' },
      },
      {
        field: 'sendTime',
        title: '发送时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'expireTime',
        title: '过期时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'createTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'actions',
        title: '操作',
        width: 140,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: { query: ({ page }) => handleQueryApi(page) },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}
function handleReset() {
  queryParams.keyword = ''
  queryParams.notificationType = undefined
  queryParams.notificationStatus = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增通知')
const submitLoading = ref(false)
const formData = ref<Partial<SysNotification>>({})

function resetForm() {
  formData.value = {
    title: '',
    content: '',
    notificationType: 0,
    isGlobal: false,
    needConfirm: false,
  }
}
function handleAdd() {
  modalTitle.value = '新增通知'
  resetForm()
  modalVisible.value = true
}
function handleEdit(row: SysNotification) {
  modalTitle.value = '编辑通知'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await notificationApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId)
      await notificationApi.update(formData.value.basicId, formData.value)
    else await notificationApi.create(formData.value)
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}

function getNStatusType(status: number) {
  const map: Record<number, 'default' | 'info' | 'success' | 'error'> = {
    0: 'default',
    1: 'success',
    2: 'error',
  }
  return map[status] ?? 'default'
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索标题/内容"
          clearable
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.notificationType"
          :options="NOTIFICATION_TYPE_OPTIONS"
          placeholder="通知类型"
          clearable
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.notificationStatus"
          :options="NOTIFICATION_STATUS_OPTIONS"
          placeholder="状态"
          clearable
          style="width: 120px"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">
            新增通知
          </NButton>
        </template>
        <template #col_nStatus="{ row }">
          <NTag :type="getNStatusType(row.notificationStatus)" size="small">
            {{ getOptionLabel(NOTIFICATION_STATUS_OPTIONS, row.notificationStatus) }}
          </NTag>
        </template>
        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'info' : 'default'" size="small">
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该通知？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 560px"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="90px">
        <NFormItem label="标题" path="title">
          <NInput v-model:value="formData.title" placeholder="通知标题" />
        </NFormItem>
        <NFormItem label="通知类型" path="notificationType">
          <NSelect v-model:value="formData.notificationType" :options="NOTIFICATION_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="全局通知">
          <NSwitch v-model:value="formData.isGlobal" />
        </NFormItem>
        <NFormItem label="需要确认">
          <NSwitch v-model:value="formData.needConfirm" />
        </NFormItem>
        <NFormItem label="内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="4"
            placeholder="通知内容"
          />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
