<script lang="ts" setup>
import type { PushNotificationPayload, SysNotification } from '@/api/modules/notification'
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
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
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { notificationApi, userApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { NOTIFICATION_STATUS_OPTIONS, NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemNotificationPage' })

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

interface PushFormModel {
  title: string
  content?: string
  notificationType: number
  isGlobal: boolean
  recipientUserIds: string[]
  needConfirm: boolean
  expireTime?: string
  remark?: string
}

interface UserOption {
  label: string
  value: string
}

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const userStore = useUserStore()
const NOTIFICATION_RECEIVED_EVENT = 'xihan:notification-received'

const inboxLoading = ref(false)
const inboxRows = ref<SysNotification[]>([])
const inboxIncludeRead = ref<'all' | 'unread'>('unread')
const unreadCount = ref(0)

const INBOX_FILTER_OPTIONS = [
  { label: '仅未读', value: 'unread' },
  { label: '全部通知', value: 'all' },
]

const currentUserId = computed(() => {
  const userId = Number(userStore.userInfo?.basicId ?? 0)
  return userId > 0 ? userId : 0
})

const currentTenantId = computed<null | number>(() => {
  const tenantId = Number(userStore.userInfo?.tenantId ?? 0)
  return tenantId > 0 ? tenantId : null
})

function hasUserContext() {
  if (currentUserId.value <= 0) {
    message.warning('当前登录状态失效，请重新登录')
    return false
  }
  return true
}

const inboxOptions = useVxeTable<SysNotification>(
  {
    id: 'sys_notification_inbox',
    name: '我的通知',
    data: [],
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'title', title: '标题', minWidth: 220, showOverflow: 'tooltip' },
      { field: 'content', title: '内容', minWidth: 280, showOverflow: 'tooltip' },
      {
        field: 'notificationType',
        title: '类型',
        width: 90,
        formatter: ({ cellValue }) => getOptionLabel(NOTIFICATION_TYPE_OPTIONS, cellValue),
      },
      {
        field: 'notificationStatus',
        title: '状态',
        width: 90,
        slots: { default: 'col_inbox_status' },
      },
      {
        field: 'needConfirm',
        title: '确认',
        width: 90,
        slots: { default: 'col_inbox_confirm' },
      },
      {
        field: 'sendTime',
        title: '发送时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'actions',
        title: '操作',
        width: 180,
        fixed: 'right',
        slots: { default: 'col_inbox_actions' },
      },
    ],
  },
  {
    pagerConfig: { enabled: false },
    toolbarConfig: { enabled: false },
  },
)

async function refreshUnreadCount() {
  if (!hasUserContext()) {
    unreadCount.value = 0
    return
  }

  unreadCount.value = await notificationApi.getUnreadCount(
    currentUserId.value,
    currentTenantId.value ?? undefined,
  )
}

async function loadInboxNotifications() {
  if (!hasUserContext()) {
    inboxRows.value = []
    return
  }

  inboxLoading.value = true
  try {
    const list = await notificationApi.getUserNotifications(
      currentUserId.value,
      inboxIncludeRead.value === 'all',
      currentTenantId.value ?? undefined,
    )
    inboxRows.value = list
  }
  catch {
    message.error('获取我的通知失败')
  }
  finally {
    inboxLoading.value = false
  }
}

async function refreshInbox() {
  await Promise.all([loadInboxNotifications(), refreshUnreadCount()])
}

function handleInboxFilterChange() {
  void refreshInbox()
}

async function handleMarkNotificationRead(row: SysNotification) {
  if (!hasUserContext()) {
    return
  }

  try {
    const changed = await notificationApi.markRead(
      row.basicId,
      currentUserId.value,
      currentTenantId.value ?? undefined,
    )
    if (!changed) {
      message.warning('当前通知状态未变更')
      return
    }

    message.success('已标记为已读')
    await refreshInbox()
  }
  catch {
    message.error('标记已读失败')
  }
}

async function handleConfirmNotification(row: SysNotification) {
  if (!hasUserContext()) {
    return
  }

  try {
    const changed = await notificationApi.confirm(
      row.basicId,
      currentUserId.value,
      currentTenantId.value ?? undefined,
    )
    if (!changed) {
      message.warning('通知确认失败，可能已过期或已处理')
      return
    }

    message.success('通知确认成功')
    await refreshInbox()
  }
  catch {
    message.error('通知确认失败')
  }
}

async function handleMarkAllNotificationRead() {
  if (!hasUserContext()) {
    return
  }

  try {
    const count = await notificationApi.markAllRead(
      currentUserId.value,
      currentTenantId.value ?? undefined,
    )
    if (count <= 0) {
      message.info('暂无未读通知')
      return
    }

    message.success(`已标记 ${count} 条通知为已读`)
    await refreshInbox()
  }
  catch {
    message.error('全部已读操作失败')
  }
}

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
        field: 'notificationStatus',
        title: '状态',
        width: 80,
        slots: { default: 'col_nStatus' },
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
        width: 180,
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

function handleEdit(row: SysNotification) {
  modalTitle.value = '编辑通知'
  formData.value = {
    basicId: row.basicId,
    title: row.title,
    content: row.content ?? '',
    notificationType: row.notificationType,
    isGlobal: Boolean(row.isGlobal),
    recipientUserId: row.recipientUserId,
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

    const payload: Partial<SysNotification> = {
      ...formData.value,
      title: formData.value.title.trim(),
      sendTime: new Date(formSendTime.value ?? Date.now()).toISOString(),
      expireTime: formExpireTime.value ? new Date(formExpireTime.value).toISOString() : undefined,
      recipientUserId: formData.value.isGlobal ? undefined : formData.value.recipientUserId,
    }

    if (payload.basicId) {
      await notificationApi.update(payload.basicId, payload)
    }
    else {
      await notificationApi.create(payload)
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

const pushModalVisible = ref(false)
const pushLoading = ref(false)
const pushForm = ref<PushFormModel>(createDefaultPushForm())
const pushExpireTime = ref<number | null>(null)

function createDefaultPushForm(): PushFormModel {
  return {
    title: '',
    content: '',
    notificationType: 0,
    isGlobal: true,
    recipientUserIds: [],
    needConfirm: false,
    expireTime: undefined,
    remark: '',
  }
}

function handleOpenPush(row?: SysNotification) {
  if (row) {
    pushForm.value = {
      title: row.title,
      content: row.content ?? '',
      notificationType: row.notificationType,
      isGlobal: Boolean(row.isGlobal),
      recipientUserIds: row.isGlobal || !row.recipientUserId ? [] : [row.recipientUserId],
      needConfirm: Boolean(row.needConfirm),
      expireTime: row.expireTime,
      remark: row.remark ?? '',
    }
    pushExpireTime.value = toTimestamp(row.expireTime)
  }
  else {
    pushForm.value = createDefaultPushForm()
    pushExpireTime.value = null
  }

  pushModalVisible.value = true

  if (!pushForm.value.isGlobal) {
    void loadRecipientUsers()
  }
}

function handlePushGlobalChange(value: boolean) {
  pushForm.value.isGlobal = value
  if (value) {
    pushForm.value.recipientUserIds = []
    return
  }
  if (recipientUserOptions.value.length === 0) {
    void loadRecipientUsers()
  }
}

function validatePushForm() {
  const title = pushForm.value.title.trim()
  if (!title) {
    message.warning('请输入通知标题')
    return false
  }

  if (!pushForm.value.isGlobal && pushForm.value.recipientUserIds.length === 0) {
    message.warning('非全员通知必须选择接收用户')
    return false
  }

  if (pushExpireTime.value && pushExpireTime.value <= Date.now()) {
    message.warning('过期时间必须晚于当前时间')
    return false
  }

  return true
}

async function handleSubmitPush() {
  if (!validatePushForm()) {
    return
  }

  pushLoading.value = true
  try {
    const payload: PushNotificationPayload = {
      title: pushForm.value.title.trim(),
      content: pushForm.value.content,
      notificationType: pushForm.value.notificationType,
      isGlobal: pushForm.value.isGlobal,
      recipientUserIds: pushForm.value.isGlobal ? [] : pushForm.value.recipientUserIds,
      needConfirm: pushForm.value.needConfirm,
      expireTime: pushExpireTime.value ? new Date(pushExpireTime.value).toISOString() : undefined,
      remark: pushForm.value.remark,
    }

    const count = await notificationApi.push(payload)
    message.success(`发布成功，共推送 ${count} 条通知`)
    pushModalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('发布失败')
  }
  finally {
    pushLoading.value = false
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

onMounted(() => {
  window.addEventListener(NOTIFICATION_RECEIVED_EVENT, refreshInbox)
  void refreshInbox()
})

onUnmounted(() => {
  window.removeEventListener(NOTIFICATION_RECEIVED_EVENT, refreshInbox)
})
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <vxe-card class="xh-notice-inbox-card">
      <div class="xh-notice-inbox-toolbar">
        <div class="xh-notice-inbox-title">
          <span>我的通知收件箱</span>
          <NTag type="warning" size="small">
            未读 {{ unreadCount }}
          </NTag>
        </div>
        <NSpace>
          <NSelect
            v-model:value="inboxIncludeRead"
            :options="INBOX_FILTER_OPTIONS"
            style="width: 120px"
            @update:value="handleInboxFilterChange"
          />
          <NButton size="small" @click="refreshInbox">
            刷新
          </NButton>
          <NButton
            size="small"
            type="primary"
            secondary
            :disabled="unreadCount <= 0"
            @click="handleMarkAllNotificationRead"
          >
            全部已读
          </NButton>
        </NSpace>
      </div>
      <vxe-grid v-bind="inboxOptions" :data="inboxRows" :loading="inboxLoading">
        <template #col_inbox_status="{ row }">
          <NTag :type="getNStatusType(row.notificationStatus)" size="small">
            {{ getOptionLabel(NOTIFICATION_STATUS_OPTIONS, row.notificationStatus) }}
          </NTag>
        </template>
        <template #col_inbox_confirm="{ row }">
          <NTag :type="row.needConfirm ? 'warning' : 'default'" size="small">
            {{ row.needConfirm ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_inbox_actions="{ row }">
          <NSpace size="small">
            <NButton
              v-if="row.notificationStatus === 0"
              size="small"
              type="primary"
              text
              @click="handleMarkNotificationRead(row)"
            >
              已读
            </NButton>
            <NButton
              v-if="row.needConfirm && row.notificationStatus === 0"
              size="small"
              type="warning"
              text
              @click="handleConfirmNotification(row)"
            >
              确认
            </NButton>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

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
          <NSpace>
            <NButton v-access="['notice:create']" type="primary" size="small" @click="handleAdd">
              新增通知
            </NButton>
            <NButton v-access="['notice:create']" size="small" secondary @click="handleOpenPush()">
              发布通知
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
        <template #col_nStatus="{ row }">
          <NTag :type="getNStatusType(row.notificationStatus)" size="small">
            {{ getOptionLabel(NOTIFICATION_STATUS_OPTIONS, row.notificationStatus) }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-access="['notice:update']" size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NButton v-access="['notice:create']" size="small" type="primary" text @click="handleOpenPush(row)">
              发布
            </NButton>
            <NPopconfirm v-access="['notice:delete']" @positive-click="handleDelete(row.basicId)">
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
            v-if="!formData.basicId"
            v-access="['notice:create']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
          <NButton
            v-else
            v-access="['notice:update']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="pushModalVisible"
      title="发布通知"
      preset="card"
      style="width: 760px; max-width: 92vw"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="pushForm" label-placement="top" label-width="90px">
        <NFormItem label="标题" path="title">
          <NInput v-model:value="pushForm.title" placeholder="通知标题" maxlength="200" />
        </NFormItem>
        <NFormItem label="通知类型" path="notificationType">
          <NSelect v-model:value="pushForm.notificationType" :options="NOTIFICATION_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="全员通知">
          <NSwitch :value="pushForm.isGlobal" @update:value="handlePushGlobalChange" />
        </NFormItem>
        <NFormItem label="需要确认">
          <NSwitch v-model:value="pushForm.needConfirm" />
        </NFormItem>
        <NFormItem
          v-if="!pushForm.isGlobal"
          class="xh-form-full-row"
          label="接收用户"
          path="recipientUserIds"
        >
          <NSelect
            v-model:value="pushForm.recipientUserIds"
            :options="recipientUserOptions"
            :loading="userLoading"
            multiple
            filterable
            clearable
            placeholder="请选择接收用户"
            @search="loadRecipientUsers"
          />
        </NFormItem>
        <NFormItem label="过期时间" path="expireTime">
          <NDatePicker
            v-model:value="pushExpireTime"
            type="datetime"
            clearable
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem class="xh-form-full-row" label="内容" path="content">
          <NInput
            v-model:value="pushForm.content"
            type="textarea"
            :rows="4"
            placeholder="通知内容"
            maxlength="2000"
          />
        </NFormItem>
        <NFormItem class="xh-form-full-row" label="备注" path="remark">
          <NInput
            v-model:value="pushForm.remark"
            type="textarea"
            :rows="2"
            placeholder="备注"
            maxlength="500"
          />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="pushModalVisible = false">
            取消
          </NButton>
          <NButton
            v-access="['notice:create']"
            type="primary"
            :loading="pushLoading"
            @click="handleSubmitPush"
          >
            立即发布
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.xh-notice-inbox-card {
  flex: 0 0 auto;
}

.xh-notice-inbox-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  gap: 12px;
}

.xh-notice-inbox-title {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
}
</style>
