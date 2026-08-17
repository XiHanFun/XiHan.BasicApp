<script setup lang="ts">
import type { Tone } from '@xihan-ui/kernel'
import type {
  WorkflowDefinitionDetailDto,
  WorkflowDefinitionListItemDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhBadge, XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage, XInput, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'
import {
  workflowDefinitionApi,
  WorkflowDefinitionStatus,
  workflowInstanceApi,
} from '../../../api'
import WorkflowDesigner from './designer/WorkflowDesigner.vue'
import WorkflowGraphView from './designer/WorkflowGraphView.vue'

defineOptions({ name: 'WorkflowDefinitionPage' })

const { t } = useI18n()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const statusOptions = computed(() => [
  { label: t('workflow.definition.status_draft'), value: WorkflowDefinitionStatus.Draft },
  { label: t('workflow.definition.status_published'), value: WorkflowDefinitionStatus.Published },
  { label: t('workflow.definition.status_disabled'), value: WorkflowDefinitionStatus.Disabled },
  { label: t('workflow.definition.status_archived'), value: WorkflowDefinitionStatus.Archived },
])

function statusTag(status: WorkflowDefinitionStatus): Tone {
  switch (status) {
    case WorkflowDefinitionStatus.Published:
      return 'success'
    case WorkflowDefinitionStatus.Draft:
      return 'info'
    case WorkflowDefinitionStatus.Disabled:
      return 'warning'
    default:
      return 'neutral'
  }
}

function statusLabel(status: WorkflowDefinitionStatus) {
  return statusOptions.value.find(option => option.value === status)?.label ?? status
}

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('workflow.definition.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('workflow.definition.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'status',
    title: t('workflow.definition.status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('workflow.definition.status_placeholder'),
    width: 110,
    order: 1,
    render: (row) => {
      const r = row as unknown as WorkflowDefinitionListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: statusTag(r.status) }, () => statusLabel(r.status))
    },
  },
  { key: 'code', title: t('workflow.definition.code'), dataType: 'string', sortable: true, minWidth: 160, order: 10 },
  { key: 'name', title: t('workflow.definition.name'), dataType: 'string', sortable: true, minWidth: 180, order: 11 },
  { key: 'version', title: t('workflow.definition.version'), dataType: 'number', sortable: true, width: 90, order: 12, render: (row) => {
    const r = row as unknown as WorkflowDefinitionListItemDto
    return h(XhBadge, { variant: 'subtle', size: 'sm' }, () => `v${r.version}`)
  } },
  { key: 'category', title: t('workflow.definition.category'), dataType: 'string', searchable: true, minWidth: 120, order: 13 },
  { key: 'description', title: t('workflow.definition.description'), dataType: 'string', minWidth: 200, ellipsis: true, order: 14 },
  { key: 'publishTime', title: t('workflow.definition.publish_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 15, render: (row) => {
    const r = row as unknown as WorkflowDefinitionListItemDto
    return r.publishTime ? formatDate(r.publishTime) : '-'
  } },
  { key: 'createdTime', title: t('workflow.definition.created_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 16 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'workflow.definition',
  pageName: t('workflow.definition.page_name'),
  rowKey: 'basicId',
  scrollX: 1500,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return workflowDefinitionApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword),
        status: (f.status as WorkflowDefinitionStatus | undefined) ?? undefined,
        category: toStr(f.category),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: t('workflow.definition.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'workflow:create' },
    { key: 'view', title: t('workflow.definition.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('workflow.definition.action_edit'), scope: 'row', permission: 'workflow:update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
    { key: 'publish', title: t('workflow.definition.action_publish'), scope: 'row', type: 'success', permission: 'workflow:update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
    { key: 'start', title: t('workflow.definition.action_start'), scope: 'row', type: 'primary', permission: 'workflow:execute', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Published },
    { key: 'newVersion', title: t('workflow.definition.action_new_version'), scope: 'row', permission: 'workflow:create', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status !== WorkflowDefinitionStatus.Draft },
    { key: 'disable', title: t('workflow.definition.action_disable'), scope: 'row', type: 'warning', permission: 'workflow:update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Published },
    { key: 'archive', title: t('workflow.definition.action_archive'), scope: 'row', permission: 'workflow:update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Disabled },
    { key: 'delete', title: t('workflow.definition.action_delete'), scope: 'row', type: 'error', permission: 'workflow:delete', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
  ],
}))

// ── 详情抽屉 ───────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<WorkflowDefinitionDetailDto | null>(null)
const showDetailJson = ref(false)

async function handleDetail(row: WorkflowDefinitionListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await workflowDefinitionApi.detail(row.basicId) ?? null
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.definition.err_load_detail'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 创建/编辑（可视化设计器，JSON 视图内置其中） ────────────────
const editVisible = ref(false)
const editLoading = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const editTargetId = ref<string | null>(null)
const editJson = ref('')
const designerKey = ref(0)

// 新建时开一张空白画布：编码/名称在「流程设置」中填写，节点从左侧面板拖入
const emptyDefinitionJson = `{
  "code": "",
  "name": "",
  "nodes": [],
  "transitions": []
}`

function openCreate() {
  editMode.value = 'create'
  editTargetId.value = null
  editJson.value = emptyDefinitionJson
  designerKey.value++
  editVisible.value = true
}

async function openEdit(row: WorkflowDefinitionListItemDto) {
  const detail = await workflowDefinitionApi.detail(row.basicId)
  if (!detail) {
    toast.error(t('workflow.definition.err_load_detail'))
    return
  }
  editMode.value = 'edit'
  editTargetId.value = detail.basicId
  editJson.value = detail.definitionJson
  designerKey.value++
  editVisible.value = true
}

async function handleDesignerSave(json: string) {
  editLoading.value = true
  try {
    if (editMode.value === 'create') {
      await workflowDefinitionApi.create({ definitionJson: json })
      toast.success(t('workflow.definition.msg_created'))
    }
    else if (editTargetId.value) {
      await workflowDefinitionApi.updateDraft({ basicId: editTargetId.value, definitionJson: json })
      toast.success(t('workflow.definition.msg_updated'))
    }
    editVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.definition.err_save'))
  }
  finally {
    editLoading.value = false
  }
}

// ── 发起实例 ───────────────────────────────────────────────────
const startVisible = ref(false)
const startLoading = ref(false)
const startForm = ref({ definitionCode: '', definitionVersion: null as number | null, name: '', correlationId: '', variablesJson: '{}' })

function openStart(row: WorkflowDefinitionListItemDto) {
  startForm.value = { definitionCode: row.code, definitionVersion: row.version, name: '', correlationId: '', variablesJson: '{}' }
  startVisible.value = true
}

async function handleStart() {
  startLoading.value = true
  try {
    await workflowInstanceApi.start({
      definitionCode: startForm.value.definitionCode,
      definitionVersion: startForm.value.definitionVersion ?? undefined,
      name: startForm.value.name.trim() || undefined,
      correlationId: startForm.value.correlationId.trim() || undefined,
      variablesJson: startForm.value.variablesJson.trim() || undefined,
    })
    toast.success(t('workflow.definition.msg_started'))
    startVisible.value = false
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.definition.err_start'))
  }
  finally {
    startLoading.value = false
  }
}

// ── 生命周期操作 ───────────────────────────────────────────────
async function runLifecycle(action: 'publish' | 'newVersion' | 'disable' | 'archive' | 'delete', row: WorkflowDefinitionListItemDto) {
  try {
    switch (action) {
      case 'publish':
        await workflowDefinitionApi.publish({ basicId: row.basicId })
        toast.success(t('workflow.definition.msg_published'))
        break
      case 'newVersion':
        await workflowDefinitionApi.newVersion({ code: row.code })
        toast.success(t('workflow.definition.msg_new_version'))
        break
      case 'disable':
        await workflowDefinitionApi.disable({ basicId: row.basicId })
        toast.success(t('workflow.definition.msg_disabled'))
        break
      case 'archive':
        await workflowDefinitionApi.archive({ basicId: row.basicId })
        toast.success(t('workflow.definition.msg_archived'))
        break
      case 'delete':
        await workflowDefinitionApi.delete(row.basicId)
        toast.success(t('workflow.definition.msg_deleted'))
        break
    }
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.definition.err_operation'))
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as WorkflowDefinitionListItemDto | undefined
  switch (payload.key) {
    case 'create':
      openCreate()
      break
    case 'view':
      if (row)
        void handleDetail(row)
      break
    case 'edit':
      if (row)
        void openEdit(row)
      break
    case 'start':
      if (row)
        openStart(row)
      break
    case 'publish':
    case 'newVersion':
    case 'disable':
    case 'archive':
    case 'delete':
      if (row)
        void runLifecycle(payload.key, row)
      break
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 详情抽屉 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 720px">
        <XhDrawerTitle>{{ t('workflow.definition.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger>✕</XhDrawerCloseTrigger>
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.definition.loading') }}
        </div>
        <template v-else-if="detailData">
          <XhDescriptionsRoot :columns="2" bordered>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definition.code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.code }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definition.name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.name }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definition.version') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                v{{ detailData.version }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definition.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhBadge variant="subtle" :tone="statusTag(detailData.status)" size="sm">
                  {{ statusLabel(detailData.status) }}
                </XhBadge>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definition.category') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.category || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definition.publish_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.publishTime ? formatDate(detailData.publishTime) : '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('workflow.definition.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.description || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <div class="mb-2 mt-4 flex items-center justify-between">
            <span class="text-sm font-medium">{{ t('workflow.definition.graph_label') }}</span>
            <XhButton text size="sm" @click="showDetailJson = !showDetailJson">
              {{ showDetailJson ? t('workflow.definition.hide_json') : t('workflow.definition.show_json') }}
            </XhButton>
          </div>
          <div class="h-[440px] overflow-hidden rounded border border-gray-200 dark:border-gray-700">
            <WorkflowGraphView :definition-json="detailData.definitionJson" />
          </div>
          <pre v-if="showDetailJson" class="m-0 mt-2 max-h-96 overflow-auto whitespace-pre-wrap break-all rounded bg-gray-50 p-3 text-xs dark:bg-gray-800">{{ detailData.definitionJson }}</pre>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 创建/编辑（可视化设计器，内置 JSON 视图） -->
    <XhDialogRoot v-model:open="editVisible">
      <XhDialogContent style="width: 96vw; max-width: 1600px">
        <XhDialogTitle>{{ editMode === 'create' ? t('workflow.definition.create_title') : t('workflow.definition.edit_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>
        <WorkflowDesigner
          :key="designerKey"
          :initial-json="editJson"
          :saving="editLoading"
          @save="handleDesignerSave"
        />
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 发起实例 -->
    <XhDialogRoot v-model:open="startVisible">
      <XhDialogContent style="width: 560px">
        <XhDialogTitle>{{ t('workflow.definition.start_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>
        <XhFormRoot
          validate-on="blur"
          layout="horizontal"
        >
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.definitionCode" disabled />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.version') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="startForm.definitionVersion" :min="1" class="w-full" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.instance_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.name" :placeholder="t('workflow.definition.instance_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.correlation_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.correlationId" :placeholder="t('workflow.definition.correlation_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.variables') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.variablesJson" type="textarea" :autosize="{ minRows: 4, maxRows: 10 }" class="font-mono" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormRoot>
        <XhButton block tone="brand" :loading="startLoading" @click="handleStart">
          {{ t('workflow.definition.btn_start') }}
        </XhButton>
      </XhDialogContent>
    </XhDialogRoot>
  </SchemaPage>
</template>
