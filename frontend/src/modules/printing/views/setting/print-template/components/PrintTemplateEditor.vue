<!--
  打印模板全屏编辑器。
  职责：维护元数据与设计 JSON、阻止未保存离开、使用内存样例预览，并通过公共 FIFO API 提供直打入口和本地打印机偏好。
-->
<script setup lang="ts">
import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
import type { PrintTemplateFormModel } from './models'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerDescription, XhDrawerFooter, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFlex, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSelect } from '~/components'
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
  <!-- 关闭走 requestVisible：有未保存改动时先确认，避免误关丢设计稿 -->
  <XhDialogRoot
    :open="show"
    :close-on-interact-outside="false"
    :close-on-escape="!saveLoading && !directLoading"
    @update:open="requestVisible"
  >
    <XhDialogContent class="print-template-editor-modal">
      <XhDialogTitle>
        <div class="template-settings-header">
          <strong>{{ title }}</strong>
          <XhTagRoot variant="subtle" :tone="dirty ? 'warning' : 'success'" size="sm">
            <XhTagLabel>
              {{ dirty ? t('setting.print_template.unsaved') : t('setting.print_template.saved') }}
            </XhTagLabel>
          </XhTagRoot>
        </div>
      </XhDialogTitle>
      <XhDialogCloseTrigger v-if="!saveLoading && !directLoading" />

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
              <XhButton variant="subtle" data-testid="print-template-save" tone="brand" :loading="saveLoading" :disabled="!designerReady" @click="handleSave">
                <XhButtonIndicator />
                <span><Icon icon="tabler:device-floppy" /></span>
                <XhButtonLabel>{{ t('common.actions.save') }}</XhButtonLabel>
              </XhButton>
              <XhButton variant="subtle" :loading="previewLoading" :disabled="!designerReady" @click="openSamplePreview">
                <XhButtonIndicator />
                <span><Icon icon="tabler:eye" /></span>
                <XhButtonLabel>{{ t('setting.print_template.sample_preview') }}</XhButtonLabel>
              </XhButton>
              <XhButton variant="subtle" tone="success" :loading="directLoading" :disabled="!designerReady || (currentDetail !== null && !canDirectPrint)" @click="directPrint">
                <XhButtonIndicator />
                <span><Icon icon="tabler:printer" /></span>
                <XhButtonLabel>{{ t('setting.print_template.direct_print') }}</XhButtonLabel>
              </XhButton>

              <span class="action-divider" aria-hidden="true" />

              <XhButton
                data-testid="print-template-settings"
                :tone="metadataIncomplete ? 'warning' : 'neutral'"
                variant="subtle"
                @click="openMetadata"
              >
                <span><Icon icon="tabler:settings" /></span>
                {{ t('setting.print_template.template_settings') }}
              </XhButton>
              <XhTagRoot v-if="metadataIncomplete" variant="subtle" size="sm" tone="warning">
                <XhTagLabel>
                  {{ t('setting.print_template.metadata_incomplete') }}
                </XhTagLabel>
              </XhTagRoot>
              <span v-else class="template-context" :title="form.dataSourceCode || t('setting.print_template.free_template')">
                <Icon width="16" height="16" :icon="form.dataSourceCode ? 'tabler:database' : 'tabler:database-off'" />
                {{ form.templateCode }} · {{ form.dataSourceCode || t('setting.print_template.free_template') }}
              </span>
            </div>
          </template>

          <template #printer-actions>
            <XhFlex class="printer-actions" align="center" :wrap="false">
              <XSelect
                :value="selectedPrinter ?? ''"
                :options="printerOptions"
                class="printer-select"
                @update:value="(value: string | number | (string | number)[] | null) => updatePrinterPreference(value as string | null)"
              />
              <XhButton
                class="xh-icon-btn"
                variant="ghost"
                icon-only
                :loading="printerLoading"
                :title="t('setting.print_template.refresh_printers')"
                :aria-label="t('setting.print_template.refresh_printers')"
                @click="loadPrinters(true)"
              >
                <XhButtonIndicator />
                <span><Icon icon="tabler:refresh" /></span>
              </XhButton>
            </XhFlex>
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

      <XhDrawerRoot
        :open="metadataVisible"
        side="left"
        :close-on-interact-outside="!saveLoading"
        @update:open="handleMetadataVisible"
      >
        <!-- 取消与保存必须在抽屉面板里：抽屉是模态，面板外的按钮被遮罩与焦点陷阱挡住，新建模板永远存不上 -->
        <XhDrawerContent class="print-template-settings-drawer" style="--xh-drawer-size: min(444px, 100vw); --xh-drawer-px: 0">
          <XhDrawerHeader>
            <XhDrawerTitle>{{ t('setting.print_template.template_settings') }}</XhDrawerTitle>
            <XhDrawerDescription>{{ t('setting.print_template.template_settings_subtitle') }}</XhDrawerDescription>
          </XhDrawerHeader>
          <XhDrawerBody>
            <MetadataForm
              :key="metadataSessionKey"
              v-model="metadataDraft"
              :editing="Boolean(currentDetail)"
              :global-mode="globalMode"
              :template="draftTemplate"
            />
          </XhDrawerBody>
          <XhDrawerFooter class="template-settings-footer">
            <XhButton variant="subtle" :disabled="saveLoading" @click="cancelMetadata">
              {{ t('common.actions.cancel') }}
            </XhButton>
            <XhButton
              variant="subtle"
              tone="brand"
              :loading="saveLoading"
              :disabled="metadataDraftIncomplete"
              class="template-settings-submit"
              @click="saveMetadata"
            >
              <XhButtonIndicator />
              <XhButtonLabel>{{ t('setting.print_template.save_and_return') }}</XhButtonLabel>
              <span v-if="metadataDraftDirty && !saveLoading" class="metadata-dirty-dot" aria-hidden="true" />
            </XhButton>
          </XhDrawerFooter>
          <XhDrawerCloseTrigger />
        </XhDrawerContent>
      </XhDrawerRoot>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style>
.print-template-editor-modal {
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

/* 满屏编辑器贴满视口：定位层缺省留一圈 --xh-space-4 的视口间距，它不收作者属性，只能从面板反查 */
[data-scope='dialog'][data-part='positioner']:has(> .print-template-editor-modal) {
  --xh-dialog-positioner-padding: var(--xh-space-0);
}

.print-template-editor-modal > .editor-layout {
  height: 0;
  min-height: 0;
  flex: 1 1 0;
  padding: 0;
  overflow: hidden;
}

.print-template-editor-modal > [data-scope='dialog'][data-part='title'] {
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
  align-items: center;
  gap: var(--xh-space-2);
}

.template-settings-header strong {
  color: #1f2937;
  font-size: 18px;
  font-weight: 600;
}

/* 面板横向内衬已归零交给表单自管，头尾两段自己补回内衬 */
.print-template-settings-drawer > [data-scope='drawer'][data-part='header'],
.print-template-settings-drawer > [data-scope='drawer'][data-part='footer'] {
  padding-inline: var(--xh-surface-px-md);
}

.template-settings-footer > [data-scope='button'][data-part='root']:first-child {
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

.dark .template-context {
  color: #94a3b8;
}

.dark .template-settings-header strong {
  color: #f1f5f9;
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
