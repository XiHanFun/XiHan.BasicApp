<script setup lang="ts">
import type { JobRow, JobRunRow } from './job'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { JobRunState } from '~/contracts'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerFooter, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XDataTable, XInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { usePermission } from '~/hooks'
import { formatDate } from '~/utils'
import {
  enabledLabel,
  errorText,
  formatDuration,
  getJob,
  isRunning,
  jobResource,
  kindLabel,
  kindOptions,
  listJobRuns,
  runStateLabel,
  runStateOptions,
  runStateTone,
  runStatusLabel,
  runStatusOptions,
  runStatusTone,
  runTriggerLabel,
  sourceLabel,
  statusOptions,
  triggerJob,
  triggerStateLabel,
  triggerStateTone,
} from './job'

defineOptions({ name: 'MaintenanceJobsView' })

const { t } = useI18n()
const { hasPermission } = usePermission()
const resource = jobResource(t)

const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reloadJob() {
  void schemaPageRef.value?.reload()
}

/** 行对象在 SchemaPage 边界为宽松类型，这里收敛回列表行 */
const asRow = (row: unknown) => row as JobRow

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

/** 首个触发器（列表的触发类型、行展开的 Cron 与间隔都看它；多个触发器在详情里逐个列出） */
function primaryTrigger(row: JobRow) {
  return row.triggers[0]
}

function execStats(row: JobRow) {
  return t('maintenance.jobs.exec_stats_value', { triggers: row.triggers.length, executed: row.executedCount })
}

// ── 字段单一事实源：列 + 常用搜索（可排序的只放后端白名单：名称、编码、下次 / 上次执行、更新时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('maintenance.jobs.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('maintenance.jobs.keyword_placeholder'), width: 240, order: 0 },
  { key: 'title', title: t('maintenance.jobs.task_name'), dataType: 'string', sortable: true, minWidth: 180, order: 1 },
  { key: 'name', title: t('maintenance.jobs.task_code'), dataType: 'string', sortable: true, minWidth: 160, order: 2 },
  { key: 'group', title: t('maintenance.jobs.task_group'), dataType: 'string', minWidth: 120, order: 3 },
  {
    key: 'kind',
    title: t('maintenance.jobs.trigger_type'),
    dataType: 'enum',
    searchable: true,
    options: kindOptions(t),
    searchPlaceholder: t('maintenance.jobs.trigger_type_placeholder'),
    minWidth: 120,
    order: 4,
    render: row => h('span', { class: 'job-cell-muted' }, kindLabel(t, asRow(row).kind)),
  },
  {
    key: 'runState',
    title: t('maintenance.jobs.run_status'),
    dataType: 'enum',
    searchable: true,
    options: runStatusOptions(t),
    searchPlaceholder: t('maintenance.jobs.run_status_placeholder'),
    width: 120,
    order: 5,
    render: row => renderTag(runStatusLabel(t, asRow(row).runState), runStatusTone(asRow(row).runState)),
  },
  {
    key: 'paused',
    title: t('maintenance.jobs.status'),
    dataType: 'enum',
    searchable: true,
    options: statusOptions(t),
    searchPlaceholder: t('maintenance.jobs.status_placeholder'),
    width: 100,
    order: 6,
    render: row => renderTag(enabledLabel(t, asRow(row).paused), asRow(row).paused ? 'danger' : 'success'),
  },
  {
    key: 'allowConcurrent',
    title: t('maintenance.jobs.concurrent'),
    dataType: 'boolean',
    width: 86,
    order: 7,
    render: row => renderTag(asRow(row).allowConcurrent ? t('maintenance.jobs.allow') : t('maintenance.jobs.forbid'), asRow(row).allowConcurrent ? 'warning' : 'info'),
  },
  { key: 'executedCount', title: t('maintenance.jobs.executed_count'), dataType: 'number', minWidth: 100, order: 8 },
  { key: 'nextRunAt', title: t('maintenance.jobs.next_run'), dataType: 'datetime', sortable: true, minWidth: 170, order: 9 },
  { key: 'lastRunAt', title: t('maintenance.jobs.last_run'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'updatedAt', title: t('maintenance.jobs.updated_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'maintenance.jobs',
  pageName: t('maintenance.jobs.title'),
  statusPermission: 'maintenance.jobs.update',
  rowKey: 'name',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('maintenance.jobs.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'logs', title: t('maintenance.jobs.logs'), scope: 'row', icon: 'lucide:history', permission: 'maintenance.job-runs.read' },
    { key: 'trigger', title: t('maintenance.jobs.trigger_immediate'), scope: 'row', icon: 'lucide:play', disabled: row => isRunning(asRow(row)), permission: 'maintenance.jobs.trigger' },
    { key: 'toggle', title: t('maintenance.jobs.toggle'), scope: 'row', icon: 'lucide:power', disabled: row => isRunning(asRow(row)), permission: 'maintenance.jobs.update' },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
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
    case 'trigger':
      if (row) {
        handleTrigger(row)
      }
      break
    case 'toggle':
      if (row) {
        handleToggleStatus(row)
      }
      break
  }
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<JobRow | null>(null)

async function handleDetail(row: JobRow) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await getJob(row.name)
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.jobs.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 执行日志抽屉（按任务过滤的运行历史） ────────────────────────
const logVisible = ref(false)
const logLoading = ref(false)
const logJob = ref<JobRow | null>(null)
const logItems = ref<JobRunRow[]>([])
const logPagination = ref({ itemCount: 0, page: 1, pageSize: 10 })
const logStateFilter = ref<JobRunState | null>(null)
const logKeywordFilter = ref('')
const logStateOptions = computed(() => runStateOptions(t))

const jobRunColumns = computed<XDataTableColumn<JobRunRow>[]>(() => [
  { ellipsis: true, key: 'triggerName', render: row => row.triggerName || '-', title: t('maintenance.jobs.trigger_name'), width: 150 },
  { key: 'state', render: row => renderTag(runStateLabel(t, row.state), runStateTone(row.state)), title: t('maintenance.jobs.log_status'), width: 96 },
  { key: 'trigger', render: row => runTriggerLabel(t, row.trigger), title: t('maintenance.jobs.trigger_mode'), width: 96 },
  { key: 'startedAt', render: row => formatDate(row.startedAt), title: t('maintenance.jobs.start_time'), width: 160 },
  { key: 'completedAt', render: row => formatDate(row.completedAt), title: t('maintenance.jobs.end_time'), width: 160 },
  { key: 'durationMs', render: row => formatDuration(row.durationMs), title: t('maintenance.jobs.execution_time'), width: 90 },
  { ellipsis: true, key: 'nodeId', render: row => row.nodeId || '-', title: t('maintenance.jobs.node_id'), width: 120 },
])

// 行点击查看这次运行的详情与异常信息
function jobRunRowProps(row: JobRunRow) {
  return {
    onClick: () => handleRunDetail(row),
    style: 'cursor: pointer;',
  }
}

function handleLogs(row: JobRow) {
  logJob.value = row
  logStateFilter.value = null
  logKeywordFilter.value = ''
  logVisible.value = true
  void loadJobRuns(1)
}

function changeLogState(value: unknown) {
  logStateFilter.value = (value as JobRunState | null) ?? null
  void loadJobRuns(1)
}

async function loadJobRuns(page?: number) {
  if (!logJob.value) {
    return
  }
  if (page) {
    logPagination.value.page = page
  }
  logLoading.value = true
  try {
    const result = await listJobRuns({
      job: logJob.value.name,
      page: logPagination.value.page,
      pageSize: logPagination.value.pageSize,
      state: logStateFilter.value,
      keyword: logKeywordFilter.value,
    })
    logItems.value = result.items
    logPagination.value.itemCount = result.total
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.jobs.load_logs_failed'))
  }
  finally {
    logLoading.value = false
  }
}

// ── 执行日志详情（取自列表行：错误码 / 异常信息） ────────────────
const runDetailVisible = ref(false)
const runDetail = ref<JobRunRow | null>(null)

function handleRunDetail(row: JobRunRow) {
  runDetail.value = row
  runDetailVisible.value = true
}

// ── 行操作：立即执行 / 启停 ─────────────────────────────────────
function handleTrigger(row: JobRow) {
  if (isRunning(row)) {
    toast.warning(t('maintenance.jobs.running_cannot_trigger'))
    return
  }
  // 行内动作与详情抽屉都走这里，确认放在处理函数里才两处都覆盖；暂停的作业也可以立即执行
  void dialog.confirm({
    badge: 'warning',
    title: t('maintenance.jobs.trigger_immediate'),
    content: actionConfirmText(t, t('maintenance.jobs.trigger_immediate'), row.title),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        // 排进一个一次性的手动触发器，由任一节点执行、不等结果；结果在执行日志里
        await triggerJob(row.name)
        toast.success(t('maintenance.jobs.triggered'))
        reloadJob()
      }
      catch (error) {
        toast.danger(errorText(error) || t('maintenance.jobs.trigger_failed'))
      }
    },
  })
}

function handleToggleStatus(row: JobRow) {
  if (isRunning(row)) {
    toast.warning(t('maintenance.jobs.running_cannot_toggle'))
    return
  }
  const enabled = !row.paused
  void dialog.confirm({
    badge: 'warning',
    title: t('maintenance.jobs.toggle'),
    content: statusConfirmText(t, enabled, row.title),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        // 启用即恢复、停用即暂停（停用的作业不再按计划触发，正在运行的不受影响）
        await resource.updateStatus?.(row.name, !enabled)
        toast.success(enabled ? t('maintenance.jobs.task_disabled') : t('maintenance.jobs.task_enabled'))
        reloadJob()
      }
      catch (error) {
        toast.danger(errorText(error) || t('maintenance.jobs.toggle_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 行下拉展开：触发器信息（触发类型 / Cron / 间隔 / 运行态 / 下次与上次执行 / 上次结束与耗时 / 执行统计） -->
    <template #expand="{ row }">
      <div class="xh-trigger-expand">
        <XhDescriptionsRoot :columns="3" variant="outline" size="sm">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_type') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ kindLabel(t, asRow(row).kind) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.cron_expression') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <code v-if="primaryTrigger(asRow(row))?.cron">{{ primaryTrigger(asRow(row))?.cron }}</code>
              <span v-else>-</span>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.interval_label') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ primaryTrigger(asRow(row))?.intervalSeconds ? `${primaryTrigger(asRow(row))?.intervalSeconds}s` : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.run_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <XhTagRoot variant="subtle" size="sm" :tone="runStatusTone(asRow(row).runState)">
                <XhTagLabel>
                  {{ runStatusLabel(t, asRow(row).runState) }}
                </XhTagLabel>
              </XhTagRoot>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.next_run') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDate(asRow(row).nextRunAt) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.last_run') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDate(asRow(row).lastRunAt) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.end_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDate(asRow(row).last?.completedAt) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.exec_duration') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDuration(asRow(row).last?.durationMs) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('maintenance.jobs.exec_stats') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ execStats(asRow(row)) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>
      </div>
    </template>

    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('maintenance.jobs.detail_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div class="xh-loading-stage" :class="{ 'is-loading': detailLoading }">
            <div class="xh-loading-stage__veil">
              <XhSpinner />
            </div>
            <XhEmptyStateRoot v-if="!detailLoading && !detailData" class="xh-detail-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:inbox" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('maintenance.jobs.detail_empty') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <div v-else-if="detailData" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
              <XhDescriptionsRoot :columns="1" variant="outline" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.task_name') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ detailData.title }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.task_code') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ detailData.name }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.task_group') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ detailData.group || '-' }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.task_description') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ detailData.description || '-' }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.job_type') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ detailData.jobType }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.source') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ sourceLabel(t, detailData.source) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_type') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ kindLabel(t, detailData.kind) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.run_status') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <XhTagRoot variant="subtle" :tone="runStatusTone(detailData.runState)" size="sm">
                      <XhTagLabel>
                        {{ runStatusLabel(t, detailData.runState) }}
                      </XhTagLabel>
                    </XhTagRoot>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.status') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <XhTagRoot variant="subtle" :tone="detailData.paused ? 'danger' : 'success'" size="sm">
                      <XhTagLabel>
                        {{ enabledLabel(t, detailData.paused) }}
                      </XhTagLabel>
                    </XhTagRoot>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.allow_concurrent') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ formatBoolean(detailData.allowConcurrent) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.run_on_every_instance') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ formatBoolean(detailData.runOnEveryInstance) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.exec_stats') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ execStats(detailData) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.next_run') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ formatDate(detailData.nextRunAt) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.last_run') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ formatDate(detailData.lastRunAt) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('maintenance.jobs.updated_time') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    {{ formatDate(detailData.updatedAt) }}
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>

              <!-- 触发器：一个作业可以有多个，逐个列出计划、状态与触发情况 -->
              <div class="xh-task-log-section">
                {{ t('maintenance.jobs.triggers_title') }}
              </div>
              <div v-if="detailData.triggers.length > 0" class="xh-job-triggers">
                <XhDescriptionsRoot v-for="trigger in detailData.triggers" :key="trigger.name" :columns="2" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ trigger.name }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_state') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XhTagRoot variant="subtle" :tone="triggerStateTone(trigger.state)" size="sm">
                        <XhTagLabel>
                          {{ triggerStateLabel(t, trigger.state) }}
                        </XhTagLabel>
                      </XhTagRoot>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_type') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ kindLabel(t, trigger.kind) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.schedule') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <code>{{ trigger.schedule }}</code>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.cron_expression') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <code v-if="trigger.cron">{{ trigger.cron }}</code>
                      <span v-else>-</span>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.time_zone') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <!-- 时区只对 Cron 计划有意义，没写即 UTC -->
                      {{ trigger.cron ? (trigger.timeZone || 'UTC') : '-' }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.interval_seconds') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ trigger.intervalSeconds ? `${trigger.intervalSeconds}s` : '-' }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.fire_count') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ trigger.fireCount }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.next_fire') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatDate(trigger.nextFireAt) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.previous_fire') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatDate(trigger.previousFireAt) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem v-if="trigger.error">
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_error') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <span class="text-fg-danger">{{ trigger.error }}</span>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </div>
              <div v-else class="text-fg-muted">
                -
              </div>

              <!-- 上次执行结果：运行历史里最近完成的一次 -->
              <template v-if="detailData.last">
                <div class="xh-task-log-section">
                  {{ t('maintenance.jobs.last_outcome') }}
                </div>
                <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.log_status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XhTagRoot variant="subtle" :tone="runStateTone(detailData.last.succeeded ? 'Succeeded' : 'Failed')" size="sm">
                        <XhTagLabel>
                          {{ runStateLabel(t, detailData.last.succeeded ? 'Succeeded' : 'Failed') }}
                        </XhTagLabel>
                      </XhTagRoot>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_mode') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ runTriggerLabel(t, detailData.last.trigger) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.start_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatDate(detailData.last.startedAt) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.end_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatDate(detailData.last.completedAt) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.exec_duration') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatDuration(detailData.last.durationMs) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('maintenance.jobs.error_code') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ detailData.last.errorCode || '-' }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
                <template v-if="detailData.last.errorMessage">
                  <div class="xh-task-log-section is-error">
                    {{ t('maintenance.jobs.exception_message') }}
                  </div>
                  <pre class="xh-task-log-pre is-error">{{ detailData.last.errorMessage }}</pre>
                </template>
              </template>
            </div>
          </div>
        </XhDrawerBody>
        <XhDrawerFooter v-if="detailData">
          <!-- 与行内动作同一套权限码：详情里的入口不能绕过门控 -->
          <XhButton v-if="hasPermission('maintenance.job-runs.read')" variant="subtle" @click="handleLogs(detailData); detailVisible = false">
            <span><Icon icon="lucide:history" /></span>
            {{ t('maintenance.jobs.logs') }}
          </XhButton>
          <XhButton
            v-if="hasPermission('maintenance.jobs.trigger')"
            variant="subtle"
            tone="brand"
            :disabled="isRunning(detailData)"
            @click="handleTrigger(detailData); detailVisible = false"
          >
            <span><Icon icon="lucide:zap" /></span>
            {{ t('maintenance.jobs.trigger_immediate') }}
          </XhButton>
          <XhButton
            v-if="hasPermission('maintenance.jobs.update')"
            variant="subtle"
            :tone="detailData.paused ? 'success' : 'warning'"
            :disabled="isRunning(detailData)"
            @click="handleToggleStatus(detailData); detailVisible = false"
          >
            <span><Icon :icon="detailData.paused ? 'lucide:play' : 'lucide:pause'" /></span>
            {{ detailData.paused ? t('common.actions.enable') : t('common.actions.disable') }}
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 执行日志抽屉：按任务过滤的运行历史，行点击查看这次运行的详情与异常信息 -->
    <XhDrawerRoot v-model:open="logVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 860px">
        <XhDrawerTitle>{{ t('maintenance.jobs.log_title', { name: logJob?.title ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-task-log-toolbar">
          <XSelect
            :value="logStateFilter"
            clearable
            :options="logStateOptions"
            :placeholder="t('maintenance.jobs.log_status_filter_placeholder')"
            size="sm"
            style="width: 140px"
            @update:value="changeLogState"
          />
          <XInput
            v-model:value="logKeywordFilter"
            clearable
            :placeholder="t('maintenance.jobs.log_keyword_placeholder')"
            size="sm"
            style="width: 180px"
            @clear="loadJobRuns(1)"
            @enter="loadJobRuns(1)"
          />
          <XhButton variant="subtle" size="sm" @click="loadJobRuns(1)">
            <span><Icon icon="lucide:refresh-cw" /></span>
            {{ t('common.actions.refresh') }}
          </XhButton>
          <span class="xh-task-log-tip">{{ t('maintenance.jobs.log_row_tip') }}</span>
        </div>
        <div class="xh-task-log-body">
          <XDataTable
            class="xh-task-log-table"
            :columns="jobRunColumns"
            max-height="100%"
            :data="logItems"
            :loading="logLoading"
            :pagination="{
              page: logPagination.page,
              pageSize: logPagination.pageSize,
              itemCount: logPagination.itemCount,
              onUpdatePage: (p: number) => loadJobRuns(p) }"
            :row-key="(row: JobRunRow) => row.id"
            :row-props="jobRunRowProps"
          />
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 执行日志详情：这次运行的触发、节点、起止与耗时，失败时的错误码与异常信息 -->
    <XhDialogRoot v-model:open="runDetailVisible">
      <XhDialogContent style="--xh-dialog-max-w: 760px">
        <XhDialogTitle>{{ t('maintenance.jobs.log_detail_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <div v-if="runDetail" class="xh-scroll-area" style="max-height: 70vh">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.task_name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ logJob?.title ?? runDetail.job }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.task_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ runDetail.job }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ runDetail.triggerName || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.log_status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhTagRoot variant="subtle" :tone="runStateTone(runDetail.state)" size="sm">
                  <XhTagLabel>
                    {{ runStateLabel(t, runDetail.state) }}
                  </XhTagLabel>
                </XhTagRoot>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.trigger_mode') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ runTriggerLabel(t, runDetail.trigger) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.node_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ runDetail.nodeId || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.scheduled_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(runDetail.scheduledAt) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.start_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(runDetail.startedAt) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.end_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(runDetail.completedAt) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.exec_duration') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDuration(runDetail.durationMs) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.error_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ runDetail.errorCode || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('maintenance.jobs.cancel_requested') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatBoolean(runDetail.cancelRequested) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <template v-if="runDetail.errorMessage">
            <div class="xh-task-log-section is-error">
              {{ t('maintenance.jobs.exception_message') }}
            </div>
            <pre class="xh-task-log-pre is-error">{{ runDetail.errorMessage }}</pre>
          </template>
        </div>
      </XhDialogContent>
    </XhDialogRoot>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding-block: var(--xh-space-8);
}

.xh-trigger-expand {
  padding: var(--xh-space-1) 2px;
}

.xh-trigger-expand code,
.xh-job-triggers code {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-default);
}

.xh-job-triggers {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

/* 列 render 的 h() 节点不带本页 scope，样式须经 :deep() 才能命中 */
:deep(.job-cell-muted) {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}

.xh-task-log-toolbar {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: var(--xh-space-2);
  margin-bottom: var(--xh-space-3);
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
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.xh-task-log-section {
  margin: var(--xh-space-4) 0 var(--xh-space-2);
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
}

.xh-task-log-section.is-error {
  color: var(--xh-fg-danger);
}

.xh-task-log-pre {
  margin: 0;
  padding: var(--xh-space-2) var(--xh-space-3);
  border-radius: var(--xh-radius-md);
  background: var(--xh-bg-subtle);
  font-size: var(--xh-font-size-xs);
  line-height: var(--xh-leading-relaxed);
  white-space: pre-wrap;
  word-break: break-all;
}

/* 异常信息：危险色字、淡危险底，跟着主题走 */
.xh-task-log-pre.is-error {
  color: var(--xh-fg-danger);
  background: color-mix(in oklab, var(--xh-color-danger-600) 6%, transparent);
}
</style>
