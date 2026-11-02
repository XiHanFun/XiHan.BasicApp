<script setup lang="ts">
import type { Tone } from '@xihan-ui/core'
import type { WorkflowBusinessType, WorkflowDefinitionDetail, WorkflowDefinitionRow } from './definition-metadata'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, SchemaResource } from '~/components'
import type { WorkflowDefinitionStatus } from '~/contracts'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, deleteConfirmText, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XJsonBlock, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { toPageRequest, toSchemaPageResult, workflowDefinitions, workflowInstances } from '~/contracts'
import { formatDate } from '~/utils'
import MetadataModal from './DefinitionMetadataModal.vue'
import WorkflowDesigner from './designer/WorkflowDesigner.vue'
import WorkflowGraphView from './designer/WorkflowGraphView.vue'

defineOptions({ name: 'WorkflowDefinitionsView' })

const { t } = useI18n()

const schemaPageRef = ref<SchemaPageInstance | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
const statusOptions = computed<{ label: string, value: WorkflowDefinitionStatus }[]>(() => [
  { label: t('workflow.definitions.status_draft'), value: 'Draft' },
  { label: t('workflow.definitions.status_published'), value: 'Published' },
  { label: t('workflow.definitions.status_disabled'), value: 'Disabled' },
  { label: t('workflow.definitions.status_archived'), value: 'Archived' },
])

function statusTag(status: WorkflowDefinitionStatus): Tone {
  switch (status) {
    case 'Published':
      return 'success'
    case 'Draft':
      return 'info'
    case 'Disabled':
      return 'warning'
    default:
      return 'neutral'
  }
}

function statusLabel(status: WorkflowDefinitionStatus) {
  return statusOptions.value.find(option => option.value === status)?.label ?? status
}

/** 已登记的审批业务：业务编码即流程编码，新建弹窗据此绑定业务（列表行自带关联业务的文案键） */
const businessTypes = ref<WorkflowBusinessType[]>([])

onMounted(async () => {
  try {
    businessTypes.value = (await workflowDefinitions.businessTypes({})).items
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_business_types'))
  }
})

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用。
// 「流程编码」是定义名（name），「流程名称」是显示名（displayName）；排序由后端固定（名字升序、版本降序），列不可排序
const asRow = (row: object) => row as WorkflowDefinitionRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('workflow.definitions.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('workflow.definitions.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'status',
    title: t('workflow.definitions.status'),
    dataType: 'enum',
    searchable: true,
    options: statusOptions.value,
    searchPlaceholder: t('workflow.definitions.status_placeholder'),
    width: 110,
    order: 1,
    render: row => renderTag(statusLabel(asRow(row).status), statusTag(asRow(row).status)),
  },
  { key: 'name', title: t('workflow.definitions.code'), dataType: 'string', minWidth: 160, order: 10 },
  { key: 'displayName', title: t('workflow.definitions.name'), dataType: 'string', minWidth: 180, order: 11 },
  { key: 'business', title: t('workflow.definitions.business'), dataType: 'string', minWidth: 120, order: 11.5, render: (row) => {
    const key = asRow(row).businessNameKey
    return key ? renderTag(t(key), 'brand') : '-'
  } },
  { key: 'version', title: t('workflow.definitions.version'), dataType: 'number', width: 90, order: 12, render: row => renderTag(`v${asRow(row).version}`, 'neutral') },
  { key: 'category', title: t('workflow.definitions.category'), dataType: 'string', searchable: true, minWidth: 120, order: 13 },
  { key: 'description', title: t('workflow.definitions.description'), dataType: 'string', minWidth: 200, ellipsis: true, order: 14 },
  { key: 'publishedAt', title: t('workflow.definitions.publish_time'), dataType: 'datetime', minWidth: 170, order: 15, render: (row) => {
    const publishedAt = asRow(row).publishedAt
    return publishedAt ? formatDate(publishedAt) : '-'
  } },
  { key: 'createdAt', title: t('workflow.definitions.created_time'), dataType: 'datetime', minWidth: 170, order: 16 },
])

/** 分页：关键字匹配名字、显示名与说明；状态与分类精确匹配（后端白名单） */
const resource: SchemaResource<WorkflowDefinitionRow> = {
  page: async params => toSchemaPageResult(
    await workflowDefinitions.list(toPageRequest(params, { filters: { status: 'eq', category: 'eq' } })),
    params,
  ),
}

/** 确认框里的流程名：名称 + 版本，发布、停用、归档都按版本走 */
function definitionName(row: object) {
  const definition = asRow(row)
  return `${definition.displayName || definition.name} v${definition.version}`
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'workflow.definitions',
  pageName: t('workflow.definitions.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('workflow.definitions.action_create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'workflow.workflow-definitions.create' },
    { key: 'view', title: t('workflow.definitions.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('workflow.definitions.action_edit'), scope: 'row', icon: 'lucide:pencil', permission: 'workflow.workflow-definitions.update', visible: row => asRow(row).status === 'Draft' },
    { key: 'design', title: t('workflow.definitions.action_design'), scope: 'row', icon: 'lucide:pen-tool', permission: 'workflow.workflow-definitions.update', visible: row => asRow(row).status === 'Draft' },
    { key: 'publish', title: t('workflow.definitions.action_publish'), scope: 'row', icon: 'lucide:send', tone: 'success', confirm: true, confirmText: row => actionConfirmText(t, t('workflow.definitions.action_publish'), definitionName(row)), permission: 'workflow.workflow-definitions.update', visible: row => asRow(row).status === 'Draft' },
    { key: 'start', title: t('workflow.definitions.action_start'), scope: 'row', icon: 'lucide:play', tone: 'brand', permission: 'workflow.workflow-instances.execute', visible: row => asRow(row).status === 'Published' },
    { key: 'newVersion', title: t('workflow.definitions.action_new_version'), scope: 'row', icon: 'lucide:git-branch-plus', permission: 'workflow.workflow-definitions.create', visible: row => asRow(row).status !== 'Draft' },
    { key: 'disable', title: t('workflow.definitions.action_disable'), scope: 'row', icon: 'lucide:pause', tone: 'warning', confirm: true, confirmText: row => statusConfirmText(t, true, definitionName(row)), permission: 'workflow.workflow-definitions.update', visible: row => asRow(row).status === 'Published' },
    { key: 'archive', title: t('workflow.definitions.action_archive'), scope: 'row', icon: 'lucide:archive', tone: 'warning', confirm: true, confirmText: row => actionConfirmText(t, t('workflow.definitions.action_archive'), definitionName(row)), permission: 'workflow.workflow-definitions.update', visible: row => asRow(row).status === 'Disabled' },
    { key: 'delete', title: t('workflow.definitions.action_delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, definitionName(row)), permission: 'workflow.workflow-definitions.delete', visible: row => asRow(row).status === 'Draft' },
  ],
}))

// ── 详情抽屉 ───────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<WorkflowDefinitionDetail | null>(null)
const showDetailJson = ref(false)

async function handleDetail(row: WorkflowDefinitionRow) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await workflowDefinitions.get({ id: row.id })
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_load_detail'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 新建/编辑元数据（编码、名称、分类、描述） ──────────────────
const metadataVisible = ref(false)
/** 编辑弹窗里的草稿详情；null 是新建 */
const metadataDetail = ref<WorkflowDefinitionDetail | null>(null)

function openCreate() {
  metadataDetail.value = null
  metadataVisible.value = true
}

/** 读出最新的草稿详情再开编辑弹窗：改的是整份定义文档里的几项 */
async function openMetadata(row: WorkflowDefinitionRow) {
  try {
    metadataDetail.value = await workflowDefinitions.get({ id: row.id })
    metadataVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_load_detail'))
  }
}

/** 元数据存好后刷新列表；新建的流程还是空白的，接着打开设计器 */
function onMetadataSaved(detail: WorkflowDefinitionDetail, created: boolean) {
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

function openDesigner(detail: WorkflowDefinitionDetail) {
  editTargetId.value = detail.id
  editJson.value = detail.document
  designerKey.value++
  editVisible.value = true
}

async function openDesign(row: WorkflowDefinitionRow) {
  try {
    openDesigner(await workflowDefinitions.get({ id: row.id }))
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_load_detail'))
  }
}

async function handleDesignerSave(json: string) {
  if (!editTargetId.value)
    return
  editLoading.value = true
  try {
    await workflowDefinitions.update({ id: editTargetId.value, document: json })
    toast.success(t('workflow.definitions.msg_updated'))
    // 记为已保存再关：设计器删掉本机草稿，下次打开这个流程不再提示恢复
    workflowDesignerRef.value?.markSaved()
    editVisible.value = false
    reload()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_save'))
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
const startForm = ref({ name: '', version: null as number | null, displayName: '', correlationId: '', variables: '{}' })

function openStart(row: WorkflowDefinitionRow) {
  startForm.value = { name: row.name, version: row.version, displayName: '', correlationId: '', variables: '{}' }
  startVisible.value = true
}

async function handleStart() {
  startLoading.value = true
  try {
    await workflowInstances.start({
      name: startForm.value.name,
      version: startForm.value.version ?? null,
      displayName: startForm.value.displayName.trim() || null,
      correlationId: startForm.value.correlationId.trim() || null,
      variables: startForm.value.variables.trim() || null,
    })
    toast.success(t('workflow.definitions.msg_started'))
    startVisible.value = false
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_start'))
  }
  finally {
    startLoading.value = false
  }
}

// ── 生命周期操作 ───────────────────────────────────────────────
/** 批量操作的结果里只有失败项的错误码：被后端逐项拒绝时带着错误码报错 */
function ensureSucceeded(result: { failed: { code: string }[] }) {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(failure => failure.code).join('; '))
}

async function runLifecycle(action: 'publish' | 'newVersion' | 'disable' | 'archive' | 'delete', row: WorkflowDefinitionRow) {
  try {
    switch (action) {
      case 'publish':
        await workflowDefinitions.publish({ id: row.id })
        toast.success(t('workflow.definitions.msg_published'))
        break
      case 'newVersion':
        await workflowDefinitions.newVersion({ name: row.name })
        toast.success(t('workflow.definitions.msg_new_version'))
        break
      case 'disable':
        ensureSucceeded(await workflowDefinitions.disable({ ids: [row.id] }))
        toast.success(t('workflow.definitions.msg_disabled'))
        break
      case 'archive':
        ensureSucceeded(await workflowDefinitions.archive({ ids: [row.id] }))
        toast.success(t('workflow.definitions.msg_archived'))
        break
      case 'delete':
        ensureSucceeded(await workflowDefinitions.delete({ ids: [row.id] }))
        toast.success(t('workflow.definitions.msg_deleted'))
        break
    }
    reload()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_operation'))
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
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
        <XhDrawerTitle>{{ t('workflow.definitions.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('workflow.definitions.loading') }}
        </div>
        <template v-else-if="detailData">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definitions.code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.name }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definitions.name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.displayName || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definitions.version') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                v{{ detailData.version }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definitions.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhTagRoot variant="subtle" :tone="statusTag(detailData.status)" size="sm">
                  <XhTagLabel>
                    {{ statusLabel(detailData.status) }}
                  </XhTagLabel>
                </XhTagRoot>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definitions.category') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.category || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('workflow.definitions.publish_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.publishedAt ? formatDate(detailData.publishedAt) : '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('workflow.definitions.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ detailData.description || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <div class="mb-2 mt-4 flex items-center justify-between">
            <span class="text-sm font-medium">{{ t('workflow.definitions.graph_label') }}</span>
            <XhButton variant="subtle" size="sm" @click="showDetailJson = !showDetailJson">
              {{ showDetailJson ? t('workflow.definitions.hide_json') : t('workflow.definitions.show_json') }}
            </XhButton>
          </div>
          <div class="h-[440px] overflow-hidden rounded border border-gray-200 dark:border-gray-700">
            <WorkflowGraphView :definition-json="detailData.document" />
          </div>
          <template v-if="showDetailJson">
            <XJsonBlock class="mt-2" :raw="detailData.document" :default-expanded-depth="2" max-height="24rem" />
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
          {{ t('workflow.definitions.design_title') }}
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
      :title="t('workflow.definitions.start_title')"
      :loading="startLoading"
      :save-text="t('workflow.definitions.btn_start')"
      :form-id="startFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="startFormId"
        class="xh-edit-form-grid"
        @submit="handleStart"
      >
        <XhFormFieldGroup name="name">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definitions.code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.name" disabled />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="version">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definitions.version') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="startForm.version" :min="1" class="w-full" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="displayName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definitions.instance_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.displayName" :max-length="256" :placeholder="t('workflow.definitions.instance_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="correlationId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definitions.correlation_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.correlationId" :max-length="256" :placeholder="t('workflow.definitions.correlation_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="variables" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.definitions.variables') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="startForm.variables" type="textarea" :autosize="{ minRows: 4, maxRows: 10 }" class="font-mono" />
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
