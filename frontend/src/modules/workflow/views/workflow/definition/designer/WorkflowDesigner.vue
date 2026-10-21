<script setup lang="ts">
import type { WorkflowBusinessTypeDto } from '../../../../api'
import type { FlowDesignerEventMap, FlowDocument, XhFlowDesigner } from '~/designers/flow'
import { computed, getCurrentInstance, onMounted, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal, XInput } from '~/components'
import { toast } from '~/composables'
import { DesignerCloseButton, DesignerMenubarTitle, DesignerStatusText } from '~/designers'
import { createXihanWorkflow, xihanWorkflowTexts } from '~/designers/flow'
import { ensureFlowDesigner } from '~/designers/flow-designer'
import { useDesignerSettings } from '~/designers/settings'
import { downloadBlob } from '~/utils'
import { workflowDefinitionApi } from '../../../../api'
import { loadOrganizationDirectory } from '../../shared/workflow-directory'
import { openDefinition, serializeDefinition } from './definition-codec'
import { FLOW_DESIGNER_DEFAULT_SETTINGS } from './designer-settings'
import { createWorkflowExtension } from './workflow-extension'
import { createWorkflowHostExtension } from './workflow-host-extension'

/**
 * 流程定义设计器：XiHan 流程设计器（<xh-flow-designer>）装上工作流引擎预设与 BasicApp 的扩展。
 *
 * 菜单栏、画布、节点面板、属性面板、问题列表、撤销重做、缩放、对齐分布与自动布局都由设计器提供；
 * 这里只管装载与保存、引擎定义 JSON 的弹窗，以及经扩展接入的应用语义（见 workflow-extension.ts）。
 * 保存走设计器内置的 save 命令（「文件」菜单与 Ctrl / ⌘ + S）；引擎定义 JSON 与返回列表经宿主扩展挂进菜单栏
 * （见 workflow-host-extension.ts）；流程名称与编码放在菜单栏正中，保存状态放在状态栏右端，菜单栏右上角一颗关闭钮。
 */

const props = defineProps<{
  initialJson: string
  saving?: boolean
  /**
   * 本机草稿的键：定义 id，不同流程的草稿不串。
   * 草稿只在设置里开了自动保存时才写；宿主存好后调 markSaved()，设计器随之删掉草稿。
   */
  draftKey?: string
}>()

const emit = defineEmits<{
  save: [json: string]
  /** 返回列表 */
  close: []
}>()

const { t, locale } = useI18n()

ensureFlowDesigner()

const designerRef = ref<XhFlowDesigner | null>(null)
/** 设计器里此刻的流程定义：随 schema-change 更新，供标题与编辑器提示 */
const schema = shallowRef<FlowDocument>()

/** 设计器里有没有未保存的修改：随 dirty-change 更新 */
const dirty = ref(false)
const jsonVisible = ref(false)
const jsonText = ref('')

// ── 审批业务与审批人数据源 ─────────────────────────────────────
/** 已登记的审批业务：流程编码等于业务编码即为该业务的审批流程 */
const businessTypes = ref<WorkflowBusinessTypeDto[]>([])
void workflowDefinitionApi.businessTypes()
  .then((items) => { businessTypes.value = items })
  .catch(() => { businessTypes.value = [] })
// 每次打开设计器刷新一次角色/岗位/部门清单（多个规则编辑器共用）
void loadOrganizationDirectory(true).catch(() => undefined)

// ── 扩展 ───────────────────────────────────────────────────────
const appContext = getCurrentInstance()?.appContext ?? null

/**
 * 预设的文案随语言换：换语言就换一个扩展对象，设计器按卸下再装上处理，流程定义不变。
 * 受理人只用受理人规则一种写法，属性面板不显示旧的受理人三项。
 */
const preset = computed(() => createXihanWorkflow({ texts: xihanWorkflowTexts(locale.value), legacyAssignees: false }))

/** 审批业务清单到达后重建一次：规则按关联的业务检查结束节点，新的扩展对象让设计器重新校验 */
const workflowExtension = computed(() => {
  void businessTypes.value
  return createWorkflowExtension({
    appContext,
    businessTypes: () => businessTypes.value,
    document: () => schema.value,
    t: (key, params) => params ? t(key, params) : t(key),
  })
})

/** 引擎定义 JSON 与返回列表挂进菜单栏；建一次、引用不变，保存状态变了只让设计器重算可用 */
const hostExtension = createWorkflowHostExtension({
  t: key => t(key),
  saving: () => props.saving ?? false,
  openEngineJson,
  backToList: () => emit('close'),
})

watch(() => props.saving, () => designerRef.value?.requestUpdate())

const extensions = computed(() => [preset.value, workflowExtension.value, hostExtension])
/** 设计器设置（含面板布局、工具栏与新手引导走过与否）随用户存进用户设置，与流程定义页的列设置、搜索设置同一条记录 */
const designerSettings = useDesignerSettings('workflow_definition', FLOW_DESIGNER_DEFAULT_SETTINGS)
const { ready: settingsReady } = designerSettings
const groupTexts = computed(() => xihanWorkflowTexts(locale.value))

function applyExtensions(designer: XhFlowDesigner) {
  designer.extensions = extensions.value
  designer.paletteGroupLabels = groupTexts.value?.categories
  designer.propertyGroupLabels = groupTexts.value?.groups
}

watch([extensions, groupTexts], () => {
  if (designerRef.value)
    applyExtensions(designerRef.value)
})

// ── 装载 ───────────────────────────────────────────────────────
function load(json: string): boolean {
  try {
    const document = openDefinition(json)
    schema.value = document
    if (designerRef.value)
      designerRef.value.schema = document
    return true
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.designer.err_parse'))
    return false
  }
}

onMounted(() => load(props.initialJson))

/** 设计器等用户设置取回后才挂上，第一次铺好时就有用户的设置；挂上时装扩展、设置与已读入的流程定义 */
watch(designerRef, (designer) => {
  if (!designer)
    return
  applyExtensions(designer)
  designerSettings.attach(designer)
  if (schema.value)
    designer.schema = schema.value
}, { flush: 'post' })

watch(() => props.initialJson, json => load(json))

function onSchemaChange(event: Event) {
  schema.value = (event as FlowDesignerEventMap['schema-change']).detail.schema
}

function onDirtyChange(event: Event) {
  dirty.value = (event as FlowDesignerEventMap['dirty-change']).detail.dirty
}

/** 设计的都是库里已有的草稿，没改过就是已保存 */
const saved = computed(() => !dirty.value)

const identity = computed(() => {
  const data = schema.value?.data
  return {
    name: typeof data?.name === 'string' ? data.name : '',
    code: typeof data?.code === 'string' ? data.code : '',
  }
})

/** 设计器导出图片时按流程编码命名文件 */
function onExport(event: Event) {
  const { format, blob } = (event as FlowDesignerEventMap['export']).detail
  event.preventDefault()
  downloadBlob(blob, `${identity.value.code.trim() || 'workflow'}.${format}`)
}

// ── 引擎定义 JSON ──────────────────────────────────────────────
/** 打开弹窗编辑保存进库的那份引擎定义；设计器自带的「编辑 JSON」编辑的是设计器格式，两者并存 */
function openEngineJson() {
  if (!designerRef.value)
    return
  jsonText.value = serializeDefinition(designerRef.value.schema)
  jsonVisible.value = true
}

function applyJsonToCanvas() {
  if (load(jsonText.value)) {
    jsonVisible.value = false
    toast.success(t('workflow.designer.msg_json_applied'))
  }
}

// ── 保存 ───────────────────────────────────────────────────────
/** 设计器的 save 命令派发 save；正在保存时不再提交。错误级问题拦下保存，警告不拦；问题明细在设计器的问题列表里 */
/** 宿主存好后调用：记为已保存，设计器随之删掉本机草稿，下次打开不再提示恢复 */
function markSaved() {
  designerRef.value?.markSaved()
}

defineExpose({ markSaved })

function handleSave() {
  const designer = designerRef.value
  if (props.saving || !designer)
    return
  const errors = designer.validate().filter(issue => issue.severity === 'error')
  if (errors.length > 0) {
    toast.warning(t('workflow.designer.save_blocked', { count: errors.length }))
    return
  }
  emit('save', serializeDefinition(designer.schema))
}
</script>

<template>
  <!-- 根不定高，高度由宿主给；菜单栏、工具栏与画布都在设计器里 -->
  <div class="flex min-h-0 flex-col">
    <xh-flow-designer
      v-if="settingsReady"
      ref="designerRef"
      class="workflow-designer__canvas"
      :draft-key="draftKey"
      @schema-change="onSchemaChange"
      @dirty-change="onDirtyChange"
      @settings-change="designerSettings.onSettingsChange"
      @save="handleSave"
      @export="onExport"
    >
      <!-- 设计器把它们转进外框：名称与编码在菜单栏正中，保存状态在状态栏右端，关闭钮在菜单栏右上角；元素常驻。与打印编辑器同一个样子 -->
      <DesignerMenubarTitle
        :name="identity.name || t('workflow.designer.untitled')"
        :code="identity.code || undefined"
      />
      <DesignerStatusText
        :text="saved ? t('workflow.designer.saved') : t('workflow.designer.unsaved')"
        :tone="saved ? undefined : 'warning'"
      />
      <DesignerCloseButton :label="t('common.actions.close')" :disabled="saving" @close="emit('close')" />
    </xh-flow-designer>

    <XEditModal
      v-model:show="jsonVisible"
      :title="t('workflow.designer.engine_json')"
      :width="960"
      :save-text="t('workflow.designer.btn_apply_json')"
      @save="applyJsonToCanvas"
    >
      <XInput
        v-model:value="jsonText"
        type="textarea"
        class="workflow-designer__json font-mono"
        :placeholder="t('workflow.designer.json_placeholder')"
      />
    </XEditModal>
  </div>
</template>

<style scoped>
/* 宿主给设计器定高：视图列里的 flex-1；display 用设计器样式里的 flex，内部外框才能撑满 */
.workflow-designer__canvas {
  flex: 1 1 0;
  min-block-size: 0;
}

/* 引擎定义 JSON 一屏放得下常见的流程，再长就在文本域里滚动 */
.workflow-designer__json :deep(textarea) {
  block-size: var(--xh-viewport-h-lg);
  resize: vertical;
}
</style>
