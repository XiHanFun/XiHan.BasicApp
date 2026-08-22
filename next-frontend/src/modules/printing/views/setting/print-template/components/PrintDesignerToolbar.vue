<!--
  hiprint 设计器统一控制面板。
  职责：在同一面板中组合模板操作、画布设置、JSON 工具以及官方多选元素对齐与间距入口。
-->
<script setup lang="ts">
import type {
  PrintDesignerMode,
  PrintPaperPreset,
  PrintPaperType,
} from './models'
import type { PrintElementAlignAction, PrintElementSpacingDirection } from '~/printing'
import { XhButton, XhCollapsibleContent, XhCollapsibleRoot, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle } from '@xihan-ui/vue'
import { computed, nextTick, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XNumberInput } from '~/components'
import { Icon } from '~/iconify'
import { PRINT_PAPER_PRESETS } from './models'

defineOptions({ name: 'PrintDesignerToolbar' })

const props = defineProps<{
  designMode: PrintDesignerMode
  disabled?: boolean
  paperHeight: number
  paperType: PrintPaperType
  paperWidth: number
  zoomPercent: number
}>()

const emit = defineEmits<{
  customPaperChange: [width: number, height: number]
  elementAlign: [action: PrintElementAlignAction]
  elementSpacing: [direction: PrintElementSpacingDirection, spacing: number]
  jsonEditorOpen: []
  modeChange: [mode: PrintDesignerMode]
  paperPresetChange: [paperType: PrintPaperPreset]
  rotate: []
  zoomChange: [scale: number]
}>()

const MIN_PAPER_SIZE_MM = 10
const MAX_PAPER_SIZE_MM = 2000
const MIN_ZOOM_PERCENT = 50
const MAX_ZOOM_PERCENT = 200
const ZOOM_STEP_PERCENT = 10
const DEFAULT_ZOOM_PERCENT = 100
const ELEMENT_SPACING = 10

interface AlignmentCommand {
  action: PrintElementAlignAction
  icon: string
  labelKey: string
}

const ALIGNMENT_COMMANDS: readonly AlignmentCommand[] = [
  { action: 'left', icon: 'tabler:layout-align-left', labelKey: 'setting.print_template.align_left' },
  { action: 'vertical', icon: 'tabler:layout-align-center', labelKey: 'setting.print_template.align_horizontal_center' },
  { action: 'right', icon: 'tabler:layout-align-right', labelKey: 'setting.print_template.align_right' },
  { action: 'top', icon: 'tabler:layout-align-top', labelKey: 'setting.print_template.align_top' },
  { action: 'horizontal', icon: 'tabler:layout-align-middle', labelKey: 'setting.print_template.align_vertical_center' },
  { action: 'bottom', icon: 'tabler:layout-align-bottom', labelKey: 'setting.print_template.align_bottom' },
  { action: 'distributeHor', icon: 'tabler:layout-distribute-horizontal', labelKey: 'setting.print_template.distribute_horizontal' },
  { action: 'distributeVer', icon: 'tabler:layout-distribute-vertical', labelKey: 'setting.print_template.distribute_vertical' },
]

const { t } = useI18n()
const alignmentToolsId = `print-alignment-tools-${crypto.randomUUID()}`
const alignmentToolsVisible = ref(false)
const customPaperVisible = ref(false)
const customPaperSubmitting = ref(false)
const customPaperWidth = ref<number | null>(null)
const customPaperHeight = ref<number | null>(null)

const normalizedZoomPercent = computed(() => Math.min(
  MAX_ZOOM_PERCENT,
  Math.max(MIN_ZOOM_PERCENT, Math.round(props.zoomPercent)),
))

const canSubmitCustomPaper = computed(() => isValidPaperSize(customPaperWidth.value)
  && isValidPaperSize(customPaperHeight.value))

/** 判断纸张边长是否在 hiprint 设计器可安全处理的范围内。 */
function isValidPaperSize(value: number | null): value is number {
  return value !== null
    && Number.isFinite(value)
    && value >= MIN_PAPER_SIZE_MM
    && value <= MAX_PAPER_SIZE_MM
}

/** 打开自定义纸张对话框，并带入当前活动面板尺寸。 */
function openCustomPaperModal(): void {
  customPaperWidth.value = props.paperWidth
  customPaperHeight.value = props.paperHeight
  customPaperVisible.value = true
}

/**
 * 提交自定义纸张尺寸。
 * @returns 对话框关闭后的完成信号。
 * @throws 本方法不主动抛出异常；非法尺寸会保持对话框打开并禁用提交按钮。
 */
async function submitCustomPaper(): Promise<void> {
  if (!canSubmitCustomPaper.value || customPaperSubmitting.value)
    return

  customPaperSubmitting.value = true
  try {
    emit('customPaperChange', customPaperWidth.value!, customPaperHeight.value!)
    await nextTick()
    customPaperVisible.value = false
  }
  finally {
    customPaperSubmitting.value = false
  }
}

/** 按固定步长调整设计画布缩放，并限制在可读范围内。 */
function changeZoom(deltaPercent: number): void {
  const nextPercent = Math.min(
    MAX_ZOOM_PERCENT,
    Math.max(MIN_ZOOM_PERCENT, normalizedZoomPercent.value + deltaPercent),
  )
  emit('zoomChange', nextPercent / 100)
}

/** 将设计画布快速恢复到 100%。 */
function resetZoom(): void {
  emit('zoomChange', DEFAULT_ZOOM_PERCENT / 100)
}

/** 使用官网示例的固定值 10 调整多选元素间距。 */
function applyElementSpacing(direction: PrintElementSpacingDirection): void {
  emit('elementSpacing', direction, ELEMENT_SPACING)
}

/** 展开或收起元素对齐工具区；状态仅属于当前设计器实例，不写入模板或浏览器存储。 */
function toggleAlignmentTools(): void {
  alignmentToolsVisible.value = !alignmentToolsVisible.value
}
</script>

<template>
  <section class="designer-quick-toolbar" :aria-label="t('setting.print_template.designer_toolbar')">
    <div class="control-primary-row">
      <div class="control-primary-scroll">
        <div class="control-section template-operation-section">
          <span class="control-section-label">{{ t('setting.print_template.template_operations') }}</span>
          <div class="template-actions-slot">
            <slot name="template-actions" />
          </div>
        </div>

        <span class="control-vertical-divider" aria-hidden="true" />

        <div class="control-section canvas-settings-section">
          <span class="control-section-label">{{ t('setting.print_template.canvas_settings') }}</span>
          <div class="canvas-settings-controls">
            <XhButtonGroup class="mode-group" role="tablist" :aria-label="t('setting.print_template.panel_mode')">
              <XhButton
                :tone="designMode === 'single' ? 'brand' : 'neutral'"
                :variant="designMode === 'single' ? 'subtle' : 'ghost'"
                :disabled="disabled"
                role="tab"
                :aria-selected="designMode === 'single'"
                @click="emit('modeChange', 'single')"
              >
                <span><Icon icon="tabler:file" /></span>
                {{ t('setting.print_template.single_panel') }}
              </XhButton>
              <XhButton
                :tone="designMode === 'multi' ? 'brand' : 'neutral'"
                :variant="designMode === 'multi' ? 'subtle' : 'ghost'"
                :disabled="disabled"
                role="tab"
                :aria-selected="designMode === 'multi'"
                @click="emit('modeChange', 'multi')"
              >
                <span><Icon icon="tabler:files" /></span>
                {{ t('setting.print_template.multi_panel') }}
              </XhButton>
            </XhButtonGroup>
            <XhButton
              class="alignment-toggle-button"
              :tone="alignmentToolsVisible ? 'brand' : 'neutral'"
              :variant="alignmentToolsVisible ? 'subtle' : 'ghost'"
              :disabled="disabled"
              :title="t(alignmentToolsVisible
                ? 'setting.print_template.hide_alignment_tools'
                : 'setting.print_template.show_alignment_tools')"
              :aria-label="t(alignmentToolsVisible
                ? 'setting.print_template.hide_alignment_tools'
                : 'setting.print_template.show_alignment_tools')"
              :aria-expanded="alignmentToolsVisible"
              :aria-controls="alignmentToolsId"
              @click="toggleAlignmentTools"
            >
              <span><Icon icon="tabler:align-box-left-middle" /></span>
              {{ t('setting.print_template.alignment_tools') }}
            </XhButton>
          </div>
        </div>

        <div class="printer-actions-slot">
          <slot name="printer-actions" />
        </div>
      </div>
    </div>

    <div class="control-secondary-row">
      <div class="control-secondary-scroll">
        <XhButtonGroup class="paper-group" :aria-label="t('setting.print_template.paper_size')">
          <XhButton
            v-for="preset in PRINT_PAPER_PRESETS"
            :key="preset"
            :tone="paperType === preset ? 'brand' : 'neutral'"
            :disabled="disabled"
            @click="emit('paperPresetChange', preset)"
          >
            {{ preset }}
          </XhButton>
          <XhButton
            class="custom-paper-button"
            :tone="paperType === 'CUSTOM' ? 'brand' : 'neutral'"
            :disabled="disabled"
            @click="openCustomPaperModal"
          >
            {{ t('setting.print_template.custom_paper') }}
          </XhButton>
        </XhButtonGroup>

        <div class="canvas-view-tools">
          <XhButtonGroup class="zoom-group" :aria-label="t('setting.print_template.canvas_zoom')">
            <XhButton
              :disabled="disabled || normalizedZoomPercent <= MIN_ZOOM_PERCENT"
              :title="t('setting.print_template.zoom_out')"
              :aria-label="t('setting.print_template.zoom_out')"
              @click="changeZoom(-ZOOM_STEP_PERCENT)"
            >
              <span><Icon icon="tabler:zoom-out" /></span>
            </XhButton>
            <XhButton
              class="zoom-value"
              :disabled="disabled"
              :title="t('setting.print_template.reset_zoom')"
              @click="resetZoom"
            >
              {{ normalizedZoomPercent }}%
            </XhButton>
            <XhButton
              :disabled="disabled || normalizedZoomPercent >= MAX_ZOOM_PERCENT"
              :title="t('setting.print_template.zoom_in')"
              :aria-label="t('setting.print_template.zoom_in')"
              @click="changeZoom(ZOOM_STEP_PERCENT)"
            >
              <span><Icon icon="tabler:zoom-in" /></span>
            </XhButton>
          </XhButtonGroup>
          <XhButtonGroup class="canvas-command-group" :aria-label="t('setting.print_template.canvas_commands')">
            <XhButton
              :disabled="disabled"
              :title="t('setting.print_template.rotate_paper')"
              @click="emit('rotate')"
            >
              <span><Icon icon="tabler:rotate-clockwise" /></span>
              {{ t('setting.print_template.rotate') }}
            </XhButton>
            <XhButton
              class="json-command-button"
              :disabled="disabled"
              :title="t('setting.print_template.json_editor_title')"
              @click="emit('jsonEditorOpen')"
            >
              <span><Icon icon="tabler:braces" /></span>
              {{ t('setting.print_template.json_template') }}
            </XhButton>
          </XhButtonGroup>
        </div>
      </div>
    </div>

    <XhCollapsibleRoot :open="alignmentToolsVisible">
      <XhCollapsibleContent>
        <div
          :id="alignmentToolsId"
          class="control-alignment-row"
          role="region"
          :aria-label="t('setting.print_template.alignment_commands')"
        >
          <div class="control-alignment-scroll">
            <span class="element-alignment-label">
              {{ t('setting.print_template.element_alignment') }}
            </span>
            <XhButton
              class="spacing-command-button"
              tone="brand"
              :disabled="disabled"
              @click="applyElementSpacing('horizontal')"
            >
              {{ t('setting.print_template.horizontal_spacing', { spacing: ELEMENT_SPACING }) }}
            </XhButton>
            <XhButton
              class="spacing-command-button"
              tone="brand"
              :disabled="disabled"
              @click="applyElementSpacing('vertical')"
            >
              {{ t('setting.print_template.vertical_spacing', { spacing: ELEMENT_SPACING }) }}
            </XhButton>
            <XhButtonGroup
              class="alignment-command-group"
              :aria-label="t('setting.print_template.alignment_commands')"
            >
              <XhButton
                v-for="command in ALIGNMENT_COMMANDS"
                :key="command.action"
                tone="brand"
                variant="outline"
                :disabled="disabled"
                :title="t(command.labelKey)"
                :aria-label="t(command.labelKey)"
                @click="emit('elementAlign', command.action)"
              >
                <Icon width="20" height="20" :icon="command.icon" />
              </XhButton>
            </XhButtonGroup>
          </div>
        </div>
      </XhCollapsibleContent>
    </XhCollapsibleRoot>

    <XhDialogRoot v-model:open="customPaperVisible">
      <XhDialogContent class="custom-paper-modal" style="--xh-dialog-max-w: 420px">
        <XhDialogTitle>{{ t('setting.print_template.custom_paper') }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <div class="paper-size-fields">
          <label class="paper-size-field">
            <span>{{ t('setting.print_template.paper_width') }}</span>
            <XNumberInput
              v-model:value="customPaperWidth"
              :min="MIN_PAPER_SIZE_MM"
              :max="MAX_PAPER_SIZE_MM"
              :precision="1"
              :disabled="customPaperSubmitting"
            >
              <template #suffix>mm</template>
            </XNumberInput>
          </label>
          <label class="paper-size-field">
            <span>{{ t('setting.print_template.paper_height') }}</span>
            <XNumberInput
              v-model:value="customPaperHeight"
              :min="MIN_PAPER_SIZE_MM"
              :max="MAX_PAPER_SIZE_MM"
              :precision="1"
              :disabled="customPaperSubmitting"
            >
              <template #suffix>mm</template>
            </XNumberInput>
          </label>
        </div>
        <div class="xh-dialog-footer">
          <div class="modal-actions">
            <XhButton :disabled="customPaperSubmitting" @click="customPaperVisible = false">
              {{ t('common.actions.cancel') }}
            </XhButton>
            <XhButton
              tone="brand"
              :loading="customPaperSubmitting"
              :disabled="!canSubmitCustomPaper"
              @click="submitCustomPaper"
            >
              {{ t('setting.print_template.apply_paper') }}
            </XhButton>
          </div>
        </div>
      </XhDialogContent>
    </XhDialogRoot>
  </section>
</template>

<style scoped>
.designer-quick-toolbar {
  position: relative;
  z-index: 5;
  flex: none;
  margin: 10px 18px 8px;
  overflow: hidden;
  border: 1px solid rgba(148, 163, 184, 0.3);
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.98);
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04);
}

.control-primary-row,
.control-secondary-row,
.control-alignment-row {
  overflow-x: auto;
  scrollbar-width: thin;
}

.control-primary-scroll {
  display: flex;
  width: 100%;
  min-width: max-content;
  align-items: flex-end;
  gap: 18px;
  box-sizing: border-box;
  padding: 10px 14px 12px;
}

.control-section {
  display: flex;
  flex: none;
  flex-direction: column;
  gap: 7px;
}

.control-section-label {
  color: #334155;
  font-size: 12px;
  font-weight: 650;
  line-height: 18px;
}

.template-actions-slot,
.printer-actions-slot {
  display: flex;
  min-height: 40px;
  align-items: center;
}

.control-vertical-divider {
  width: 1px;
  height: 54px;
  flex: none;
  background: rgba(148, 163, 184, 0.3);
}

.canvas-settings-section {
  min-width: 312px;
}

.canvas-settings-controls {
  display: flex;
  align-items: center;
  gap: 8px;
}

.printer-actions-slot {
  margin-left: auto;
}

.control-secondary-row {
  border-top: 1px solid rgba(148, 163, 184, 0.22);
}

.control-alignment-row {
  border-top: 1px solid rgba(148, 163, 184, 0.22);
}

.control-secondary-scroll {
  display: flex;
  width: 100%;
  min-width: max-content;
  min-height: 62px;
  align-items: center;
  justify-content: flex-start;
  gap: 12px;
  box-sizing: border-box;
  padding: 9px 14px;
}

.control-alignment-scroll {
  display: flex;
  width: 100%;
  min-width: max-content;
  min-height: 58px;
  align-items: center;
  gap: 12px;
  box-sizing: border-box;
  padding: 8px 14px;
}

.element-alignment-label {
  flex: none;
  color: #334155;
  font-size: 14px;
  line-height: 22px;
}

.spacing-command-button {
  min-width: 118px;
}

.alignment-command-group :deep([data-scope='button'][data-part='root']) {
  width: 52px;
  min-height: 38px;
}

.mode-group :deep([data-scope='button'][data-part='root']),
.paper-group :deep([data-scope='button'][data-part='root']),
.zoom-group :deep([data-scope='button'][data-part='root']),
.canvas-command-group :deep([data-scope='button'][data-part='root']) {
  min-height: 38px;
}

.mode-group :deep([data-scope='button'][data-part='root']) {
  min-width: 108px;
}

.paper-group {
  flex: none;
}

.paper-group :deep([data-scope='button'][data-part='root']) {
  min-width: 52px;
}

.custom-paper-button {
  min-width: 104px !important;
}

.zoom-value {
  min-width: 76px;
  font-variant-numeric: tabular-nums;
}

.canvas-view-tools {
  display: flex;
  flex: none;
  align-items: center;
  /* 仅在缩放与画布命令两个组件之间留白；各按钮组内部继续保持连续样式。 */
  gap: 10px;
}

.canvas-command-group :deep([data-scope='button'][data-part='root']:first-child) {
  min-width: 88px;
}

.canvas-command-group :deep(.json-command-button) {
  min-width: 112px;
}

.canvas-settings-controls :deep(.alignment-toggle-button) {
  min-width: 88px;
}

.paper-size-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.paper-size-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  color: #334155;
  font-size: 14px;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

:global(.dark) .designer-quick-toolbar {
  border-color: rgba(100, 116, 139, 0.42);
  background: rgba(17, 24, 39, 0.98);
}

:global(.dark) .control-secondary-row {
  border-top-color: rgba(100, 116, 139, 0.32);
}

:global(.dark) .control-alignment-row {
  border-top-color: rgba(100, 116, 139, 0.32);
}

:global(.dark) .control-section-label,
:global(.dark) .element-alignment-label {
  color: #e2e8f0;
}

:global(.dark) .paper-size-field {
  color: #e2e8f0;
}
</style>
