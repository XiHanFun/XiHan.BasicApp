<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { TaskDetailDto, TaskListItemDto } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { createPageRequest, EnableStatus, jobManagementApi, RunTaskStatus, TriggerType } from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformJobPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<TaskDetailDto | null>(null)
const actionLoading = ref(false)
const tableLoading = ref(false)
const dataList = ref<TaskListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

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

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await jobManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: currentPage.value,
          pageSize: pageSize.value,
        },
      }),
      keyword: normalizeNullable(queryParams.keyword),
      runTaskStatus: queryParams.runTaskStatus,
      status: queryParams.status,
      triggerType: queryParams.triggerType,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询任务调度失败')
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<TaskListItemDto>>(() => [
  { key: 'taskName', title: '任务名称', minWidth: 180, ellipsis: { tooltip: true } },
  { key: 'taskCode', title: '任务编码', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'taskGroup', title: '任务分组', minWidth: 120, ellipsis: { tooltip: true } },
  {
    key: 'triggerType',
    title: '触发类型',
    minWidth: 120,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(triggerTypeOptions, row.triggerType))
    },
  },
  {
    key: 'runTaskStatus',
    title: '运行状态',
    width: 120,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: runStatusTag(row.runTaskStatus), bordered: false }, () => getOptionLabel(runTaskStatusOptions, row.runTaskStatus))
    },
  },
  {
    key: 'status',
    title: '启停状态',
    width: 100,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: statusTag(row.status), bordered: false }, () => getOptionLabel(statusOptions, row.status))
    },
  },
  {
    key: 'allowConcurrent',
    title: '并发',
    width: 86,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.allowConcurrent ? 'warning' : 'info', bordered: false }, () => row.allowConcurrent ? '允许' : '禁止')
    },
  },
  { key: 'executedCount', title: '已执行', minWidth: 100 },
  { key: 'retryCount', title: '重试次数', minWidth: 100 },
  { key: 'priority', title: '优先级', minWidth: 86, sorter: true },
  {
    key: 'nextRunTime',
    title: '下次执行',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, row.nextRunTime ? formatDate(row.nextRunTime) : '-')
    },
  },
  {
    key: 'lastRunTime',
    title: '上次执行',
    minWidth: 170,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, row.lastRunTime ? formatDate(row.lastRunTime) : '-')
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
    width: 160,
    render(row) {
      return h(NSpace, { size: 4 }, () => [
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleDetail(row) }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })),
            }),
          default: () => '详情',
        }),
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              ariaLabel: '启动',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'info',
              disabled: row.runTaskStatus === RunTaskStatus.Running || row.status !== EnableStatus.Enabled,
              onClick: () => handleTriggerViaRow(row),
            }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:play' })),
            }),
          default: () => '触发执行',
        }),
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              ariaLabel: '启停',
              circle: true,
              quaternary: true,
              size: 'small',
              type: row.status === EnableStatus.Enabled ? 'warning' : 'success',
              onClick: () => handleToggleStatus(row),
            }, {
              icon: () => h(NIcon, { icon: row.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play' }),
            }),
          default: () => row.status === EnableStatus.Enabled ? '停用' : '启用',
        }),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () =>
            h(NButton, { ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error', loading: actionLoading.value }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })),
            }),
          default: () => '确定删除该任务？删除后不可恢复。',
        }),
      ])
    },
  },
])

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.runTaskStatus = undefined
  queryParams.status = undefined
  queryParams.triggerType = undefined
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
    fetchData()
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
    fetchData()
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
    fetchData()
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
    fetchData()
  }
  catch {
    message.error('删除任务失败')
  }
  finally {
    actionLoading.value = false
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页</div>
        <NPagination :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

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
