<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { LoginLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, loginLogApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLoginLogPage' })

interface LogGridResult {
  items: LoginLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<LoginLogListItemDto>>()

const queryParams = reactive({
  keyword: '',
  loginResult: undefined as string | undefined,
  loginType: undefined as string | undefined,
})

const loginTypeOptions = [
  { label: '账号密码', value: 'Password' },
  { label: '手机验证码', value: 'PhoneCode' },
  { label: '邮箱验证码', value: 'EmailCode' },
  { label: '第三方登录', value: 'ThirdParty' },
  { label: '令牌刷新', value: 'TokenRefresh' },
]

const loginResultOptions = [
  { label: '成功', value: 'Success' },
  { label: '失败', value: 'Failed' },
  { label: '账户锁定', value: 'Locked' },
]

function loginResultType(result: string) {
  switch (result) {
    case 'Success': return 'success'
    case 'Failed': return 'error'
    case 'Locked': return 'warning'
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
      loginType: queryParams.loginType,
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
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户' },
      { field: 'loginType', minWidth: 100, showOverflow: 'tooltip', title: '登录方式' },
      {
        field: 'loginResult',
        slots: { default: 'col_result' },
        title: '登录结果',
        width: 100,
      },
      { field: 'loginIp', minWidth: 130, showOverflow: 'tooltip', title: '登录IP' },
      { field: 'loginLocation', minWidth: 120, showOverflow: 'tooltip', title: '登录地点' },
      { field: 'browser', minWidth: 100, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'os', minWidth: 100, showOverflow: 'tooltip', title: '操作系统' },
      { field: 'failReason', minWidth: 160, showOverflow: 'tooltip', title: '失败原因' },
      {
        field: 'loginTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '登录时间',
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
  queryParams.loginType = undefined
  xGrid.value?.commitProxy('reload')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索用户/IP/地点"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.loginType"
          :options="loginTypeOptions"
          clearable
          placeholder="登录方式"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.loginResult"
          :options="loginResultOptions"
          clearable
          placeholder="登录结果"
          style="width: 110px"
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
            {{ row.loginResult }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
