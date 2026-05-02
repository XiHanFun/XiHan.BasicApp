<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  NotificationDetailDto,
  NotificationListItemDto,
  UserNotificationDetailDto,
  UserNotificationListItemDto,
} from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInputNumber,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, notificationApi, NotificationStatus, NotificationType } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { NOTIFICATION_STATUS_OPTIONS, NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemNotificationPage' })

type NotificationTab = 'notification' | 'user'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

interface NotificationGridResult {
  items: NotificationListItemDto[]
  total: number
}

interface UserNotificationGridResult {
  items: UserNotificationListItemDto[]
  total: number
}

const message = useMessage()
const activeTab = ref<NotificationTab>('notification')
const notificationGrid = ref<VxeGridInstance<NotificationListItemDto>>()
const userNotificationGrid = ref<VxeGridInstance<UserNotificationListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailTab = ref<NotificationTab>('notification')
const currentNotificationDetail = ref<NotificationDetailDto | null>(null)
const currentUserNotificationDetail = ref<UserNotificationDetailDto | null>(null)

const notificationQuery = reactive({
  businessId: null as ApiId | null,
  businessType: '',
  isBroadcast: undefined as number | undefined,
  isPublished: undefined as number | undefined,
  keyword: '',
  needConfirm: undefined as number | undefined,
  notificationType: null as NotificationType | null,
  sendUserId: null as ApiId | null,
})

const userNotificationQuery = reactive({
  notificationId: null as ApiId | null,
  notificationStatus: null as NotificationStatus | null,
  userId: null as ApiId | null,
})

const notificationTabOptions = [
  { label: '系统通知', value: 'notification' },
  { label: '用户通知', value: 'user' },
]

const booleanOptions = [
  { label: '是', value: 1 },
  { label: '否', value: 0 },
]

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function normalizeId(value: ApiId | null) {
  return value && value > 0 ? value : null
}

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
}

function getNotificationTypeTagType(type: NotificationType): TagType {
  if (type === NotificationType.Error) {
    return 'error'
  }

  if (type === NotificationType.Warning) {
    return 'warning'
  }

  if (type === NotificationType.Announcement) {
    return 'info'
  }

  return 'default'
}

function getNotificationStatusTagType(status: NotificationStatus): TagType {
  if (status === NotificationStatus.Read) {
    return 'success'
  }

  if (status === NotificationStatus.Deleted) {
    return 'default'
  }

  return 'warning'
}

function formatFlag(value: boolean) {
  return value ? '是' : '否'
}

function handleNotificationQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
): Promise<NotificationGridResult> {
  return notificationApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      businessId: normalizeId(notificationQuery.businessId),
      businessType: normalizeNullable(notificationQuery.businessType),
      isBroadcast: toOptionalBoolean(notificationQuery.isBroadcast),
      isPublished: toOptionalBoolean(notificationQuery.isPublished),
      keyword: normalizeNullable(notificationQuery.keyword),
      needConfirm: toOptionalBoolean(notificationQuery.needConfirm),
      notificationType: notificationQuery.notificationType,
      sendUserId: normalizeId(notificationQuery.sendUserId),
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询系统通知失败')
      return {
        items: [],
        total: 0,
      }
    })
}

function handleUserNotificationQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
): Promise<UserNotificationGridResult> {
  return notificationApi
    .userPage({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      notificationId: normalizeId(userNotificationQuery.notificationId),
      notificationStatus: userNotificationQuery.notificationStatus,
      userId: normalizeId(userNotificationQuery.userId),
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询用户通知失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const notificationTableOptions = useVxeTable<NotificationListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'title', minWidth: 220, showOverflow: 'tooltip', title: '通知标题' },
      {
        field: 'notificationType',
        slots: { default: 'col_notification_type' },
        title: '通知类型',
        width: 110,
      },
      {
        field: 'isPublished',
        slots: { default: 'col_published' },
        title: '发布',
        width: 82,
      },
      {
        field: 'isBroadcast',
        slots: { default: 'col_broadcast' },
        title: '全员',
        width: 82,
      },
      {
        field: 'needConfirm',
        slots: { default: 'col_confirm' },
        title: '确认',
        width: 82,
      },
      { field: 'businessType', minWidth: 130, showOverflow: 'tooltip', title: '业务类型' },
      { field: 'sendUserId', minWidth: 110, title: '发送用户' },
      {
        field: 'sendTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '发送时间',
      },
      {
        field: 'expireTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '过期时间',
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
        slots: { default: 'col_notification_actions' },
        title: '操作',
        width: 90,
      },
    ],
    id: 'sys_notification',
    name: '系统通知',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleNotificationQueryApi(page),
      },
    },
  },
)

const userNotificationTableOptions = useVxeTable<UserNotificationListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'notificationId', minWidth: 130, title: '通知主键' },
      { field: 'userId', minWidth: 120, title: '用户主键' },
      {
        field: 'notificationStatus',
        slots: { default: 'col_user_notification_status' },
        title: '通知状态',
        width: 110,
      },
      {
        field: 'readTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '阅读时间',
      },
      {
        field: 'confirmTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '确认时间',
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
        slots: { default: 'col_user_notification_actions' },
        title: '操作',
        width: 90,
      },
    ],
    id: 'sys_user_notification',
    name: '用户通知',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleUserNotificationQueryApi(page),
      },
    },
  },
)

function reloadActiveGrid() {
  if (activeTab.value === 'notification') {
    notificationGrid.value?.commitProxy('reload')
    return
  }

  userNotificationGrid.value?.commitProxy('reload')
}

function handleSearch() {
  reloadActiveGrid()
}

function handleTabChanged() {
  reloadActiveGrid()
}

function handleReset() {
  if (activeTab.value === 'notification') {
    notificationQuery.businessId = null
    notificationQuery.businessType = ''
    notificationQuery.isBroadcast = undefined
    notificationQuery.isPublished = undefined
    notificationQuery.keyword = ''
    notificationQuery.needConfirm = undefined
    notificationQuery.notificationType = null
    notificationQuery.sendUserId = null
  }
  else {
    userNotificationQuery.notificationId = null
    userNotificationQuery.notificationStatus = null
    userNotificationQuery.userId = null
  }

  reloadActiveGrid()
}

async function handleNotificationDetail(row: NotificationListItemDto) {
  detailTab.value = 'notification'
  detailVisible.value = true
  detailLoading.value = true
  currentUserNotificationDetail.value = null

  try {
    currentNotificationDetail.value = await notificationApi.detail(row.basicId)
  }
  catch {
    currentNotificationDetail.value = null
    message.error('加载系统通知详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function handleUserNotificationDetail(row: UserNotificationListItemDto) {
  detailTab.value = 'user'
  detailVisible.value = true
  detailLoading.value = true
  currentNotificationDetail.value = null

  try {
    currentUserNotificationDetail.value = await notificationApi.userDetail(row.basicId)
  }
  catch {
    currentUserNotificationDetail.value = null
    message.error('加载用户通知详情失败')
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="activeTab"
          :options="notificationTabOptions"
          style="width: 120px"
          @update:value="handleTabChanged"
        />

        <template v-if="activeTab === 'notification'">
          <vxe-input
            v-model="notificationQuery.keyword"
            clearable
            placeholder="搜索标题/业务类型"
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
          <NSelect
            v-model:value="notificationQuery.notificationType"
            :options="NOTIFICATION_TYPE_OPTIONS"
            clearable
            placeholder="通知类型"
            style="width: 120px"
          />
          <NSelect
            v-model:value="notificationQuery.isPublished"
            :options="booleanOptions"
            clearable
            placeholder="发布"
            style="width: 100px"
          />
          <NSelect
            v-model:value="notificationQuery.isBroadcast"
            :options="booleanOptions"
            clearable
            placeholder="全员"
            style="width: 100px"
          />
          <NSelect
            v-model:value="notificationQuery.needConfirm"
            :options="booleanOptions"
            clearable
            placeholder="确认"
            style="width: 100px"
          />
          <vxe-input
            v-model="notificationQuery.businessType"
            clearable
            placeholder="业务类型"
            style="width: 130px"
            @keyup.enter="handleSearch"
          />
          <NInputNumber
            v-model:value="notificationQuery.sendUserId"
            clearable
            placeholder="发送用户"
            style="width: 120px"
          />
        </template>

        <template v-else>
          <NInputNumber
            v-model:value="userNotificationQuery.notificationId"
            clearable
            placeholder="通知主键"
            style="width: 130px"
          />
          <NInputNumber
            v-model:value="userNotificationQuery.userId"
            clearable
            placeholder="用户主键"
            style="width: 120px"
          />
          <NSelect
            v-model:value="userNotificationQuery.notificationStatus"
            :options="NOTIFICATION_STATUS_OPTIONS"
            clearable
            placeholder="通知状态"
            style="width: 120px"
          />
        </template>

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

    <vxe-card v-show="activeTab === 'notification'" class="flex-1" style="height: 0">
      <vxe-grid ref="notificationGrid" v-bind="notificationTableOptions">
        <template #toolbar_buttons />

        <template #col_notification_type="{ row }">
          <NTag :type="getNotificationTypeTagType(row.notificationType)" round size="small">
            {{ getOptionLabel(NOTIFICATION_TYPE_OPTIONS, row.notificationType) }}
          </NTag>
        </template>

        <template #col_published="{ row }">
          <NTag :type="row.isPublished ? 'success' : 'default'" round size="small">
            {{ row.isPublished ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_broadcast="{ row }">
          <NTag :type="row.isBroadcast ? 'info' : 'default'" round size="small">
            {{ row.isBroadcast ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_confirm="{ row }">
          <NTag :type="row.needConfirm ? 'warning' : 'default'" round size="small">
            {{ row.needConfirm ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_notification_actions="{ row }">
          <!-- 操作列仅图标 -->
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleNotificationDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <vxe-card v-show="activeTab === 'user'" class="flex-1" style="height: 0">
      <vxe-grid ref="userNotificationGrid" v-bind="userNotificationTableOptions">
        <template #toolbar_buttons />

        <template #col_user_notification_status="{ row }">
          <NTag :type="getNotificationStatusTagType(row.notificationStatus)" round size="small">
            {{ getOptionLabel(NOTIFICATION_STATUS_OPTIONS, row.notificationStatus) }}
          </NTag>
        </template>

        <template #col_user_notification_actions="{ row }">
          <!-- 操作列仅图标 -->
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleUserNotificationDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="620">
      <NDrawerContent closable :title="detailTab === 'notification' ? '系统通知详情' : '用户通知详情'">
        <NSpace v-if="detailLoading" justify="center">
          加载中...
        </NSpace>

        <NDescriptions
          v-else-if="detailTab === 'notification' && currentNotificationDetail"
          :column="1"
          bordered
          size="small"
        >
          <NDescriptionsItem label="通知标题">
            {{ currentNotificationDetail.title }}
          </NDescriptionsItem>
          <NDescriptionsItem label="通知类型">
            {{ getOptionLabel(NOTIFICATION_TYPE_OPTIONS, currentNotificationDetail.notificationType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="业务引用">
            {{ currentNotificationDetail.businessType || '-' }} / {{ currentNotificationDetail.businessId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="发送用户">
            {{ currentNotificationDetail.sendUserId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="发布标记">
            已发布 {{ formatFlag(currentNotificationDetail.isPublished) }}，全员 {{ formatFlag(currentNotificationDetail.isBroadcast) }}，需确认 {{ formatFlag(currentNotificationDetail.needConfirm) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="内容标记">
            正文 {{ formatFlag(currentNotificationDetail.hasBody) }}，视觉标识 {{ formatFlag(currentNotificationDetail.hasVisualMark) }}，跳转动作 {{ formatFlag(currentNotificationDetail.hasAction) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ formatFlag(currentNotificationDetail.hasNote) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="发送时间">
            {{ formatDate(currentNotificationDetail.sendTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="过期时间">
            {{ currentNotificationDetail.expireTime ? formatDate(currentNotificationDetail.expireTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDate(currentNotificationDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ currentNotificationDetail.modifiedTime ? formatDate(currentNotificationDetail.modifiedTime) : '-' }}
          </NDescriptionsItem>
        </NDescriptions>

        <NDescriptions
          v-else-if="detailTab === 'user' && currentUserNotificationDetail"
          :column="1"
          bordered
          size="small"
        >
          <NDescriptionsItem label="通知主键">
            {{ currentUserNotificationDetail.notificationId }}
          </NDescriptionsItem>
          <NDescriptionsItem label="用户主键">
            {{ currentUserNotificationDetail.userId }}
          </NDescriptionsItem>
          <NDescriptionsItem label="通知状态">
            {{ getOptionLabel(NOTIFICATION_STATUS_OPTIONS, currentUserNotificationDetail.notificationStatus) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="阅读时间">
            {{ currentUserNotificationDetail.readTime ? formatDate(currentUserNotificationDetail.readTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="确认时间">
            {{ currentUserNotificationDetail.confirmTime ? formatDate(currentUserNotificationDetail.confirmTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDate(currentUserNotificationDetail.createdTime) }}
          </NDescriptionsItem>
        </NDescriptions>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
