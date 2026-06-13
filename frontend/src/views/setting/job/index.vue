<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  PageResult,
  TaskCreateDto,
  TaskDetailDto,
  TaskListItemDto,
  TaskLogDetailDto,
  TaskLogListItemDto,
  TaskUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { createPageRequest, EnableStatus, jobManagementApi, RunTaskStatus, taskLogApi, TriggerType } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformJobPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

interface JobFormModel {
  allowConcurrent: boolean
  basicId?: string
  cronExpression?: string | null
  // 运行统计：非用户编辑字段，仅用于在编辑时回填原值，避免提交时被覆盖（DTO 中为必填）
  executedCount: number
  intervalSeconds?: number | null
  maxRetryCount: number
  priority: number
  remark?: string | null
  repeatCount: number
  retryCount: number
  taskClass: string
  taskCode: string
  taskDescription?: string | null
  taskGroup?: string | null
  taskMethod?: string | null
  taskName: string
  taskParams?: string | null
  timeoutSeconds: number
  triggerType: TriggerType
}

const message = useMessage()
const statusOptions = STATUS_OPTIONS

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

// boolean 选项以 1/0 表达（SchemaSelectOption.value 仅 string|number），查询时 toBool 还原
const concurrentOptions = [
  { label: '允许', value: 1 },
  { label: '禁止', value: 0 },
]

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadJob() {
  void schemaPageRef.value?.reload()
}

// ── 过滤值清洗 ──────────────────────────────────────────────────
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : Boolean(Number(v))
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

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? '是' : '否'
}

// ── 字段单一事实源：列 + 常用搜索 ──────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不作为列）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索任务名称/编码/分组', width: 240, order: 0 },
  // 列 + 列
  { key: 'taskName', title: '任务名称', dataType: 'string', minWidth: 180, order: 1 },
  { key: 'taskCode', title: '任务编码', dataType: 'string', searchable: true, searchPlaceholder: '任务编码', minWidth: 160, order: 2 },
  { key: 'taskGroup', title: '任务分组', dataType: 'string', searchable: true, searchPlaceholder: '任务分组', minWidth: 120, order: 3 },
  {
    key: 'triggerType',
    title: '触发类型',
    dataType: 'enum',
    searchable: true,
    options: triggerTypeOptions,
    searchPlaceholder: '触发类型',
    minWidth: 120,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(triggerTypeOptions, (row as unknown as TaskListItemDto).triggerType)),
  },
  {
    key: 'runTaskStatus',
    title: '运行状态',
    dataType: 'enum',
    searchable: true,
    options: runTaskStatusOptions,
    searchPlaceholder: '运行状态',
    width: 120,
    order: 5,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(NTag, { size: 'small', round: true, type: runStatusTag(r.runTaskStatus), bordered: false }, () => getOptionLabel(runTaskStatusOptions, r.runTaskStatus))
    },
  },
  {
    key: 'status',
    title: '启停状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '启停状态',
    width: 100,
    order: 6,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(NTag, { size: 'small', round: true, type: statusTag(r.status), bordered: false }, () => getOptionLabel(statusOptions, r.status))
    },
  },
  {
    key: 'allowConcurrent',
    title: '并发',
    dataType: 'boolean',
    searchable: true,
    options: concurrentOptions,
    searchPlaceholder: '并发',
    width: 86,
    order: 7,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(NTag, { size: 'small', round: true, type: r.allowConcurrent ? 'warning' : 'info', bordered: false }, () => (r.allowConcurrent ? '允许' : '禁止'))
    },
  },
  { key: 'executedCount', title: '已执行', dataType: 'number', minWidth: 100, order: 8 },
  { key: 'retryCount', title: '重试次数', dataType: 'number', minWidth: 100, order: 9 },
  { key: 'priority', title: '优先级', dataType: 'number', sortable: true, width: 86, order: 10 },
  { key: 'nextRunTime', title: '下次执行', dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
  { key: 'lastRunTime', title: '上次执行', dataType: 'datetime', minWidth: 170, order: 12 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 13 },
]

const schema: PageSchema = {
  pageCode: 'platform.job',
  exportPermission: 'saas:task:export',
  pageName: '任务调度',
  batchRemovable: true,
  removePermission: 'saas:task:delete',
  rowKey: 'basicId',
  scrollX: 2000,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return jobManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        taskCode: toStr(f.taskCode),
        taskGroup: toStr(f.taskGroup),
        triggerType: (f.triggerType as TriggerType | undefined) ?? undefined,
        runTaskStatus: (f.runTaskStatus as RunTaskStatus | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
        allowConcurrent: toBool(f.allowConcurrent),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => jobManagementApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增任务', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'logs', title: '执行日志', scope: 'row', icon: 'lucide:history', permission: 'saas:task-log:read' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pencil' },
    { key: 'trigger', title: '立即执行', scope: 'row', icon: 'lucide:play', disabled: row => triggerDisabled(row as unknown as TaskListItemDto) },
    { key: 'toggle', title: '启用/停用', scope: 'row', icon: 'lucide:power', disabled: row => (row as unknown as TaskListItemDto).runTaskStatus === RunTaskStatus.Running },
    { key: 'delete', title: '删除', scope: 'row', icon: 'lucide:trash-2', disabled: row => (row as unknown as TaskListItemDto).runTaskStatus === RunTaskStatus.Running },
  ],
}

function triggerDisabled(row: TaskListItemDto) {
  return row.runTaskStatus === RunTaskStatus.Running || row.status !== EnableStatus.Enabled
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as TaskListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleDetail(row)
      }
      break
    case 'logs':
      if (row) {
        handleLogs(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'trigger':
      if (row) {
        void handleTrigger(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<TaskDetailDto | null>(null)

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

// ── 执行日志抽屉（按任务过滤的执行历史） ────────────────────────
const logVisible = ref(false)
const logLoading = ref(false)
const logTask = ref<TaskListItemDto | null>(null)
const logItems = ref<TaskLogListItemDto[]>([])
const logPagination = ref({ itemCount: 0, page: 1, pageSize: 10 })
const logStatusFilter = ref<RunTaskStatus | null>(null)
const logBatchFilter = ref('')

function formatExecutionTime(value: string) {
  const ms = Number(value)
  if (!Number.isFinite(ms)) {
    return '-'
  }
  return ms >= 1000 ? `${(ms / 1000).toFixed(2)} s` : `${ms} ms`
}

const taskLogColumns: DataTableColumns<TaskLogListItemDto> = [
  { ellipsis: { tooltip: true }, key: 'batchNumber', render: row => row.batchNumber || '-', title: '批次号', width: 150 },
  {
    key: 'taskStatus',
    render: row => h(NTag, { bordered: false, round: true, size: 'small', type: runStatusTag(row.taskStatus) }, () => getOptionLabel(runTaskStatusOptions, row.taskStatus)),
    title: '状态',
    width: 96,
  },
  { key: 'triggerMode', render: row => row.triggerMode || '-', title: '触发方式', width: 96 },
  { key: 'startTime', render: row => formatDate(row.startTime), title: '开始时间', width: 160 },
  { key: 'endTime', render: row => formatNullableDate(row.endTime), title: '结束时间', width: 160 },
  { key: 'executionTime', render: row => formatExecutionTime(row.executionTime), title: '耗时', width: 90 },
  { key: 'retryCount', title: '重试', width: 64 },
]

// 行点击查看异常堆栈/输出日志详情
function taskLogRowProps(row: TaskLogListItemDto) {
  return {
    onClick: () => void handleLogDetail(row),
    style: 'cursor: pointer;',
  }
}

function handleLogs(row: TaskListItemDto) {
  logTask.value = row
  logStatusFilter.value = null
  logBatchFilter.value = ''
  logVisible.value = true
  void loadTaskLogs(1)
}

async function loadTaskLogs(page?: number) {
  if (!logTask.value) {
    return
  }
  if (page) {
    logPagination.value.page = page
  }
  logLoading.value = true
  try {
    const result = await taskLogApi.page({
      ...createPageRequest({ page: { pageIndex: logPagination.value.page, pageSize: logPagination.value.pageSize } }),
      taskId: logTask.value.basicId,
      taskStatus: logStatusFilter.value ?? undefined,
      batchNumber: logBatchFilter.value.trim() || undefined,
    })
    logItems.value = result.items
    logPagination.value.itemCount = result.page.totalCount
  }
  catch (e) {
    message.error((e as Error).message || '加载执行日志失败')
  }
  finally {
    logLoading.value = false
  }
}

// ── 执行日志详情（异常堆栈 / 输出日志） ─────────────────────────
const logDetailVisible = ref(false)
const logDetailLoading = ref(false)
const logDetail = ref<TaskLogDetailDto | null>(null)

async function handleLogDetail(row: TaskLogListItemDto) {
  logDetailVisible.value = true
  logDetailLoading.value = true
  logDetail.value = null
  try {
    logDetail.value = await taskLogApi.detail(row.basicId) ?? null
  }
  catch (e) {
    message.error((e as Error).message || '加载日志详情失败')
  }
  finally {
    logDetailLoading.value = false
  }
}

// ── 行操作：立即执行 / 启停 / 删除 ──────────────────────────────
async function handleTrigger(row: TaskListItemDto) {
  if (row.status !== EnableStatus.Enabled) {
    message.warning('停用的任务不能触发')
    return
  }
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning('运行中的任务不能再次触发')
    return
  }
  try {
    // 经调度器真正触发一次执行（旧实现仅改写运行状态字段，不会执行任务）
    await jobManagementApi.run(row.basicId)
    message.success('任务已触发，执行结果见任务日志')
    reloadJob()
  }
  catch (e) {
    message.error((e as Error)?.message || '触发任务失败')
  }
}

async function handleToggleStatus(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning('运行中的任务不能更改启停状态')
    return
  }
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await jobManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? '任务已启用' : '任务已停用')
    reloadJob()
  }
  catch {
    message.error('更新任务状态失败')
  }
}

async function handleDelete(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning('运行中的任务不能删除')
    return
  }
  try {
    await jobManagementApi.delete(row.basicId)
    message.success('任务已删除')
    reloadJob()
  }
  catch {
    message.error('删除任务失败')
  }
}

// ── 新增 / 编辑表单 ─────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const jobForm = ref<JobFormModel>(createDefaultJobForm())
const modalTitle = computed(() => (jobForm.value.basicId ? '编辑任务' : '新增任务'))

function createDefaultJobForm(): JobFormModel {
  return {
    allowConcurrent: false,
    cronExpression: null,
    executedCount: 0,
    intervalSeconds: null,
    maxRetryCount: 3,
    priority: 5,
    remark: null,
    // -1 表示无限重复（与后端 SysTask.RepeatCount 默认语义一致；0 会导致任务永不执行）
    repeatCount: -1,
    retryCount: 0,
    taskClass: '',
    taskCode: '',
    taskDescription: null,
    taskGroup: null,
    taskMethod: null,
    taskName: '',
    taskParams: null,
    timeoutSeconds: 60,
    triggerType: TriggerType.Cron,
  }
}

function handleAdd() {
  jobForm.value = createDefaultJobForm()
  modalVisible.value = true
}

async function handleEdit(row: TaskListItemDto) {
  // 取详情以补齐 taskClass/taskMethod/taskParams 等列表未返回字段
  let detail: TaskDetailDto | null = null
  try {
    detail = await jobManagementApi.detail(row.basicId) ?? null
  }
  catch {
    message.error('加载任务详情失败')
    return
  }
  const src = detail ?? (row as unknown as TaskDetailDto)
  jobForm.value = {
    allowConcurrent: src.allowConcurrent,
    basicId: src.basicId,
    cronExpression: src.cronExpression ?? null,
    executedCount: src.executedCount,
    intervalSeconds: src.intervalSeconds ?? null,
    maxRetryCount: src.maxRetryCount,
    priority: src.priority,
    remark: src.remark ?? null,
    repeatCount: src.repeatCount,
    retryCount: src.retryCount,
    taskClass: src.taskClass ?? '',
    taskCode: src.taskCode,
    taskDescription: src.taskDescription ?? null,
    taskGroup: src.taskGroup ?? null,
    taskMethod: src.taskMethod ?? null,
    taskName: src.taskName,
    taskParams: src.taskParams ?? null,
    timeoutSeconds: src.timeoutSeconds,
    triggerType: src.triggerType,
  }
  modalVisible.value = true
}

function validateJobForm() {
  if (!jobForm.value.taskName.trim()) {
    message.warning('请输入任务名称')
    return false
  }
  if (!jobForm.value.basicId && !jobForm.value.taskCode.trim()) {
    message.warning('请输入任务编码')
    return false
  }
  if (!jobForm.value.taskClass.trim()) {
    message.warning('请输入任务类名')
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateJobForm()) {
    return
  }
  submitLoading.value = true
  try {
    if (jobForm.value.basicId) {
      const updateInput: TaskUpdateDto = {
        allowConcurrent: jobForm.value.allowConcurrent,
        basicId: jobForm.value.basicId,
        cronExpression: jobForm.value.cronExpression,
        // 回填详情原值，避免覆盖服务端运行统计（DTO 中三者为必填）
        executedCount: jobForm.value.executedCount,
        intervalSeconds: jobForm.value.intervalSeconds,
        maxRetryCount: jobForm.value.maxRetryCount,
        priority: jobForm.value.priority,
        remark: jobForm.value.remark,
        repeatCount: jobForm.value.repeatCount,
        retryCount: jobForm.value.retryCount,
        taskClass: jobForm.value.taskClass.trim(),
        taskDescription: jobForm.value.taskDescription,
        taskGroup: jobForm.value.taskGroup,
        taskMethod: jobForm.value.taskMethod,
        taskName: jobForm.value.taskName.trim(),
        taskParams: jobForm.value.taskParams,
        timeoutSeconds: jobForm.value.timeoutSeconds,
        triggerType: jobForm.value.triggerType,
      }
      await jobManagementApi.update(updateInput)
    }
    else {
      const createInput: TaskCreateDto = {
        allowConcurrent: jobForm.value.allowConcurrent,
        cronExpression: jobForm.value.cronExpression,
        // 新增时运行统计初值为 0（来自 createDefaultJobForm）
        executedCount: jobForm.value.executedCount,
        intervalSeconds: jobForm.value.intervalSeconds,
        maxRetryCount: jobForm.value.maxRetryCount,
        priority: jobForm.value.priority,
        remark: jobForm.value.remark,
        repeatCount: jobForm.value.repeatCount,
        retryCount: jobForm.value.retryCount,
        runTaskStatus: RunTaskStatus.Pending,
        status: EnableStatus.Enabled,
        taskClass: jobForm.value.taskClass.trim(),
        taskCode: jobForm.value.taskCode.trim(),
        taskDescription: jobForm.value.taskDescription,
        taskGroup: jobForm.value.taskGroup,
        taskMethod: jobForm.value.taskMethod,
        taskName: jobForm.value.taskName.trim(),
        taskParams: jobForm.value.taskParams,
        timeoutSeconds: jobForm.value.timeoutSeconds,
        triggerType: jobForm.value.triggerType,
      }
      await jobManagementApi.create(createInput)
    }
    message.success('保存成功')
    modalVisible.value = false
    reloadJob()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NDrawer v-model:show="detailVisible" :width="640">
      <NDrawerContent closable title="任务详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !detailData" class="xh-detail-empty" description="暂无任务详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="detailData" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="1" bordered label-placement="left" size="small">
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
          </NScrollbar>
        </NSpin>
        <template v-if="detailData" #footer>
          <NSpace justify="end">
            <NButton @click="handleLogs(detailData); detailVisible = false">
              <template #icon>
                <NIcon><Icon icon="lucide:history" /></NIcon>
              </template>
              执行日志
            </NButton>
            <NButton
              type="primary"
              :disabled="triggerDisabled(detailData)"
              @click="handleTrigger(detailData); detailVisible = false"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:zap" /></NIcon>
              </template>
              立即执行
            </NButton>
            <NButton
              :type="detailData.status === EnableStatus.Enabled ? 'warning' : 'success'"
              :disabled="detailData.runTaskStatus === RunTaskStatus.Running"
              @click="handleToggleStatus(detailData); detailVisible = false"
            >
              <template #icon>
                <NIcon><Icon :icon="detailData.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" /></NIcon>
              </template>
              {{ detailData.status === EnableStatus.Enabled ? '停用' : '启用' }}
            </NButton>
          </NSpace>
        </template>
      </NDrawerContent>
    </NDrawer>

    <!-- 执行日志抽屉：按任务过滤的执行历史，行点击查看异常堆栈/输出日志 -->
    <NDrawer v-model:show="logVisible" :width="860">
      <NDrawerContent closable :title="`执行日志 - ${logTask?.taskName ?? ''}`">
        <div class="xh-task-log-toolbar">
          <NSelect
            v-model:value="logStatusFilter"
            clearable
            :options="runTaskStatusOptions"
            placeholder="任务状态"
            size="small"
            style="width: 140px"
            @update:value="loadTaskLogs(1)"
          />
          <NInput
            v-model:value="logBatchFilter"
            clearable
            placeholder="批次号"
            size="small"
            style="width: 180px"
            @clear="loadTaskLogs(1)"
            @keyup.enter="loadTaskLogs(1)"
          />
          <NButton size="small" @click="loadTaskLogs(1)">
            <template #icon>
              <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
            </template>
            刷新
          </NButton>
          <span class="xh-task-log-tip">点击行查看执行结果与异常堆栈</span>
        </div>
        <NDataTable
          :columns="taskLogColumns"
          :data="logItems"
          :loading="logLoading"
          :pagination="{
            page: logPagination.page,
            pageSize: logPagination.pageSize,
            itemCount: logPagination.itemCount,
            onUpdatePage: (p: number) => loadTaskLogs(p),
          }"
          :row-key="(row: TaskLogListItemDto) => row.basicId"
          :row-props="taskLogRowProps"
          remote
          size="small"
        />
      </NDrawerContent>
    </NDrawer>

    <!-- 执行日志详情：执行结果 / 异常信息 / 异常堆栈 / 输出日志 -->
    <NModal
      v-model:show="logDetailVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 760px; max-width: 92vw"
      title="执行日志详情"
    >
      <NSpin :show="logDetailLoading">
        <NEmpty v-if="!logDetailLoading && !logDetail" class="xh-detail-empty" description="暂无日志详情">
          <template #icon>
            <NIcon><Icon icon="lucide:inbox" /></NIcon>
          </template>
        </NEmpty>
        <NScrollbar v-else-if="logDetail" style="max-height: 70vh">
          <NDescriptions :column="2" bordered label-placement="left" size="small">
            <NDescriptionsItem label="任务名称">
              {{ logDetail.taskName }}
            </NDescriptionsItem>
            <NDescriptionsItem label="任务编码">
              {{ logDetail.taskCode }}
            </NDescriptionsItem>
            <NDescriptionsItem label="批次号">
              {{ logDetail.batchNumber || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="状态">
              <NTag :type="runStatusTag(logDetail.taskStatus)" round size="small">
                {{ getOptionLabel(runTaskStatusOptions, logDetail.taskStatus) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem label="触发方式">
              {{ logDetail.triggerMode || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="重试次数">
              {{ logDetail.retryCount }}
            </NDescriptionsItem>
            <NDescriptionsItem label="开始时间">
              {{ formatDate(logDetail.startTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="结束时间">
              {{ formatNullableDate(logDetail.endTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="执行耗时">
              {{ formatExecutionTime(logDetail.executionTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="备注">
              {{ logDetail.remark || '-' }}
            </NDescriptionsItem>
          </NDescriptions>

          <template v-if="logDetail.executionResult">
            <div class="xh-task-log-section">
              执行结果
            </div>
            <pre class="xh-task-log-pre">{{ logDetail.executionResult }}</pre>
          </template>
          <template v-if="logDetail.exceptionMessage">
            <div class="xh-task-log-section is-error">
              异常信息
            </div>
            <pre class="xh-task-log-pre is-error">{{ logDetail.exceptionMessage }}</pre>
          </template>
          <template v-if="logDetail.exceptionStackTrace">
            <div class="xh-task-log-section is-error">
              异常堆栈
            </div>
            <pre class="xh-task-log-pre is-error">{{ logDetail.exceptionStackTrace }}</pre>
          </template>
          <template v-if="logDetail.outputLog">
            <div class="xh-task-log-section">
              输出日志
            </div>
            <pre class="xh-task-log-pre">{{ logDetail.outputLog }}</pre>
          </template>
        </NScrollbar>
      </NSpin>
    </NModal>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="jobForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="任务编码" path="taskCode">
          <NInput
            v-model:value="jobForm.taskCode"
            clearable
            :disabled="Boolean(jobForm.basicId)"
            placeholder="如: SyncOrderJob"
          />
        </NFormItem>
        <NFormItem label="任务名称" path="taskName">
          <NInput v-model:value="jobForm.taskName" clearable placeholder="请输入任务名称" />
        </NFormItem>
        <NFormItem label="任务分组" path="taskGroup">
          <NInput v-model:value="jobForm.taskGroup" clearable placeholder="请输入任务分组" />
        </NFormItem>
        <NFormItem label="触发类型" path="triggerType">
          <NSelect v-model:value="jobForm.triggerType" :options="triggerTypeOptions" />
        </NFormItem>
        <NFormItem label="任务类名" path="taskClass">
          <NInput v-model:value="jobForm.taskClass" clearable placeholder="如: XiHan.Jobs.SyncOrderJob" />
        </NFormItem>
        <NFormItem label="任务方法" path="taskMethod">
          <NInput v-model:value="jobForm.taskMethod" clearable placeholder="如: Execute" />
        </NFormItem>
        <NFormItem label="Cron 表达式" path="cronExpression">
          <NInput v-model:value="jobForm.cronExpression" clearable placeholder="如: 0 0/5 * * * ?" />
        </NFormItem>
        <NFormItem label="执行间隔(秒)" path="intervalSeconds">
          <NInputNumber v-model:value="jobForm.intervalSeconds" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="jobForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="超时时间(秒)" path="timeoutSeconds">
          <NInputNumber v-model:value="jobForm.timeoutSeconds" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="最大重试次数" path="maxRetryCount">
          <NInputNumber v-model:value="jobForm.maxRetryCount" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="允许并发" path="allowConcurrent">
          <NSwitch v-model:value="jobForm.allowConcurrent" />
        </NFormItem>
        <NFormItem label="任务参数" path="taskParams">
          <NInput
            v-model:value="jobForm.taskParams"
            clearable
            placeholder="JSON 参数"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="任务描述" path="taskDescription">
          <NInput
            v-model:value="jobForm.taskDescription"
            clearable
            placeholder="请输入任务描述"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="jobForm.remark"
            clearable
            placeholder="请输入备注"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-task-log-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.xh-task-log-tip {
  margin-left: auto;
  font-size: 12px;
  opacity: 0.65;
}

.xh-task-log-section {
  margin: 16px 0 6px;
  font-size: 13px;
  font-weight: 600;
}

.xh-task-log-section.is-error {
  color: var(--n-color-target, #d03050);
}

.xh-task-log-pre {
  margin: 0;
  padding: 10px 12px;
  border-radius: 6px;
  background: var(--n-action-color, rgb(128 128 128 / 8%));
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
}

.xh-task-log-pre.is-error {
  color: #d03050;
  background: rgb(208 48 80 / 6%);
}
</style>
