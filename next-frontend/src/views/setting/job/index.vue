<script setup lang="ts">
import type { Tone } from '@xihan-ui/kernel'
import type {
  PageResult,
  TaskCreateDto,
  TaskDetailDto,
  TaskListItemDto,
  TaskLogDetailDto,
  TaskLogListItemDto,
  TaskUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, XDataTableColumn } from '~/components'
import { XhBadge, XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhSpinner, XhSwitch } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, EnableStatus, jobManagementApi, RunTaskStatus, taskLogApi, TriggerType } from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XDataTable, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import CronExpression from '~/components/common/CronExpression.vue'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformJobPage' })

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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

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
  { label: t('common.statuses.allow'), value: 1 },
  { label: t('common.statuses.forbid'), value: 0 },
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

function runStatusTag(status: RunTaskStatus): Tone {
  switch (status) {
    case RunTaskStatus.Success:
      return 'success'
    case RunTaskStatus.Failed:
      return 'danger'
    case RunTaskStatus.Running:
      return 'warning'
    case RunTaskStatus.Paused:
    case RunTaskStatus.Stopped:
      return 'neutral'
    default:
      return 'info'
  }
}

function statusTag(status: EnableStatus): Tone {
  return status === EnableStatus.Enabled ? 'success' : 'danger'
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

/** 行展开插槽的行对象在 SchemaPage 边界为宽松类型，这里收敛回具名 DTO */
function asTask(row: unknown): TaskListItemDto {
  return row as TaskListItemDto
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
    dictionaryCode: 'TriggerType',
    options: triggerTypeOptions.value,
    searchPlaceholder: t('setting.job.trigger_type_placeholder'),
    minWidth: 120,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:hsl(var(--muted-foreground));' }, getOptionLabel(triggerTypeOptions.value, (row as unknown as TaskListItemDto).triggerType)),
  },
  {
    key: 'runTaskStatus',
    title: t('setting.job.run_status'),
    dataType: 'enum',
    searchable: true,
    dictionaryCode: 'RunTaskStatus',
    options: runTaskStatusOptions.value,
    searchPlaceholder: t('setting.job.run_status_placeholder'),
    width: 120,
    order: 5,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: runStatusTag(r.runTaskStatus) }, () => getOptionLabel(runTaskStatusOptions.value, r.runTaskStatus))
    },
  },
  {
    key: 'status',
    title: t('setting.job.status'),
    dataType: 'enum',
    searchable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions.value,
    searchPlaceholder: t('setting.job.status_placeholder'),
    width: 100,
    order: 6,
    render: (row) => {
      const r = row as unknown as TaskListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: statusTag(r.status) }, () => getOptionLabel(statusOptions.value, r.status))
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
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.allowConcurrent ? 'warning' : 'info' }, () => (r.allowConcurrent ? t('common.statuses.allow') : t('common.statuses.forbid')))
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
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'trigger', title: t('setting.job.trigger_immediate'), scope: 'row', icon: 'lucide:play', disabled: row => triggerDisabled(row as unknown as TaskListItemDto) },
    { key: 'toggle', title: t('setting.job.toggle'), scope: 'row', icon: 'lucide:power', disabled: row => (row as unknown as TaskListItemDto).runTaskStatus === RunTaskStatus.Running },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', disabled: row => (row as unknown as TaskListItemDto).runTaskStatus === RunTaskStatus.Running },
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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.job.load_detail_failed'))
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

const taskLogColumns = computed<XDataTableColumn<TaskLogListItemDto>[]>(() => [
  { ellipsis: true, key: 'batchNumber', render: row => row.batchNumber || '-', title: t('setting.job.batch_number'), width: 150 },
  {
    key: 'taskStatus',
    render: row => h(XhBadge, { variant: 'subtle', size: 'sm', tone: runStatusTag(row.taskStatus) }, () => getOptionLabel(runTaskStatusOptions.value, row.taskStatus)),
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
    toast.error((e as Error).message || t('setting.job.load_logs_failed'))
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
    toast.error((e as Error).message || t('setting.job.load_log_detail_failed'))
  }
  finally {
    logDetailLoading.value = false
  }
}

// ── 行操作：立即执行 / 启停 / 删除 ──────────────────────────────
async function handleTrigger(row: TaskListItemDto) {
  if (row.status !== EnableStatus.Enabled) {
    toast.warning(t('setting.job.disabled_cannot_trigger'))
    return
  }
  if (row.runTaskStatus === RunTaskStatus.Running) {
    toast.warning(t('setting.job.running_cannot_trigger'))
    return
  }
  try {
    // 经调度器真正触发一次执行（旧实现仅改写运行状态字段，不会执行任务）
    await jobManagementApi.run(row.basicId)
    toast.success(t('setting.job.triggered'))
    reloadJob()
  }
  catch (e) {
    toast.error((e as Error)?.message || t('setting.job.trigger_failed'))
  }
}

async function handleToggleStatus(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    toast.warning(t('setting.job.running_cannot_toggle'))
    return
  }
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await jobManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    toast.success(newStatus === EnableStatus.Enabled ? t('setting.job.task_enabled') : t('setting.job.task_disabled'))
    reloadJob()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.job.toggle_failed'))
  }
}

async function handleDelete(row: TaskListItemDto) {
  if (row.runTaskStatus === RunTaskStatus.Running) {
    toast.warning(t('setting.job.running_cannot_delete'))
    return
  }
  try {
    await jobManagementApi.delete(row.basicId)
    toast.success(t('setting.job.task_deleted'))
    reloadJob()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.job.delete_failed'))
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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.job.load_detail_failed'))
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
    toast.warning(t('setting.job.validate_task_name'))
    return false
  }
  if (!jobForm.value.basicId && !jobForm.value.taskCode.trim()) {
    toast.warning(t('setting.job.validate_task_code'))
    return false
  }
  if (!jobForm.value.taskClass.trim()) {
    toast.warning(t('setting.job.validate_task_class'))
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
    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadJob()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
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
    <!-- 行下拉展开：触发器信息（触发类型 / Cron / 间隔 / 运行态 / 上下次执行 / 起止 / 执行统计） -->
    <template #expand="{ row }">
      <div class="xh-trigger-expand">
        <XhDescriptionsRoot :columns="3" bordered placement="left" size="sm">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.trigger_type') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ getOptionLabel(triggerTypeOptions, asTask(row).triggerType) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.cron_expression') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <code v-if="asTask(row).cronExpression">{{ asTask(row).cronExpression }}</code>
              <span v-else>-</span>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.interval_label') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ asTask(row).intervalSeconds ? `${asTask(row).intervalSeconds}s` : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.run_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <XhBadge variant="subtle" size="sm" :tone="runStatusTag(asTask(row).runTaskStatus)">
                {{ getOptionLabel(runTaskStatusOptions, asTask(row).runTaskStatus) }}
              </XhBadge>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.next_run') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(asTask(row).nextRunTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.last_run') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(asTask(row).lastRunTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.start_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(asTask(row).startTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.end_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(asTask(row).endTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.job.exec_stats') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ t('setting.job.exec_stats_value', { executed: asTask(row).executedCount, repeat: asTask(row).repeatCount, retry: asTask(row).retryCount, maxRetry: asTask(row).maxRetryCount }) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>
      </div>
    </template>

    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerTitle>{{ t('setting.job.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger>✕</XhDrawerCloseTrigger>
        <div class="xh-loading-stage">
          <div v-if="detailLoading" class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !detailData" class="xh-detail-empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('setting.job.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="detailData" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <XhDescriptionsRoot :columns="1" bordered placement="left" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_name') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.taskName }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_code') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.taskCode }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_group') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.taskGroup || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_description') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.taskDescription || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_class') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.taskClass }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_method') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.taskMethod || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_params') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.taskParams || '-' }}</pre>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.trigger_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ getOptionLabel(triggerTypeOptions, detailData.triggerType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.cron_expression') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.cronExpression || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.run_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhBadge variant="subtle" :tone="runStatusTag(detailData.runTaskStatus)" size="sm">
                    {{ getOptionLabel(runTaskStatusOptions, detailData.runTaskStatus) }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhBadge variant="subtle" :tone="statusTag(detailData.status)" size="sm">
                    {{ getOptionLabel(statusOptions, detailData.status) }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.priority') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.priority }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.allow_concurrent') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(detailData.allowConcurrent) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.timeout_seconds') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.timeoutSeconds }}s
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.interval_seconds') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.intervalSeconds ? `${detailData.intervalSeconds}s` : '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.exec_stats') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ t('setting.job.exec_stats_value', { executed: detailData.executedCount, repeat: detailData.repeatCount, retry: detailData.retryCount, maxRetry: detailData.maxRetryCount }) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.start_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.startTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.end_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.endTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.next_run') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.nextRunTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.last_run') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.lastRunTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.created_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.createdTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.modified_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.modifiedTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.remark') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.remark || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </div>
        </div>
        <template v-if="detailData" #footer>
          <XhFlex justify="end" gap="md">
            <XhButton @click="handleLogs(detailData); detailVisible = false">
              <span><Icon icon="lucide:history" /></span>
              {{ t('setting.job.logs') }}
            </XhButton>
            <XhButton
              tone="brand"
              :disabled="triggerDisabled(detailData)"
              @click="handleTrigger(detailData); detailVisible = false"
            >
              <span><Icon icon="lucide:zap" /></span>
              {{ t('setting.job.trigger_immediate') }}
            </XhButton>
            <XhButton
              :tone="detailData.status === EnableStatus.Enabled ? 'warning' : 'success'"
              :disabled="detailData.runTaskStatus === RunTaskStatus.Running"
              @click="handleToggleStatus(detailData); detailVisible = false"
            >
              <span><Icon :icon="detailData.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" /></span>
              {{ detailData.status === EnableStatus.Enabled ? t('common.actions.disable') : t('common.actions.enable') }}
            </XhButton>
          </XhFlex>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 执行日志抽屉：按任务过滤的执行历史，行点击查看异常堆栈/输出日志 -->
    <XhDrawerRoot v-model:open="logVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 860px">
        <XhDrawerTitle>{{ t('setting.job.log_title', { name: logTask?.taskName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger>✕</XhDrawerCloseTrigger>
        <div class="xh-task-log-toolbar">
          <XSelect
            v-model:value="logStatusFilter"
            clearable
            :options="runTaskStatusOptions"
            :placeholder="t('setting.job.log_status_filter_placeholder')"
            size="sm"
            style="width: 140px"
            @update:value="loadTaskLogs(1)"
          />
          <XInput
            v-model:value="logBatchFilter"
            clearable
            :placeholder="t('setting.job.batch_number_placeholder')"
            size="sm"
            style="width: 180px"
            @clear="loadTaskLogs(1)"
            @keyup.enter="loadTaskLogs(1)"
          />
          <XhButton size="sm" @click="loadTaskLogs(1)">
            <span><Icon icon="lucide:refresh-cw" /></span>
            {{ t('common.actions.refresh') }}
          </XhButton>
          <span class="xh-task-log-tip">{{ t('setting.job.log_row_tip') }}</span>
        </div>
        <div class="xh-task-log-body">
          <XDataTable
            class="xh-task-log-table"
            :columns="taskLogColumns"
            :data="logItems"
            :loading="logLoading"
            :pagination="{
              page: logPagination.page,
              pageSize: logPagination.pageSize,
              itemCount: logPagination.itemCount,
              onUpdatePage: (p: number) => loadTaskLogs(p) }"
            :row-key="(row: TaskLogListItemDto) => row.basicId"
            :row-props="taskLogRowProps"
            size="sm"
          />
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 执行日志详情：执行结果 / 异常信息 / 异常堆栈 / 输出日志 -->
    <XhDialogRoot v-model:open="logDetailVisible">
      <XhDialogContent style="--xh-dialog-max-w: 760px">
        <XhDialogTitle>{{ t('setting.job.log_detail_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>
        <div class="xh-loading-stage">
          <div v-if="logDetailLoading" class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!logDetailLoading && !logDetail" class="xh-detail-empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('setting.job.log_detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="logDetail" class="xh-scroll-area" style="max-height: 70vh">
            <XhDescriptionsRoot :columns="2" bordered placement="left" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_name') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ logDetail.taskName }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.task_code') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ logDetail.taskCode }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.batch_number') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ logDetail.batchNumber || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.log_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhBadge variant="subtle" :tone="runStatusTag(logDetail.taskStatus)" size="sm">
                    {{ getOptionLabel(runTaskStatusOptions, logDetail.taskStatus) }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.trigger_mode') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ logDetail.triggerMode || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.retry_count') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ logDetail.retryCount }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.start_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatDate(logDetail.startTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.end_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(logDetail.endTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.exec_duration') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatExecutionTime(logDetail.executionTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.job.remark') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ logDetail.remark || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>

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
          </div>
        </div>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="jobForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="taskCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="jobForm.taskCode"
                clearable
                :disabled="Boolean(jobForm.basicId)"
                :placeholder="t('setting.job.task_code_input_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="taskName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="jobForm.taskName" clearable :placeholder="t('setting.job.task_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="taskGroup">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_group') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="jobForm.taskGroup" clearable :placeholder="t('setting.job.task_group_input_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="triggerType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.trigger_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="jobForm.triggerType" :options="triggerTypeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="taskClass">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_class') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="jobForm.taskClass" clearable :placeholder="t('setting.job.task_class_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="taskMethod">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_method') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="jobForm.taskMethod" clearable :placeholder="t('setting.job.task_method_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="cronExpression" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.cron_expression') }}</XhFieldLabel>
            <XhFieldControl>
              <CronExpression v-model:value="jobForm.cronExpression" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="intervalSeconds">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.interval_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="jobForm.intervalSeconds" :min="0" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="priority">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.priority') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="jobForm.priority" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="timeoutSeconds">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.timeout_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="jobForm.timeoutSeconds" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="maxRetryCount">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.max_retry_count') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="jobForm.maxRetryCount" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="allowConcurrent">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.allow_concurrent') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="jobForm.allowConcurrent" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="taskParams" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_params') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="jobForm.taskParams"
                clearable
                :placeholder="t('setting.job.task_params_placeholder')"
                :rows="3"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="taskDescription" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.task_description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="jobForm.taskDescription"
                clearable
                :placeholder="t('setting.job.task_description_placeholder')"
                :rows="2"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.job.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="jobForm.remark"
                clearable
                :placeholder="t('setting.job.remark_placeholder')"
                :rows="2"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-trigger-expand {
  padding: 4px 2px;
}

.xh-trigger-expand code {
  font-family: ui-monospace, SFMono-Regular, monospace;
  font-size: 12px;
  color: hsl(var(--foreground));
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
  color: var(--xh-color-danger-600);
}

.xh-task-log-pre {
  margin: 0;
  padding: 10px 12px;
  border-radius: 6px;
  background: var(--xh-bg-subtle);
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
