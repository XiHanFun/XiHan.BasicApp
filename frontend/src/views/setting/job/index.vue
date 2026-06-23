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
import { useI18n } from 'vue-i18n'
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

const { t } = useI18n()
const message = useMessage()
const statusOptions = STATUS_OPTIONS

const triggerTypeOptions = computed(() => [
  { label: t('setting.job.trigger_immediate'), value: TriggerType.Immediate },
  { label: t('setting.job.trigger_schedule'), value: TriggerType.Schedule },
  { label: t('setting.job.trigger_recurring'), value: TriggerType.Recurring },
  { label: t('setting.job.trigger_cron'), value: TriggerType.Cron },
])

const runTaskStatusOptions = computed(() => [
  { label: t('setting.job.run_pending'), value: RunTaskStatus.Pending },
  { label: t('setting.job.run_running'), value: RunTaskStatus.Running },
  { label: t('setting.job.run_success'), value: RunTaskStatus.Success },
  { label: t('setting.job.run_failed'), value: RunTaskStatus.Failed },
  { label: t('setting.job.run_stopped'), value: RunTaskStatus.Stopped },
  { label: t('setting.job.run_paused'), value: RunTaskStatus.Paused },
])

// boolean 选项以 1/0 表达（SchemaSelectOption.value 仅 string|number），查询时 toBool 还原
const concurrentOptions = computed(() => [
  { label: t('setting.common.allow'), value: 1 },
  { label: t('setting.common.forbid'), value: 0 },
])

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
  return value ? t('setting.common.yes') : t('setting.common.no')
}

// ── 字段单一事实源：列 + 常用搜索 ──────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('setting.job.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.job.keyword_placeholder'), width: 240, order: 0 },
  // 列 + 列
  { key: 'taskName', title: t('setting.job.task_name'), dataType: 'string', minWidth: 180, order: 1 },
  { key: 'taskCode', title: t('setting.job.task_code'), dataType: 'string', searchable: true, searchPlaceholder: t('setting.job.task_code_placeholder'), minWidth: 160, order: 2 },
  { key: 'taskGroup', title: t('setting.job.task_group'), dataType: 'string', searchable: true, searchPlaceholder: t('setting.job.task_group_placeholder'), minWidth: 120, order: 3 },
  {
    key: 'triggerType',
    title: t('setting.job.trigger_type'),
    dataType: 'enum',
    searchable: true,
    options: triggerTypeOptions.value,
    searchPlaceholder: t('setting.job.trigger_type_placeholder'),
    minWidth: 120,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(triggerTypeOptions.value, (row as unknown as TaskListItemDto).triggerType)),
  },
  {
    key: 'runTaskStatus',
    title: t('setting.job.run_status'),
    dataType: 'enum',
    searchable: true,
    options: runTaskStatusOptions.value,
    searchPlaceholder: t('setting.job.run_status_placeholder'),
    width: 120,
    order: 5,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(NTag, { size: 'small', round: true, type: runStatusTag(r.runTaskStatus), bordered: false }, () => getOptionLabel(runTaskStatusOptions.value, r.runTaskStatus))
    },
  },
  {
    key: 'status',
    title: t('setting.job.status'),
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: t('setting.job.status_placeholder'),
    width: 100,
    order: 6,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(NTag, { size: 'small', round: true, type: statusTag(r.status), bordered: false }, () => getOptionLabel(statusOptions, r.status))
    },
  },
  {
    key: 'allowConcurrent',
    title: t('setting.job.concurrent'),
    dataType: 'boolean',
    searchable: true,
    options: concurrentOptions.value,
    searchPlaceholder: t('setting.job.concurrent_placeholder'),
    width: 86,
    order: 7,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(NTag, { size: 'small', round: true, type: r.allowConcurrent ? 'warning' : 'info', bordered: false }, () => (r.allowConcurrent ? t('setting.common.allow') : t('setting.common.forbid')))
    },
  },
  { key: 'executedCount', title: t('setting.job.executed_count'), dataType: 'number', minWidth: 100, order: 8 },
  { key: 'retryCount', title: t('setting.job.retry_count'), dataType: 'number', minWidth: 100, order: 9 },
  { key: 'priority', title: t('setting.job.priority'), dataType: 'number', sortable: true, width: 86, order: 10 },
  { key: 'nextRunTime', title: t('setting.job.next_run'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
  { key: 'lastRunTime', title: t('setting.job.last_run'), dataType: 'datetime', minWidth: 170, order: 12 },
  { key: 'createdTime', title: t('setting.job.created_time'), dataType: 'datetime', minWidth: 170, order: 13 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.job',
  exportPermission: 'saas:task:export',
  pageName: t('setting.job.page_name'),
  batchRemovable: true,
  removePermission: 'saas:task:delete',
  statusPermission: 'saas:task:status',
  rowKey: 'basicId',
  scrollX: 2000,
  fields: fields.value,
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
    updateStatus: (id, enabled) => jobManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: t('setting.job.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('setting.job.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'logs', title: t('setting.job.logs'), scope: 'row', icon: 'lucide:history', permission: 'saas:task-log:read' },
    { key: 'edit', title: t('setting.common.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'trigger', title: t('setting.job.trigger_immediate'), scope: 'row', icon: 'lucide:play', disabled: row => triggerDisabled(row as unknown as TaskListItemDto) },
    { key: 'toggle', title: t('setting.job.toggle'), scope: 'row', icon: 'lucide:power', disabled: row => (row as unknown as TaskListItemDto).runTaskStatus === RunTaskStatus.Running },
    { key: 'delete', title: t('setting.common.delete'), scope: 'row', icon: 'lucide:trash-2', disabled: row => (row as unknown as TaskListItemDto).runTaskStatus === RunTaskStatus.Running },
  ],
}))

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
    message.error(t('setting.job.load_detail_failed'))
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

const taskLogColumns = computed<DataTableColumns<TaskLogListItemDto>>(() => [
  { ellipsis: { tooltip: true }, key: 'batchNumber', render: row => row.batchNumber || '-', title: t('setting.job.batch_number'), width: 150 },
  {
    key: 'taskStatus',
    render: row => h(NTag, { bordered: false, round: true, size: 'small', type: runStatusTag(row.taskStatus) }, () => getOptionLabel(runTaskStatusOptions.value, row.taskStatus)),
    title: t('setting.job.log_status'),
    width: 96,
  },
  { key: 'triggerMode', render: row => row.triggerMode || '-', title: t('setting.job.trigger_mode'), width: 96 },
  { key: 'startTime', render: row => formatDate(row.startTime), title: t('setting.job.start_time'), width: 160 },
  { key: 'endTime', render: row => formatNullableDate(row.endTime), title: t('setting.job.end_time'), width: 160 },
  { key: 'executionTime', render: row => formatExecutionTime(row.executionTime), title: t('setting.job.execution_time'), width: 90 },
  { key: 'retryCount', title: t('setting.job.retry'), width: 64 },
])

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
    message.error((e as Error).message || t('setting.job.load_logs_failed'))
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
    message.error((e as Error).message || t('setting.job.load_log_detail_failed'))
  }
  finally {
    logDetailLoading.value = false
  }
}

// ── 行操作：立即执行 / 启停 / 删除 ──────────────────────────────
async function handleTrigger(row: TaskListItemDto) {
  if (row.status !== EnableStatus.Enabled) {
    message.warning(t('setting.job.disabled_cannot_trigger'))
    return
  }
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning(t('setting.job.running_cannot_trigger'))
    return
  }
  try {
    // 经调度器真正触发一次执行（旧实现仅改写运行状态字段，不会执行任务）
    await jobManagementApi.run(row.basicId)
    message.success(t('setting.job.triggered'))
    reloadJob()
  }
  catch (e) {
    message.error((e as Error)?.message || t('setting.job.trigger_failed'))
  }
}

async function handleToggleStatus(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning(t('setting.job.running_cannot_toggle'))
    return
  }
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await jobManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? t('setting.job.task_enabled') : t('setting.job.task_disabled'))
    reloadJob()
  }
  catch {
    message.error(t('setting.job.toggle_failed'))
  }
}

async function handleDelete(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    message.warning(t('setting.job.running_cannot_delete'))
    return
  }
  try {
    await jobManagementApi.delete(row.basicId)
    message.success(t('setting.job.task_deleted'))
    reloadJob()
  }
  catch {
    message.error(t('setting.job.delete_failed'))
  }
}

// ── 新增 / 编辑表单 ─────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const jobForm = ref<JobFormModel>(createDefaultJobForm())
const modalTitle = computed(() => (jobForm.value.basicId ? t('setting.job.edit_title') : t('setting.job.add_title')))

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
    message.error(t('setting.job.load_detail_failed'))
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
    message.warning(t('setting.job.validate_task_name'))
    return false
  }
  if (!jobForm.value.basicId && !jobForm.value.taskCode.trim()) {
    message.warning(t('setting.job.validate_task_code'))
    return false
  }
  if (!jobForm.value.taskClass.trim()) {
    message.warning(t('setting.job.validate_task_class'))
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
    message.success(t('setting.common.save_success'))
    modalVisible.value = false
    reloadJob()
  }
  catch {
    message.error(t('setting.common.save_failed'))
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
      <NDrawerContent closable :title="t('setting.job.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !detailData" class="xh-detail-empty" :description="t('setting.job.detail_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="detailData" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="1" bordered label-placement="left" size="small">
              <NDescriptionsItem :label="t('setting.job.task_name')">
                {{ detailData.taskName }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.task_code')">
                {{ detailData.taskCode }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.task_group')">
                {{ detailData.taskGroup || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.task_description')">
                {{ detailData.taskDescription || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.task_class')">
                {{ detailData.taskClass }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.task_method')">
                {{ detailData.taskMethod || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.task_params')">
                <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.taskParams || '-' }}</pre>
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.trigger_type')">
                {{ getOptionLabel(triggerTypeOptions, detailData.triggerType) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.cron_expression')">
                {{ detailData.cronExpression || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.run_status')">
                <NTag :type="runStatusTag(detailData.runTaskStatus)" round size="small">
                  {{ getOptionLabel(runTaskStatusOptions, detailData.runTaskStatus) }}
                </NTag>
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.status')">
                <NTag :type="statusTag(detailData.status)" round size="small">
                  {{ getOptionLabel(statusOptions, detailData.status) }}
                </NTag>
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.priority')">
                {{ detailData.priority }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.allow_concurrent')">
                {{ formatBoolean(detailData.allowConcurrent) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.timeout_seconds')">
                {{ detailData.timeoutSeconds }}s
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.interval_seconds')">
                {{ detailData.intervalSeconds ? `${detailData.intervalSeconds}s` : '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.exec_stats')">
                {{ t('setting.job.exec_stats_value', { executed: detailData.executedCount, repeat: detailData.repeatCount, retry: detailData.retryCount, maxRetry: detailData.maxRetryCount }) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.start_time')">
                {{ formatNullableDate(detailData.startTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.end_time')">
                {{ formatNullableDate(detailData.endTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.next_run')">
                {{ formatNullableDate(detailData.nextRunTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.last_run')">
                {{ formatNullableDate(detailData.lastRunTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.created_time')">
                {{ formatNullableDate(detailData.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.modified_time')">
                {{ formatNullableDate(detailData.modifiedTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.job.remark')">
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
              {{ t('setting.job.logs') }}
            </NButton>
            <NButton
              type="primary"
              :disabled="triggerDisabled(detailData)"
              @click="handleTrigger(detailData); detailVisible = false"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:zap" /></NIcon>
              </template>
              {{ t('setting.job.trigger_immediate') }}
            </NButton>
            <NButton
              :type="detailData.status === EnableStatus.Enabled ? 'warning' : 'success'"
              :disabled="detailData.runTaskStatus === RunTaskStatus.Running"
              @click="handleToggleStatus(detailData); detailVisible = false"
            >
              <template #icon>
                <NIcon><Icon :icon="detailData.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" /></NIcon>
              </template>
              {{ detailData.status === EnableStatus.Enabled ? t('setting.common.disable') : t('setting.common.enable') }}
            </NButton>
          </NSpace>
        </template>
      </NDrawerContent>
    </NDrawer>

    <!-- 执行日志抽屉：按任务过滤的执行历史，行点击查看异常堆栈/输出日志 -->
    <NDrawer v-model:show="logVisible" :width="860">
      <NDrawerContent
        closable
        :title="t('setting.job.log_title', { name: logTask?.taskName ?? '' })"
        :body-content-style="{ height: '100%', display: 'flex', flexDirection: 'column', overflow: 'hidden' }"
      >
        <div class="xh-task-log-toolbar">
          <NSelect
            v-model:value="logStatusFilter"
            clearable
            :options="runTaskStatusOptions"
            :placeholder="t('setting.job.log_status_filter_placeholder')"
            size="small"
            style="width: 140px"
            @update:value="loadTaskLogs(1)"
          />
          <NInput
            v-model:value="logBatchFilter"
            clearable
            :placeholder="t('setting.job.batch_number_placeholder')"
            size="small"
            style="width: 180px"
            @clear="loadTaskLogs(1)"
            @keyup.enter="loadTaskLogs(1)"
          />
          <NButton size="small" @click="loadTaskLogs(1)">
            <template #icon>
              <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
            </template>
            {{ t('setting.common.refresh') }}
          </NButton>
          <span class="xh-task-log-tip">{{ t('setting.job.log_row_tip') }}</span>
        </div>
        <div class="xh-task-log-body">
          <NDataTable
            class="xh-task-log-table"
            flex-height
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
        </div>
      </NDrawerContent>
    </NDrawer>

    <!-- 执行日志详情：执行结果 / 异常信息 / 异常堆栈 / 输出日志 -->
    <NModal
      v-model:show="logDetailVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 760px; max-width: 92vw"
      :title="t('setting.job.log_detail_title')"
    >
      <NSpin :show="logDetailLoading">
        <NEmpty v-if="!logDetailLoading && !logDetail" class="xh-detail-empty" :description="t('setting.job.log_detail_empty')">
          <template #icon>
            <NIcon><Icon icon="lucide:inbox" /></NIcon>
          </template>
        </NEmpty>
        <NScrollbar v-else-if="logDetail" style="max-height: 70vh">
          <NDescriptions :column="2" bordered label-placement="left" size="small">
            <NDescriptionsItem :label="t('setting.job.task_name')">
              {{ logDetail.taskName }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.task_code')">
              {{ logDetail.taskCode }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.batch_number')">
              {{ logDetail.batchNumber || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.log_status')">
              <NTag :type="runStatusTag(logDetail.taskStatus)" round size="small">
                {{ getOptionLabel(runTaskStatusOptions, logDetail.taskStatus) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.trigger_mode')">
              {{ logDetail.triggerMode || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.retry_count')">
              {{ logDetail.retryCount }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.start_time')">
              {{ formatDate(logDetail.startTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.end_time')">
              {{ formatNullableDate(logDetail.endTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.exec_duration')">
              {{ formatExecutionTime(logDetail.executionTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.job.remark')">
              {{ logDetail.remark || '-' }}
            </NDescriptionsItem>
          </NDescriptions>

          <template v-if="logDetail.executionResult">
            <div class="xh-task-log-section">
              {{ t('setting.job.exec_result') }}
            </div>
            <pre class="xh-task-log-pre">{{ logDetail.executionResult }}</pre>
          </template>
          <template v-if="logDetail.exceptionMessage">
            <div class="xh-task-log-section is-error">
              {{ t('setting.job.exception_message') }}
            </div>
            <pre class="xh-task-log-pre is-error">{{ logDetail.exceptionMessage }}</pre>
          </template>
          <template v-if="logDetail.exceptionStackTrace">
            <div class="xh-task-log-section is-error">
              {{ t('setting.job.exception_stack') }}
            </div>
            <pre class="xh-task-log-pre is-error">{{ logDetail.exceptionStackTrace }}</pre>
          </template>
          <template v-if="logDetail.outputLog">
            <div class="xh-task-log-section">
              {{ t('setting.job.output_log') }}
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
        <NFormItem :label="t('setting.job.task_code')" path="taskCode">
          <NInput
            v-model:value="jobForm.taskCode"
            clearable
            :disabled="Boolean(jobForm.basicId)"
            :placeholder="t('setting.job.task_code_input_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('setting.job.task_name')" path="taskName">
          <NInput v-model:value="jobForm.taskName" clearable :placeholder="t('setting.job.task_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.job.task_group')" path="taskGroup">
          <NInput v-model:value="jobForm.taskGroup" clearable :placeholder="t('setting.job.task_group_input_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.job.trigger_type')" path="triggerType">
          <NSelect v-model:value="jobForm.triggerType" :options="triggerTypeOptions" />
        </NFormItem>
        <NFormItem :label="t('setting.job.task_class')" path="taskClass">
          <NInput v-model:value="jobForm.taskClass" clearable :placeholder="t('setting.job.task_class_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.job.task_method')" path="taskMethod">
          <NInput v-model:value="jobForm.taskMethod" clearable :placeholder="t('setting.job.task_method_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.job.cron_expression')" path="cronExpression">
          <NInput v-model:value="jobForm.cronExpression" clearable :placeholder="t('setting.job.cron_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.job.interval_label')" path="intervalSeconds">
          <NInputNumber v-model:value="jobForm.intervalSeconds" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('setting.job.priority')" path="priority">
          <NInputNumber v-model:value="jobForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('setting.job.timeout_label')" path="timeoutSeconds">
          <NInputNumber v-model:value="jobForm.timeoutSeconds" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('setting.job.max_retry_count')" path="maxRetryCount">
          <NInputNumber v-model:value="jobForm.maxRetryCount" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('setting.job.allow_concurrent')" path="allowConcurrent">
          <NSwitch v-model:value="jobForm.allowConcurrent" />
        </NFormItem>
        <NFormItem :label="t('setting.job.task_params')" path="taskParams">
          <NInput
            v-model:value="jobForm.taskParams"
            clearable
            :placeholder="t('setting.job.task_params_placeholder')"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('setting.job.task_description')" path="taskDescription">
          <NInput
            v-model:value="jobForm.taskDescription"
            clearable
            :placeholder="t('setting.job.task_description_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('setting.job.remark')" path="remark">
          <NInput
            v-model:value="jobForm.remark"
            clearable
            :placeholder="t('setting.job.remark_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('setting.common.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('setting.common.save') }}
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
  flex-shrink: 0;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.xh-task-log-body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.xh-task-log-table {
  flex: 1;
  min-height: 0;
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
