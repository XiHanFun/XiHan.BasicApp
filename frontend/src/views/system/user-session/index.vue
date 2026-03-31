<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysUserSession } from '~/types'
import {
  NButton,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { deleteUserSessionApi, revokeUserSessionsApi } from '@/api'
import { buildPageRequest, flattenPageResponse } from '@/api/helpers'
import requestClient from '@/api/request'
import { DEVICE_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemUserSessionPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  deviceType: undefined as number | undefined,
  isOnline: undefined as number | undefined,
})

const ONLINE_OPTIONS = [
  { label: '在线', value: 1 },
  { label: '离线', value: 0 },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return requestClient.post('/api/UserSession/Page', buildPageRequest({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    deviceType: queryParams.deviceType,
    isOnline: queryParams.isOnline,
  }, {
    keywordFields: ['UserSessionId', 'DeviceName', 'IpAddress'],
    filterFieldMap: { deviceType: 'DeviceType', isOnline: 'IsOnline' },
  })).then(flattenPageResponse)
}

const options = useVxeTable<SysUserSession>({
  id: 'sys_user_session',
  name: '用户会话',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'userId', title: '用户ID', minWidth: 130, showOverflow: 'tooltip' },
    { field: 'userSessionId', title: '会话ID', minWidth: 200, showOverflow: 'tooltip' },
    {
      field: 'deviceType',
      title: '设备类型',
      width: 110,
      formatter: ({ cellValue }) => getOptionLabel(DEVICE_TYPE_OPTIONS, cellValue),
    },
    { field: 'deviceName', title: '设备名称', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'operatingSystem', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'ipAddress', title: 'IP 地址', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'location', title: '位置', minWidth: 130, showOverflow: 'tooltip' },
    {
      field: 'isOnline',
      title: '在线',
      width: 70,
      slots: { default: 'col_online' },
    },
    {
      field: 'isRevoked',
      title: '已撤销',
      width: 80,
      slots: { default: 'col_revoked' },
    },
    { field: 'loginTime', title: '登录时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    { field: 'lastActivityTime', title: '最后活动', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      title: '操作',
      width: 140,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  proxyConfig: {
    autoLoad: true,
    ajax: { query: ({ page }) => handleQueryApi(page) },
  },
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.deviceType = undefined
  queryParams.isOnline = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleRevoke(row: SysUserSession) {
  try {
    await revokeUserSessionsApi(row.userId, '管理员强制下线')
    message.success('撤销成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('撤销失败')
  }
}

async function handleDelete(id: string) {
  try {
    await deleteUserSessionApi(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}
</script>

<template>
  <div class="h-full flex flex-col gap-2 overflow-hidden p-3">
    <vxe-card style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input v-model="queryParams.keyword" placeholder="搜索会话ID/设备/IP" clearable style="width: 260px" @keyup.enter="handleSearch" />
        <NSelect v-model:value="queryParams.deviceType" :options="DEVICE_TYPE_OPTIONS" placeholder="设备类型" clearable style="width: 140px" />
        <NSelect v-model:value="queryParams.isOnline" :options="ONLINE_OPTIONS" placeholder="在线状态" clearable style="width: 120px" />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #col_online="{ row }">
          <NTag :type="row.isOnline ? 'success' : 'default'" size="small">
            {{ row.isOnline ? '在线' : '离线' }}
          </NTag>
        </template>
        <template #col_revoked="{ row }">
          <NTag v-if="row.isRevoked" type="error" size="small">
            已撤销
          </NTag>
          <span v-else class="text-gray-300">-</span>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NPopconfirm v-if="row.isOnline && !row.isRevoked" @positive-click="handleRevoke(row)">
              <template #trigger>
                <NButton size="small" type="warning" text>
                  强制下线
                </NButton>
              </template>
              确认强制该用户下线？
            </NPopconfirm>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该会话记录？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
