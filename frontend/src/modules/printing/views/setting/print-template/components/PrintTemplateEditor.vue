<!--
  打印模板全屏编辑器。
  职责：维护元数据与设计 JSON、阻止未保存离开、使用内存样例预览，并通过公共 FIFO API 提供直打入口和本地打印机偏好。
-->
<script setup lang="ts">
import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
import type { PrintTemplateFormModel } from './models'
import {
  NButton,
  NDrawer,
  NDrawerContent,
  NIcon,
  NModal,
  NSelect,
  NSpace,
  NTag,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import DesignerCanvas from './HiprintDesignerCanvas.vue'
import SampleDataModal from './PrintSampleDataModal.vue'
import MetadataForm from './PrintTemplateMetadataForm.vue'
import { usePrintTemplateEditor } from './use-print-template-editor'

defineOptions({ name: 'PrintTemplateEditor' })

const props = defineProps<{
  detail: PrintTemplateDetailDto | null
  globalMode: boolean
  scope: PrintTemplateScope
  show: boolean
}>()

const emit = defineEmits<{
  'saved': [detail: PrintTemplateDetailDto]
  'update:show': [value: boolean]
}>()

const { t } = useI18n()
const {
  canDirectPrint,
  confirmDiscard,
  currentDetail,
  designerKey,
  designerReady,
  directLoading,
  directPrint,
  dirty,
  draftTemplate,
  form,
  loadPrinters,
  onDesignerChanged,
  onDesignerReady,
  openSamplePreview,
  preview,
  previewLoading,
  printerLoading,
  printerOptions,
  requestVisible,
  save,
  saveLoading,
  samplePreviewTemplate,
  samplePreviewVisible,
  selectedPrinter,
  setDesignerRef,
  title,
  updatePrinterPreference,
} = usePrintTemplateEditor(props, emit)

const metadataVisible = ref(false)
const metadataSessionKey = ref(0)
const metadataDraft = ref<PrintTemplateFormModel>(cloneMetadata(form.value))
const metadataBaseline = ref<PrintTemplateFormModel>(cloneMetadata(form.value))
const metadataIncomplete = computed(() => !form.value.templateCode.trim()
  || !form.value.templateName.trim())
const metadataDraftIncomplete = computed(() => !metadataDraft.value.templateCode.trim()
  || !metadataDraft.value.templateName.trim())
const metadataDraftDirty = computed(() => !isSameMetadata(metadataDraft.value, metadataBaseline.value))

watch(
  () => props.show,
  (show) => {
    if (!show)
      metadataVisible.value = false
  },
)

/** 打开设置抽屉并创建隔离草稿，使关闭抽屉不会污染当前设计会话。 */
function openMetadata(): void {
  const snapshot = cloneMetadata(form.value)
  metadataDraft.value = snapshot
  metadataBaseline.value = cloneMetadata(snapshot)
  metadataSessionKey.value += 1
  metadataVisible.value = true
}

/** 关闭设置抽屉并丢弃尚未提交的局部草稿。 */
function cancelMetadata(): void {
  if (saveLoading.value)
    return
  metadataDraft.value = cloneMetadata(metadataBaseline.value)
  metadataVisible.value = false
}

/** 接收抽屉遮罩或关闭按钮的可见性变化，并统一走取消语义。 */
function handleMetadataVisible(value: boolean): void {
  if (!value)
    cancelMetadata()
}

/** 使用抽屉草稿保存完整模板；API 成功后才写回主会话并关闭抽屉。 */
async function saveMetadata(): Promise<void> {
  if (saveLoading.value || metadataDraftIncomplete.value)
    return
  const savedDetail = await save(cloneMetadata(metadataDraft.value))
  if (!savedDetail)
    return
  metadataBaseline.value = cloneMetadata(form.value)
  metadataDraft.value = cloneMetadata(form.value)
  metadataVisible.value = false
}

/**
 * 保存当前设计；基础信息缺失时先打开设置抽屉，避免用户只收到提示却找不到填写入口。
 * @returns 无返回值；成功后关闭设置抽屉，失败信息由编辑会话统一提示。
 */
async function handleSave(): Promise<void> {
  if (metadataIncomplete.value) {
    openMetadata()
    return
  }
  await save()
}

/** 克隆扁平元数据对象，阻断抽屉草稿与主表单之间的响应式引用共享。 */
function cloneMetadata(value: PrintTemplateFormModel): PrintTemplateFormModel {
  return { ...value }
}

/** 比较两个元数据快照；字段均为标量，逐字段比较比 JSON 序列化更明确且无额外分配。 */
function isSameMetadata(left: PrintTemplateFormModel, right: PrintTemplateFormModel): boolean {
  return left.allowTenantUse === right.allowTenantUse
    && left.dataSourceCode === right.dataSourceCode
    && left.engineVersion === right.engineVersion
    && left.remark === right.remark
    && left.sort === right.sort
    && left.status === right.status
    && left.templateCode === right.templateCode
    && left.templateName === right.templateName
}

defineExpose({ confirmDiscard })
</script>

<template>
  <NModal
    :show="show"
    preset="card"
    :title="title"
    :mask-closable="false"
    :closable="!saveLoading && !directLoading"
    class="print-template-editor-modal"
    @update:show="requestVisible"
  >
    <template #header-extra>
      <NTag :type="dirty ? 'warning' : 'success'" size="small">
        {{ dirty ? t('setting.print_template.unsaved') : t('setting.print_template.saved') }}
      </NTag>
    </template>

    <div class="editor-layout">
      <DesignerCanvas
        :key="`${designerKey}:${form.dataSourceCode}`"
        :ref="setDesignerRef"
        class="min-h-0 flex-1"
        :data-source-code="form.dataSourceCode"
        :template="draftTemplate"
        @changed="onDesignerChanged"
        @ready="onDesignerReady"
      >
        <template #template-actions>
          <div class="toolbar-primary-actions">
            <NButton data-testid="print-template-save" type="primary" :loading="saveLoading" :disabled="!designerReady" @click="handleSave">
              <template #icon>
                <NIcon><Icon icon="tabler:device-floppy" /></NIcon>
              </template>
              {{ t('common.actions.save') }}
            </NButton>
            <NButton :loading="previewLoading" :disabled="!designerReady" @click="openSamplePreview">
              <template #icon>
                <NIcon><Icon icon="tabler:eye" /></NIcon>
              </template>
              {{ t('setting.print_template.sample_preview') }}
            </NButton>
            <NButton type="success" :loading="directLoading" :disabled="!designerReady || (currentDetail !== null && !canDirectPrint)" @click="directPrint">
              <template #icon>
                <NIcon><Icon icon="tabler:printer" /></NIcon>
              </template>
              {{ t('setting.print_template.direct_print') }}
            </NButton>

            <span class="action-divider" aria-hidden="true" />

            <NButton
              data-testid="print-template-settings"
              :type="metadataIncomplete ? 'warning' : 'default'"
              secondary
              @click="openMetadata"
            >
              <template #icon>
                <NIcon><Icon icon="tabler:settings" /></NIcon>
              </template>
              {{ t('setting.print_template.template_settings') }}
            </NButton>
            <NTag v-if="metadataIncomplete" size="small" type="warning" :bordered="false">
              {{ t('setting.print_template.metadata_incomplete') }}
            </NTag>
            <span v-else class="template-context" :title="form.dataSourceCode || t('setting.print_template.free_template')">
              <NIcon :size="16"><Icon :icon="form.dataSourceCode ? 'tabler:database' : 'tabler:database-off'" /></NIcon>
              {{ form.templateCode }} · {{ form.dataSourceCode || t('setting.print_template.free_template') }}
            </span>
          </div>
        </template>

        <template #printer-actions>
          <NSpace class="printer-actions" align="center" :wrap="false">
            <NSelect
              :value="selectedPrinter ?? ''"
              :options="printerOptions"
              :loading="printerLoading"
              class="printer-select"
              @update:value="updatePrinterPreference"
            />
            <NButton quaternary circle :loading="printerLoading" :title="t('setting.print_template.refresh_printers')" @click="loadPrinters(true)">
              <template #icon>
                <NIcon><Icon icon="tabler:refresh" /></NIcon>
              </template>
            </NButton>
          </NSpace>
        </template>
      </DesignerCanvas>
    </div>

    <SampleDataModal
      v-model:show="samplePreviewVisible"
      :data-source-code="form.dataSourceCode"
      :submitting="previewLoading"
      :template="samplePreviewTemplate"
      :template-code="form.templateCode || undefined"
      :template-name="form.templateName || undefined"
      @preview="preview"
    />

    <NDrawer
      :show="metadataVisible"
      width="min(444px, 100vw)"
      placement="left"
      :mask-closable="!saveLoading"
      class="print-template-settings-drawer"
      @update:show="handleMetadataVisible"
    >
      <NDrawerContent :closable="!saveLoading" :native-scrollbar="false">
        <template #header>
          <div class="template-settings-header">
            <strong>{{ t('setting.print_template.template_settings') }}</strong>
            <span>{{ t('setting.print_template.template_settings_subtitle') }}</span>
          </div>
        </template>
        <MetadataForm
          :key="metadataSessionKey"
          v-model="metadataDraft"
          :editing="Boolean(currentDetail)"
          :global-mode="globalMode"
          :template="draftTemplate"
        />
        <template #footer>
          <div class="template-settings-footer">
            <NButton :disabled="saveLoading" @click="cancelMetadata">
              {{ t('common.actions.cancel') }}
            </NButton>
            <NButton
              type="primary"
              :loading="saveLoading"
              :disabled="metadataDraftIncomplete"
              class="template-settings-submit"
              @click="saveMetadata"
            >
              {{ t('setting.print_template.save_and_return') }}
              <span v-if="metadataDraftDirty && !saveLoading" class="metadata-dirty-dot" aria-hidden="true" />
            </NButton>
          </div>
        </template>
      </NDrawerContent>
    </NDrawer>
  </NModal>
</template>

<style>
.print-template-editor-modal {
  display: flex;
  width: 100vw !important;
  height: 100vh !important;
  max-height: 100vh;
  flex-direction: column;
  margin: 0;
  overflow: hidden;
  border-radius: 0 !important;
}

.print-template-editor-modal > .n-card-content {
  height: 0;
  min-height: 0;
  flex: 1 1 0;
  padding: 0;
  overflow: hidden;
}

.print-template-editor-modal > .n-card-header {
  flex: none;
  padding: 16px 24px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.2);
}

.editor-layout {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
  overflow: hidden;
}

.toolbar-primary-actions {
  display: flex;
  min-width: max-content;
  align-items: center;
  gap: 10px;
}

.action-divider {
  width: 1px;
  height: 24px;
  margin: 0 2px;
  background: rgba(148, 163, 184, 0.36);
}

.template-context {
  display: inline-flex;
  max-width: 320px;
  align-items: center;
  gap: 6px;
  overflow: hidden;
  color: #64748b;
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.printer-actions {
  min-width: max-content;
}

.printer-select {
  width: 238px;
}

.template-settings-header {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.template-settings-header strong {
  color: #1f2937;
  font-size: 18px;
  font-weight: 600;
}

.template-settings-header span {
  color: #64748b;
  font-size: 12px;
  font-weight: 400;
}

.template-settings-footer {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.template-settings-footer > .n-button:first-child {
  min-width: 92px;
}

.template-settings-submit {
  position: relative;
  min-width: 210px;
}

.metadata-dirty-dot {
  position: absolute;
  top: 8px;
  right: 8px;
  width: 7px;
  height: 7px;
  border: 1px solid #fff;
  border-radius: 50%;
  background: #f59e0b;
  box-shadow: 0 0 0 1px rgba(180, 83, 9, 0.12);
}

/* 分层表单自行控制水平留白，移除 Naive UI 默认左右内边距以避免双重缩进。 */
.print-template-settings-drawer .n-drawer-body-content-wrapper {
  padding-right: 0 !important;
  padding-left: 0 !important;
}

.dark .template-context {
  color: #94a3b8;
}

.dark .template-settings-header strong {
  color: #f1f5f9;
}

.dark .template-settings-header span {
  color: #94a3b8;
}

@media (max-width: 1200px) {
  .printer-select {
    width: 210px;
  }
}

@media (max-width: 520px) {
  .template-settings-submit {
    min-width: 0;
    flex: 1;
  }
}
</style>
