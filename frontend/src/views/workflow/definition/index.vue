<script setup lang="ts">
import type { PageResult, WorkflowDefinitionDetailDto, WorkflowDefinitionListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
  workflowDefinitionApi,
  WorkflowDefinitionStatus,
  workflowInstanceApi,
} from '@/api'
import { SchemaPage } from '~/components'
import { formatDate } from '~/utils'
import WorkflowDesigner from './designer/WorkflowDesigner.vue'
import WorkflowGraphView from './designer/WorkflowGraphView.vue'

defineOptions({ name: 'WorkflowDefinitionPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const { t } = useI18n()
const message = useMessage()

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

function statusTag(status: WorkflowDefinitionStatus): TagType {
  switch (status) {
    case WorkflowDefinitionStatus.Published:
      return 'success'
    case WorkflowDefinitionStatus.Draft:
      return 'info'
    case WorkflowDefinitionStatus.Disabled:
      return 'warning'
    default:
      return 'default'
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
      return h(NTag, { size: 'small', round: true, bordered: false, type: statusTag(r.status) }, () => statusLabel(r.status))
    },
  },
  { key: 'code', title: t('workflow.definition.code'), dataType: 'string', sortable: true, minWidth: 160, order: 10 },
  { key: 'name', title: t('workflow.definition.name'), dataType: 'string', sortable: true, minWidth: 180, order: 11 },
  { key: 'version', title: t('workflow.definition.version'), dataType: 'number', sortable: true, width: 90, order: 12, render: (row) => {
    const r = row as unknown as WorkflowDefinitionListItemDto
    return h(NTag, { size: 'small', bordered: false }, () => `v${r.version}`)
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
  catch {
    message.error(t('workflow.definition.err_load_detail'))
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
    message.error(t('workflow.definition.err_load_detail'))
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
      message.success(t('workflow.definition.msg_created'))
    }
    else if (editTargetId.value) {
      await workflowDefinitionApi.updateDraft({ basicId: editTargetId.value, definitionJson: json })
      message.success(t('workflow.definition.msg_updated'))
    }
    editVisible.value = false
    reload()
  }
  catch {
    message.error(t('workflow.definition.err_save'))
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
    message.success(t('workflow.definition.msg_started'))
    startVisible.value = false
  }
  catch {
    message.error(t('workflow.definition.err_start'))
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
        message.success(t('workflow.definition.msg_published'))
        break
      case 'newVersion':
        await workflowDefinitionApi.newVersion({ code: row.code })
        message.success(t('workflow.definition.msg_new_version'))
        break
      case 'disable':
        await workflowDefinitionApi.disable({ basicId: row.basicId })
        message.success(t('workflow.definition.msg_disabled'))
        break
      case 'archive':
        await workflowDefinitionApi.archive({ basicId: row.basicId })
        message.success(t('workflow.definition.msg_archived'))
        break
      case 'delete':
        await workflowDefinitionApi.delete(row.basicId)
        message.success(t('workflow.definition.msg_deleted'))
        break
    }
    reload()
  }
  catch {
    message.error(t('workflow.definition.err_operation'))
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
    <NDrawer v-model:show="detailVisible" :width="720">
      <NDrawerContent closable :title="t('workflow.definition.detail_title')">
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.definition.loading') }}
        </div>
        <template v-else-if="detailData">
          <NDescriptions :column="2" bordered label-placement="left" size="small">
            <NDescriptionsItem :label="t('workflow.definition.code')">
              {{ detailData.code }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.definition.name')">
              {{ detailData.name }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.definition.version')">
              v{{ detailData.version }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.definition.status')">
              <NTag :type="statusTag(detailData.status)" round size="small">
                {{ statusLabel(detailData.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.definition.category')">
              {{ detailData.category || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.definition.publish_time')">
              {{ detailData.publishTime ? formatDate(detailData.publishTime) : '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('workflow.definition.description')" :span="2">
              {{ detailData.description || '-' }}
            </NDescriptionsItem>
          </NDescriptions>
          <div class="mb-2 mt-4 flex items-center justify-between">
            <span class="text-sm font-medium">{{ t('workflow.definition.graph_label') }}</span>
            <NButton text size="tiny" @click="showDetailJson = !showDetailJson">
              {{ showDetailJson ? t('workflow.definition.hide_json') : t('workflow.definition.show_json') }}
            </NButton>
          </div>
          <div class="h-[440px] overflow-hidden rounded border border-gray-200 dark:border-gray-700">
            <WorkflowGraphView :definition-json="detailData.definitionJson" />
          </div>
          <pre v-if="showDetailJson" class="m-0 mt-2 max-h-96 overflow-auto whitespace-pre-wrap break-all rounded bg-gray-50 p-3 text-xs dark:bg-gray-800">{{ detailData.definitionJson }}</pre>
        </template>
      </NDrawerContent>
    </NDrawer>

    <!-- 创建/编辑（可视化设计器，内置 JSON 视图） -->
    <NModal
      v-model:show="editVisible"
      preset="card"
      :title="editMode === 'create' ? t('workflow.definition.create_title') : t('workflow.definition.edit_title')"
      style="width: 96vw; max-width: 1600px"
    >
      <WorkflowDesigner
        :key="designerKey"
        :initial-json="editJson"
        :saving="editLoading"
        @save="handleDesignerSave"
      />
    </NModal>

    <!-- 发起实例 -->
    <NModal v-model:show="startVisible" preset="card" :title="t('workflow.definition.start_title')" style="width: 560px">
      <NForm label-placement="left" :label-width="100">
        <NFormItem :label="t('workflow.definition.code')">
          <NInput v-model:value="startForm.definitionCode" disabled />
        </NFormItem>
        <NFormItem :label="t('workflow.definition.version')">
          <NInputNumber v-model:value="startForm.definitionVersion" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem :label="t('workflow.definition.instance_name')">
          <NInput v-model:value="startForm.name" :placeholder="t('workflow.definition.instance_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('workflow.definition.correlation_id')">
          <NInput v-model:value="startForm.correlationId" :placeholder="t('workflow.definition.correlation_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('workflow.definition.variables')">
          <NInput v-model:value="startForm.variablesJson" type="textarea" :autosize="{ minRows: 4, maxRows: 10 }" class="font-mono" />
        </NFormItem>
      </NForm>
      <NButton block type="primary" :loading="startLoading" @click="handleStart">
        {{ t('workflow.definition.btn_start') }}
      </NButton>
    </NModal>
  </SchemaPage>
</template>
