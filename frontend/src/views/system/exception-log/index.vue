<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ExceptionLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, exceptionLogApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemExceptionLogPage' })

interface LogGridResult {
  items: ExceptionLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ExceptionLogListItemDto>>()

const queryParams = reactive({
  isHandled: undefined as boolean | undefined,
  keyword: '',
  severityLevel: undefined as number | undefined,
})

const handledOptions = [
  { label: '已处理', value: true },
  { label: '未处理', value: false },
]

const severityOptions = [
  { label: '低', value: 0 },
  { label: '中', value: 1 },
  { label: '高', value: 2 },
  { label: '严重', value: 3 },
]

function severityType(level: number) {
  switch (level) {
    case 0: return 'info'
    case 1: return 'warning'
    case 2: return 'error'
    case 3: return 'error'
    default: return 'default'
  }
}

function severityLabel(level: number) {
  return severityOptions.find(o => o.value === level)?.label ?? '未知'
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return exceptionLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isHandled: queryParams.isHandled,
      keyword: queryParams.keyword?.trim() || undefined,
      severityLevel: queryParams.severityLevel,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询异常日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<ExceptionLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户' },
      { field: 'exceptionType', minWidth: 160, showOverflow: 'tooltip', title: '异常类型' },
      { field: 'exceptionMessage', minWidth: 240, showOverflow: 'tooltip', title: '异常信息' },
      { field: 'requestPath', minWidth: 200, showOverflow: 'tooltip', title: '请求路径' },
      { field: 'requestMethod', title: '请求方法', width: 90 },
      {
        field: 'severityLevel',
        slots: { default: 'col_severity' },
        title: '严重等级',
        width: 90,
      },
      {
        field: 'isHandled',
        slots: { default: 'col_handled' },
        title: '处理状态',
        width: 90,
      },
      {
        field: 'exceptionTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '异常时间',
      },
    ],
    id: 'sys_exception_log',
    name: '异常日志',
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
  queryParams.isHandled = undefined
  queryParams.severityLevel = undefined
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
          placeholder="搜索异常类型/信息/用户"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.severityLevel"
          :options="severityOptions"
          clearable
          placeholder="严重等级"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.isHandled"
          :options="handledOptions"
          clearable
          placeholder="处理状态"
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
        <template #col_severity="{ row }">
          <NTag :type="severityType(row.severityLevel)" round size="small">
            {{ severityLabel(row.severityLevel) }}
          </NTag>
        </template>
        <template #col_handled="{ row }">
          <NTag :type="row.isHandled ? 'success' : 'warning'" round size="small">
            {{ row.isHandled ? '已处理' : '未处理' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
