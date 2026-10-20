<!--
  打印设计器工作区。
  职责：承载 XiHan 打印设计器（<xh-print-designer>），给它数据源的字段树与样例数据，提供 JSON 模板的导出与回写；
  画布、物料、页面模板、标尺、属性与问题列表都由设计器提供。
-->
<script setup lang="ts">
import type { PrintDesignerEventMap, PrintDocument, XhPrintDesigner } from '~/printing/designer'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton } from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDesignerSettings } from '~/designers/settings'
import { Icon } from '~/iconify'
import {
  createDefaultPrintSampleData,
  ensureRemotePrintDataSourcesLoaded,
  getPrintDataSource,
  normalizePrintSampleData,
  parsePrintTemplateJson,
  previewPrintDocument,
  toPrintDataFields,
} from '~/printing'
import { ensurePrintDesigner } from '~/printing/designer'
import { PRINT_TEMPLATE_PAGE_CODE } from './models'
import JsonTemplateModal from './PrintDesignerJsonModal.vue'

defineOptions({ name: 'PrintDesignerCanvas' })

const props = defineProps<{
  dataSourceCode: null | string
  template: PrintDocument
}>()

const emit = defineEmits<{
  changed: [template: PrintDocument]
  ready: []
  save: []
}>()

const { t } = useI18n()

ensurePrintDesigner()

const designerRef = ref<XhPrintDesigner | null>(null)
const loadError = ref('')
const jsonEditorVisible = ref(false)
const jsonEditorText = ref('')
const jsonEditorError = ref('')
const jsonApplying = ref(false)

/** 设计器设置（含面板布局、工具栏与新手引导走过与否）随用户存进用户设置，与打印模板页的列设置同一条记录 */
const designerSettings = useDesignerSettings(PRINT_TEMPLATE_PAGE_CODE)
const { ready: settingsReady } = designerSettings

/** 设计器等用户设置取回后才挂上，第一次铺好时就有用户的设置；挂上时赋设置，再装载模板与数据源 */
watch(designerRef, (designer) => {
  if (!designer)
    return
  designerSettings.attach(designer)
  void initialize()
}, { flush: 'post' })

watch(() => props.dataSourceCode, () => {
  void applyDataSource()
})

/** 装载模板与数据源；数据源目录在用到前确保已加载。 */
async function initialize(): Promise<void> {
  loadError.value = ''
  const designer = designerRef.value
  if (!designer)
    return
  try {
    designer.schema = props.template
    await applyDataSource()
    emit('ready')
  }
  catch (error) {
    loadError.value = (error as Error).message || t('setting.print_template.designer_init_failed')
  }
}

/** 数据源的字段树交给属性面板，样例数据交给画布与设计器的预览态；自由模板两者都不给。 */
async function applyDataSource(): Promise<void> {
  const designer = designerRef.value
  if (!designer)
    return
  const code = props.dataSourceCode?.trim() || null
  if (code)
    await ensureRemotePrintDataSourcesLoaded()
  const source = getPrintDataSource(code)
  designer.fields = source ? toPrintDataFields(source.fields) : undefined
  const sample = source ? normalizePrintSampleData(await createDefaultPrintSampleData(code)).records[0] : undefined
  designer.sampleData = sample
  designer.data = sample
}

/** 编辑与撤销重做都上报最新模板；装载新模板不算修改。 */
function onSchemaChange(event: Event): void {
  const { schema, kind } = (event as PrintDesignerEventMap['schema-change']).detail
  if (kind !== 'load')
    emit('changed', schema)
}

/**
 * 读取当前设计。
 * @returns 设计器里此刻的模板。
 * @throws 设计器尚未就绪。
 */
function getJson(): PrintDocument {
  if (!designerRef.value)
    throw new Error(t('setting.print_template.designer_not_ready'))
  return designerRef.value.schema
}

/** 按当前扩展立即校验一次模板，保存前据此拦下错误级问题。 */
function validate() {
  return designerRef.value?.validate() ?? []
}

/** 设计器保存成功后把当前状态记为已保存。 */
function markSaved(): void {
  designerRef.value?.markSaved()
}

/**
 * 使用当前未保存设计和内存样例数据调起浏览器打印。
 * @param data 模拟数据弹窗输出的单对象或对象数组。
 * @returns 打印对话框已调起的完成信号。
 * @throws 设计器尚未就绪、模板有错误级问题或排版失败。
 */
async function preview(data: Record<string, unknown> | Record<string, unknown>[]): Promise<void> {
  const designer = designerRef.value
  if (!designer)
    throw new Error(t('setting.print_template.designer_not_ready'))
  const records = normalizePrintSampleData(data).records
  designer.data = records[0]
  await previewPrintDocument(designer.schema, data)
}

function openJsonEditor(): void {
  jsonEditorError.value = ''
  exportCurrentTemplateToTextArea()
  jsonEditorVisible.value = true
}

function exportCurrentTemplateToTextArea(): void {
  try {
    jsonEditorText.value = JSON.stringify(getJson(), null, 2)
    jsonEditorError.value = ''
  }
  catch (error) {
    jsonEditorError.value = (error as Error).message
  }
}

function updateJsonEditorText(value: string): void {
  jsonEditorText.value = value
  jsonEditorError.value = ''
}

/** 回写前确认是当前格式的打印模板；赋给设计器即装载，撤销历史随之清空，按一次修改上报。 */
function applyJsonTemplate(): void {
  const designer = designerRef.value
  if (!designer || jsonApplying.value)
    return
  jsonApplying.value = true
  try {
    const document = parsePrintTemplateJson(jsonEditorText.value)
    designer.schema = document
    emit('changed', designer.schema)
    jsonEditorVisible.value = false
  }
  catch (error) {
    jsonEditorError.value = (error as Error).message
  }
  finally {
    jsonApplying.value = false
  }
}

defineExpose({ getJson, markSaved, preview, validate })
</script>

<template>
  <div class="designer-shell">
    <div class="designer-toolbar">
      <slot name="template-actions" />
      <XhButton variant="subtle" :disabled="Boolean(loadError)" @click="openJsonEditor">
        <span><Icon icon="tabler:code" /></span>
        {{ t('setting.print_template.json_template') }}
      </XhButton>
      <div class="designer-toolbar__end">
        <slot name="printer-actions" />
      </div>
    </div>

    <JsonTemplateModal
      :show="jsonEditorVisible"
      :value="jsonEditorText"
      :error="jsonEditorError"
      :applying="jsonApplying"
      :disabled="Boolean(loadError)"
      @update:show="jsonEditorVisible = $event"
      @update:value="updateJsonEditorText"
      @export="exportCurrentTemplateToTextArea"
      @apply="applyJsonTemplate"
    />

    <XhAlertRoot v-if="loadError" tone="danger" class="designer-error">
      <XhAlertIndicator>
        <Icon icon="lucide:alert-circle" width="16" />
      </XhAlertIndicator>
      <XhAlertContent>
        <XhAlertDescription>
          {{ loadError }}
        </XhAlertDescription>
      </XhAlertContent>
    </XhAlertRoot>

    <xh-print-designer
      v-if="settingsReady"
      ref="designerRef"
      class="designer-host"
      data-testid="print-designer-canvas-stage"
      @schema-change="onSchemaChange"
      @settings-change="designerSettings.onSettingsChange"
      @save="emit('save')"
    />
  </div>
</template>

<style scoped>
.designer-shell {
  display: flex;
  min-block-size: 0;
  flex-direction: column;
  gap: var(--xh-space-2);
}

/* 模板动作在左、打印机在右，放不下时整组换行 */
.designer-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.designer-toolbar__end {
  margin-inline-start: auto;
}

/* 设计器铺满自身：宿主给它定高，这里是编辑器列里的 flex-1；display 用设计器样式自带的纵向 flex */
.designer-host {
  flex: 1 1 0;
  min-block-size: 0;
}
</style>
