<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysNotification } from '@/api/modules/notification'
import {
  NButton,
  NDatePicker,
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
import { notificationApi, userApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { usePermission, useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemNotificationPage' })

const { hasPermission } = usePermission()
const canCreate = hasPermission(['notice:create'])
const canUpdate = hasPermission(['notice:update'])
const canDelete = hasPermission(['notice:delete'])

interface NotificationFormModel {
  basicId?: string
  title: string
  content?: string
  notificationType: number
  isGlobal: boolean
  recipientUserId?: string
  needConfirm: boolean
  sendTime?: string
  expireTime?: string
  remark?: string
}

interface UserOption {
  label: string
  value: string
}

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  notificationType: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return notificationApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    notificationType: queryParams.notificationType,
  })
}

const options = useVxeTable<SysNotification>(
  {
    id: 'sys_notification',
    name: '通知管理',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'title', title: '标题', minWidth: 220, showOverflow: 'tooltip', sortable: true },
      { field: 'content', title: '内容', minWidth: 260, showOverflow: 'tooltip' },
      {
        field: 'notificationType',
        title: '类型',
        width: 100,
        formatter: ({ cellValue }) => getOptionLabel(NOTIFICATION_TYPE_OPTIONS, cellValue),
      },
      {
        field: 'scope',
        title: '范围',
        width: 80,
        slots: { default: 'col_scope' },
      },
      {
        field: 'needConfirm',
        title: '需确认',
        width: 90,
        slots: { default: 'col_confirm' },
      },
      {
        field: 'isPublished',
        title: '状态',
        width: 90,
        slots: { default: 'col_published' },
      },
      {
        field: 'sendTime',
        title: '发送时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
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
        width: 200,
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
  xGrid.value?.commitProxy('reload')
}

const recipientUserOptions = ref<UserOption[]>([])
const userLoading = ref(false)

function buildUserLabel(userName: string, nickName?: string) {
  if (!nickName)
    return userName
  return `${userName}（${nickName}）`
}

async function loadRecipientUsers(keyword = '') {
  userLoading.value = true
  try {
    const result = await userApi.page({
      page: 1,
      pageSize: 200,
      keyword: keyword.trim() || undefined,
      status: 1,
    })
    recipientUserOptions.value = result.items.map(item => ({
      value: item.basicId,
      label: buildUserLabel(item.userName, item.nickName),
    }))
  }
  catch {
    message.error('加载用户列表失败')
  }
  finally {
    userLoading.value = false
  }
}

const modalVisible = ref(false)
const modalTitle = ref('新增通知')
const submitLoading = ref(false)
const formData = ref<NotificationFormModel>(createDefaultForm())
const formSendTime = ref<number | null>(Date.now())
const formExpireTime = ref<number | null>(null)

function createDefaultForm(): NotificationFormModel {
  return {
    title: '',
    content: '',
    notificationType: 0,
    isGlobal: true,
    recipientUserId: undefined,
    needConfirm: false,
    sendTime: new Date().toISOString(),
    expireTime: undefined,
    remark: '',
  }
}

function toTimestamp(value?: string): null | number {
  if (!value)
    return null
  const timestamp = Date.parse(value)
  return Number.isNaN(timestamp) ? null : timestamp
}

function handleAdd() {
  modalTitle.value = '新增通知'
  formData.value = createDefaultForm()
  formSendTime.value = Date.now()
  formExpireTime.value = null
  modalVisible.value = true
}

async function handleEdit(row: SysNotification) {
  modalTitle.value = '编辑通知'
  formData.value = {
    basicId: row.basicId,
    title: row.title,
    content: row.content ?? '',
    notificationType: row.notificationType,
    isGlobal: Boolean(row.isGlobal),
    recipientUserId: undefined,
    needConfirm: Boolean(row.needConfirm),
    sendTime: row.sendTime,
    expireTime: row.expireTime,
    remark: row.remark ?? '',
  }
  formSendTime.value = toTimestamp(row.sendTime) ?? Date.now()
  formExpireTime.value = toTimestamp(row.expireTime)
  modalVisible.value = true
  if (!formData.value.isGlobal) {
    void loadRecipientUsers()
    try {
      const recipientIds = await notificationApi.getRecipients(row.basicId)
      if (recipientIds.length > 0) {
        formData.value.recipientUserId = recipientIds[0]
      }
    }
    catch {
      // 接收人回填失败不影响编辑
    }
  }
}

function handleFormGlobalChange(value: boolean) {
  formData.value.isGlobal = value
  if (value) {
    formData.value.recipientUserId = undefined
    return
  }
  if (recipientUserOptions.value.length === 0) {
    void loadRecipientUsers()
  }
}

function validateForm() {
  const title = formData.value.title.trim()
  if (!title) {
    message.warning('请输入通知标题')
    return false
  }

  if (!formData.value.isGlobal && !formData.value.recipientUserId) {
    message.warning('非全员通知必须选择接收用户')
    return false
  }

  if (formExpireTime.value && formSendTime.value && formExpireTime.value <= formSendTime.value) {
    message.warning('过期时间必须晚于发送时间')
    return false
  }

  return true
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
  if (!validateForm()) {
    return
  }

  try {
    submitLoading.value = true

    const formPayload = {
      ...formData.value,
      title: formData.value.title.trim(),
      sendTime: new Date(formSendTime.value ?? Date.now()).toISOString(),
      expireTime: formExpireTime.value ? new Date(formExpireTime.value).toISOString() : undefined,
      recipientUserId: formData.value.isGlobal ? undefined : formData.value.recipientUserId,
    }

    if (formPayload.basicId) {
      await notificationApi.update(formPayload.basicId, formPayload)
    }
    else {
      await notificationApi.create(formPayload)
    }

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

async function handlePublish(row: SysNotification) {
  try {
    const count = await notificationApi.publish(row.basicId)
    message.success(`发布成功，共推送 ${count} 条通知`)
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('发布失败')
  }
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
          <NSpace>
            <NButton v-if="canCreate" type="primary" size="small" @click="handleAdd">
              新增通知
            </NButton>
          </NSpace>
        </template>
        <template #col_scope="{ row }">
          <NTag :type="row.isGlobal ? 'info' : 'default'" size="small">
            {{ row.isGlobal ? '全员' : '指定' }}
          </NTag>
        </template>
        <template #col_confirm="{ row }">
          <NTag :type="row.needConfirm ? 'warning' : 'default'" size="small">
            {{ row.needConfirm ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_published="{ row }">
          <NTag :type="row.isPublished ? 'success' : 'default'" size="small">
            {{ row.isPublished ? '已发布' : '草稿' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton
              v-if="canUpdate && !row.isPublished"
              size="small"
              type="primary"
              text
              @click="handleEdit(row)"
            >
              编辑
            </NButton>
            <NPopconfirm
              v-if="canCreate && !row.isPublished"
              @positive-click="handlePublish(row)"
            >
              <template #trigger>
                <NButton size="small" type="success" text>
                  发布
                </NButton>
              </template>
              确认发布该通知？发布后不可编辑和删除。
            </NPopconfirm>
            <NPopconfirm
              v-if="canDelete && !row.isPublished"
              @positive-click="handleDelete(row.basicId)"
            >
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
      style="width: 760px; max-width: 92vw"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="90px">
        <NFormItem label="标题" path="title">
          <NInput v-model:value="formData.title" placeholder="通知标题" maxlength="200" />
        </NFormItem>
        <NFormItem label="通知类型" path="notificationType">
          <NSelect v-model:value="formData.notificationType" :options="NOTIFICATION_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="全员通知">
          <NSwitch :value="formData.isGlobal" @update:value="handleFormGlobalChange" />
        </NFormItem>
        <NFormItem label="需要确认">
          <NSwitch v-model:value="formData.needConfirm" />
        </NFormItem>
        <NFormItem
          v-if="!formData.isGlobal"
          class="xh-form-full-row"
          label="接收用户"
          path="recipientUserId"
        >
          <NSelect
            v-model:value="formData.recipientUserId"
            :options="recipientUserOptions"
            :loading="userLoading"
            filterable
            clearable
            placeholder="请选择接收用户"
            @search="loadRecipientUsers"
          />
        </NFormItem>
        <NFormItem label="发送时间" path="sendTime">
          <NDatePicker
            v-model:value="formSendTime"
            type="datetime"
            clearable
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="过期时间" path="expireTime">
          <NDatePicker
            v-model:value="formExpireTime"
            type="datetime"
            clearable
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem class="xh-form-full-row" label="内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="4"
            placeholder="通知内容"
            maxlength="2000"
          />
        </NFormItem>
        <NFormItem class="xh-form-full-row" label="备注" path="remark">
          <NInput
            v-model:value="formData.remark"
            type="textarea"
            :rows="2"
            placeholder="备注"
            maxlength="500"
          />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
