<!--
  打印模板全屏编辑器。
  职责：维护元数据与设计 JSON、阻止未保存离开、使用内存样例预览，并通过公共 FIFO API 提供直打入口和本地打印机偏好。
  编辑器的操作（样例预览、直接打印、打印机、模板设置、返回列表）挂进设计器的菜单栏，保存走设计器的 save 命令；
  标题、保存状态与模板信息放在菜单栏右边。
-->
<script setup lang="ts">
import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
import type { PrintTemplateFormModel } from './models'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerDescription, XhDrawerFooter, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { createPrintHostExtension } from './print-host-extension'
import DesignerCanvas from './PrintDesignerCanvas.vue'
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

const { t, locale } = useI18n()
const {
  canDirectPrint,
  confirmDiscard,
  currentDetail,
  designerKey,
  designerReady,
  directLoading,
  directPrint,
  dirty,
  draftKey,
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

const canvasRef = shallowRef<InstanceType<typeof DesignerCanvas> | null>(null)

/** 画布实例交给编辑会话（保存、预览要读设计），这里另留一份用来刷新菜单栏上的宿主命令。 */
function bindCanvas(instance: unknown): void {
  setDesignerRef(instance)
  canvasRef.value = instance as InstanceType<typeof DesignerCanvas> | null
}

/** 编辑器的操作写成设计器的命令，挂进菜单栏「文件」与工具栏；建一次，命令执行时现读编辑会话的状态。 */
const hostExtension = createPrintHostExtension({
  t: key => t(key),
  ready: () => designerReady.value,
  previewing: () => previewLoading.value,
  printing: () => directLoading.value,
  // 新模板先保存再打；已有模板停用了不能直打
  canDirectPrint: () => currentDetail.value === null || canDirectPrint.value,
  busy: () => saveLoading.value || directLoading.value,
  printers: () => printerOptions.value,
  selectedPrinter: () => selectedPrinter.value,
  loadingPrinters: () => printerLoading.value,
  openSamplePreview,
  directPrint: () => void directPrint(),
  selectPrinter: printer => void updatePrinterPreference(printer),
  refreshPrinters: () => void loadPrinters(true),
  openTemplateSettings: openMetadata,
  backToList: () => void requestVisible(false),
})

// 编辑会话的状态变了设计器不知道：让它重算宿主命令的可用、打印机的勾与随语言变的文字
watch(
  [designerReady, previewLoading, directLoading, saveLoading, canDirectPrint, currentDetail, printerOptions, selectedPrinter, printerLoading, locale],
  () => canvasRef.value?.refreshCommands(),
)

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
      <!-- 标题只给读屏作弹窗的名字；看得见的标题与保存状态在设计器菜单栏的右边，返回列表在「文件」菜单与工具栏上 -->
      <XhDialogTitle class="sr-only">
        {{ title }}
      </XhDialogTitle>

      <div class="editor-layout">
        <DesignerCanvas
          :key="designerKey"
          :ref="bindCanvas"
          class="min-h-0 flex-1"
          :data-source-code="form.dataSourceCode"
          :draft-key="draftKey"
          :host-extension="hostExtension"
          :template="draftTemplate"
          @changed="onDesignerChanged"
          @ready="onDesignerReady"
          @save="handleSave"
        >
          <template #menubar-end>
            <XhTagRoot v-if="metadataIncomplete" variant="subtle" size="sm" tone="warning">
              <XhTagLabel>
                {{ t('setting.print_template.metadata_incomplete') }}
              </XhTagLabel>
            </XhTagRoot>
            <span v-else class="template-context" :title="form.dataSourceCode || t('setting.print_template.free_template')">
              <Icon class="shrink-0" width="16" height="16" :icon="form.dataSourceCode ? 'tabler:database' : 'tabler:database-off'" />
              <span class="template-context__text">{{ form.templateCode }} · {{ form.dataSourceCode || t('setting.print_template.free_template') }}</span>
            </span>
            <strong class="template-title" :title="title">{{ title }}</strong>
            <XhTagRoot class="shrink-0" variant="subtle" :tone="dirty ? 'warning' : 'success'" size="sm">
              <XhTagLabel>
                {{ dirty ? t('setting.print_template.unsaved') : t('setting.print_template.saved') }}
              </XhTagLabel>
            </XhTagRoot>
          </template>
        </DesignerCanvas>
      </div>

      <SampleDataModal
        v-model:show="samplePreviewVisible"
        :data-source-code="form.dataSourceCode"
        :submitting="previewLoading"
        :template="draftTemplate"
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

.editor-layout {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
  overflow: hidden;
}

/* 菜单栏右边：模板信息弱一档、放不下先收窄，标题取正常字色，保存状态不收窄 */
.template-context {
  display: inline-flex;
  min-width: 0;
  align-items: center;
  gap: var(--xh-space-1);
  color: var(--xh-fg-muted);
}

.template-context__text,
.template-title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.template-title {
  min-width: 0;
  color: var(--xh-fg-default);
  font-weight: var(--xh-font-weight-semibold);
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
  /* 描一圈面色把圆点从按钮底上抠出来，深浅两态都清楚 */
  border: 1px solid var(--xh-bg-surface);
  border-radius: 50%;
  background: var(--xh-color-warning-500);
  box-shadow: 0 0 0 1px color-mix(in oklab, var(--xh-color-warning-600) 12%, transparent);
}

@media (max-width: 520px) {
  .template-settings-submit {
    min-width: 0;
    flex: 1;
  }
}
</style>
