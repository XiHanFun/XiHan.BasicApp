<script setup lang="ts">
import type { Tone } from '@xihan-ui/kernel'
import type {
  WorkflowInstanceDetailDto,
  WorkflowInstanceListItemDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { DiagramNodeStatus } from '~/diagram'
import { XhBadge, XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot, XhSeparator } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage, XInput, XJsonBlock } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'
import {
  workflowDefinitionApi,
  workflowInstanceApi,
  WorkflowInstanceStatus,
  WorkflowNodeInstanceStatus,
} from '../../../api'
import WorkflowGraphView from '../definition/designer/WorkflowGraphView.vue'

defineOptions({ name: 'WorkflowInstancePage' })

const { t } = useI18n()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const statusOptions = computed(() => [
  { label: t('workflow.instance.status_running'), value: WorkflowInstanceStatus.Running },
  { label: t('workflow.instance.status_suspended'), value: WorkflowInstanceStatus.Suspended },
  { label: t('workflow.instance.status_completed'), value: WorkflowInstanceStatus.Completed },
  { label: t('workflow.instance.status_canceled'), value: WorkflowInstanceStatus.Canceled },
  { label: t('workflow.instance.status_faulted'), value: WorkflowInstanceStatus.Faulted },
  { label: t('workflow.instance.status_terminated'), value: WorkflowInstanceStatus.Terminated },
])

function statusTag(status: WorkflowInstanceStatus): Tone {
  switch (status) {
    case WorkflowInstanceStatus.Completed:
      return 'success'
    case WorkflowInstanceStatus.Faulted:
      return 'danger'
    case WorkflowInstanceStatus.Running:
      return 'info'
    case WorkflowInstanceStatus.Suspended:
      return 'warning'
    default:
      return 'neutral'
  }
}

function statusLabel(status: WorkflowInstanceStatus) {
  return statusOptions.value.find(option => option.value === status)?.label ?? status
}

function nodeStatusTag(status: WorkflowNodeInstanceStatus): Tone {
  switch (status) {
    case WorkflowNodeInstanceStatus.Completed:
      return 'success'
    case WorkflowNodeInstanceStatus.Faulted:
      return 'danger'
    case WorkflowNodeInstanceStatus.Suspended:
      return 'warning'
    case WorkflowNodeInstanceStatus.Running:
      return 'info'
    default:
      return 'neutral'
  }
}

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

function isRunning(row: WorkflowInstanceListItemDto) {
  return row.status === WorkflowInstanceStatus.Running
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('workflow.instance.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('workflow.instance.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'status',
    title: t('workflow.instance.status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('workflow.instance.status_placeholder'),
    width: 110,
    order: 1,
    render: (row) => {
      const r = row as unknown as WorkflowInstanceListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: statusTag(r.status) }, () => statusLabel(r.status))
    },
  },
  { key: 'definitionCode', title: t('workflow.instance.definition_code'), dataType: 'string', searchable: true, sortable: true, minWidth: 150, order: 2 },
  { key: 'name', title: t('workflow.instance.name'), dataType: 'string', sortable: true, minWidth: 180, order: 10 },
  { key: 'definitionVersion', title: t('workflow.instance.version'), dataType: 'number', width: 80, order: 11, render: (row) => {
    const r = row as unknown as WorkflowInstanceListItemDto
    return h(XhBadge, { variant: 'subtle', size: 'sm' }, () => `v${r.definitionVersion}`)
  } },
  { key: 'correlationId', title: t('workflow.instance.correlation_id'), dataType: 'string', searchable: true, minWidth: 140, order: 12 },
  { key: 'starterId', title: t('workflow.instance.starter'), dataType: 'string', minWidth: 110, order: 13 },
  { key: 'faultMessage', title: t('workflow.instance.fault_message'), dataType: 'string', minWidth: 200, ellipsis: true, order: 14 },
  { key: 'creationTime', title: t('workflow.instance.creation_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 15 },
  { key: 'endTime', title: t('workflow.instance.end_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 16, render: (row) => {
    const r = row as unknown as WorkflowInstanceListItemDto
    return r.endTime ? formatDate(r.endTime) : '-'
  } },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'workflow.instance',
  pageName: t('workflow.instance.page_name'),
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return workflowInstanceApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword),
        status: (f.status as WorkflowInstanceStatus | undefined) ?? undefined,
        definitionCode: toStr(f.definitionCode),
        correlationId: toStr(f.correlationId),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'signal', title: t('workflow.instance.action_signal'), scope: 'page', icon: 'lucide:radio', permission: 'workflow:execute' },
    { key: 'view', title: t('workflow.instance.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'suspend', title: t('workflow.instance.action_suspend'), scope: 'row', type: 'warning', permission: 'workflow:update', visible: row => isRunning(row as unknown as WorkflowInstanceListItemDto) },
    { key: 'resume', title: t('workflow.instance.action_resume'), scope: 'row', type: 'success', permission: 'workflow:update', visible: row => (row as unknown as WorkflowInstanceListItemDto).status === WorkflowInstanceStatus.Suspended },
    { key: 'retry', title: t('workflow.instance.action_retry'), scope: 'row', type: 'primary', permission: 'workflow:execute', visible: row => (row as unknown as WorkflowInstanceListItemDto).status === WorkflowInstanceStatus.Faulted },
    { key: 'cancel', title: t('workflow.instance.action_cancel'), scope: 'row', type: 'warning', permission: 'workflow:execute', visible: row => isRunning(row as unknown as WorkflowInstanceListItemDto) || (row as unknown as WorkflowInstanceListItemDto).status === WorkflowInstanceStatus.Suspended },
    { key: 'terminate', title: t('workflow.instance.action_terminate'), scope: 'row', type: 'error', permission: 'workflow:execute', visible: row => !['Completed', 'Canceled', 'Faulted', 'Terminated'].includes((row as unknown as WorkflowInstanceListItemDto).status) },
  ],
}))

// ── 详情抽屉 ───────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<WorkflowInstanceDetailDto | null>(null)
const detailDefinitionJson = ref<string | null>(null)

/** 节点实例/运行态 → 图节点着色状态 */
const NODE_STATUS_MAP: Record<WorkflowNodeInstanceStatus, DiagramNodeStatus> = {
  [WorkflowNodeInstanceStatus.Running]: 'running',
  [WorkflowNodeInstanceStatus.Completed]: 'completed',
  [WorkflowNodeInstanceStatus.Faulted]: 'faulted',
  [WorkflowNodeInstanceStatus.Suspended]: 'waiting',
  [WorkflowNodeInstanceStatus.Canceled]: 'canceled',
  [WorkflowNodeInstanceStatus.Compensated]: 'compensated',
}

/** 定义节点 id → 运行态（取该节点最近一次实例状态；等待中的书签补 waiting） */
const nodeStatuses = computed<Record<string, DiagramNodeStatus | null>>(() => {
  const result: Record<string, DiagramNodeStatus | null> = {}
  const detail = detailData.value
  if (!detail)
    return result
  for (const node of detail.nodeInstances)
    result[node.nodeId] = NODE_STATUS_MAP[node.status]
  for (const bookmark of detail.pendingBookmarks) {
    if (result[bookmark.nodeId] === undefined)
      result[bookmark.nodeId] = 'waiting'
  }
  return result
})

/** 按 code + version 取定义 JSON（实例只携带 code/version，需回查定义画图） */
async function loadDefinitionJson(code: string, version: number): Promise<string | null> {
  const res = await workflowDefinitionApi.page({
    ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 }, conditions: { sorts: [], filters: [] } }),
    keyword: code,
  })
  const match = res.items.find(item => item.code === code && item.version === version)
  if (!match)
    return null
  const detail = await workflowDefinitionApi.detail(match.basicId)
  return detail?.definitionJson ?? null
}

async function handleDetail(row: WorkflowInstanceListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  detailDefinitionJson.value = null
  try {
    detailData.value = await workflowInstanceApi.detail(row.basicId) ?? null
    if (detailData.value) {
      // 定义回查失败不阻断详情，仅隐藏轨迹图
      try {
        detailDefinitionJson.value = await loadDefinitionJson(detailData.value.definitionCode, detailData.value.definitionVersion)
      }
      catch {
        detailDefinitionJson.value = null
      }
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.instance.err_load_detail'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 带原因的操作（取消/终止/挂起） ──────────────────────────────
const reasonVisible = ref(false)
const reasonLoading = ref(false)
const reasonAction = ref<'cancel' | 'terminate' | 'suspend'>('cancel')
const reasonText = ref('')
const reasonTarget = ref<WorkflowInstanceListItemDto | null>(null)

function openReason(action: 'cancel' | 'terminate' | 'suspend', row: WorkflowInstanceListItemDto) {
  reasonAction.value = action
  reasonText.value = ''
  reasonTarget.value = row
  reasonVisible.value = true
}

const reasonTitle = computed(() => {
  switch (reasonAction.value) {
    case 'cancel':
      return t('workflow.instance.cancel_title')
    case 'terminate':
      return t('workflow.instance.terminate_title')
    default:
      return t('workflow.instance.suspend_title')
  }
})

async function handleReasonConfirm() {
  if (!reasonTarget.value)
    return
  reasonLoading.value = true
  const input = { basicId: reasonTarget.value.basicId, reason: reasonText.value.trim() || undefined }
  try {
    if (reasonAction.value === 'cancel')
      await workflowInstanceApi.cancel(input)
    else if (reasonAction.value === 'terminate')
      await workflowInstanceApi.terminate(input)
    else
      await workflowInstanceApi.suspend(input)
    toast.success(t('workflow.instance.msg_operated'))
    reasonVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.instance.err_operation'))
  }
  finally {
    reasonLoading.value = false
  }
}

async function handleSimple(action: 'retry' | 'resume', row: WorkflowInstanceListItemDto) {
  try {
    if (action === 'retry')
      await workflowInstanceApi.retry({ basicId: row.basicId })
    else
      await workflowInstanceApi.resume({ basicId: row.basicId })
    toast.success(t('workflow.instance.msg_operated'))
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.instance.err_operation'))
  }
}

// ── 发布信号 ───────────────────────────────────────────────────
const signalVisible = ref(false)
const signalLoading = ref(false)
const signalForm = ref({ signalName: '', correlationId: '', payloadJson: '{}' })

async function handleSignal() {
  if (!signalForm.value.signalName.trim()) {
    toast.warning(t('workflow.instance.signal_name_required'))
    return
  }
  signalLoading.value = true
  try {
    const result = await workflowInstanceApi.publishSignal({
      signalName: signalForm.value.signalName.trim(),
      correlationId: signalForm.value.correlationId.trim() || undefined,
      payloadJson: signalForm.value.payloadJson.trim() || undefined,
    })
    toast.success(t('workflow.instance.msg_signal', { count: result.resumedCount }))
    signalVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.instance.err_operation'))
  }
  finally {
    signalLoading.value = false
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as WorkflowInstanceListItemDto | undefined
  switch (payload.key) {
    case 'signal':
      signalForm.value = { signalName: '', correlationId: '', payloadJson: '{}' }
      signalVisible.value = true
      break
    case 'view':
      if (row)
        void handleDetail(row)
      break
    case 'cancel':
    case 'terminate':
    case 'suspend':
      if (row)
        openReason(payload.key, row)
      break
    case 'retry':
    case 'resume':
      if (row)
        void handleSimple(payload.key, row)
      break
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 详情抽屉：实例信息 + 变量 + 执行历史 + 等待点 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 820px">
        <XhDrawerTitle>{{ t('workflow.instance.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.instance.loading') }}
        </div>
        <template v-else-if="detailData">
          <XhDescriptionsRoot :columns="2" bordered placement="left" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instance.name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.name }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instance.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhBadge variant="subtle" :tone="statusTag(detailData.status)" size="sm">
                  {{ statusLabel(detailData.status) }}
                </XhBadge>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instance.definition_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.definitionCode }} (v{{ detailData.definitionVersion }})
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instance.correlation_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.correlationId || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instance.starter') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.starterId || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instance.creation_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(detailData.creationTime) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <XhDescriptionsRoot
            v-if="detailData.faultMessage || detailData.cancellationReason"
            :columns="1"
            bordered
            placement="left"
            size="sm"
          >
            <XhDescriptionsItem v-if="detailData.faultMessage">
              <XhDescriptionsLabel>{{ t('workflow.instance.fault_message') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span class="text-red-500">{{ detailData.faultMessage }}</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="detailData.cancellationReason">
              <XhDescriptionsLabel>{{ t('workflow.instance.cancellation_reason') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.cancellationReason }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <!-- 运行轨迹（只读图 + 节点状态着色） -->
          <template v-if="detailDefinitionJson">
            <div class="flex items-center gap-3 my-3">
              <XhSeparator class="flex-1" /><span class="text-xs text-[hsl(var(--muted-foreground))]">{{ t('workflow.instance.graph_label') }}</span><XhSeparator class="flex-1" />
            </div>
            <div class="h-[380px] overflow-hidden rounded border border-gray-200 dark:border-gray-700">
              <WorkflowGraphView :definition-json="detailDefinitionJson" :statuses="nodeStatuses" />
            </div>
            <div class="mt-2 flex flex-wrap gap-3 text-xs text-gray-500">
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-green-500" />{{ t('workflow.instance.legend_completed') }}</span>
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-blue-500" />{{ t('workflow.instance.legend_running') }}</span>
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-amber-500" />{{ t('workflow.instance.legend_waiting') }}</span>
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-red-500" />{{ t('workflow.instance.legend_faulted') }}</span>
            </div>
          </template>

          <div class="flex items-center gap-3 my-3">
            <XhSeparator class="flex-1" /><span class="text-xs text-[hsl(var(--muted-foreground))]">{{ t('workflow.instance.variables_label') }}</span><XhSeparator class="flex-1" />
          </div>
          <XJsonBlock :raw="detailData.variablesJson" :default-expanded-depth="2" max-height="12rem" />

          <div class="flex items-center gap-3 my-3">
            <XhSeparator class="flex-1" /><span class="text-xs text-[hsl(var(--muted-foreground))]">{{ t('workflow.instance.history_label') }}</span><XhSeparator class="flex-1" />
          </div>
          <table class="xh-plain-table">
            <thead>
              <tr>
                <th>{{ t('workflow.instance.node') }}</th>
                <th>{{ t('workflow.instance.activity_type') }}</th>
                <th>{{ t('workflow.instance.node_status') }}</th>
                <th>{{ t('workflow.instance.try_count') }}</th>
                <th>{{ t('workflow.instance.start_time') }}</th>
                <th>{{ t('workflow.instance.end_time') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="node in detailData.nodeInstances" :key="node.id">
                <td>{{ node.name }} ({{ node.nodeId }})</td>
                <td>{{ node.activityType }}</td>
                <td>
                  <XhBadge variant="subtle" :tone="nodeStatusTag(node.status)" size="sm">
                    {{ node.status }}
                  </XhBadge>
                </td>
                <td>{{ node.tryCount }}</td>
                <td>{{ formatDate(node.startTime) }}</td>
                <td>{{ node.endTime ? formatDate(node.endTime) : '-' }}</td>
              </tr>
            </tbody>
          </table>

          <template v-if="detailData.pendingBookmarks.length > 0">
            <div class="flex items-center gap-3 my-3">
              <XhSeparator class="flex-1" /><span class="text-xs text-[hsl(var(--muted-foreground))]">{{ t('workflow.instance.bookmarks_label') }}</span><XhSeparator class="flex-1" />
            </div>
            <XhFlex direction="column" gap="xs">
              <div v-for="bookmark in detailData.pendingBookmarks" :key="bookmark.id" class="text-xs text-gray-500">
                <XhBadge variant="subtle" size="sm">
                  {{ bookmark.kind }}
                </XhBadge>
                {{ t('workflow.instance.bookmark_node') }}: {{ bookmark.nodeId }}
                <template v-if="bookmark.key">
                  / {{ t('workflow.instance.bookmark_key') }}: {{ bookmark.key }}
                </template>
                <template v-if="bookmark.dueTime">
                  / {{ t('workflow.instance.bookmark_due') }}: {{ formatDate(bookmark.dueTime) }}
                </template>
              </div>
            </XhFlex>
          </template>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 带原因操作 -->
    <XhDialogRoot v-model:open="reasonVisible">
      <XhDialogContent style="--xh-dialog-max-w: 480px">
        <XhDialogTitle>{{ reasonTitle }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <XhFlex direction="column" gap="md">
          <XInput
            v-model:value="reasonText"
            type="textarea"
            :autosize="{ minRows: 2, maxRows: 5 }"
            :placeholder="t('workflow.instance.reason_placeholder')"
          />
          <XhButton block :tone="reasonAction === 'terminate' ? 'danger' : 'warning'" :loading="reasonLoading" @click="handleReasonConfirm">
            {{ t('workflow.instance.btn_confirm') }}
          </XhButton>
        </XhFlex>
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 发布信号 -->
    <XhDialogRoot v-model:open="signalVisible">
      <XhDialogContent style="--xh-dialog-max-w: 520px">
        <XhDialogTitle>{{ t('workflow.instance.signal_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <XhFormRoot
          validate-on="blur"
          layout="horizontal"
        >
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instance.signal_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="signalForm.signalName" :placeholder="t('workflow.instance.signal_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instance.correlation_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="signalForm.correlationId" :placeholder="t('workflow.instance.signal_correlation_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instance.signal_payload') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="signalForm.payloadJson" type="textarea" :autosize="{ minRows: 3, maxRows: 8 }" class="font-mono" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormRoot>
        <XhButton block tone="brand" :loading="signalLoading" @click="handleSignal">
          {{ t('workflow.instance.btn_signal') }}
        </XhButton>
      </XhDialogContent>
    </XhDialogRoot>
  </SchemaPage>
</template>
