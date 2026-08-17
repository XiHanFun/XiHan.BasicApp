import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
/**
 * 打印模板编辑器会话 Composable。
 * 职责：集中管理元数据与设计草稿、未保存守卫、保存并发锁、样例预览、FIFO 直打和本地打印机偏好。
 */
import type HiprintDesignerCanvas from './HiprintDesignerCanvas.vue'
import type { PrintTemplateFormModel } from './models'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { EnableStatus } from '@/api'
import { dialog, toast } from '~/composables'
import {
  createDefaultPrintSampleData,
  directPrintByCode,
  getPreferredPrinter,
  listPrinters,
  refreshPrinters,
  setPreferredPrinter,
} from '~/printing'
import { printTemplateApi } from '../../../../api/print-template'

/** 编辑器父组件传入的响应式属性。 */
interface PrintTemplateEditorProps {
  detail: PrintTemplateDetailDto | null
  globalMode: boolean
  scope: PrintTemplateScope
  show: boolean
}

/** 编辑器向父组件发出的事件。 */
interface PrintTemplateEditorEmit {
  (event: 'saved', detail: PrintTemplateDetailDto): void
  (event: 'update:show', value: boolean): void
}

/**
 * 创建一个打印模板编辑会话。
 * @param props 编辑器详情、作用域和显示状态；必须直接传入 defineProps 返回的响应式对象。
 * @param emit 保存完成与可见性变更事件发送器。
 * @returns 供编辑器视图绑定的状态、计算属性和操作方法。
 * @throws 必须在 Vue setup 生命周期内调用；初始化时模板 JSON 损坏会提示并关闭编辑器。
 */
export function usePrintTemplateEditor(
  props: Readonly<PrintTemplateEditorProps>,
  emit: PrintTemplateEditorEmit,
) {
  const { t } = useI18n()
  const designerRef = ref<InstanceType<typeof HiprintDesignerCanvas> | null>(null)
  const form = ref<PrintTemplateFormModel>(createDefaultForm())
  const draftTemplate = ref<Record<string, unknown>>(createEmptyTemplate())
  const currentDetail = ref<PrintTemplateDetailDto | null>(null)
  const designerKey = ref(0)
  const dirty = ref(false)
  const resetting = ref(false)
  const designerReady = ref(false)
  const saveLoading = ref(false)
  const previewLoading = ref(false)
  const samplePreviewVisible = ref(false)
  const samplePreviewTemplate = ref<Record<string, unknown> | null>(null)
  const directLoading = ref(false)
  const printerLoading = ref(false)
  const printers = ref<{ label: string, value: string }[]>([])
  const selectedPrinter = ref<string | null>(null)
  let abortController = new AbortController()

  const title = computed(() => currentDetail.value
    ? t('setting.print_template.edit_title', { code: currentDetail.value.templateCode })
    : t('setting.print_template.add_title'))
  const canDirectPrint = computed(() => currentDetail.value?.status === EnableStatus.Enabled)
  const printerOptions = computed(() => [
    { label: t('setting.print_template.client_default_printer'), value: '' },
    ...printers.value,
  ])

  watch(
    () => props.show,
    (show) => {
      if (show) {
        void resetEditor()
      }
      else {
        samplePreviewVisible.value = false
        samplePreviewTemplate.value = null
        abortController.abort(new DOMException('打印模板编辑器已关闭。', 'AbortError'))
      }
    },
  )

  watch(form, () => {
    if (props.show && !resetting.value)
      dirty.value = true
  }, { deep: true })

  watch(
    () => form.value.dataSourceCode,
    (nextCode, previousCode) => {
      if (!props.show || resetting.value || nextCode === previousCode)
        return
      try {
        // 数据源切换会通过组件 key 重建设计器；销毁前同步抓取画布，避免未保存元素随旧实例一起丢失。
        draftTemplate.value = (designerRef.value?.getJson() ?? draftTemplate.value) as Record<string, unknown>
        designerReady.value = false
      }
      catch (error) {
        toast.error((error as Error).message || t('setting.print_template.designer_not_ready'))
      }
    },
    { flush: 'sync' },
  )

  onMounted(() => window.addEventListener('beforeunload', preventUnload))
  onBeforeUnmount(() => {
    abortController.abort(new DOMException('打印模板编辑器已卸载。', 'AbortError'))
    window.removeEventListener('beforeunload', preventUnload)
    samplePreviewVisible.value = false
    samplePreviewTemplate.value = null
  })

  /** 重置本次编辑会话并重新构建设计器。 */
  async function resetEditor(): Promise<void> {
    resetting.value = true
    designerReady.value = false
    abortController.abort()
    abortController = new AbortController()
    samplePreviewVisible.value = false
    samplePreviewTemplate.value = null
    try {
      currentDetail.value = props.detail
      form.value = props.detail ? toForm(props.detail) : createDefaultForm()
      draftTemplate.value = props.detail ? parseTemplate(props.detail.templateJson) : createEmptyTemplate()
      // 新建模板在用户填写稳定编码前没有可用的偏好存储键；此时保持客户端默认打印机，避免空编码阻断编辑器打开。
      selectedPrinter.value = form.value.templateCode.trim()
        ? getPreferredPrinter(form.value.templateCode)
        : null
      designerKey.value += 1
      await nextTick()
      dirty.value = false
      void loadPrinters(false)
    }
    catch (error) {
      toast.error((error as Error).message || t('setting.print_template.invalid_json'))
      emit('update:show', false)
    }
    finally {
      resetting.value = false
    }
  }

  /** 设计器就绪后才接收其变更事件，避免初始化布局误标记脏状态。 */
  function onDesignerReady(): void {
    designerReady.value = true
  }

  /** 接收设计变化并标记未保存。 */
  function onDesignerChanged(template: unknown): void {
    if (!designerReady.value)
      return
    draftTemplate.value = template as Record<string, unknown>
    dirty.value = true
  }

  /** 接收 Vue 组件模板 ref，并把宽泛的运行时实例收窄为设计器公开句柄。 */
  function setDesignerRef(instance: unknown): void {
    designerRef.value = instance as InstanceType<typeof HiprintDesignerCanvas> | null
  }

  /**
   * 保存模板；loading 锁阻止重复提交，行版本用于拒绝覆盖并行编辑。
   * @param metadataOverride 可选的设置抽屉隔离草稿；只有 API 成功后才写回主表单。
   * @returns 保存后的模板详情；校验失败、并发提交或接口失败时返回 null。
   */
  async function save(metadataOverride?: PrintTemplateFormModel): Promise<PrintTemplateDetailDto | null> {
    if (saveLoading.value)
      return null
    const effectiveForm = metadataOverride ?? form.value
    if (!effectiveForm.templateCode.trim() || !effectiveForm.templateName.trim()) {
      toast.warning(t('setting.print_template.required_fields'))
      return null
    }
    const templateSnapshot = (designerRef.value?.getJson() ?? draftTemplate.value) as Record<string, unknown>
    const templateJson = JSON.stringify(templateSnapshot)
    const previousDataSourceCode = form.value.dataSourceCode?.trim() || null

    saveLoading.value = true
    try {
      const common = {
        scope: props.scope,
        dataSourceCode: effectiveForm.dataSourceCode?.trim() || null,
        templateName: effectiveForm.templateName.trim(),
        templateJson,
        engineVersion: effectiveForm.engineVersion.trim(),
        allowTenantUse: props.globalMode && effectiveForm.allowTenantUse,
        sort: effectiveForm.sort,
        remark: effectiveForm.remark.trim() || null,
      }
      const saved = currentDetail.value
        ? await printTemplateApi.update({
            ...common,
            basicId: currentDetail.value.basicId,
            rowVersion: currentDetail.value.rowVersion,
          })
        : await printTemplateApi.create({
            ...common,
            templateCode: effectiveForm.templateCode.trim(),
            status: effectiveForm.status,
          })

      const dataSourceChanged = previousDataSourceCode !== (saved.dataSourceCode?.trim() || null)
      resetting.value = true
      try {
        currentDetail.value = saved
        // 数据源改变会触发 DesignerCanvas key 重建；保存当前快照，确保新实例回显刚保存的画布。
        if (dataSourceChanged) {
          draftTemplate.value = templateSnapshot
          designerReady.value = false
        }
        form.value = toForm(saved)
        await nextTick()
        dirty.value = false
      }
      finally {
        resetting.value = false
      }
      selectedPrinter.value = getPreferredPrinter(saved.templateCode)
      emit('saved', saved)
      toast.success(t('setting.print_template.save_success'))
      return saved
    }
    catch (error) {
      toast.error((error as Error).message || t('setting.print_template.save_failed'))
      return null
    }
    finally {
      saveLoading.value = false
    }
  }

  /** 读取当前未保存画布快照并打开模板驱动的模拟数据表单。 */
  function openSamplePreview(): void {
    if (!designerReady.value)
      return
    try {
      samplePreviewTemplate.value = (designerRef.value?.getJson() ?? draftTemplate.value) as Record<string, unknown>
      samplePreviewVisible.value = true
    }
    catch (error) {
      toast.error((error as Error).message || t('setting.print_template.preview_failed'))
    }
  }

  /**
   * 使用当前未保存画布和用户填写的内存模拟数据打开浏览器预览。
   * @param sample 模拟数据弹窗输出的单对象或对象数组。
   * @returns 预览调用完成信号。
   */
  async function preview(sample: Record<string, unknown> | Record<string, unknown>[]): Promise<void> {
    if (previewLoading.value)
      return
    previewLoading.value = true
    try {
      await designerRef.value?.preview(sample)
      samplePreviewVisible.value = false
    }
    catch (error) {
      toast.error((error as Error).message || t('setting.print_template.preview_failed'))
    }
    finally {
      previewLoading.value = false
    }
  }

  /** 先保存最新设计，再通过公共 FIFO 直打服务提交内存样例。 */
  async function directPrint(): Promise<void> {
    if (directLoading.value)
      return
    directLoading.value = true
    try {
      const saved = dirty.value || !currentDetail.value ? await save() : currentDetail.value
      if (!saved)
        return
      if (saved.status !== EnableStatus.Enabled)
        throw new Error(t('setting.print_template.disabled_cannot_print'))
      const sample = await createDefaultPrintSampleData(parseTemplate(saved.templateJson), saved.dataSourceCode)
      await directPrintByCode(saved.templateCode, sample, {
        scope: props.scope,
        printerName: selectedPrinter.value || undefined,
        title: saved.templateName,
        signal: abortController.signal,
      })
      toast.success(t('setting.print_template.direct_success'))
    }
    catch (error) {
      if ((error as Error).name !== 'AbortError')
        toast.error((error as Error).message || t('setting.print_template.direct_failed'))
    }
    finally {
      directLoading.value = false
    }
  }

  /** 加载或刷新客户端打印机列表；离线时保留编辑能力。 */
  async function loadPrinters(forceRefresh: boolean): Promise<void> {
    if (printerLoading.value)
      return
    printerLoading.value = true
    try {
      const values = forceRefresh ? await refreshPrinters() : await listPrinters()
      printers.value = values.map(printer => ({ label: printer.displayName || printer.name, value: printer.name }))
    }
    catch (error) {
      printers.value = []
      if (forceRefresh)
        toast.error((error as Error).message || t('setting.print_template.printer_load_failed'))
    }
    finally {
      printerLoading.value = false
    }
  }

  /** 更新本地偏好；空字符串表示交给客户端默认打印机。 */
  async function updatePrinterPreference(value: null | string): Promise<void> {
    selectedPrinter.value = value || null
    if (!form.value.templateCode.trim())
      return
    try {
      await setPreferredPrinter(form.value.templateCode, selectedPrinter.value)
    }
    catch (error) {
      toast.error((error as Error).message)
    }
  }

  /** 拦截关闭并在存在未保存变更时请求确认。 */
  async function requestVisible(value: boolean): Promise<void> {
    if (value || await confirmDiscard())
      emit('update:show', value)
  }

  /** 返回是否允许丢弃当前未保存更改，供路由离开守卫复用。 */
  function confirmDiscard(): Promise<boolean> {
    if (!dirty.value)
      return Promise.resolve(true)
    // confirm 本身以布尔兑现：确定为 true，取消与关闭都是 false
    return dialog.confirm({
      badge: 'warning',
      title: t('setting.print_template.unsaved_title'),
      content: t('setting.print_template.unsaved_content'),
      okText: t('setting.print_template.discard'),
      cancelText: t('common.actions.cancel'),
    })
  }

  /** 浏览器刷新/关闭时使用原生未保存提示。 */
  function preventUnload(event: BeforeUnloadEvent): void {
    if (!props.show || !dirty.value)
      return
    event.preventDefault()
    event.returnValue = ''
  }

  return {
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
  }
}

/** 创建新模板安全默认元数据。 */
function createDefaultForm(): PrintTemplateFormModel {
  return {
    templateCode: '',
    dataSourceCode: null,
    templateName: '',
    engineVersion: '0.0.60',
    allowTenantUse: false,
    status: EnableStatus.Enabled,
    sort: 100,
    remark: '',
  }
}

/** 从详情创建独立表单对象。 */
function toForm(detail: PrintTemplateDetailDto): PrintTemplateFormModel {
  return {
    templateCode: detail.templateCode,
    dataSourceCode: detail.dataSourceCode,
    templateName: detail.templateName,
    engineVersion: detail.engineVersion,
    allowTenantUse: detail.allowTenantUse,
    status: detail.status,
    sort: detail.sort,
    remark: detail.remark ?? '',
  }
}

/** A4 纵向纸张宽度，单位 mm。 */
const A4_PAPER_WIDTH_MM = 210
/** A4 纵向纸张高度，单位 mm。 */
const A4_PAPER_HEIGHT_MM = 297
/** A4 纵向纸张高度，单位 pt，作为默认页脚参考线。 */
const A4_PAPER_HEIGHT_PT = 841.89
/** 官网空白模板采用的 20mm 页眉参考线，单位 pt。 */
const DEFAULT_PAPER_HEADER_PT = 56.69

/** 创建后端校验和 hiprint 均可接受、且带默认页眉参考线的 A4 空白模板。 */
function createEmptyTemplate(): Record<string, unknown> {
  return {
    panels: [{
      index: 0,
      paperType: 'A4',
      width: A4_PAPER_WIDTH_MM,
      height: A4_PAPER_HEIGHT_MM,
      paperHeader: DEFAULT_PAPER_HEADER_PT,
      paperFooter: A4_PAPER_HEIGHT_PT,
      printElements: [],
    }],
  }
}

/** 解析详情 JSON；异常由页面统一提示并阻止打开损坏设计。 */
function parseTemplate(value: string): Record<string, unknown> {
  const parsed = JSON.parse(value) as unknown
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
    throw new Error('打印模板 JSON 根节点必须是对象。')
  return parsed as Record<string, unknown>
}
