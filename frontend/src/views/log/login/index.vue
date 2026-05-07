<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { LogDetailField } from '../_components/log-detail.types'
import type { LoginLogDetailDto, LoginLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, loginLogApi, LoginResult } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'SystemLoginLogPage' })

interface LogGridResult {
  items: LoginLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<LoginLogListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<LoginLogDetailDto | null>(null)

const queryParams = reactive({
  keyword: '',
  loginResult: undefined as LoginResult | undefined,
})

const loginResultOptions = [
  { label: '成功', value: LoginResult.Success },
  { label: '密码错误', value: LoginResult.InvalidCredentials },
  { label: '账号锁定', value: LoginResult.AccountLocked },
  { label: '账号禁用', value: LoginResult.AccountDisabled },
  { label: '需二次验证', value: LoginResult.RequiresTwoFactor },
  { label: '二次验证失败', value: LoginResult.TwoFactorFailed },
  { label: '登出', value: LoginResult.Logout },
  { label: '其他失败', value: LoginResult.Failed },
]

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'loginIp', label: '登录 IP' },
  { key: 'loginLocation', label: '登录位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'device', label: '设备' },
  { key: 'deviceId', label: '设备标识' },
  { key: 'loginResult', label: '登录/登出结果', options: loginResultOptions, type: 'enum' },
  { key: 'isRiskLogin', falseText: '否', label: '是否风险登录', trueText: '是', type: 'boolean' },
  { key: 'loginTime', label: '登录时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'message', label: '消息', type: 'code' },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
]

function loginResultType(result: LoginResult) {
  switch (result) {
    case LoginResult.Success: return 'success'
    case LoginResult.Logout: return 'info'
    case LoginResult.InvalidCredentials:
    case LoginResult.TwoFactorFailed:
    case LoginResult.Failed: return 'error'
    case LoginResult.AccountLocked:
    case LoginResult.AccountDisabled:
    case LoginResult.RequiresTwoFactor: return 'warning'
    default: return 'default'
  }
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return loginLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      keyword: queryParams.keyword?.trim() || undefined,
      loginResult: queryParams.loginResult,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询登录日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<LoginLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'loginIp', minWidth: 130, showOverflow: 'tooltip', title: '登录 IP' },
      { field: 'loginLocation', minWidth: 160, showOverflow: 'tooltip', title: '登录位置' },
      { field: 'browser', minWidth: 120, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'os', minWidth: 120, showOverflow: 'tooltip', title: '操作系统' },
      { field: 'device', minWidth: 120, showOverflow: 'tooltip', title: '设备' },
      { field: 'deviceId', minWidth: 160, showOverflow: 'tooltip', title: '设备标识' },
      { field: 'userAgent', minWidth: 260, showOverflow: 'tooltip', title: 'User-Agent' },
      {
        field: 'loginResult',
        slots: { default: 'col_result' },
        title: '登录/登出结果',
        width: 120,
      },
      { field: 'message', minWidth: 220, showOverflow: 'tooltip', title: '消息' },
      {
        field: 'isRiskLogin',
        slots: { default: 'col_risk' },
        title: '是否风险登录',
        width: 110,
      },
      {
        field: 'loginTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '登录时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 86,
      },
    ],
    id: 'sys_login_log',
    name: '登录日志',
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
  queryParams.loginResult = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleDetail(row: LoginLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await loginLogApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载登录日志详情失败')
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
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索用户/会话/链路"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.loginResult"
          :options="loginResultOptions"
          clearable
          placeholder="登录/登出结果"
          style="width: 130px"
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
        <template #col_result="{ row }">
          <NTag :type="loginResultType(row.loginResult)" round size="small">
            {{ getOptionLabel(loginResultOptions, row.loginResult) }}
          </NTag>
        </template>
        <template #col_risk="{ row }">
          <NTag :type="row.isRiskLogin ? 'error' : 'info'" round size="small">
            {{ row.isRiskLogin ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="登录日志详情"
    />
  </div>
</template>
