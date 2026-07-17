<script setup lang="ts">
import type { PageResult, WorkflowInstanceDetailDto, WorkflowInstanceListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { DiagramNodeStatus } from '~/diagram'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDivider,
  NDrawer,
  NDrawerContent,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NSpace,
  NTable,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
  workflowDefinitionApi,
  workflowInstanceApi,
  WorkflowInstanceStatus,
  WorkflowNodeInstanceStatus,
} from '@/api'
import { SchemaPage } from '~/components'
import { formatDate } from '~/utils'
import WorkflowGraphView from '../definition/designer/WorkflowGraphView.vue'

defineOptions({ name: 'WorkflowInstancePage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const { t } = useI18n()
const message = useMessage()

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

function statusTag(status: WorkflowInstanceStatus): TagType {
  switch (status) {
    case WorkflowInstanceStatus.Completed:
      return 'success'
    case WorkflowInstanceStatus.Faulted:
      return 'error'
    case WorkflowInstanceStatus.Running:
      return 'info'
    case WorkflowInstanceStatus.Suspended:
      return 'warning'
    default:
      return 'default'
  }
}

function statusLabel(status: WorkflowInstanceStatus) {
  return statusOptions.value.find(option => option.value === status)?.label ?? status
}

function nodeStatusTag(status: WorkflowNodeInstanceStatus): TagType {
  switch (status) {
    case WorkflowNodeInstanceStatus.Completed:
      return 'success'
    case WorkflowNodeInstanceStatus.Faulted:
      return 'error'
    case WorkflowNodeInstanceStatus.Suspended:
      return 'warning'
    case WorkflowNodeInstanceStatus.Running:
      return 'info'
    default:
      return 'default'
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
      return h(NTag, { size: 'small', round: true, bordered: false, type: statusTag(r.status) }, () => statusLabel(r.status))
    },
  },
  { key: 'definitionCode', title: t('workflow.instance.definition_code'), dataType: 'string', searchable: true, sortable: true, minWidth: 150, order: 2 },
  { key: 'name', title: t('workflow.instance.name'), dataType: 'string', sortable: true, minWidth: 180, order: 10 },
  { key: 'definitionVersion', title: t('workflow.instance.version'), dataType: 'number', width: 80, order: 11, render: (row) => {
    const r = row as unknown as WorkflowInstanceListItemDto
    return h(NTag, { size: 'small', bordered: false }, () => `v${r.definitionVersion}`)
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
  scrollX: 1700,
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
  catch {
    message.error(t('workflow.instance.err_load_detail'))
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
    message.success(t('workflow.instance.msg_operated'))
    reasonVisible.value = false
    reload()
  }
  catch {
    message.error(t('workflow.instance.err_operation'))
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
    message.success(t('workflow.instance.msg_operated'))
    reload()
  }
  catch {
    message.error(t('workflow.instance.err_operation'))
  }
}

// ── 发布信号 ───────────────────────────────────────────────────
const signalVisible = ref(false)
const signalLoading = ref(false)
const signalForm = ref({ signalName: '', correlationId: '', payloadJson: '{}' })

async function handleSignal() {
  if (!signalForm.value.signalName.trim()) {
    message.warning(t('workflow.instance.signal_name_required'))
    return
  }
  signalLoading.value = true
  try {
    const result = await workflowInstanceApi.publishSignal({
      signalName: signalForm.value.signalName.trim(),
      correlationId: signalForm.value.correlationId.trim() || undefined,
      payloadJson: signalForm.value.payloadJson.trim() || undefined,
    })
    message.success(t('workflow.instance.msg_signal', { count: result.resumedCount }))
    signalVisible.value = false
    reload()
  }
  catch {
    message.error(t('workflow.instance.err_operation'))
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
    <NDrawer v-model:show="detailVisible" :width="820">
      <NDrawerContent closable :title="t('workflow.instance.detail_title')">
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.instance.loading') }}
        </div>
        <template v-else-if="detailData">
          <NDescriptions :column="2" bordered label-placement="left" size="small">
            <NDescriptionsItem :label="t('workflow.instance.name')">
              {{ detailData.name }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.instance.status')">
              <NTag :type="statusTag(detailData.status)" round size="small">
                {{ statusLabel(detailData.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.instance.definition_code')">
              {{ detailData.definitionCode }} (v{{ detailData.definitionVersion }})
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.instance.correlation_id')">
              {{ detailData.correlationId || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.instance.starter')">
              {{ detailData.starterId || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.instance.creation_time')">
              {{ formatDate(detailData.creationTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="detailData.faultMessage" :label="t('workflow.instance.fault_message')" :span="2">
              <span class="text-red-500">{{ detailData.faultMessage }}</span>
            </NDescriptionsItem>
            <NDescriptionsItem v-if="detailData.cancellationReason" :label="t('workflow.instance.cancellation_reason')" :span="2">
              {{ detailData.cancellationReason }}
            </NDescriptionsItem>
          </NDescriptions>

          <!-- 运行轨迹（只读图 + 节点状态着色） -->
          <template v-if="detailDefinitionJson">
            <NDivider>{{ t('workflow.instance.graph_label') }}</NDivider>
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

          <NDivider>{{ t('workflow.instance.variables_label') }}</NDivider>
          <pre class="m-0 max-h-48 overflow-auto whitespace-pre-wrap break-all rounded bg-gray-50 p-3 text-xs dark:bg-gray-800">{{ detailData.variablesJson }}</pre>

          <NDivider>{{ t('workflow.instance.history_label') }}</NDivider>
          <NTable size="small" :bordered="false" :single-line="false">
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
                  <NTag :type="nodeStatusTag(node.status)" round size="small">
                    {{ node.status }}
                  </NTag>
                </td>
                <td>{{ node.tryCount }}</td>
                <td>{{ formatDate(node.startTime) }}</td>
                <td>{{ node.endTime ? formatDate(node.endTime) : '-' }}</td>
              </tr>
            </tbody>
          </NTable>

          <template v-if="detailData.pendingBookmarks.length > 0">
            <NDivider>{{ t('workflow.instance.bookmarks_label') }}</NDivider>
            <NSpace vertical :size="4">
              <div v-for="bookmark in detailData.pendingBookmarks" :key="bookmark.id" class="text-xs text-gray-500">
                <NTag size="small" bordered>
                  {{ bookmark.kind }}
                </NTag>
                {{ t('workflow.instance.bookmark_node') }}: {{ bookmark.nodeId }}
                <template v-if="bookmark.key">
                  / {{ t('workflow.instance.bookmark_key') }}: {{ bookmark.key }}
                </template>
                <template v-if="bookmark.dueTime">
                  / {{ t('workflow.instance.bookmark_due') }}: {{ formatDate(bookmark.dueTime) }}
                </template>
              </div>
            </NSpace>
          </template>
        </template>
      </NDrawerContent>
    </NDrawer>

    <!-- 带原因操作 -->
    <NModal v-model:show="reasonVisible" preset="card" :title="reasonTitle" style="width: 480px">
      <NSpace vertical>
        <NInput
          v-model:value="reasonText"
          type="textarea"
          :autosize="{ minRows: 2, maxRows: 5 }"
          :placeholder="t('workflow.instance.reason_placeholder')"
        />
        <NButton block :type="reasonAction === 'terminate' ? 'error' : 'warning'" :loading="reasonLoading" @click="handleReasonConfirm">
          {{ t('workflow.instance.btn_confirm') }}
        </NButton>
      </NSpace>
    </NModal>

    <!-- 发布信号 -->
    <NModal v-model:show="signalVisible" preset="card" :title="t('workflow.instance.signal_title')" style="width: 520px">
      <NForm label-placement="left" :label-width="100">
        <NFormItem :label="t('workflow.instance.signal_name')">
          <NInput v-model:value="signalForm.signalName" :placeholder="t('workflow.instance.signal_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('workflow.instance.correlation_id')">
          <NInput v-model:value="signalForm.correlationId" :placeholder="t('workflow.instance.signal_correlation_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('workflow.instance.signal_payload')">
          <NInput v-model:value="signalForm.payloadJson" type="textarea" :autosize="{ minRows: 3, maxRows: 8 }" class="font-mono" />
        </NFormItem>
      </NForm>
      <NButton block type="primary" :loading="signalLoading" @click="handleSignal">
        {{ t('workflow.instance.btn_signal') }}
      </NButton>
    </NModal>
  </SchemaPage>
</template>
