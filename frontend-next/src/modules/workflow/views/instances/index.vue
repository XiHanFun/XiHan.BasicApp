<script setup lang="ts">
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, SchemaResource } from '~/components'
import type { ResponseOf, WorkflowInstanceStatus } from '~/contracts'
import type { FlowNodeStatus } from '~/designers/flow'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhSeparator, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, renderTag, SchemaPage, XEditModal, XInput, XJsonBlock } from '~/components'
import { toast } from '~/composables'
import { toPageRequest, toSchemaPageResult, workflowInstances } from '~/contracts'
import { throwOnFailed } from '~/request'
import { formatDate } from '~/utils'
import WorkflowGraphView from '../definitions/designer/WorkflowGraphView.vue'
import { graphStatuses, instanceStatusTone, nodeStatusTone, nodeTitles, outcomeTone, standardOutcome } from './instance-labels'

defineOptions({ name: 'WorkflowInstancesView' })

type WorkflowInstanceRow = ResponseOf<'workflow-instances.list'>['items'][number]
type WorkflowInstanceDetail = ResponseOf<'workflow-instances.get'>

const { t } = useI18n()

const schemaPageRef = ref<SchemaPageInstance | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
const statusOptions = computed<{ label: string, value: WorkflowInstanceStatus }[]>(() => [
  { label: t('workflow.instances.status_running'), value: 'Running' },
  { label: t('workflow.instances.status_suspended'), value: 'Suspended' },
  { label: t('workflow.instances.status_completed'), value: 'Completed' },
  { label: t('workflow.instances.status_canceled'), value: 'Canceled' },
  { label: t('workflow.instances.status_faulted'), value: 'Faulted' },
  { label: t('workflow.instances.status_terminated'), value: 'Terminated' },
])

function statusLabel(status: WorkflowInstanceStatus) {
  return statusOptions.value.find(option => option.value === status)?.label ?? status
}

/** 标准结果有文案，业务自定义的结果原样显示 */
function outcomeLabel(outcome: string) {
  switch (standardOutcome(outcome)) {
    case 'approved':
      return t('workflow.instances.outcome_approved')
    case 'rejected':
      return t('workflow.instances.outcome_rejected')
    case 'timeout':
      return t('workflow.instances.outcome_timeout')
    default:
      return outcome
  }
}

function isRunning(row: WorkflowInstanceRow) {
  return row.status === 'Running'
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
// 照 4.x 五列可排序、可 Shift 追加次级排序，不排序时创建时间降序；关键字匹配实例名称（标题）与关联 ID
const asRow = (row: object) => row as WorkflowInstanceRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('workflow.instances.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('workflow.instances.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'status',
    title: t('workflow.instances.status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('workflow.instances.status_placeholder'),
    width: 110,
    order: 1,
    render: row => renderTag(statusLabel(asRow(row).status), instanceStatusTone(asRow(row).status)),
  },
  { key: 'outcome', title: t('workflow.instances.outcome'), dataType: 'string', width: 100, order: 2, render: (row) => {
    const outcome = asRow(row).outcome
    return outcome ? renderTag(outcomeLabel(outcome), outcomeTone(outcome)) : '-'
  } },
  { key: 'definitionName', title: t('workflow.instances.definition_code'), dataType: 'string', searchable: true, sortable: true, minWidth: 150, order: 3 },
  { key: 'title', title: t('workflow.instances.name'), dataType: 'string', sortable: true, minWidth: 180, order: 10 },
  { key: 'definitionVersion', title: t('workflow.instances.version'), dataType: 'number', width: 80, order: 11, render: row => renderTag(`v${asRow(row).definitionVersion}`, 'neutral') },
  { key: 'correlationId', title: t('workflow.instances.correlation_id'), dataType: 'string', searchable: true, minWidth: 140, order: 12 },
  { key: 'starterName', title: t('workflow.instances.starter'), dataType: 'string', minWidth: 110, order: 13, render: row => asRow(row).starterName || asRow(row).starterId || '-' },
  { key: 'faultMessage', title: t('workflow.instances.fault_message'), dataType: 'string', minWidth: 200, ellipsis: true, order: 14 },
  { key: 'createdAt', title: t('workflow.instances.creation_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 15 },
  { key: 'endedAt', title: t('workflow.instances.end_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 16, render: (row) => {
    const endedAt = asRow(row).endedAt
    return endedAt ? formatDate(endedAt) : '-'
  } },
])

/** 分页：状态、定义名与业务单号精确匹配，创建时间按区间（后端白名单） */
const resource: SchemaResource<WorkflowInstanceRow> = {
  page: async params => toSchemaPageResult(
    await workflowInstances.list(toPageRequest(params, { filters: { status: 'eq', definitionName: 'eq', correlationId: 'eq' }, ranges: ['createdAt'] })),
    params,
  ),
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'workflow.instances',
  pageName: t('workflow.instances.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'signal', title: t('workflow.instances.action_signal'), scope: 'page', icon: 'lucide:radio', permission: 'workflow.workflow-instances.execute' },
    { key: 'view', title: t('workflow.instances.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'suspend', title: t('workflow.instances.action_suspend'), scope: 'row', icon: 'lucide:pause', tone: 'warning', permission: 'workflow.workflow-instances.update', visible: row => isRunning(asRow(row)) },
    { key: 'resume', title: t('workflow.instances.action_resume'), scope: 'row', icon: 'lucide:play', tone: 'success', confirm: true, confirmText: row => actionConfirmText(t, t('workflow.instances.action_resume'), asRow(row).title), permission: 'workflow.workflow-instances.update', visible: row => asRow(row).status === 'Suspended' },
    { key: 'retry', title: t('workflow.instances.action_retry'), scope: 'row', icon: 'lucide:rotate-cw', tone: 'brand', confirm: true, confirmText: row => actionConfirmText(t, t('workflow.instances.action_retry'), asRow(row).title), permission: 'workflow.workflow-instances.execute', visible: row => asRow(row).status === 'Faulted' },
    { key: 'cancel', title: t('workflow.instances.action_cancel'), scope: 'row', icon: 'lucide:circle-x', tone: 'warning', permission: 'workflow.workflow-instances.execute', visible: row => isRunning(asRow(row)) || asRow(row).status === 'Suspended' },
    { key: 'terminate', title: t('workflow.instances.action_terminate'), scope: 'row', icon: 'lucide:octagon-x', tone: 'danger', permission: 'workflow.workflow-instances.execute', visible: row => !['Completed', 'Canceled', 'Faulted', 'Terminated'].includes(asRow(row).status) },
  ],
}))

// ── 详情抽屉 ───────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<WorkflowInstanceDetail | null>(null)

/** 定义节点 id → 运行态（取该节点最近一次执行的状态；等待中的补 waiting） */
const nodeStatuses = computed<Record<string, FlowNodeStatus | null>>(() => {
  const detail = detailData.value
  return detail ? graphStatuses(detail.nodes, detail.waits) : {}
})

/** 执行历史只带节点 id，节点名从实例所用的定义里取 */
const titles = computed(() => nodeTitles(detailData.value?.document))

async function handleDetail(row: WorkflowInstanceRow) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await workflowInstances.get({ id: row.id })
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.instances.err_load_detail'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 批量操作的结果 ─────────────────────────────────────────────
// ── 带原因的操作（取消/终止/挂起） ──────────────────────────────
/** 弹窗底部的确认钮靠这个 id 关联到表单，点它走表单提交 */
const reasonFormId = useId()
const reasonVisible = ref(false)
const reasonLoading = ref(false)
const reasonAction = ref<'cancel' | 'terminate' | 'suspend'>('cancel')
const reasonText = ref('')
const reasonTarget = ref<WorkflowInstanceRow | null>(null)

function openReason(action: 'cancel' | 'terminate' | 'suspend', row: WorkflowInstanceRow) {
  reasonAction.value = action
  reasonText.value = ''
  reasonTarget.value = row
  reasonVisible.value = true
}

const reasonTitle = computed(() => {
  switch (reasonAction.value) {
    case 'cancel':
      return t('workflow.instances.cancel_title')
    case 'terminate':
      return t('workflow.instances.terminate_title')
    default:
      return t('workflow.instances.suspend_title')
  }
})

async function handleReasonConfirm() {
  if (!reasonTarget.value)
    return
  reasonLoading.value = true
  const input = { ids: [reasonTarget.value.id], reason: reasonText.value.trim() || null }
  try {
    if (reasonAction.value === 'cancel')
      throwOnFailed(await workflowInstances.cancel(input))
    else if (reasonAction.value === 'terminate')
      throwOnFailed(await workflowInstances.terminate(input))
    else
      throwOnFailed(await workflowInstances.suspend(input))
    toast.success(t('workflow.instances.msg_operated'))
    reasonVisible.value = false
    reload()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.instances.err_operation'))
  }
  finally {
    reasonLoading.value = false
  }
}

async function handleSimple(action: 'retry' | 'resume', row: WorkflowInstanceRow) {
  try {
    if (action === 'retry')
      throwOnFailed(await workflowInstances.retry({ ids: [row.id] }))
    else
      throwOnFailed(await workflowInstances.resume({ ids: [row.id] }))
    toast.success(t('workflow.instances.msg_operated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.instances.err_operation'))
  }
}

// ── 发布信号 ───────────────────────────────────────────────────
/** 弹窗底部的发布钮靠这个 id 关联到表单，点它走表单提交 */
const signalFormId = useId()
const signalVisible = ref(false)
const signalLoading = ref(false)
const signalForm = ref({ name: '', correlationId: '', payload: '{}' })

async function handleSignal() {
  if (!signalForm.value.name.trim()) {
    toast.warning(t('workflow.instances.signal_name_required'))
    return
  }
  signalLoading.value = true
  try {
    const result = await workflowInstances.signal({
      name: signalForm.value.name.trim(),
      correlationId: signalForm.value.correlationId.trim() || null,
      payload: signalForm.value.payload.trim() || null,
    })
    toast.success(t('workflow.instances.msg_signal', { count: result.resumed }))
    signalVisible.value = false
    reload()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.instances.err_operation'))
  }
  finally {
    signalLoading.value = false
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'signal':
      signalForm.value = { name: '', correlationId: '', payload: '{}' }
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
        <XhDrawerTitle>{{ t('workflow.instances.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.instances.loading') }}
        </div>
        <template v-else-if="detailData">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instances.name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.instance.title }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instances.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span class="inline-flex items-center gap-1">
                  <XhTagRoot variant="subtle" :tone="instanceStatusTone(detailData.instance.status)" size="sm">
                    <XhTagLabel>
                      {{ statusLabel(detailData.instance.status) }}
                    </XhTagLabel>
                  </XhTagRoot>
                  <XhTagRoot v-if="detailData.instance.outcome" variant="subtle" :tone="outcomeTone(detailData.instance.outcome)" size="sm">
                    <XhTagLabel>
                      {{ outcomeLabel(detailData.instance.outcome) }}
                    </XhTagLabel>
                  </XhTagRoot>
                </span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instances.definition_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.instance.definitionName }} (v{{ detailData.instance.definitionVersion }})
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instances.correlation_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.instance.correlationId || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instances.starter') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.instance.starterName || detailData.instance.starterId || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.instances.creation_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(detailData.instance.createdAt) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <XhDescriptionsRoot
            v-if="detailData.instance.faultMessage || detailData.lastError || detailData.cancellationReason"
            :columns="1"
            variant="outline"
            size="sm"
          >
            <XhDescriptionsItem v-if="detailData.instance.faultMessage">
              <XhDescriptionsLabel>{{ t('workflow.instances.fault_message') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span class="text-red-500">{{ detailData.instance.faultMessage }}</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <!-- 流程捕获的最近一次错误（变量 lastError）：后端按界面语言给出，不显示变量里进程缺省区域的 message -->
            <XhDescriptionsItem v-if="detailData.lastError">
              <XhDescriptionsLabel>{{ t('workflow.instances.last_error') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.lastError }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="detailData.cancellationReason">
              <XhDescriptionsLabel>{{ t('workflow.instances.cancellation_reason') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.cancellationReason }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <!-- 运行轨迹（只读图 + 节点状态着色）：实例详情带着所用的定义，不再回查
               分节文字放进分隔线的默认插槽；separator 的子节点对读屏是展示性的，名字另由 aria-label 给 -->
          <template v-if="detailData.document">
            <XhSeparator class="my-3" :aria-label="t('workflow.instances.graph_label')">
              {{ t('workflow.instances.graph_label') }}
            </XhSeparator>
            <div class="h-[380px] overflow-hidden rounded border border-gray-200 dark:border-gray-700">
              <WorkflowGraphView :definition-json="detailData.document" :statuses="nodeStatuses" />
            </div>
            <div class="mt-2 flex flex-wrap gap-3 text-xs text-gray-500">
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-green-500" />{{ t('workflow.instances.legend_completed') }}</span>
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-blue-500" />{{ t('workflow.instances.legend_running') }}</span>
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-amber-500" />{{ t('workflow.instances.legend_waiting') }}</span>
              <span class="flex items-center gap-1"><span class="h-2.5 w-2.5 rounded-full bg-red-500" />{{ t('workflow.instances.legend_faulted') }}</span>
            </div>
          </template>

          <XhSeparator class="my-3" :aria-label="t('workflow.instances.variables_label')">
            {{ t('workflow.instances.variables_label') }}
          </XhSeparator>
          <XJsonBlock :raw="detailData.variables" :default-expanded-depth="2" max-height="12rem" />

          <XhSeparator class="my-3" :aria-label="t('workflow.instances.history_label')">
            {{ t('workflow.instances.history_label') }}
          </XhSeparator>
          <table class="xh-plain-table">
            <thead>
              <tr>
                <th>{{ t('workflow.instances.node') }}</th>
                <th>{{ t('workflow.instances.activity_type') }}</th>
                <th>{{ t('workflow.instances.node_status') }}</th>
                <th>{{ t('workflow.instances.try_count') }}</th>
                <th>{{ t('workflow.instances.start_time') }}</th>
                <th>{{ t('workflow.instances.end_time') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="node in detailData.nodes" :key="node.id">
                <td>{{ titles[node.nodeId] ?? node.nodeId }} ({{ node.nodeId }})</td>
                <td>{{ node.activityType }}</td>
                <td>
                  <XhTagRoot variant="subtle" :tone="nodeStatusTone(node.status)">
                    <XhTagLabel>
                      {{ node.statusName }}
                    </XhTagLabel>
                  </XhTagRoot>
                </td>
                <td>{{ node.attempt }}</td>
                <td>{{ formatDate(node.startedAt) }}</td>
                <td>{{ node.endedAt ? formatDate(node.endedAt) : '-' }}</td>
              </tr>
            </tbody>
          </table>

          <template v-if="detailData.waits.length > 0">
            <XhSeparator class="my-3" :aria-label="t('workflow.instances.bookmarks_label')">
              {{ t('workflow.instances.bookmarks_label') }}
            </XhSeparator>
            <XhFlex orientation="vertical" gap="xs">
              <div v-for="wait in detailData.waits" :key="wait.id" class="text-xs text-gray-500">
                <XhTagRoot variant="subtle" size="sm">
                  <XhTagLabel>
                    {{ wait.kind }}
                  </XhTagLabel>
                </XhTagRoot>
                {{ t('workflow.instances.bookmark_node') }}: {{ titles[wait.nodeId] ?? wait.nodeId }}
                <template v-if="wait.key">
                  / {{ t('workflow.instances.bookmark_key') }}: {{ wait.key }}
                </template>
                <template v-if="wait.dueAt">
                  / {{ t('workflow.instances.bookmark_due') }}: {{ formatDate(wait.dueAt) }}
                </template>
              </div>
            </XhFlex>
          </template>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 带原因操作 -->
    <XEditModal
      v-model:show="reasonVisible"
      :title="reasonTitle"
      :width="480"
      :loading="reasonLoading"
      :save-text="t('workflow.instances.btn_confirm')"
      :save-tone="reasonAction === 'terminate' ? 'danger' : 'warning'"
      :form-id="reasonFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="reasonFormId"
        class="xh-edit-form-grid"
        @submit="handleReasonConfirm"
      >
        <XhFormFieldGroup name="reason" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instances.reason') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="reasonText"
                type="textarea"
                :max-length="500"
                :autosize="{ minRows: 2, maxRows: 5 }"
                :placeholder="t('workflow.instances.reason_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <!-- 发布信号 -->
    <XEditModal
      v-model:show="signalVisible"
      :title="t('workflow.instances.signal_title')"
      :loading="signalLoading"
      :save-text="t('workflow.instances.btn_signal')"
      :form-id="signalFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="signalFormId"
        class="xh-edit-form-grid"
        @submit="handleSignal"
      >
        <XhFormFieldGroup name="name">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instances.signal_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="signalForm.name" :max-length="256" :placeholder="t('workflow.instances.signal_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="correlationId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instances.correlation_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="signalForm.correlationId" :max-length="256" :placeholder="t('workflow.instances.signal_correlation_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="payload" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.instances.signal_payload') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="signalForm.payload" type="textarea" :autosize="{ minRows: 3, maxRows: 8 }" class="font-mono" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
