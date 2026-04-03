<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysTaskLog } from '@/api'
import { NButton, NDescriptions, NDescriptionsItem, NModal, NPopconfirm, NScrollbar, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { taskLogApi } from '@/api'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLogTaskPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
})

// 详情弹窗
const detailVisible = ref(false)
const detailRow = ref<SysTaskLog | null>(null)

function handleQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
  _sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams,
) {
  return taskLogApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
  })
}

// 任务状态映射
const taskStatusMap: Record<number, { label: string, type: 'success' | 'error' | 'warning' | 'info' | 'default' }> = {
  0: { label: '等待中', type: 'default' },
  1: { label: '运行中', type: 'info' },
  2: { label: '已完成', type: 'success' },
  3: { label: '已失败', type: 'error' },
  4: { label: '已取消', type: 'warning' },
  5: { label: '已超时', type: 'error' },
}

const options = useVxeTable(
  {
    id: 'sys_task_log',
    name: '调度日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'taskName', title: '任务名称', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'taskCode', title: '任务编码', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'batchNumber', title: '批次号', minWidth: 140, showOverflow: 'tooltip' },
      {
        field: 'taskStatus',
        title: '任务状态',
        width: 100,
        slots: { default: 'col_taskStatus' },
      },
      {
        field: 'startTime',
        title: '开始时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'endTime',
        title: '结束时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      { field: 'executionTime', title: '耗时(ms)', width: 100, sortable: true },
      { field: 'triggerMode', title: '触发方式', width: 100 },
      { field: 'retryCount', title: '重试次数', width: 90 },
      { field: 'serverName', title: '服务器', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'exceptionMessage', title: '异常信息', minWidth: 200, showOverflow: 'tooltip' },
      {
        field: 'createdTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        title: '操作',
        width: 80,
        fixed: 'right',
        slots: { default: 'col_action' },
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
    await taskLogApi.clear()
    message.success('清空成功')
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('清空失败')
  }
}

function handleDetail(row: SysTaskLog) {
  detailRow.value = row
  detailVisible.value = true
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <vxe-card style="padding: 10px 16px">
      <div class="flex gap-3 items-center">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索任务名称/编码/批次号"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NPopconfirm @positive-click="handleClear">
            <template #trigger>
              <NButton type="error" size="small">
                清空日志
              </NButton>
            </template>
            确认清空所有调度日志？
          </NPopconfirm>
        </template>
        <template #col_taskStatus="{ row }">
          <NTag
            :type="taskStatusMap[row.taskStatus]?.type ?? 'default'"
            size="small"
          >
            {{ taskStatusMap[row.taskStatus]?.label ?? '未知' }}
          </NTag>
        </template>
        <template #col_action="{ row }">
          <NButton text type="primary" size="small" @click="handleDetail(row)">
            详情
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <!-- 详情弹窗 -->
    <NModal
      v-model:show="detailVisible"
      preset="card"
      title="调度日志详情"
      style="width: 720px; max-width: 90vw"
    >
      <NDescriptions v-if="detailRow" label-placement="left" bordered :column="2">
        <NDescriptionsItem label="任务名称">
          {{ detailRow.taskName }}
        </NDescriptionsItem>
        <NDescriptionsItem label="任务编码">
          {{ detailRow.taskCode }}
        </NDescriptionsItem>
        <NDescriptionsItem label="批次号">
          {{ detailRow.batchNumber }}
        </NDescriptionsItem>
        <NDescriptionsItem label="触发方式">
          {{ detailRow.triggerMode }}
        </NDescriptionsItem>
        <NDescriptionsItem label="任务状态">
          <NTag
            :type="taskStatusMap[detailRow.taskStatus ?? 0]?.type ?? 'default'"
            size="small"
          >
            {{ taskStatusMap[detailRow.taskStatus ?? 0]?.label ?? '未知' }}
          </NTag>
        </NDescriptionsItem>
        <NDescriptionsItem label="重试次数">
          {{ detailRow.retryCount }}
        </NDescriptionsItem>
        <NDescriptionsItem label="开始时间">
          {{ formatDate(detailRow.startTime ?? '') }}
        </NDescriptionsItem>
        <NDescriptionsItem label="结束时间">
          {{ formatDate(detailRow.endTime ?? '') }}
        </NDescriptionsItem>
        <NDescriptionsItem label="执行时长">
          {{ detailRow.executionTime }} ms
        </NDescriptionsItem>
        <NDescriptionsItem label="服务器">
          {{ detailRow.serverName }}
        </NDescriptionsItem>
        <NDescriptionsItem label="内存使用">
          {{ detailRow.memoryUsage ? `${detailRow.memoryUsage} MB` : '-' }}
        </NDescriptionsItem>
        <NDescriptionsItem label="CPU使用率">
          {{ detailRow.cpuUsage ? `${detailRow.cpuUsage}%` : '-' }}
        </NDescriptionsItem>
        <NDescriptionsItem label="执行结果" :span="2">
          <NScrollbar style="max-height: 120px">
            <pre class="text-xs whitespace-pre-wrap">{{ detailRow.executionResult || '-' }}</pre>
          </NScrollbar>
        </NDescriptionsItem>
        <NDescriptionsItem label="异常信息" :span="2">
          <NScrollbar style="max-height: 120px">
            <pre class="text-xs text-red-500 whitespace-pre-wrap">{{ detailRow.exceptionMessage || '-' }}</pre>
          </NScrollbar>
        </NDescriptionsItem>
        <NDescriptionsItem label="异常堆栈" :span="2">
          <NScrollbar style="max-height: 160px">
            <pre class="text-xs text-red-400 whitespace-pre-wrap">{{ detailRow.exceptionStackTrace || '-' }}</pre>
          </NScrollbar>
        </NDescriptionsItem>
        <NDescriptionsItem label="输出日志" :span="2">
          <NScrollbar style="max-height: 160px">
            <pre class="text-xs whitespace-pre-wrap">{{ detailRow.outputLog || '-' }}</pre>
          </NScrollbar>
        </NDescriptionsItem>
      </NDescriptions>
    </NModal>
  </div>
</template>
