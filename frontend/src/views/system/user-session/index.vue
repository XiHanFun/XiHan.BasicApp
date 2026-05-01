<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { UserSessionListItemDto } from '@/api'
import { NButton, NIcon, NPopconfirm, NSelect, NSpace, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, userSessionApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { DEVICE_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserSessionPage' })

interface UserSessionGridResult {
  items: UserSessionListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<UserSessionListItemDto>>()

const queryParams = reactive({
  deviceType: undefined as UserSessionListItemDto['deviceType'] | undefined,
  isOnline: undefined as number | undefined,
  isRevoked: undefined as number | undefined,
  keyword: '',
})

const onlineOptions = [
  { label: '在线', value: 1 },
  { label: '离线', value: 0 },
]

const revokedOptions = [
  { label: '已撤销', value: 1 },
  { label: '未撤销', value: 0 },
]

function getDisplayName(row: UserSessionListItemDto) {
  return row.realName || row.nickName || row.userName || String(row.userId)
}

function getSessionState(row: UserSessionListItemDto) {
  if (row.isRevoked) {
    return { label: '已撤销', type: 'error' as const }
  }

  if (row.isExpired) {
    return { label: '已过期', type: 'warning' as const }
  }

  if (row.isOnline) {
    return { label: '在线', type: 'success' as const }
  }

  return { label: '离线', type: 'default' as const }
}

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<UserSessionGridResult> {
  return userSessionApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      deviceType: queryParams.deviceType,
      isOnline: toOptionalBoolean(queryParams.isOnline),
      isRevoked: toOptionalBoolean(queryParams.isRevoked),
      keyword: queryParams.keyword.trim() || undefined,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询用户会话失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<UserSessionListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'userId', minWidth: 110, showOverflow: 'tooltip', title: '用户ID' },
      {
        field: 'displayName',
        minWidth: 140,
        showOverflow: 'tooltip',
        slots: { default: 'col_user' },
        title: '用户',
      },
      { field: 'userSessionId', minWidth: 220, showOverflow: 'tooltip', title: '会话标识' },
      {
        field: 'deviceType',
        formatter: ({ cellValue }) => getOptionLabel(DEVICE_TYPE_OPTIONS, cellValue),
        minWidth: 110,
        title: '设备类型',
      },
      { field: 'deviceName', minWidth: 140, showOverflow: 'tooltip', title: '设备名称' },
      { field: 'operatingSystem', minWidth: 130, showOverflow: 'tooltip', title: '操作系统' },
      { field: 'browser', minWidth: 120, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'ipAddressMasked', minWidth: 140, showOverflow: 'tooltip', title: 'IP 地址' },
      {
        field: 'state',
        slots: { default: 'col_state' },
        title: '状态',
        width: 92,
      },
      {
        field: 'loginTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '登录时间',
      },
      {
        field: 'lastActivityTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '最后活动',
      },
      {
        field: 'expiresAt',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '过期时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 180,
      },
    ],
    id: 'sys_user_session',
    name: '用户会话',
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
  queryParams.deviceType = undefined
  queryParams.isOnline = undefined
  queryParams.isRevoked = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleRevokeSession(row: UserSessionListItemDto) {
  await userSessionApi.revokeSession({
    basicId: row.basicId,
    reason: '管理员强制下线',
  })
  message.success('会话已撤销')
  xGrid.value?.commitProxy('query')
}

async function handleRevokeUserSessions(row: UserSessionListItemDto) {
  const count = await userSessionApi.revokeUserSessions({
    reason: '管理员强制下线全部会话',
    userId: row.userId,
  })
  message.success(`已撤销 ${count} 个会话`)
  xGrid.value?.commitProxy('query')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索会话标识/设备/浏览器"
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.deviceType"
          :options="DEVICE_TYPE_OPTIONS"
          clearable
          placeholder="设备类型"
          style="width: 140px"
        />
        <NSelect
          v-model:value="queryParams.isOnline"
          :options="onlineOptions"
          clearable
          placeholder="在线状态"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.isRevoked"
          :options="revokedOptions"
          clearable
          placeholder="撤销状态"
          style="width: 120px"
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
        <template #toolbar_buttons />

        <template #col_user="{ row }">
          <span>{{ getDisplayName(row) }}</span>
        </template>

        <template #col_state="{ row }">
          <NTag :type="getSessionState(row).type" round size="small">
            {{ getSessionState(row).label }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NPopconfirm
              v-if="row.isOnline && !row.isRevoked"
              @positive-click="handleRevokeSession(row)"
            >
              <template #trigger>
                <NButton size="small" text type="warning">
                  <template #icon>
                    <NIcon><Icon icon="lucide:log-out" /></NIcon>
                  </template>
                  下线
                </NButton>
              </template>
              确认强制该会话下线？
            </NPopconfirm>

            <NPopconfirm @positive-click="handleRevokeUserSessions(row)">
              <template #trigger>
                <NButton size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:user-x" /></NIcon>
                  </template>
                  全部下线
                </NButton>
              </template>
              确认强制该用户全部会话下线？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
