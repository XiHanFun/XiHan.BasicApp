<!--
  hiprint 打印设计器工作区。
  职责：组合统一控制面板、官网式素材面板、带网格的打印画布、JSON 模板编辑与元素属性面板。
-->
<script setup lang="ts">
import type {
  PrintDesignerMode,
  PrintPaperPreset,
  PrintPaperType,
} from './models'
import type {
  PrintDesignerHandle,
  PrintElementAlignAction,
  PrintElementSpacingDirection,
} from '~/printing'
import { NAlert, NEmpty, NIcon, NSpin, useMessage } from 'naive-ui'
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { createPrintDesigner, enablePrintFieldDragging } from '~/printing'
import { PRINT_PAPER_PRESETS } from './models'
import JsonTemplateModal from './PrintDesignerJsonModal.vue'
import MaterialPalette from './PrintDesignerMaterialPalette.vue'
import DesignerToolbar from './PrintDesignerToolbar.vue'

defineOptions({ name: 'HiprintDesignerCanvas' })

const props = defineProps<{
  dataSourceCode: null | string
  template: Record<string, unknown>
}>()

const emit = defineEmits<{
  changed: [template: unknown]
  ready: []
}>()

const message = useMessage()
const { t } = useI18n()
const instanceId = `hiprint-${crypto.randomUUID()}`
const canvasId = `${instanceId}-canvas`
const settingId = `${instanceId}-setting`
const paginationClass = `${instanceId}-pagination`
const DEFAULT_PAPER_WIDTH_MM = 210
const DEFAULT_PAPER_HEIGHT_MM = 297
const DEFAULT_ZOOM_PERCENT = 100
const materialsRef = ref<HTMLElement | null>(null)
const paginationRef = ref<HTMLElement | null>(null)
const loading = ref(true)
const loadError = ref('')
const designMode = ref<PrintDesignerMode>(getPanelCount(props.template) > 1 ? 'multi' : 'single')
const activePanelIndex = ref(0)
const activePaperType = ref<PrintPaperType>('A4')
const activePaperWidth = ref(DEFAULT_PAPER_WIDTH_MM)
const activePaperHeight = ref(DEFAULT_PAPER_HEIGHT_MM)
const zoomPercent = ref(DEFAULT_ZOOM_PERCENT)
const jsonEditorVisible = ref(false)
const jsonEditorText = ref('')
const jsonEditorError = ref('')
const jsonApplying = ref(false)
let designer: PrintDesignerHandle | null = null
let paginationSyncTimer: number | null = null

onMounted(() => {
  void initialize()
})

onBeforeUnmount(() => {
  // 上游未提供 destroy；清空模板与宿主 DOM 可释放节点，打印事件由公共直打服务按任务精确清理。
  if (paginationSyncTimer !== null)
    window.clearTimeout(paginationSyncTimer)
  designer?.clear()
  jsonEditorVisible.value = false
  document.getElementById(canvasId)?.replaceChildren()
  document.getElementById(settingId)?.replaceChildren()
  designer = null
})

/**
 * 初始化 hiprint 设计器并按需激活当前数据源建议字段拖拽。
 * @returns 完成信号。
 * @throws 动态依赖、数据源或 DOM 容器异常会转为画布内错误提示。
 */
async function initialize(): Promise<void> {
  loading.value = true
  loadError.value = ''
  try {
    await nextTick()
    designer = await createPrintDesigner({
      canvas: `#${canvasId}`,
      settingContainer: `#${settingId}`,
      paginationContainer: `.${paginationClass}`,
      dataSourceCode: props.dataSourceCode,
      template: props.template,
      onDataChanged: (json) => {
        emit('changed', json)
        // hiprint 在面板、纸张或缩放变化后才生成最新 JSON，微任务阶段再同步工具栏可避免读取旧值。
        queueMicrotask(syncToolbarState)
      },
    })
    const materials = materialsRef.value?.querySelectorAll('.ep-draggable-item') ?? []
    await enablePrintFieldDragging(materials)
    syncToolbarState()
    emit('ready')
  }
  catch (error) {
    loadError.value = (error as Error).message || t('setting.print_template.designer_init_failed')
  }
  finally {
    loading.value = false
  }
}

/** 判断未知值是否为可读取字段的普通对象。 */
function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
}

/** 从模板 JSON 读取面板数量，用于初始化单/多面板标签。 */
function getPanelCount(template: unknown): number {
  if (!isRecord(template) || !Array.isArray(template.panels))
    return 0
  return template.panels.length
}

/** 判断纸张类型是否属于工具栏支持的标准快捷项。 */
function isPaperPreset(value: unknown): value is PrintPaperPreset {
  return typeof value === 'string'
    && (PRINT_PAPER_PRESETS as readonly string[]).includes(value.toUpperCase())
}

/** 将未知值转换为有效正数；无效时使用给定回退值。 */
function toPositiveNumber(value: unknown, fallback: number): number {
  return typeof value === 'number' && Number.isFinite(value) && value > 0 ? value : fallback
}

/** 同步当前活动面板的纸张与缩放状态到快捷工具栏。 */
function syncToolbarState(): void {
  if (!designer)
    return

  const templateJson = designer.getJson()
  if (!isRecord(templateJson) || !Array.isArray(templateJson.panels) || templateJson.panels.length === 0)
    return

  activePanelIndex.value = Math.min(activePanelIndex.value, templateJson.panels.length - 1)
  const activePanel = templateJson.panels[activePanelIndex.value]
  if (!isRecord(activePanel))
    return

  const rawPaperType = typeof activePanel.paperType === 'string'
    ? activePanel.paperType.toUpperCase()
    : ''
  activePaperType.value = isPaperPreset(rawPaperType) ? rawPaperType : 'CUSTOM'
  activePaperWidth.value = toPositiveNumber(activePanel.width, DEFAULT_PAPER_WIDTH_MM)
  activePaperHeight.value = toPositiveNumber(activePanel.height, DEFAULT_PAPER_HEIGHT_MM)
  zoomPercent.value = Math.round(toPositiveNumber(activePanel.scale, 1) * 100)
}

/** 切换单/多面板设计模式；已有多面板时禁止隐藏面板管理，避免误导用户。 */
function changeDesignMode(mode: PrintDesignerMode): void {
  if (mode === 'single' && (designer?.getPanelCount() ?? getPanelCount(props.template)) > 1) {
    message.warning(t('setting.print_template.multi_panel_cannot_switch_single'))
    return
  }
  designMode.value = mode
}

/** 将活动面板切换到标准纸张，并交由 hiprint 更新实际画布尺寸。 */
function changePaperPreset(paperType: PrintPaperPreset): void {
  if (!designer)
    return
  activePaperType.value = paperType
  designer.setPaperPreset(paperType)
  queueMicrotask(syncToolbarState)
}

/** 将活动面板切换到用户输入的毫米尺寸。 */
function changeCustomPaper(width: number, height: number): void {
  if (!designer)
    return
  activePaperType.value = 'CUSTOM'
  activePaperWidth.value = width
  activePaperHeight.value = height
  designer.setCustomPaper(width, height)
  queueMicrotask(syncToolbarState)
}

/** 调整活动面板画布缩放比例。 */
function changeZoom(scale: number): void {
  if (!designer)
    return
  zoomPercent.value = Math.round(scale * 100)
  designer.setZoom(scale)
  queueMicrotask(syncToolbarState)
}

/** 旋转当前活动面板，并在失败时保留画布与显示友好提示。 */
function rotatePaper(): void {
  if (!designer)
    return
  try {
    designer.rotatePaper()
    queueMicrotask(syncToolbarState)
  }
  catch (error) {
    message.error((error as Error).message || t('setting.print_template.rotate_failed'))
  }
}

/** 对当前多选元素执行边缘、中心或等距分布，并把选择数量不足转换为本地化提示。 */
function alignElements(action: PrintElementAlignAction): void {
  if (!designer)
    return

  const isDistribution = action === 'distributeHor' || action === 'distributeVer'
  const selectedElementCount = designer.getSelectedElementCount()
  if (selectedElementCount < (isDistribution ? 3 : 2)) {
    message.warning(t(isDistribution
      ? 'setting.print_template.distribute_requires_three'
      : 'setting.print_template.align_requires_two'))
    return
  }

  try {
    designer.alignElements(action)
  }
  catch (error) {
    message.error((error as Error).message || t('setting.print_template.alignment_failed'))
  }
}

/** 使用指定固定值调整当前多选元素间距；操作同步且只作用于当前实例，不需要异步 loading 锁。 */
function setElementSpacing(direction: PrintElementSpacingDirection, spacing: number): void {
  if (!designer)
    return
  if (designer.getSelectedElementCount() < 2) {
    message.warning(t('setting.print_template.spacing_requires_two'))
    return
  }

  try {
    designer.setElementSpacing(spacing, direction)
  }
  catch (error) {
    message.error((error as Error).message || t('setting.print_template.spacing_failed'))
  }
}

/** 在画布焦点范围内支持 Ctrl/Cmd+A 全选，避免拦截属性输入框和可编辑文本的原生全选。 */
function handleCanvasShortcut(event: KeyboardEvent): void {
  const target = event.target as Element | null
  const isEditableTarget = Boolean(target?.closest('input, textarea, [contenteditable="true"]'))
  if (!(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== 'a' || isEditableTarget)
    return

  event.preventDefault()
  designer?.selectAllElements()
}

/** 打开 JSON 模板编辑器，并以当前画布快照初始化 TextArea。 */
function openJsonEditor(): void {
  if (!designer)
    return
  jsonEditorVisible.value = true
  exportCurrentTemplateToTextArea(false)
}

/**
 * 将当前画布 JSON 格式化写入内存 TextArea。
 * @param notifySuccess 是否显示导出成功提示；打开弹窗时静默初始化。
 * @returns 无返回值。
 * @throws 本方法捕获序列化异常并转为界面提示，不向事件循环抛出。
 */
function exportCurrentTemplateToTextArea(notifySuccess = true): void {
  try {
    jsonEditorText.value = JSON.stringify(getJson(), null, 2)
    jsonEditorError.value = ''
    if (notifySuccess)
      message.success(t('setting.print_template.json_exported'))
  }
  catch (error) {
    jsonEditorError.value = (error as Error).message || t('setting.print_template.json_export_failed')
  }
}

/** 用户编辑 TextArea 后清理上一轮校验错误，避免错误状态与当前内容不一致。 */
function updateJsonEditorText(value: string): void {
  jsonEditorText.value = value
  if (jsonEditorError.value)
    jsonEditorError.value = ''
}

/** 解析并校验可交给 hiprint 更新的根模板对象。 */
function parseJsonTemplate(value: string): Record<string, unknown> {
  let parsed: unknown
  try {
    parsed = JSON.parse(value)
  }
  catch {
    throw new Error(t('setting.print_template.json_syntax_invalid'))
  }

  if (!isRecord(parsed))
    throw new Error(t('setting.print_template.invalid_json'))
  if (!Array.isArray(parsed.panels) || parsed.panels.length === 0 || !parsed.panels.every(isRecord))
    throw new Error(t('setting.print_template.json_panels_required'))
  return parsed
}

/**
 * 使用 TextArea 中的完整 JSON 更新画布。
 * @returns 更新与界面状态同步完成信号。
 * @throws 本方法捕获 JSON 和 hiprint 异常并显示在弹窗内，不向事件循环抛出。
 */
async function applyJsonTemplate(): Promise<void> {
  if (!designer || jsonApplying.value)
    return

  jsonApplying.value = true
  jsonEditorError.value = ''
  try {
    const nextTemplate = parseJsonTemplate(jsonEditorText.value)
    // 先让 loading 状态提交给 DOM，避免较大模板同步更新期间出现可重复点击窗口。
    await nextTick()
    document.getElementById(settingId)?.replaceChildren()
    designer.updateTemplate(nextTemplate, 0)
    activePanelIndex.value = 0
    designMode.value = designer.getPanelCount() > 1 ? 'multi' : 'single'
    syncToolbarState()
    jsonEditorText.value = JSON.stringify(designer.getJson(), null, 2)
    message.success(t('setting.print_template.json_applied'))
  }
  catch (error) {
    jsonEditorError.value = (error as Error).message || t('setting.print_template.json_apply_failed')
  }
  finally {
    jsonApplying.value = false
  }
}

/** 记录用户点击的面板索引，并在 hiprint 完成增删或切换后刷新工具栏状态。 */
function handlePaginationClick(event: MouseEvent): void {
  const eventTarget = event.target as Element | null
  const clickedItem = eventTarget?.closest('li')
  const isDeleteAction = Boolean(eventTarget?.closest('a'))
  const panelItems = paginationRef.value
    ? Array.from(paginationRef.value.querySelectorAll('li'))
    : []
  const clickedIndex = clickedItem ? panelItems.indexOf(clickedItem) : -1
  const currentPanelCount = designer?.getPanelCount() ?? 0

  // 至少保留一个面板，避免 hiprint 删除最后一个面板后进入无法继续设计和保存的空状态。
  if (isDeleteAction && currentPanelCount <= 1) {
    event.preventDefault()
    event.stopPropagation()
    message.warning(t('setting.print_template.keep_one_panel'))
    return
  }

  if (clickedIndex >= 0) {
    activePanelIndex.value = isDeleteAction
      ? Math.max(0, clickedIndex - 1)
      : clickedIndex
  }
  if (paginationSyncTimer !== null)
    window.clearTimeout(paginationSyncTimer)
  paginationSyncTimer = window.setTimeout(() => {
    paginationSyncTimer = null
    // hiprint 0.0.60 删除当前面板后不会自动重绑 editingPanel，显式选择可确保后续纸张与缩放作用于可见面板。
    if (isDeleteAction && designer) {
      const nextIndex = Math.min(activePanelIndex.value, designer.getPanelCount() - 1)
      designer.selectPanel(Math.max(0, nextIndex))
    }
    syncToolbarState()
  }, 0)
}

/**
 * 读取当前设计 JSON。
 * @returns hiprint 根模板对象。
 * @throws 设计器尚未就绪。
 */
function getJson(): unknown {
  if (!designer)
    throw new Error(t('setting.print_template.designer_not_ready'))
  return designer.getJson()
}

/**
 * 使用当前未保存设计和内存样例数据打开浏览器预览。
 * @param data 当前数据源产生的样例数据。
 * @returns 预览窗口调用完成信号。
 * @throws 设计器尚未就绪或浏览器预览失败。
 */
async function preview(data: unknown): Promise<void> {
  if (!designer)
    throw new Error(t('setting.print_template.designer_not_ready'))
  await new Promise<void>((resolve, reject) => {
    try {
      designer!.template.print(data, {}, { callback: resolve })
      queueMicrotask(resolve)
    }
    catch (error) {
      reject(error)
    }
  })
}

/** 撤销最近一次设计变更。 */
function undo(): void {
  designer?.undo()
}

/** 重做最近一次撤销。 */
function redo(): void {
  designer?.redo()
}

/** 清空当前模板面板内容。 */
function clear(): void {
  designer?.clear()
  // hiprint 0.0.60 清空画布后不会同步移除旧属性表单；主动清理可避免用户误以为已删除元素仍可编辑。
  document.getElementById(settingId)?.replaceChildren()
  message.success(t('setting.print_template.canvas_cleared'))
}

defineExpose({ clear, getJson, preview, redo, undo })
</script>

<template>
  <div class="designer-shell">
    <DesignerToolbar
      :design-mode="designMode"
      :disabled="loading || Boolean(loadError)"
      :paper-height="activePaperHeight"
      :paper-type="activePaperType"
      :paper-width="activePaperWidth"
      :zoom-percent="zoomPercent"
      @mode-change="changeDesignMode"
      @paper-preset-change="changePaperPreset"
      @custom-paper-change="changeCustomPaper"
      @element-align="alignElements"
      @element-spacing="setElementSpacing"
      @json-editor-open="openJsonEditor"
      @rotate="rotatePaper"
      @zoom-change="changeZoom"
    >
      <template #template-actions>
        <slot name="template-actions" />
      </template>
      <template #printer-actions>
        <slot name="printer-actions" />
      </template>
    </DesignerToolbar>

    <JsonTemplateModal
      :show="jsonEditorVisible"
      :value="jsonEditorText"
      :error="jsonEditorError"
      :applying="jsonApplying"
      :disabled="loading || Boolean(loadError)"
      @update:show="jsonEditorVisible = $event"
      @update:value="updateJsonEditorText"
      @export="exportCurrentTemplateToTextArea"
      @apply="applyJsonTemplate"
    />

    <div class="designer-workspace">
      <aside ref="materialsRef" class="materials-panel">
        <MaterialPalette :data-source-code="dataSourceCode" @undo="undo" @redo="redo" @clear="clear" />
      </aside>

      <section class="canvas-column">
        <main
          class="canvas-stage"
          data-testid="print-designer-canvas-stage"
          tabindex="0"
          @keydown="handleCanvasShortcut"
        >
          <NSpin v-if="loading" class="absolute inset-0 z-10 flex items-center justify-center" :description="t('setting.print_template.designer_loading')" />
          <NAlert v-if="loadError" class="canvas-error" type="error" :show-icon="true">
            {{ loadError }}
          </NAlert>
          <div :id="canvasId" class="hiprint-canvas-host" />
        </main>
        <!-- 多面板导航放在画布底部，避免占用刻度尺上方的连续编辑空间。 -->
        <div
          v-show="designMode === 'multi'"
          ref="paginationRef"
          class="panel-pagination"
          :class="paginationClass"
          :aria-label="t('setting.print_template.multi_panel_manager')"
          @click.capture="handlePaginationClick"
        />
      </section>

      <aside class="property-panel">
        <div class="property-heading">
          <NIcon :size="18">
            <Icon icon="tabler:adjustments-horizontal" />
          </NIcon>
          <span>{{ t('setting.print_template.element_properties') }}</span>
        </div>
        <div :id="settingId" class="property-setting-host" />
        <div class="property-empty">
          <NEmpty size="small" :description="t('setting.print_template.properties_empty')" />
        </div>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.designer-shell {
  display: flex;
  width: 100%;
  height: 100%;
  min-height: 0;
  flex-direction: column;
  overflow: hidden;
  background: #fff;
}

.designer-workspace {
  display: grid;
  width: 100%;
  min-height: 0;
  flex: 1 1 0;
  grid-template-columns: 380px minmax(620px, 1fr) 288px;
  overflow: auto;
  border-top: 1px solid rgba(148, 163, 184, 0.24);
  background: #fff;
}

.materials-panel,
.property-panel {
  min-height: 0;
  overflow: auto;
  background: #fff;
}

.materials-panel {
  border-right: 1px solid rgba(148, 163, 184, 0.28);
}

.canvas-column {
  display: flex;
  min-width: 620px;
  min-height: 0;
  flex-direction: column;
  overflow: hidden;
  background: #f1f3f5;
}

.canvas-stage {
  position: relative;
  flex: 1;
  min-width: 620px;
  min-height: 0;
  overflow: auto;
  padding: 14px 16px 28px;
  background: #f1f3f5;
}

.canvas-stage:focus-visible {
  outline: 2px solid rgba(32, 128, 240, 0.6);
  outline-offset: -2px;
}

.canvas-error {
  position: sticky;
  top: 0;
  z-index: 20;
  margin-bottom: 12px;
}

.hiprint-canvas-host {
  min-width: max-content;
  min-height: 100%;
}

.panel-pagination {
  flex: none;
  min-height: 58px;
  padding: 10px 16px;
  overflow-x: auto;
  border-top: 1px solid rgba(148, 163, 184, 0.28);
  background: rgba(255, 255, 255, 0.98);
}

.property-panel {
  border-left: 1px solid rgba(148, 163, 184, 0.28);
}

.property-heading {
  position: sticky;
  top: 0;
  z-index: 2;
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 50px;
  padding: 0 18px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.22);
  background: rgba(255, 255, 255, 0.96);
  color: #334155;
  font-size: 14px;
  font-weight: 650;
}

.property-setting-host {
  padding: 14px 16px 24px;
}

.property-empty {
  display: flex;
  min-height: 240px;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.property-setting-host:not(:empty) + .property-empty {
  display: none;
}

/* 网格仅挂在设计态纸张上，既会随纸张滚动和缩放，也不会进入预览或实际打印。 */
:deep(.hiprint-printPaper.design) {
  background-color: #fff !important;
  background-image:
    linear-gradient(to right, rgba(100, 116, 139, 0.18) 1px, transparent 1px),
    linear-gradient(to bottom, rgba(100, 116, 139, 0.18) 1px, transparent 1px) !important;
  background-size: 5mm 5mm !important;
  box-shadow: 0 2px 10px rgba(15, 23, 42, 0.08);
}

:deep(.hiprint-printPaper.design .hiprint-printPaper-content) {
  background-color: transparent !important;
}

/* 强化实际 paperHeader 参考线的辨识度；位置仍完全由模板 JSON 控制，避免视觉与打印分页语义脱节。 */
:deep(.hiprint-printPaper.design .hiprint-headerLine) {
  z-index: 1;
  border-top-color: rgba(239, 68, 68, 0.82) !important;
}

:global(.dark) .designer-shell,
:global(.dark) .designer-workspace,
:global(.dark) .materials-panel,
:global(.dark) .property-panel {
  background: #111827;
}

:global(.dark) .canvas-stage {
  background: #080d18;
}

:global(.dark) .canvas-column {
  background: #080d18;
}

:global(.dark) .panel-pagination {
  border-top-color: rgba(100, 116, 139, 0.38);
  background: rgba(17, 24, 39, 0.98);
}

:global(.dark) .property-heading {
  background: rgba(17, 24, 39, 0.96);
  color: #e2e8f0;
}

@media (max-width: 1500px) {
  .designer-workspace {
    grid-template-columns: 310px minmax(560px, 1fr) 268px;
  }

  .canvas-stage {
    min-width: 560px;
  }

  .canvas-column {
    min-width: 560px;
  }
}
</style>
