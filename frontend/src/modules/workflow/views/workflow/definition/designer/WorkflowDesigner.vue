<script setup lang="ts">
import type { WorkflowBusinessTypeDto } from '../../../../api'
import type { FlowDesignerEventMap, FlowDocument, XhFlowDesigner } from '~/designers/flow'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhSeparator } from '@xihan-ui/vue'
import { computed, getCurrentInstance, onMounted, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XInput, XSegmented } from '~/components'
import { toast } from '~/composables'
import { createXihanWorkflow, xihanWorkflowTexts } from '~/designers/flow'
import { ensureFlowDesigner } from '~/designers/flow-designer'
import { downloadBlob } from '~/utils'
import { workflowDefinitionApi } from '../../../../api'
import { loadOrganizationDirectory } from '../../shared/workflow-directory'
import { openDefinition, serializeDefinition } from './definition-codec'
import { createWorkflowExtension } from './workflow-extension'

/**
 * 流程定义设计器：XiHan 流程设计器（<xh-flow-designer>）装上工作流引擎预设与 BasicApp 的扩展。
 *
 * 画布、节点面板、属性面板、问题列表、撤销重做、缩放、对齐分布与自动布局都由设计器提供；
 * 这里只管装载与保存、设计 / JSON 两个视图，以及经扩展接入的应用语义（见 workflow-extension.ts）。
 */

const props = defineProps<{
  initialJson: string
  saving?: boolean
  /** 编辑已有草稿：编码与关联业务只读（后端更新草稿沿用原编码） */
  lockCode?: boolean
}>()

const emit = defineEmits<{
  save: [json: string]
}>()

const { t, locale } = useI18n()

ensureFlowDesigner()

const designerRef = ref<XhFlowDesigner | null>(null)
/** 设计器里此刻的流程定义：随 schema-change 更新，供标题与编辑器提示 */
const schema = shallowRef<FlowDocument>()

const activeTab = ref<'design' | 'json'>('design')
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
    lockCode: props.lockCode ?? false,
    businessTypes: () => businessTypes.value,
    document: () => schema.value,
    designer: () => designerRef.value,
    t: key => t(key),
  })
})

const extensions = computed(() => [preset.value, workflowExtension.value])
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

onMounted(() => {
  if (designerRef.value)
    applyExtensions(designerRef.value)
  load(props.initialJson)
})

watch(() => props.initialJson, json => load(json))

function onSchemaChange(event: Event) {
  schema.value = (event as FlowDesignerEventMap['schema-change']).detail.schema
}

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

// ── JSON 视图 ──────────────────────────────────────────────────
watch(activeTab, (tab) => {
  if (tab === 'json' && designerRef.value)
    jsonText.value = serializeDefinition(designerRef.value.schema)
})

function applyJsonToCanvas() {
  if (load(jsonText.value)) {
    activeTab.value = 'design'
    toast.success(t('workflow.designer.msg_json_applied'))
  }
}

// ── 保存 ───────────────────────────────────────────────────────
/** 错误级问题拦下保存，警告不拦；问题明细在设计器的问题列表里 */
function handleSave() {
  if (activeTab.value === 'json' && !load(jsonText.value))
    return
  const designer = designerRef.value
  if (!designer)
    return
  const errors = designer.validate().filter(issue => issue.severity === 'error')
  if (errors.length > 0) {
    activeTab.value = 'design'
    toast.warning(t('workflow.designer.save_blocked', { count: errors.length }))
    return
  }
  emit('save', serializeDefinition(designer.schema))
}
</script>

<template>
  <!-- 根不定高，高度由宿主给 -->
  <div class="flex min-h-0 flex-col">
    <!-- 工具栏：视图切换与正在编辑的流程在左，保存在右；画布自己的命令在设计器的工具栏里 -->
    <div class="mb-2 flex flex-wrap items-center gap-x-1 gap-y-2">
      <div class="flex min-w-0 items-center gap-1">
        <XSegmented v-model:value="activeTab" class="shrink-0" :options="[{ value: 'design', label: t('workflow.designer.tab_design') }, { value: 'json', label: t('workflow.designer.tab_json') }]" size="sm" />
        <XhSeparator orientation="vertical" class="workflow-designer__tool-divider !mx-1" />
        <!-- 正在编辑的是哪个流程：名称为主，编码等宽小字为辅；编码不是状态，不套标签 -->
        <div class="workflow-designer__identity">
          <span class="workflow-designer__identity-name">{{ identity.name || t('workflow.designer.untitled') }}</span>
          <span v-if="identity.code" class="workflow-designer__identity-code">{{ identity.code }}</span>
        </div>
      </div>

      <div class="ml-auto flex items-center gap-1">
        <XhButton variant="subtle" size="sm" tone="brand" :loading="saving" @click="handleSave">
          <XhButtonIndicator />
          <Icon icon="lucide:save" />
          <XhButtonLabel>{{ t('workflow.designer.btn_save') }}</XhButtonLabel>
        </XhButton>
      </div>
    </div>

    <!-- 工具栏以下的高度全给当前视图；设计视图用 v-show 保留设计器：切到 JSON 再切回不卸载、不丢撤销历史 -->
    <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
      <xh-flow-designer
        v-show="activeTab === 'design'"
        ref="designerRef"
        class="workflow-designer__canvas"
        @schema-change="onSchemaChange"
        @save="handleSave"
        @export="onExport"
      />

      <!-- JSON 视图（文本域铺满视图、不走 autosize：autosize 的外壳另有滚动上限，与文本域自己的滚动叠成两条滚动条） -->
      <div v-show="activeTab === 'json'" class="flex min-h-0 flex-1 flex-col gap-2">
        <XInput
          v-model:value="jsonText"
          type="textarea"
          class="workflow-designer__json min-h-0 flex-1 font-mono"
          :placeholder="t('workflow.designer.json_placeholder')"
        />
        <XhButton variant="subtle" size="sm" @click="applyJsonToCanvas">
          {{ t('workflow.designer.btn_apply_json') }}
        </XhButton>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 宿主给设计器定高：视图列里的 flex-1；display 用设计器样式里的 flex，内部外框才能撑满 */
.workflow-designer__canvas {
  flex: 1 1 0;
  min-block-size: 0;
}

/* 根已是视图里的 flex-1；视觉盒随根拉满，文本域在盒内拉伸到盒高，滚动只留文本域自己这一层 */
.workflow-designer__json :deep(.x-input__control) {
  flex: 1 1 0;
  min-block-size: 0;
  align-items: stretch;
}

.workflow-designer__json :deep(textarea) {
  resize: none;
}

/* 工具栏的竖分隔线与组件库 Toolbar 同一把尺：条目高的一半、垂直居中 */
.workflow-designer__tool-divider {
  align-self: center;
  block-size: calc(var(--xh-control-h-sm) / 2);
  min-block-size: 0;
}

.workflow-designer__identity {
  display: flex;
  align-items: baseline;
  gap: var(--xh-space-2);
  min-inline-size: 0;
}

.workflow-designer__identity-name {
  overflow: hidden;
  font-size: var(--xh-text-label-size);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.workflow-designer__identity-code {
  overflow: hidden;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
