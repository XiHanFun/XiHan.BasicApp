<script setup lang="ts">
import type { ApprovalIntent } from './approval-intent'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, SchemaResource } from '~/components'
import type { ResponseOf, WorkflowInstanceStatus } from '~/contracts'
import { XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { approvals, WorkflowInstanceStatus as INSTANCE_STATUSES, tasks, toPageRequest, toSchemaPageResult } from '~/contracts'
import { useTabs } from '~/hooks'
import { formatDate } from '~/utils'
import { instanceStatusTone, outcomeTone } from '../instances/instance-labels'
import { stashApprovalIntent, takeApprovalIntent } from './approval-intent'
import { instanceStatusLabel, outcomeLabel, recordActionLabel } from './approval-labels'
import ApprovalDetailDrawer from './ApprovalDetailDrawer.vue'

/**
 * 审批中心：待我审批 / 我已审批 / 我发起的 / 抄送我的
 * 主体由服务端锁定为当前用户；办理、转办、加签与撤回都在详情抽屉里完成
 * 路由参数 tab 选中页签，instanceId 直接打开该流程的详情（业务页「查看审批」、通知与工作台待办跳转过来），taskId 指明办哪一条待办
 */
defineOptions({ name: 'WorkflowApprovalsView' })

type ApprovalTab = 'todo' | 'done' | 'started' | 'cc'
const TABS: ApprovalTab[] = ['todo', 'done', 'started', 'cc']

type TodoRow = ResponseOf<'tasks.list'>['items'][number]
type RecordRow = ResponseOf<'approvals.list-done'>['items'][number]
type StartedRow = ResponseOf<'approvals.list-started'>['items'][number]

const { t } = useI18n()
const route = useRoute()

const { consumeQuery } = useTabs()
/** 本页的路径：页面被缓存时路由在别的页面上变化，不归本页处理 */
const ownPath = route.path

const activeTab = ref<ApprovalTab>('todo')
const pageRefs: Partial<Record<ApprovalTab, SchemaPageInstance | null>> = {}

const drawerOpen = ref(false)
const drawerInstanceId = ref<string | null>(null)
const drawerTaskId = ref<string | null>(null)

function readTab(value: unknown): ApprovalTab {
  return TABS.includes(value as ApprovalTab)
    ? value as ApprovalTab
    : 'todo'
}

function toStr(value: unknown) {
  return typeof value === 'string'
    ? value.trim() || undefined
    : undefined
}

function bindPage(tab: ApprovalTab) {
  return (instance: unknown) => {
    pageRefs[tab] = instance as SchemaPageInstance | null
  }
}

function reloadAll() {
  for (const tab of TABS)
    void pageRefs[tab]?.reload()
}

function openDetail(instanceId: string, taskId: string | null = null) {
  drawerInstanceId.value = instanceId
  drawerTaskId.value = taskId
  drawerOpen.value = true
}

function applyIntent(intent: ApprovalIntent) {
  if (intent.tab !== undefined)
    activeTab.value = readTab(intent.tab)
  if (intent.instanceId)
    openDetail(intent.instanceId, intent.taskId ?? null)
}

// 去掉参数后页面按新地址重建：重建出来的实例先取走上一个实例记下的意图
const carried = takeApprovalIntent()
if (carried)
  applyIntent(carried)

// 深链参数只用一次：tab 选中页签（通知里的「去审批」），instanceId 打开该流程的详情（业务页的「查看审批」、工作台待办），
// taskId 指明办哪一条待办。用完就从地址里去掉，标签停在 /workflow/approvals 上，切回来或刷新不会再弹出详情
watch(() => [
  route.path,
  route.query.tab,
  route.query.instanceId,
  route.query.taskId,
] as const, ([path, tab, instanceId, taskId]) => {
  if (path !== ownPath || (tab === undefined && instanceId === undefined && taskId === undefined))
    return
  const intent: ApprovalIntent = {
    tab: tab === undefined
      ? undefined
      : readTab(tab),
    instanceId: toStr(instanceId),
    taskId: toStr(taskId) ?? null,
  }
  applyIntent(intent)
  stashApprovalIntent(intent)
  void consumeQuery(['tab', 'instanceId', 'taskId'])
}, { immediate: true })

function outcomeTag(outcome?: string | null) {
  return outcome
    ? renderTag(outcomeLabel(t, outcome), outcomeTone(outcome))
    : '-'
}

function statusTag(status?: WorkflowInstanceStatus | null) {
  return status
    ? renderTag(instanceStatusLabel(t, status), instanceStatusTone(status))
    : '-'
}

/** 审批业务显示名：后端给的是文案键（不是审批业务的流程为空） */
function businessName(nameKey?: string | null) {
  return nameKey
    ? t(nameKey)
    : '-'
}

// SchemaPage 的行类型在框架边界放宽（Record）：回调里按各页签的行类型取用
const asTodo = (row: object) => row as TodoRow
const asRecord = (row: object) => row as RecordRow
const asStarted = (row: object) => row as StartedRow

const keywordField = computed<ListFieldSchema>(() => ({
  key: 'keyword',
  title: t('workflow.approvals.keyword'),
  dataType: 'string',
  visible: false,
  searchable: true,
  searchPlaceholder: t('workflow.approvals.keyword_placeholder'),
  width: 240,
  order: 0,
}))

const viewAction = computed(() => ({
  key: 'view',
  title: t('workflow.approvals.action_view'),
  scope: 'row' as const,
  icon: 'lucide:eye',
}))

// ── 待我审批 ───────────────────────────────────────────────────
// 排序由后端固定（到达时间降序），列不可排序；关键字之外没有筛选
const todoResource: SchemaResource<TodoRow> = {
  page: async params => toSchemaPageResult(await tasks.list(toPageRequest(params)), params),
}

const todoSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow.approvals.todo',
  pageName: t('workflow.approvals.tab_todo'),
  rowKey: 'id',
  fields: [
    keywordField.value,
    {
      key: 'instanceTitle',
      title: t('workflow.approvals.col_title'),
      dataType: 'string',
      minWidth: 220,
      order: 10,
    },
    {
      key: 'businessNameKey',
      title: t('workflow.approvals.col_business'),
      dataType: 'string',
      minWidth: 120,
      order: 11,
      render: row => businessName(asTodo(row).businessNameKey),
    },
    {
      key: 'nodeName',
      title: t('workflow.approvals.col_node'),
      dataType: 'string',
      minWidth: 130,
      order: 12,
    },
    {
      key: 'starterName',
      title: t('workflow.approvals.col_starter'),
      dataType: 'string',
      minWidth: 120,
      order: 13,
      render: row => asTodo(row).starterName || asTodo(row).starterId || '-',
    },
    {
      key: 'createdAt',
      title: t('workflow.approvals.col_arrived_time'),
      dataType: 'datetime',
      minWidth: 170,
      order: 14,
    },
  ],
  resource: todoResource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'handle',
      title: t('workflow.approvals.action_handle'),
      scope: 'row',
      icon: 'lucide:stamp',
      tone: 'brand',
    },
  ],
}))

// ── 我已审批 / 抄送我的（同一记录列表形状） ─────────────────────
function recordFields(kind: 'done' | 'cc'): ListFieldSchema[] {
  const fields: ListFieldSchema[] = [
    keywordField.value,
    {
      key: 'instanceTitle',
      title: t('workflow.approvals.col_title'),
      dataType: 'string',
      minWidth: 220,
      order: 10,
    },
    {
      key: 'businessNameKey',
      title: t('workflow.approvals.col_business'),
      dataType: 'string',
      minWidth: 120,
      order: 11,
      render: row => businessName(asRecord(row).businessNameKey),
    },
    {
      key: 'nodeName',
      title: t('workflow.approvals.col_node'),
      dataType: 'string',
      minWidth: 130,
      order: 12,
    },
    {
      key: 'starterName',
      title: t('workflow.approvals.col_starter'),
      dataType: 'string',
      minWidth: 120,
      order: 13,
      render: row => asRecord(row).starterName || asRecord(row).starterId || '-',
    },
  ]
  if (kind === 'done') {
    fields.push(
      {
        key: 'action',
        title: t('workflow.approvals.col_action'),
        dataType: 'string',
        width: 100,
        order: 14,
        render: row => recordActionLabel(t, asRecord(row).action),
      },
      {
        key: 'outcome',
        title: t('workflow.approvals.col_my_outcome'),
        dataType: 'string',
        width: 120,
        order: 15,
        render: row => outcomeTag(asRecord(row).outcome),
      },
      {
        key: 'comment',
        title: t('workflow.approvals.col_comment'),
        dataType: 'string',
        minWidth: 180,
        ellipsis: true,
        order: 16,
      },
    )
  }
  fields.push(
    {
      key: 'instanceStatus',
      title: t('workflow.approvals.col_status'),
      dataType: 'string',
      width: 120,
      order: 20,
      render: row => statusTag(asRecord(row).instanceStatus),
    },
    {
      key: 'instanceOutcome',
      title: t('workflow.approvals.col_outcome'),
      dataType: 'string',
      width: 120,
      order: 21,
      render: row => outcomeTag(asRecord(row).instanceOutcome),
    },
    {
      key: 'createdAt',
      title: kind === 'done'
        ? t('workflow.approvals.col_handled_time')
        : t('workflow.approvals.col_cc_time'),
      dataType: 'datetime',
      minWidth: 170,
      order: 22,
    },
  )
  return fields
}

const doneResource: SchemaResource<RecordRow> = {
  page: async params => toSchemaPageResult(await approvals.listDone(toPageRequest(params)), params),
}

const ccResource: SchemaResource<RecordRow> = {
  page: async params => toSchemaPageResult(await approvals.listCc(toPageRequest(params)), params),
}

const doneSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow.approvals.done',
  pageName: t('workflow.approvals.tab_done'),
  rowKey: 'id',
  fields: recordFields('done'),
  resource: doneResource as unknown as PageSchema['resource'],
  actions: [viewAction.value],
}))

const ccSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow.approvals.cc',
  pageName: t('workflow.approvals.tab_cc'),
  rowKey: 'id',
  fields: recordFields('cc'),
  resource: ccResource as unknown as PageSchema['resource'],
  actions: [viewAction.value],
}))

// ── 我发起的 ───────────────────────────────────────────────────
const statusOptions = computed(() => INSTANCE_STATUSES.map(status => ({
  label: instanceStatusLabel(t, status),
  value: status,
})))

/** 分页：关键字之外只有状态（精确匹配，后端白名单） */
const startedResource: SchemaResource<StartedRow> = {
  page: async params => toSchemaPageResult(await approvals.listStarted(toPageRequest(params, {
    filters: { status: 'eq' },
  })), params),
}

const startedSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow.approvals.started',
  pageName: t('workflow.approvals.tab_started'),
  rowKey: 'id',
  fields: [
    keywordField.value,
    {
      key: 'status',
      title: t('workflow.approvals.col_status'),
      dataType: 'enum',
      searchable: true,
      options: statusOptions.value,
      searchPlaceholder: t('workflow.approvals.status_placeholder'),
      width: 120,
      order: 1,
      render: row => statusTag(asStarted(row).status),
    },
    {
      key: 'title',
      title: t('workflow.approvals.col_title'),
      dataType: 'string',
      minWidth: 220,
      order: 10,
    },
    {
      key: 'businessNameKey',
      title: t('workflow.approvals.col_business'),
      dataType: 'string',
      minWidth: 120,
      order: 11,
      render: row => businessName(asStarted(row).businessNameKey),
    },
    {
      key: 'outcome',
      title: t('workflow.approvals.col_outcome'),
      dataType: 'string',
      width: 120,
      order: 12,
      render: row => outcomeTag(asStarted(row).outcome),
    },
    {
      key: 'pendingNodeNames',
      title: t('workflow.approvals.col_current_node'),
      dataType: 'string',
      minWidth: 140,
      order: 13,
      render: row => asStarted(row).pendingNodeNames.join('、') || '-',
    },
    {
      key: 'pendingAssigneeNames',
      title: t('workflow.approvals.col_current_assignees'),
      dataType: 'string',
      minWidth: 160,
      ellipsis: true,
      order: 14,
      render: row => asStarted(row).pendingAssigneeNames.join('、') || '-',
    },
    {
      key: 'faultMessage',
      title: t('workflow.approvals.col_fault'),
      dataType: 'string',
      minWidth: 180,
      ellipsis: true,
      order: 15,
    },
    {
      key: 'createdAt',
      title: t('workflow.approvals.col_started_time'),
      dataType: 'datetime',
      minWidth: 170,
      order: 16,
    },
    {
      key: 'endedAt',
      title: t('workflow.approvals.col_end_time'),
      dataType: 'datetime',
      minWidth: 170,
      order: 17,
      render: (row) => {
        const endedAt = asStarted(row).endedAt
        return endedAt
          ? formatDate(endedAt)
          : '-'
      },
    },
  ],
  resource: startedResource as unknown as PageSchema['resource'],
  actions: [viewAction.value],
}))

function onTodoAction(payload: SchemaActionPayload) {
  if (payload.row && payload.key === 'handle') {
    const row = asTodo(payload.row)
    openDetail(row.instanceId, row.id)
  }
}

function onRecordAction(payload: SchemaActionPayload) {
  if (payload.row && payload.key === 'view')
    openDetail(asRecord(payload.row).instanceId)
}

function onStartedAction(payload: SchemaActionPayload) {
  if (payload.row && payload.key === 'view')
    openDetail(asStarted(payload.row).id)
}
</script>

<template>
  <div class="approval-page">
    <XhTabsRoot v-model:value="activeTab" variant="line">
      <XhTabsList>
        <XhTabsTrigger value="todo">
          {{ t('workflow.approvals.tab_todo') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="done">
          {{ t('workflow.approvals.tab_done') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="started">
          {{ t('workflow.approvals.tab_started') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="cc">
          {{ t('workflow.approvals.tab_cc') }}
        </XhTabsTrigger>
        <XhTabsIndicator />
      </XhTabsList>
      <XhTabsContent value="todo">
        <SchemaPage :ref="bindPage('todo')" :schema="todoSchema" @action="onTodoAction" />
      </XhTabsContent>
      <XhTabsContent value="done">
        <SchemaPage :ref="bindPage('done')" :schema="doneSchema" @action="onRecordAction" />
      </XhTabsContent>
      <XhTabsContent value="started">
        <SchemaPage :ref="bindPage('started')" :schema="startedSchema" @action="onStartedAction" />
      </XhTabsContent>
      <XhTabsContent value="cc">
        <SchemaPage :ref="bindPage('cc')" :schema="ccSchema" @action="onRecordAction" />
      </XhTabsContent>
    </XhTabsRoot>

    <ApprovalDetailDrawer
      v-model:open="drawerOpen"
      :instance-id="drawerInstanceId"
      :task-id="drawerTaskId"
      @changed="reloadAll"
    />
  </div>
</template>

<style scoped>
.approval-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 定高表格卡片，分页贴底），
   被标签页包裹时补全高度传递链，与根级 SchemaPage 页面布局一致 */
.approval-page :deep([data-scope='tabs'][data-part='root']) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* 标签条与下方 SchemaPage 内容左对齐，并留出上方呼吸空间 */
.approval-page :deep([data-scope='tabs'][data-part='list']) {
  padding: var(--xh-space-2) var(--xh-space-3) 0;
}

.approval-page :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  height: 0;
  padding: 0;
}
</style>
