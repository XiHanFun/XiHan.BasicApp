<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { LogDetailField } from '../_components/log-detail.types'
import type { LoginLogDetailDto, LoginLogListItemDto } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NIcon,
  NInput,
  NPagination,
  NSelect,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { createPageRequest, LoginResult, logManagementApi } from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogLoginPage' })

const message = useMessage()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<LoginLogDetailDto | null>(null)
const tableLoading = ref(false)
const dataList = ref<LoginLogListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await logManagementApi.login.page({
      ...createPageRequest({
        page: { pageIndex: currentPage.value, pageSize: pageSize.value },
      }),
      keyword: queryParams.keyword?.trim() || undefined,
      loginResult: queryParams.loginResult,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询登录日志失败')
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<LoginLogListItemDto>>(() => [
  { key: 'sessionId', title: '会话标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'traceId', title: '链路追踪 ID', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'userName', title: '用户名', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'userId', title: '用户主键', minWidth: 80, ellipsis: { tooltip: true } },
  { key: 'loginIp', title: '登录 IP', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'loginLocation', title: '登录位置', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'browser', title: '浏览器', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'os', title: '操作系统', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'device', title: '设备', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'deviceId', title: '设备标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'userAgent', title: 'User-Agent', minWidth: 260, ellipsis: { tooltip: true } },
  {
    key: 'loginResult',
    title: '登录/登出结果',
    width: 120,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: loginResultType(row.loginResult), bordered: false }, () => getOptionLabel(loginResultOptions, row.loginResult))
    },
  },
  { key: 'message', title: '消息', minWidth: 220, ellipsis: { tooltip: true } },
  {
    key: 'isRiskLogin',
    title: '是否风险登录',
    width: 110,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.isRiskLogin ? 'error' : 'info', bordered: false }, () => row.isRiskLogin ? '是' : '否')
    },
  },
  {
    key: 'loginTime',
    title: '登录时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.loginTime))
    },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 86,
    fixed: 'right',
    render(row) {
      return h('div', { style: 'display:flex;align-items:center;gap:2px;' }, [
        h(NTooltip, {}, {
          trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, type: 'primary', onClick: () => handleDetail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '详情',
        }),
      ])
    },
  },
])

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.loginResult = undefined
  currentPage.value = 1
  fetchData()
}

function handlePageChange(page: number) {
  currentPage.value = page
  fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  fetchData()
}

async function handleDetail(row: LoginLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.login.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载登录日志详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
          clearable
          placeholder="搜索用户/会话/链路"
          style="width:220px"
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
        :scroll-x="2400"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">
          共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页
        </div>
        <NPagination :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="登录日志详情"
    />
  </div>
</template>
