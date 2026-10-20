<!--
  打印设计器工作区。
  职责：承载 XiHan 打印设计器（<xh-print-designer>），给它字段树与样例数据（数据源的，或自由模板这次导入的），装上宿主的扩展（编辑器的操作挂进菜单栏），
  把宿主的标题与保存状态放进菜单栏右边一段（slot="menubar-end"）；画布、物料、页面模板、标尺、属性、问题列表、JSON 编辑与菜单栏都由设计器提供。
-->
<script setup lang="ts">
import type { PrintDesignerEventMap, PrintDesignerExtension, PrintDocument, XhPrintDesigner } from '~/printing/designer'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot } from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDesignerSettings } from '~/designers/settings'
import { Icon } from '~/iconify'
import {
  createDefaultPrintSampleData,
  ensureRemotePrintDataSourcesLoaded,
  getPrintDataSource,
  normalizePrintSampleData,
  previewPrintDocument,
  toPrintDataFields,
} from '~/printing'
import { ensurePrintDesigner } from '~/printing/designer'
import { PRINT_TEMPLATE_PAGE_CODE } from './models'

defineOptions({ name: 'PrintDesignerCanvas' })

const props = defineProps<{
  dataSourceCode: null | string
  template: PrintDocument
  /**
   * 本机草稿的键：编辑已有模板时是模板 id；新建不给，设计器按页面地址区分。
   * 草稿只在设置里开了自动保存时才写；宿主存好后调 markSaved()，设计器随之删掉草稿。
   */
  draftKey?: string
  /** 宿主的扩展：编辑器的操作写成命令挂进菜单栏与工具栏；引用要稳定，换引用设计器当作卸下再装上 */
  hostExtension?: PrintDesignerExtension
}>()

const emit = defineEmits<{
  changed: [template: PrintDocument]
  dirtyChange: [dirty: boolean]
  ready: []
  save: []
}>()

const { t } = useI18n()

ensurePrintDesigner()

const designerRef = ref<XhPrintDesigner | null>(null)
const loadError = ref('')

/** 设计器设置（含面板布局、工具栏与新手引导走过与否）随用户存进用户设置，与打印模板页的列设置同一条记录 */
const designerSettings = useDesignerSettings(PRINT_TEMPLATE_PAGE_CODE)
const { ready: settingsReady } = designerSettings

/** 设计器等用户设置取回后才挂上，第一次铺好时就有用户的设置；挂上时赋设置与宿主的扩展，再装载模板与数据源 */
watch(designerRef, (designer) => {
  if (!designer)
    return
  designerSettings.attach(designer)
  designer.extensions = props.hostExtension ? [props.hostExtension] : undefined
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

/** 自由模板在这次编辑里导入的字段与示例数据：后端没有存处，关掉编辑器就没了；换了数据源再换回自由模板时赋回。 */
let importedFields: PrintDesignerEventMap['fields-change']['detail']['fields'] | undefined
let importedSample: unknown

/**
 * 字段树交给物料面板与属性面板，样例数据交给画布与设计器的预览态。
 * 绑了数据源的模板用数据源的字段与缺省样例，字段由后端元数据定、不许在设计器里导入；自由模板用这次导入的。
 */
async function applyDataSource(): Promise<void> {
  const designer = designerRef.value
  if (!designer)
    return
  const code = props.dataSourceCode?.trim() || null
  if (code)
    await ensureRemotePrintDataSourcesLoaded()
  designer.fieldsReadonly = code !== null
  if (code === null) {
    designer.fields = importedFields
    designer.sampleData = importedSample
    designer.data = importedSample
    return
  }
  const source = getPrintDataSource(code)
  designer.fields = source ? toPrintDataFields(source.fields) : undefined
  const sample = source ? normalizePrintSampleData(await createDefaultPrintSampleData(code)).records[0] : undefined
  designer.sampleData = sample
  designer.data = sample
}

/** 自由模板导入了字段：设计器已换上，记下来供换回自由模板时赋回。 */
function onFieldsChange(event: Event): void {
  importedFields = (event as PrintDesignerEventMap['fields-change']).detail.fields
}

/** 导入字段时带了示例数据：设计器已换上 sampleData，记下来，并交给设计器的预览态。 */
function onSampleDataChange(event: Event): void {
  importedSample = (event as PrintDesignerEventMap['sample-data-change']).detail.sampleData
  if (designerRef.value)
    designerRef.value.data = importedSample
}

/** 编辑与撤销重做都上报最新模板；装载新模板不算修改。 */
function onSchemaChange(event: Event): void {
  const { schema, kind } = (event as PrintDesignerEventMap['schema-change']).detail
  if (kind !== 'load')
    emit('changed', schema)
}

/** 有没有未保存的修改以设计器的判定为准：改了又撤销回原样时它报干净。 */
function onDirtyChange(event: Event): void {
  emit('dirtyChange', (event as PrintDesignerEventMap['dirty-change']).detail.dirty)
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

/** 宿主的状态变了（如保存中、打印机列表到了）：让设计器重算菜单栏与工具栏上宿主命令的可用与勾选。 */
function refreshCommands(): void {
  designerRef.value?.requestUpdate()
}

defineExpose({ getJson, markSaved, preview, refreshCommands, validate })
</script>

<template>
  <div class="designer-shell">
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
      :draft-key="draftKey"
      @schema-change="onSchemaChange"
      @dirty-change="onDirtyChange"
      @settings-change="designerSettings.onSettingsChange"
      @fields-change="onFieldsChange"
      @sample-data-change="onSampleDataChange"
      @save="emit('save')"
    >
      <!-- 设计器把它转进外框，显示在菜单栏右边；这个容器常驻，宿主只换里面的内容 -->
      <div slot="menubar-end" class="designer-menubar-end">
        <slot name="menubar-end" />
      </div>
    </xh-print-designer>
  </div>
</template>

<style scoped>
.designer-shell {
  display: flex;
  min-block-size: 0;
  flex-direction: column;
  gap: var(--xh-space-2);
}

/* 设计器铺满自身：宿主给它定高，这里是编辑器列里的 flex-1；display 用设计器样式自带的纵向 flex */
.designer-host {
  flex: 1 1 0;
  min-block-size: 0;
}

/* 菜单栏右段里宿主的一行：放不下时跟着外框的右段一起收窄 */
.designer-menubar-end {
  display: flex;
  min-inline-size: 0;
  align-items: center;
  gap: var(--xh-space-2);
}
</style>
