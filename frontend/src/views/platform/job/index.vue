<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { TaskDetailDto, TaskListItemDto } from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, EnableStatus, jobManagementApi, RunTaskStatus, TriggerType } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformJobPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

interface TaskGridResult {
  items: TaskListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<TaskListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<TaskDetailDto | TaskListItemDto | null>(null)

const queryParams = reactive({
  keyword: '',
  runTaskStatus: undefined as RunTaskStatus | undefined,
  status: undefined as EnableStatus | undefined,
  triggerType: undefined as TriggerType | undefined,
})

const triggerTypeOptions = [
  { label: '立即执行', value: TriggerType.Immediate },
  { label: '定时执行', value: TriggerType.Schedule },
  { label: '循环执行', value: TriggerType.Recurring },
  { label: 'Cron 表达式', value: TriggerType.Cron },
]

const runTaskStatusOptions = [
  { label: '待执行', value: RunTaskStatus.Pending },
  { label: '执行中', value: RunTaskStatus.Running },
  { label: '执行成功', value: RunTaskStatus.Success },
  { label: '执行失败', value: RunTaskStatus.Failed },
  { label: '已停止', value: RunTaskStatus.Stopped },
  { label: '已暂停', value: RunTaskStatus.Paused },
]

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function runStatusTag(status: RunTaskStatus): TagType {
  switch (status) {
    case RunTaskStatus.Success:
      return 'success'
    case RunTaskStatus.Failed:
      return 'error'
    case RunTaskStatus.Running:
      return 'warning'
    case RunTaskStatus.Paused:
    case RunTaskStatus.Stopped:
      return 'default'
    default:
      return 'info'
  }
}

function statusTag(status: EnableStatus): TagType {
  return status === EnableStatus.Enabled ? 'success' : 'default'
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<TaskGridResult> {
  return jobManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      keyword: normalizeNullable(queryParams.keyword),
      runTaskStatus: queryParams.runTaskStatus,
      status: queryParams.status,
      triggerType: queryParams.triggerType,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询任务调度失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<TaskListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'taskName', minWidth: 180, showOverflow: 'tooltip', title: '任务名称' },
      { field: 'taskCode', minWidth: 160, showOverflow: 'tooltip', title: '任务编码' },
      { field: 'taskGroup', minWidth: 120, showOverflow: 'tooltip', title: '任务分组' },
      {
        field: 'triggerType',
        formatter: ({ cellValue }) => getOptionLabel(triggerTypeOptions, cellValue),
        minWidth: 120,
        title: '触发类型',
      },
      {
        field: 'runTaskStatus',
        slots: { default: 'col_run_status' },
        title: '运行状态',
        width: 120,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '启停状态',
        width: 100,
      },
      {
        field: 'allowConcurrent',
        slots: { default: 'col_allow_concurrent' },
        title: '并发',
        width: 86,
      },
      { field: 'executedCount', minWidth: 100, title: '已执行' },
      { field: 'retryCount', minWidth: 100, title: '重试次数' },
      { field: 'priority', minWidth: 86, sortable: true, title: '优先级' },
      {
        field: 'nextRunTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '下次执行',
      },
      {
        field: 'lastRunTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '上次执行',
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
    id: 'sys_task',
    name: '任务调度',
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
  queryParams.runTaskStatus = undefined
  queryParams.status = undefined
  queryParams.triggerType = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleDetail(row: TaskListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await jobManagementApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载任务详情失败')
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
          placeholder="搜索任务名称/编码/分组"
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.triggerType"
          :options="triggerTypeOptions"
          clearable
          placeholder="触发类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.runTaskStatus"
          :options="runTaskStatusOptions"
          clearable
          placeholder="运行状态"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="启停状态"
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
        <template #col_run_status="{ row }">
          <NTag :type="runStatusTag(row.runTaskStatus)" round size="small">
            {{ getOptionLabel(runTaskStatusOptions, row.runTaskStatus) }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="statusTag(row.status)" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>
        <template #col_allow_concurrent="{ row }">
          <NTag :type="row.allowConcurrent ? 'warning' : 'info'" round size="small">
            {{ row.allowConcurrent ? '允许' : '禁止' }}
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

    <NDrawer v-model:show="detailVisible" :width="620">
      <NDrawerContent closable title="任务详情">
        <NDescriptions v-if="detailData" :column="1" bordered label-placement="left" size="small">
          <NDescriptionsItem label="任务名称">
            {{ detailData.taskName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务编码">
            {{ detailData.taskCode }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务分组">
            {{ detailData.taskGroup || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务类名">
            {{ 'taskClass' in detailData ? detailData.taskClass : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务方法">
            {{ 'taskMethod' in detailData ? detailData.taskMethod || '-' : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务参数">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ 'taskParams' in detailData ? detailData.taskParams || '-' : '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem label="触发类型">
            {{ getOptionLabel(triggerTypeOptions, detailData.triggerType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="运行状态">
            {{ getOptionLabel(runTaskStatusOptions, detailData.runTaskStatus) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="启停状态">
            {{ getOptionLabel(statusOptions, detailData.status) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="Cron 表达式">
            {{ detailData.cronExpression || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="下次执行">
            {{ formatNullableDate(detailData.nextRunTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="上次执行">
            {{ formatNullableDate(detailData.lastRunTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ 'remark' in detailData ? detailData.remark || '-' : '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div v-else-if="detailLoading" class="py-8 text-center text-gray-400">
          正在加载...
        </div>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
