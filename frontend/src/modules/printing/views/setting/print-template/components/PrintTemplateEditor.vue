<!--
  打印模板全屏编辑器。
  职责：设计已有模板的版面、阻止未保存离开、使用内存样例预览，并通过公共 FIFO API 提供直打入口和本地打印机偏好。
  编辑器的操作（样例预览、直接打印、打印机、返回列表）挂进设计器的菜单栏，保存走设计器的 save 命令；
  模板名称、编码、所绑数据源与保存状态放在菜单栏右边。名称、编码、数据源等在列表页的编辑弹窗里改。
-->
<script setup lang="ts">
import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
import { XhDialogContent, XhDialogRoot, XhDialogTitle, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { DesignerMenubarTitle } from '~/designers'
import { createPrintHostExtension } from './print-host-extension'
import DesignerCanvas from './PrintDesignerCanvas.vue'
import SampleDataModal from './PrintSampleDataModal.vue'
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
  // 停用的模板不能直打
  canDirectPrint: () => canDirectPrint.value,
  busy: () => saveLoading.value || directLoading.value,
  printers: () => printerOptions.value,
  selectedPrinter: () => selectedPrinter.value,
  loadingPrinters: () => printerLoading.value,
  openSamplePreview,
  directPrint: () => void directPrint(),
  selectPrinter: printer => void updatePrinterPreference(printer),
  refreshPrinters: () => void loadPrinters(true),
  backToList: () => void requestVisible(false),
})

// 编辑会话的状态变了设计器不知道：让它重算宿主命令的可用、打印机的勾与随语言变的文字
watch(
  [designerReady, previewLoading, directLoading, saveLoading, canDirectPrint, currentDetail, printerOptions, selectedPrinter, printerLoading, locale],
  () => canvasRef.value?.refreshCommands(),
)

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
          :data-source-code="currentDetail?.dataSourceCode ?? null"
          :draft-key="draftKey"
          :host-extension="hostExtension"
          :template="draftTemplate"
          @changed="onDesignerChanged"
          @ready="onDesignerReady"
          @save="save"
        >
          <!-- 菜单栏右边：模板名称、编码、所绑数据源与保存状态，与流程编辑器同一个样子 -->
          <template #menubar-end>
            <DesignerMenubarTitle
              :name="currentDetail?.templateName || title"
              :code="currentDetail?.templateCode"
              :saved="!dirty"
              :status-text="dirty ? t('setting.print_template.unsaved') : t('setting.print_template.saved')"
            >
              <XhTagRoot variant="outline" size="sm" :title="t('setting.print_template.data_source')">
                <XhTagLabel>{{ currentDetail?.dataSourceCode || t('setting.print_template.free_template') }}</XhTagLabel>
              </XhTagRoot>
            </DesignerMenubarTitle>
          </template>
        </DesignerCanvas>
      </div>

      <SampleDataModal
        v-model:show="samplePreviewVisible"
        :data-source-code="currentDetail?.dataSourceCode ?? null"
        :submitting="previewLoading"
        :template="draftTemplate"
        :template-code="currentDetail?.templateCode"
        :template-name="currentDetail?.templateName"
        @preview="preview"
      />
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
</style>
