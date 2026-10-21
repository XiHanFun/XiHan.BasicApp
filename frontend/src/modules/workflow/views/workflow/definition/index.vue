<script setup lang="ts">
import type { Tone } from '@xihan-ui/core'
import type {
  WorkflowBusinessTypeDto,
  WorkflowDefinitionDetailDto,
  WorkflowDefinitionListItemDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { actionConfirmText, deleteConfirmText, SchemaPage, statusConfirmText, XEditModal, XInput, XJsonBlock, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'
import {
  workflowDefinitionApi,
  WorkflowDefinitionStatus,
  workflowInstanceApi,
} from '../../../api'
import MetadataModal from './DefinitionMetadataModal.vue'
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

/** 已登记的审批业务：业务编码即流程编码，列表据此标出流程绑定的业务 */
const businessTypes = ref<WorkflowBusinessTypeDto[]>([])

function businessName(code: string) {
  return businessTypes.value.find(business => business.code === code)?.name
}

onMounted(async () => {
  try {
    businessTypes.value = await workflowDefinitionApi.businessTypes()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definition.err_business_types'))
  }
})

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
      return h(XhTagRoot, { variant: 'subtle', tone: statusTag(r.status) }, () => h(XhTagLabel, () => statusLabel(r.status)))
    },
  },
  { key: 'code', title: t('workflow.definition.code'), dataType: 'string', sortable: true, minWidth: 160, order: 10 },
  { key: 'name', title: t('workflow.definition.name'), dataType: 'string', sortable: true, minWidth: 180, order: 11 },
  { key: 'business', title: t('workflow.definition.business'), dataType: 'string', minWidth: 120, order: 11.5, render: (row) => {
    const name = businessName((row as unknown as WorkflowDefinitionListItemDto).code)
    return name ? h(XhTagRoot, { variant: 'subtle', tone: 'brand' }, () => h(XhTagLabel, () => name)) : '-'
  } },
  { key: 'version', title: t('workflow.definition.version'), dataType: 'number', sortable: true, width: 90, order: 12, render: (row) => {
    const r = row as unknown as WorkflowDefinitionListItemDto
    return h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => `v${r.version}`))
  } },
  { key: 'category', title: t('workflow.definition.category'), dataType: 'string', searchable: true, minWidth: 120, order: 13 },
  { key: 'description', title: t('workflow.definition.description'), dataType: 'string', minWidth: 200, ellipsis: true, order: 14 },
  { key: 'publishTime', title: t('workflow.definition.publish_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 15, render: (row) => {
    const r = row as unknown as WorkflowDefinitionListItemDto
    return r.publishTime ? formatDate(r.publishTime) : '-'
  } },
  { key: 'createdTime', title: t('workflow.definition.created_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 16 },
])

/** 确认框里的流程名：名称 + 版本，发布、停用、归档都按版本走 */
function definitionName(row: unknown) {
  const definition = row as WorkflowDefinitionListItemDto
  return `${definition.name} v${definition.version}`
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'workflow_definition',
  pageName: t('workflow.definition.page_name'),
  rowKey: 'basicId',
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
    { key: 'create', title: t('workflow.definition.action_create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'workflow_definition.create' },
    { key: 'view', title: t('workflow.definition.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('workflow.definition.action_edit'), scope: 'row', icon: 'lucide:pencil', permission: 'workflow_definition.update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
    { key: 'design', title: t('workflow.definition.action_design'), scope: 'row', icon: 'lucide:pen-tool', permission: 'workflow_definition.update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
    { key: 'publish', title: t('workflow.definition.action_publish'), scope: 'row', icon: 'lucide:send', tone: 'success', confirm: true, confirmText: row => actionConfirmText(t, t('workflow.definition.action_publish'), definitionName(row)), permission: 'workflow_definition.update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
    { key: 'start', title: t('workflow.definition.action_start'), scope: 'row', icon: 'lucide:play', tone: 'brand', permission: 'workflow_definition.execute', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Published },
    { key: 'newVersion', title: t('workflow.definition.action_new_version'), scope: 'row', icon: 'lucide:git-branch-plus', permission: 'workflow_definition.create', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status !== WorkflowDefinitionStatus.Draft },
    { key: 'disable', title: t('workflow.definition.action_disable'), scope: 'row', icon: 'lucide:pause', tone: 'warning', confirm: true, confirmText: row => statusConfirmText(t, true, definitionName(row)), permission: 'workflow_definition.update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Published },
    { key: 'archive', title: t('workflow.definition.action_archive'), scope: 'row', icon: 'lucide:archive', tone: 'warning', confirm: true, confirmText: row => actionConfirmText(t, t('workflow.definition.action_archive'), definitionName(row)), permission: 'workflow_definition.update', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Disabled },
    { key: 'delete', title: t('workflow.definition.action_delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, definitionName(row)), permission: 'workflow_definition.delete', visible: row => (row as unknown as WorkflowDefinitionListItemDto).status === WorkflowDefinitionStatus.Draft },
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
    toast.danger((error as Error)?.message || t('workflow.definition.err_load_detail'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 新建/编辑元数据（编码、名称、分类、描述） ──────────────────
const metadataVisible = ref(false)
/** 编辑弹窗里的草稿详情；null 是新建 */
const metadataDetail = ref<WorkflowDefinitionDetailDto | null>(null)

function openCreate() {
  metadataDetail.value = null
  metadataVisible.value = true
}

/** 读出最新的草稿详情再开编辑弹窗：改的是整份定义 JSON 里的几项 */
async function openMetadata(row: WorkflowDefinitionListItemDto) {
  try {
    const detail = await workflowDefinitionApi.detail(row.basicId)
    if (!detail)
      throw new Error(t('workflow.definition.err_load_detail'))
    metadataDetail.value = detail
    metadataVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definition.err_load_detail'))
  }
}

/** 元数据存好后刷新列表；新建的流程还是空白的，接着打开设计器 */
function onMetadataSaved(detail: WorkflowDefinitionDetailDto, created: boolean) {
  reload()
  if (created)
    openDesigner(detail)
}

// ── 设计（可视化设计器，只改已有草稿的节点、连线与执行设置） ────────
const editVisible = ref(false)
const editLoading = ref(false)
const editTargetId = ref<string | null>(null)
const editJson = ref('')
const designerKey = ref(0)
const workflowDesignerRef = ref<InstanceType<typeof WorkflowDesigner> | null>(null)

function openDesigner(detail: WorkflowDefinitionDetailDto) {
  editTargetId.value = detail.basicId
  editJson.value = detail.definitionJson
  designerKey.value++
  editVisible.value = true
}

async function openDesign(row: WorkflowDefinitionListItemDto) {
  try {
    const detail = await workflowDefinitionApi.detail(row.basicId)
    if (!detail)
      throw new Error(t('workflow.definition.err_load_detail'))
    openDesigner(detail)
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definition.err_load_detail'))
  }
}

async function handleDesignerSave(json: string) {
  if (!editTargetId.value)
    return
  editLoading.value = true
  try {
    await workflowDefinitionApi.updateDraft({ basicId: editTargetId.value, definitionJson: json })
    toast.success(t('workflow.definition.msg_updated'))
    // 记为已保存再关：设计器删掉本机草稿，下次打开这个流程不再提示恢复
    workflowDesignerRef.value?.markSaved()
    editVisible.value = false
    reload()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definition.err_save'))
  }
  finally {
    editLoading.value = false
  }
}

// ── 发起实例 ───────────────────────────────────────────────────
/** 弹窗底部的发起钮靠这个 id 关联到表单，点它走表单提交 */
const startFormId = useId()
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
    toast.danger((error as Error)?.message || t('workflow.definition.err_start'))
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
    toast.danger((error as Error)?.message || t('workflow.definition.err_operation'))
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
        void openMetadata(row)
      break
    case 'design':
      if (row)
        void openDesign(row)
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
        <XhDrawerCloseTrigger />
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.definition.loading') }}
        </div>
        <template v-else-if="detailData">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
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
                <XhTagRoot variant="subtle" :tone="statusTag(detailData.status)" size="sm">
                  <XhTagLabel>
                    {{ statusLabel(detailData.status) }}
                  </XhTagLabel>
                </XhTagRoot>
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
            <XhButton variant="subtle" size="sm" @click="showDetailJson = !showDetailJson">
              {{ showDetailJson ? t('workflow.definition.hide_json') : t('workflow.definition.show_json') }}
            </XhButton>
          </div>
          <div class="h-[440px] overflow-hidden rounded border border-gray-200 dark:border-gray-700">
            <WorkflowGraphView :definition-json="detailData.definitionJson" />
          </div>
          <template v-if="showDetailJson">
            <XJsonBlock class="mt-2" :raw="detailData.definitionJson" :default-expanded-depth="2" max-height="24rem" />
          </template>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 新建、编辑元数据：编码、名称、分类、描述 -->
    <MetadataModal
      v-model:show="metadataVisible"
      :detail="metadataDetail"
      :business-types="businessTypes"
      @saved="onMetadataSaved"
    />

    <!-- 设计（可视化设计器）：与打印模板设计一样满屏打开；标题只给读屏作弹窗的名字，返回列表在设计器的「文件」菜单与工具栏上 -->
    <XhDialogRoot v-model:open="editVisible">
      <XhDialogContent class="workflow-designer-modal">
        <XhDialogTitle class="sr-only">
          {{ t('workflow.definition.design_title') }}
        </XhDialogTitle>
        <WorkflowDesigner
          :key="designerKey"
          ref="workflowDesignerRef"
          class="workflow-designer-modal__body"
          :draft-key="editTargetId ?? undefined"
          :initial-json="editJson"
          :saving="editLoading"
          @save="handleDesignerSave"
          @close="editVisible = false"
        />
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 发起实例 -->
    <XEditModal
      v-model:show="startVisible"
      :title="t('workflow.definition.start_title')"
      :loading="startLoading"
      :save-text="t('workflow.definition.btn_start')"
      :form-id="startFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="startFormId"
        class="xh-edit-form-grid"
        @submit="handleStart"
      >
        <XhFormFieldGroup name="definitionCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.definitionCode" disabled />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="definitionVersion">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.version') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="startForm.definitionVersion" :min="1" class="w-full" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="name">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.instance_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.name" :placeholder="t('workflow.definition.instance_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="correlationId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.correlation_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.correlationId" :placeholder="t('workflow.definition.correlation_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="variablesJson" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definition.variables') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.variablesJson" type="textarea" :autosize="{ minRows: 4, maxRows: 10 }" class="font-mono" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style>
.workflow-designer-modal {
  /* 皮肤的宽度上限读这个公开槽，缺省 32rem 会把满屏弹窗夹成 512px */
  --xh-dialog-max-w: 100vw;

  display: flex;
  width: 100vw;
  height: 100vh;
  max-height: 100vh;
  flex-direction: column;
  margin: 0;
  overflow: hidden;
  border-radius: 0;
}

/* 满屏编辑器贴满视口：定位层缺省留一圈视口间距，它不收作者属性，只能从面板反查 */
[data-scope='dialog'][data-part='positioner']:has(> .workflow-designer-modal) {
  --xh-dialog-positioner-padding: var(--xh-space-0);
}

/* 高度全给设计器 */
.workflow-designer-modal > .workflow-designer-modal__body {
  flex: 1 1 0;
  min-height: 0;
}
</style>
