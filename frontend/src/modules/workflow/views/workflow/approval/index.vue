<script setup lang="ts">
import type {
  WorkflowApprovalInstanceItemDto,
  WorkflowApprovalRecordItemDto,
  WorkflowTodoListItemDto,
} from '../../../api'
import type { PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { createPageRequest } from '@/api'
import { SchemaPage } from '~/components'
import { useTabs } from '~/hooks'
import { formatDate } from '~/utils'
import { workflowApprovalApi, WorkflowInstanceStatus, workflowTodoApi } from '../../../api'
import { instanceStatusLabelKey, instanceStatusTone, outcomeLabelKey, outcomeTone, recordActionLabelKey } from './approval-labels'
import ApprovalDetailDrawer from './ApprovalDetailDrawer.vue'

/**
 * 审批中心：待我审批 / 我已审批 / 我发起的 / 抄送我的。
 * 主体由服务端锁定为当前用户；办理、转办、加签与撤回都在详情抽屉里完成。
 * 路由参数 tab 选中页签，instanceId 直接打开该流程的详情（业务页「查看审批」跳转过来）。
 */
defineOptions({ name: 'WorkflowApprovalPage' })

type ApprovalTab = 'todo' | 'done' | 'started' | 'cc'
const TABS: ApprovalTab[] = ['todo', 'done', 'started', 'cc']

const { t } = useI18n()
const route = useRoute()

const { consumeQuery } = useTabs()
/** 本页的路径：页面被缓存时路由在别的页面上变化，不归本页处理 */
const ownPath = route.path

const activeTab = ref<ApprovalTab>('todo')
const pageRefs: Partial<Record<ApprovalTab, { reload: () => Promise<void> } | null>> = {}

const drawerOpen = ref(false)
const drawerInstanceId = ref<string | null>(null)
const drawerTaskId = ref<string | null>(null)

function readTab(value: unknown): ApprovalTab {
  return TABS.includes(value as ApprovalTab) ? value as ApprovalTab : 'todo'
}

function toStr(value: unknown) {
  return (value as string | undefined)?.trim() || undefined
}

function bindPage(tab: ApprovalTab) {
  return (instance: unknown) => {
    pageRefs[tab] = instance as { reload: () => Promise<void> } | null
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

// 深链参数只用一次：tab 选中页签（通知里的「去审批」），instanceId 打开该流程的详情（业务页的「查看审批」）。
// 用完就从地址里去掉，标签停在 /approval/center 上，切回来或刷新不会再弹出详情
watch(() => [route.path, route.query.tab, route.query.instanceId] as const, ([path, tab, instanceId]) => {
  if (path !== ownPath || (tab === undefined && instanceId === undefined))
    return
  if (tab !== undefined)
    activeTab.value = readTab(tab)
  const id = toStr(instanceId)
  if (id)
    openDetail(id)
  void consumeQuery(['tab', 'instanceId'])
}, { immediate: true })

function outcomeTag(outcome?: string | null) {
  if (!outcome)
    return '-'
  const key = outcomeLabelKey(outcome)
  return h(XhTagRoot, { variant: 'subtle', tone: outcomeTone(outcome) }, () => h(XhTagLabel, () => key ? t(key) : outcome))
}

function statusTag(status: WorkflowInstanceStatus) {
  return h(XhTagRoot, { variant: 'subtle', tone: instanceStatusTone(status) }, () => h(XhTagLabel, () => t(instanceStatusLabelKey(status))))
}

function pageRequest(page: number, pageSize: number) {
  return createPageRequest({ page: { pageIndex: page, pageSize } })
}

const keywordField = computed<ListFieldSchema>(() => ({
  key: 'keyword',
  title: t('workflow.approval.keyword'),
  dataType: 'string',
  visible: false,
  searchable: true,
  searchPlaceholder: t('workflow.approval.keyword_placeholder'),
  width: 240,
  order: 0,
}))

const viewAction = computed(() => ({ key: 'view', title: t('workflow.approval.action_view'), scope: 'row' as const, icon: 'lucide:eye' }))

// ── 待我审批 ───────────────────────────────────────────────────
const todoSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow_approval.todo',
  pageName: t('workflow.approval.tab_todo'),
  rowKey: 'taskId',
  fields: [
    keywordField.value,
    { key: 'instanceName', title: t('workflow.approval.col_title'), dataType: 'string', minWidth: 220, order: 10 },
    { key: 'businessName', title: t('workflow.approval.col_business'), dataType: 'string', minWidth: 120, order: 11, render: row => (row as unknown as WorkflowTodoListItemDto).businessName || '-' },
    { key: 'nodeName', title: t('workflow.approval.col_node'), dataType: 'string', minWidth: 130, order: 12 },
    { key: 'starterName', title: t('workflow.approval.col_starter'), dataType: 'string', minWidth: 120, order: 13, render: (row) => {
      const r = row as unknown as WorkflowTodoListItemDto
      return r.starterName || r.starterId || '-'
    } },
    { key: 'creationTime', title: t('workflow.approval.col_arrived_time'), dataType: 'datetime', minWidth: 170, order: 14 },
  ],
  resource: {
    page: params => workflowTodoApi.page({
      ...pageRequest(params.page, params.pageSize),
      keyword: toStr(params.filters.keyword),
    }) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'handle', title: t('workflow.approval.action_handle'), scope: 'row', icon: 'lucide:stamp', tone: 'brand' },
  ],
}))

// ── 我已审批 / 抄送我的（同一记录列表形状） ─────────────────────
function recordFields(kind: 'done' | 'cc'): ListFieldSchema[] {
  const fields: ListFieldSchema[] = [
    keywordField.value,
    { key: 'instanceName', title: t('workflow.approval.col_title'), dataType: 'string', minWidth: 220, order: 10 },
    { key: 'businessName', title: t('workflow.approval.col_business'), dataType: 'string', minWidth: 120, order: 11, render: row => (row as unknown as WorkflowApprovalRecordItemDto).businessName || '-' },
    { key: 'nodeName', title: t('workflow.approval.col_node'), dataType: 'string', minWidth: 130, order: 12 },
    { key: 'starterName', title: t('workflow.approval.col_starter'), dataType: 'string', minWidth: 120, order: 13, render: (row) => {
      const r = row as unknown as WorkflowApprovalRecordItemDto
      return r.starterName || r.starterId || '-'
    } },
  ]
  if (kind === 'done') {
    fields.push(
      { key: 'action', title: t('workflow.approval.col_action'), dataType: 'string', width: 100, order: 14, render: row => t(recordActionLabelKey((row as unknown as WorkflowApprovalRecordItemDto).action)) },
      { key: 'outcome', title: t('workflow.approval.col_my_outcome'), dataType: 'string', width: 120, order: 15, render: row => outcomeTag((row as unknown as WorkflowApprovalRecordItemDto).outcome) },
      { key: 'comment', title: t('workflow.approval.col_comment'), dataType: 'string', minWidth: 180, ellipsis: true, order: 16 },
    )
  }
  fields.push(
    { key: 'instanceStatus', title: t('workflow.approval.col_status'), dataType: 'string', width: 120, order: 20, render: row => statusTag((row as unknown as WorkflowApprovalRecordItemDto).instanceStatus) },
    { key: 'instanceOutcome', title: t('workflow.approval.col_outcome'), dataType: 'string', width: 120, order: 21, render: row => outcomeTag((row as unknown as WorkflowApprovalRecordItemDto).instanceOutcome) },
    { key: 'createdTime', title: t(kind === 'done' ? 'workflow.approval.col_handled_time' : 'workflow.approval.col_cc_time'), dataType: 'datetime', minWidth: 170, order: 22 },
  )
  return fields
}

const doneSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow_approval.done',
  pageName: t('workflow.approval.tab_done'),
  rowKey: 'basicId',
  fields: recordFields('done'),
  resource: {
    page: params => workflowApprovalApi.donePage({
      ...pageRequest(params.page, params.pageSize),
      keyword: toStr(params.filters.keyword),
    }) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [viewAction.value],
}))

const ccSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow_approval.cc',
  pageName: t('workflow.approval.tab_cc'),
  rowKey: 'basicId',
  fields: recordFields('cc'),
  resource: {
    page: params => workflowApprovalApi.ccPage({
      ...pageRequest(params.page, params.pageSize),
      keyword: toStr(params.filters.keyword),
    }) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [viewAction.value],
}))

// ── 我发起的 ───────────────────────────────────────────────────
const statusOptions = computed(() => Object.values(WorkflowInstanceStatus).map(status => ({
  label: t(instanceStatusLabelKey(status)),
  value: status,
})))

const startedSchema = computed<PageSchema>(() => ({
  pageCode: 'workflow_approval.started',
  pageName: t('workflow.approval.tab_started'),
  rowKey: 'basicId',
  fields: [
    keywordField.value,
    {
      key: 'status',
      title: t('workflow.approval.col_status'),
      dataType: 'enum',
      searchable: true,
      options: statusOptions.value,
      searchPlaceholder: t('workflow.approval.status_placeholder'),
      width: 120,
      order: 1,
      render: row => statusTag((row as unknown as WorkflowApprovalInstanceItemDto).status),
    },
    { key: 'name', title: t('workflow.approval.col_title'), dataType: 'string', minWidth: 220, order: 10 },
    { key: 'businessName', title: t('workflow.approval.col_business'), dataType: 'string', minWidth: 120, order: 11, render: row => (row as unknown as WorkflowApprovalInstanceItemDto).businessName || '-' },
    { key: 'outcome', title: t('workflow.approval.col_outcome'), dataType: 'string', width: 120, order: 12, render: row => outcomeTag((row as unknown as WorkflowApprovalInstanceItemDto).outcome) },
    { key: 'pendingNodeNames', title: t('workflow.approval.col_current_node'), dataType: 'string', minWidth: 140, order: 13, render: row => (row as unknown as WorkflowApprovalInstanceItemDto).pendingNodeNames.join('、') || '-' },
    { key: 'pendingAssigneeNames', title: t('workflow.approval.col_current_assignees'), dataType: 'string', minWidth: 160, ellipsis: true, order: 14, render: row => (row as unknown as WorkflowApprovalInstanceItemDto).pendingAssigneeNames.join('、') || '-' },
    { key: 'faultMessage', title: t('workflow.approval.col_fault'), dataType: 'string', minWidth: 180, ellipsis: true, order: 15 },
    { key: 'creationTime', title: t('workflow.approval.col_started_time'), dataType: 'datetime', minWidth: 170, order: 16 },
    { key: 'endTime', title: t('workflow.approval.col_end_time'), dataType: 'datetime', minWidth: 170, order: 17, render: (row) => {
      const r = row as unknown as WorkflowApprovalInstanceItemDto
      return r.endTime ? formatDate(r.endTime) : '-'
    } },
  ],
  resource: {
    page: (params) => {
      const f = params.filters
      return workflowApprovalApi.startedPage({
        ...pageRequest(params.page, params.pageSize),
        keyword: toStr(f.keyword),
        status: (f.status as WorkflowInstanceStatus | undefined) ?? null,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [viewAction.value],
}))

function onTodoAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as WorkflowTodoListItemDto | undefined
  if (row && payload.key === 'handle')
    openDetail(String(row.instanceId), row.taskId)
}

function onRecordAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as WorkflowApprovalRecordItemDto | undefined
  if (row && payload.key === 'view')
    openDetail(String(row.instanceId))
}

function onStartedAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as WorkflowApprovalInstanceItemDto | undefined
  if (row && payload.key === 'view')
    openDetail(String(row.basicId))
}
</script>

<template>
  <div class="approval-page">
    <XhTabsRoot v-model:value="activeTab" variant="line">
      <XhTabsList>
        <XhTabsTrigger value="todo">
          {{ t('workflow.approval.tab_todo') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="done">
          {{ t('workflow.approval.tab_done') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="started">
          {{ t('workflow.approval.tab_started') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="cc">
          {{ t('workflow.approval.tab_cc') }}
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
