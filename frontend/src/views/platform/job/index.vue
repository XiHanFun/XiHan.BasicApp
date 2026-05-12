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
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
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
const detailData = ref<TaskDetailDto | null>(null)
const actionLoading = ref(false)

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

const canTrigger = computed(() => {
  if (!detailData.value) return false
  return detailData.value.status === EnableStatus.Enabled
    && detailData.value.runTaskStatus !== RunTaskStatus.Running
})

const canToggle = computed(() => {
  if (!detailData.value) return false
  return detailData.value.runTaskStatus !== RunTaskStatus.Running
})

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value: boolean) {
  return value ? '是' : '否'
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
        width: 160,
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

function reload() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  reload()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.runTaskStatus = undefined
  queryParams.status = undefined
  queryParams.triggerType = undefined
  reload()
}

async function handleDetail(row: TaskListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await jobManagementApi.detail(row.basicId) ?? null
  }
  catch {
    message.error('加载任务详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function handleTriggerViaRow(row: TaskListItemDto) {
  if (row.status !== EnableStatus.Enabled) {
    message.warning('停用的任务不能触发')
    return
  }
  actionLoading.value = true
  try {
    await jobManagementApi.updateRunStatus({
      basicId: row.basicId,
      runTaskStatus: RunTaskStatus.Running,
    })
    message.success('任务已触发')
    reload()
  }
  catch {
    message.error('触发任务失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleTrigger() {
  if (!detailData.value) return
  actionLoading.value = true
  try {
    await jobManagementApi.updateRunStatus({
      basicId: detailData.value.basicId,
      runTaskStatus: RunTaskStatus.Running,
    })
    message.success('任务已触发')
    detailVisible.value = false
    reload()
  }
  catch {
    message.error('触发任务失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleToggleStatus(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning('运行中的任务不能更改启停状态')
    return
  }
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  actionLoading.value = true
  try {
    await jobManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? '任务已启用' : '任务已停用')
    reload()
  }
  catch {
    message.error('更新任务状态失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleDelete(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning('运行中的任务不能删除')
    return
  }
  actionLoading.value = true
  try {
    await jobManagementApi.delete(row.basicId)
    message.success('任务已删除')
    reload()
  }
  catch {
    message.error('删除任务失败')
  }
  finally {
    actionLoading.value = false
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
        <template #empty>
          <div class="py-12 text-center text-gray-400">
            暂无任务数据
          </div>
        </template>
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
          <NSpace :size="4">
            <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton
              aria-label="启动"
              circle
              quaternary
              size="small"
              type="info"
              :disabled="row.runTaskStatus === RunTaskStatus.Running || row.status !== EnableStatus.Enabled"
              @click="handleTriggerViaRow(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:play" /></NIcon>
              </template>
            </NButton>
            <NButton
              aria-label="启停"
              circle
              quaternary
              size="small"
              :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
              @click="handleToggleStatus(row)"
            >
              <template #icon>
                <NIcon :icon="row.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" />
              </template>
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton
                  aria-label="删除"
                  circle
                  quaternary
                  size="small"
                  type="error"
                  :loading="actionLoading"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确定删除该任务？删除后不可恢复。
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="640">
      <NDrawerContent closable title="任务详情">
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          正在加载...
        </div>
        <NDescriptions v-else-if="detailData" :column="1" bordered label-placement="left" size="small">
          <NDescriptionsItem label="任务名称">
            {{ detailData.taskName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务编码">
            {{ detailData.taskCode }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务分组">
            {{ detailData.taskGroup || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务描述">
            {{ detailData.taskDescription || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务类名">
            {{ detailData.taskClass }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务方法">
            {{ detailData.taskMethod || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="任务参数">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.taskParams || '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem label="触发类型">
            {{ getOptionLabel(triggerTypeOptions, detailData.triggerType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="Cron 表达式">
            {{ detailData.cronExpression || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="运行状态">
            <NTag :type="runStatusTag(detailData.runTaskStatus)" round size="small">
              {{ getOptionLabel(runTaskStatusOptions, detailData.runTaskStatus) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem label="启停状态">
            <NTag :type="statusTag(detailData.status)" round size="small">
              {{ getOptionLabel(statusOptions, detailData.status) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem label="优先级">
            {{ detailData.priority }}
          </NDescriptionsItem>
          <NDescriptionsItem label="允许并发">
            {{ formatBoolean(detailData.allowConcurrent) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="超时时间">
            {{ detailData.timeoutSeconds }}s
          </NDescriptionsItem>
          <NDescriptionsItem label="执行间隔">
            {{ detailData.intervalSeconds ? `${detailData.intervalSeconds}s` : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="执行统计">
            已执行 {{ detailData.executedCount }} 次，重复 {{ detailData.repeatCount }} 次，重试 {{ detailData.retryCount }}/{{ detailData.maxRetryCount }}
          </NDescriptionsItem>
          <NDescriptionsItem label="开始时间">
            {{ formatNullableDate(detailData.startTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="结束时间">
            {{ formatNullableDate(detailData.endTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="下次执行">
            {{ formatNullableDate(detailData.nextRunTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="上次执行">
            {{ formatNullableDate(detailData.lastRunTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatNullableDate(detailData.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ formatNullableDate(detailData.modifiedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ detailData.remark || '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div v-else class="py-8 text-center text-gray-400">
          暂无任务详情
        </div>
        <template v-if="detailData" #footer>
          <NSpace justify="end">
            <NButton
              type="primary"
              :disabled="!canTrigger"
              :loading="actionLoading"
              @click="handleTrigger"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:zap" /></NIcon>
              </template>
              触发执行
            </NButton>
            <NButton
              :type="detailData.status === EnableStatus.Enabled ? 'warning' : 'success'"
              :disabled="!canToggle"
              :loading="actionLoading"
              @click="handleToggleStatus(detailData)"
            >
              <template #icon>
                <NIcon :icon="detailData.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" />
              </template>
              {{ detailData.status === EnableStatus.Enabled ? '停用' : '启用' }}
            </NButton>
            <NPopconfirm @positive-click="handleDelete(detailData); detailVisible = false">
              <template #trigger>
                <NButton type="error" :loading="actionLoading">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确定删除该任务？
            </NPopconfirm>
          </NSpace>
        </template>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
