<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { loginLogApi } from '@/api'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'
import { XSystemQueryPanel } from '~/components'

defineOptions({ name: 'SystemLogLoginPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
  _sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams,
) {
  return loginLogApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
  })
}

// 登录结果枚举映射
const loginResultMap: Record<number, { label: string, type: 'success' | 'error' | 'warning' | 'info' }> = {
  0: { label: '成功', type: 'success' },
  1: { label: '失败', type: 'error' },
  2: { label: '锁定', type: 'warning' },
  3: { label: '禁止', type: 'error' },
}

const options = useVxeTable(
  {
    id: 'sys_login_log',
    name: '登录日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'traceId', title: '链路ID', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'sessionId', title: '会话ID', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'loginIp', title: '登录IP', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'loginLocation', title: '登录地点', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'os', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'userAgent', title: 'User-Agent', minWidth: 200, showOverflow: 'tooltip', visible: false },
      { field: 'device', title: '设备类型', minWidth: 100, showOverflow: 'tooltip' },
      { field: 'deviceId', title: '设备标识', minWidth: 160, showOverflow: 'tooltip' },
      {
        field: 'isRiskLogin',
        title: '风险登录',
        width: 90,
        slots: { default: 'col_isRiskLogin' },
      },
      {
        field: 'loginResult',
        title: '登录结果',
        width: 100,
        slots: { default: 'col_loginResult' },
      },
      { field: 'message', title: '消息', minWidth: 200, showOverflow: 'tooltip' },
      {
        field: 'loginTime',
        title: '登录时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'createdTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page, sort }) => handleQueryApi(page, sort),
      },
    },
    toolbarConfig: {
      slots: { buttons: 'toolbar_buttons' },
      refresh: true,
      export: true,
      zoom: true,
      custom: true,
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

async function handleClear() {
  try {
    await loginLogApi.clear()
    message.success('清空成功')
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('清空失败')
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索用户名/IP地址"
          clearable
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
      </div>
    </XSystemQueryPanel>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NPopconfirm @positive-click="handleClear">
            <template #trigger>
              <NButton type="error" size="small">
                清空日志
              </NButton>
            </template>
            确认清空所有登录日志？
          </NPopconfirm>
        </template>
        <template #col_isRiskLogin="{ row }">
          <NTag :type="row.isRiskLogin ? 'error' : 'success'" size="small">
            {{ row.isRiskLogin ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_loginResult="{ row }">
          <NTag
            :type="loginResultMap[row.loginResult]?.type ?? 'default'"
            size="small"
          >
            {{ loginResultMap[row.loginResult]?.label ?? '未知' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
