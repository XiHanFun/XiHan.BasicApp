import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
import type { PrintTemplateFormModel } from './models'
/**
 * 打印模板编辑器会话 Composable。
 * 职责：集中管理元数据与设计草稿、未保存守卫、保存并发锁、样例预览、FIFO 直打和本地打印机偏好。
 */
import type PrintDesignerCanvas from './PrintDesignerCanvas.vue'
import type { PrintDocument } from '~/printing/designer'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { EnableStatus } from '@/api'
import { dialog, toast } from '~/composables'
import {
  createDefaultPrintSampleData,
  directPrintByCode,
  getPreferredPrinter,
  listPrinters,
  parsePrintTemplateJson,
  PrintTemplateFormatError,
  refreshPrinters,
  setPreferredPrinter,
} from '~/printing'
import { createEmptyPrintDocument } from '~/printing/designer'
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
 * @throws 必须在 Vue setup 生命周期内调用。
 */
export function usePrintTemplateEditor(
  props: Readonly<PrintTemplateEditorProps>,
  emit: PrintTemplateEditorEmit,
) {
  const { t } = useI18n()
  const designerRef = ref<InstanceType<typeof PrintDesignerCanvas> | null>(null)
  const form = ref<PrintTemplateFormModel>(createDefaultForm())
  const draftTemplate = shallowRef<PrintDocument>(createEmptyPrintDocument())
  const currentDetail = ref<PrintTemplateDetailDto | null>(null)
  const designerKey = ref(0)
  const dirty = ref(false)
  const resetting = ref(false)
  const designerReady = ref(false)
  const saveLoading = ref(false)
  const previewLoading = ref(false)
  const samplePreviewVisible = ref(false)
  const directLoading = ref(false)
  const printerLoading = ref(false)
  const printers = ref<{ label: string, value: string }[]>([])
  const selectedPrinter = ref<string | null>(null)
  let abortController = new AbortController()

  const title = computed(() => currentDetail.value
    ? t('setting.print_template.edit_title', { code: currentDetail.value.templateCode })
    : t('setting.print_template.add_title'))
  const canDirectPrint = computed(() => currentDetail.value?.status === EnableStatus.Enabled)
  /** 设计器本机草稿的键：编辑已有模板时是模板 id，不同模板的草稿不串；新建时不给，设计器按页面地址区分 */
  const draftKey = computed(() => currentDetail.value?.basicId || undefined)
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
        abortController.abort(new DOMException('打印模板编辑器已关闭。', 'AbortError'))
      }
    },
  )

  watch(form, () => {
    if (props.show && !resetting.value)
      dirty.value = true
  }, { deep: true })

  onMounted(() => window.addEventListener('beforeunload', preventUnload))
  onBeforeUnmount(() => {
    abortController.abort(new DOMException('打印模板编辑器已卸载。', 'AbortError'))
    window.removeEventListener('beforeunload', preventUnload)
    samplePreviewVisible.value = false
  })

  /** 重置本次编辑会话并重新装载设计器。 */
  async function resetEditor(): Promise<void> {
    resetting.value = true
    designerReady.value = false
    abortController.abort()
    abortController = new AbortController()
    samplePreviewVisible.value = false
    let legacyFormat = false
    try {
      currentDetail.value = props.detail
      form.value = props.detail ? toForm(props.detail) : createDefaultForm()
      if (props.detail) {
        try {
          draftTemplate.value = parsePrintTemplateJson(props.detail.templateJson, props.detail.templateCode)
        }
        catch (error) {
          if (!(error instanceof PrintTemplateFormatError))
            throw error
          // 旧格式或损坏的模板打不开：换成空白模板并明确提示，保存后覆盖原内容
          draftTemplate.value = createEmptyPrintDocument()
          legacyFormat = true
          toast.warning(t('setting.print_template.legacy_format_reset'))
        }
      }
      else {
        draftTemplate.value = createEmptyPrintDocument()
      }
      // 新建模板在用户填写稳定编码前没有可用的偏好存储键；此时保持客户端默认打印机，避免空编码阻断编辑器打开。
      selectedPrinter.value = form.value.templateCode.trim()
        ? getPreferredPrinter(form.value.templateCode)
        : null
      designerKey.value += 1
      await nextTick()
      dirty.value = legacyFormat
      void loadPrinters(false)
    }
    catch (error) {
      toast.danger((error as Error).message || t('setting.print_template.invalid_json'))
      emit('update:show', false)
    }
    finally {
      resetting.value = false
    }
  }

  /** 设计器就绪后才接收其变更事件，避免装载误标记脏状态。 */
  function onDesignerReady(): void {
    designerReady.value = true
  }

  /** 接收设计变化并标记未保存。 */
  function onDesignerChanged(template: PrintDocument): void {
    if (!designerReady.value)
      return
    draftTemplate.value = template
    dirty.value = true
  }

  /** 接收 Vue 组件模板 ref，并把宽泛的运行时实例收窄为设计器公开句柄。 */
  function setDesignerRef(instance: unknown): void {
    designerRef.value = instance as InstanceType<typeof PrintDesignerCanvas> | null
  }

  /**
   * 保存模板；loading 锁阻止重复提交，行版本用于拒绝覆盖并行编辑，模板有错误级问题时拦下。
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
    const errorCount = designerRef.value?.validate().filter(issue => issue.severity === 'error').length ?? 0
    if (errorCount > 0) {
      toast.warning(t('setting.print_template.save_blocked_issues', { count: errorCount }))
      return null
    }
    const templateSnapshot = designerRef.value?.getJson() ?? draftTemplate.value
    const templateJson = JSON.stringify(templateSnapshot)

    saveLoading.value = true
    try {
      const common = {
        scope: props.scope,
        dataSourceCode: effectiveForm.dataSourceCode?.trim() || null,
        templateName: effectiveForm.templateName.trim(),
        templateJson,
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

      resetting.value = true
      try {
        // 先记为已保存、再换上存好的那份：新建模板的草稿在页面地址那把键下，换成模板 id 之前让设计器删掉它
        designerRef.value?.markSaved()
        currentDetail.value = saved
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
      toast.danger((error as Error).message || t('setting.print_template.save_failed'))
      return null
    }
    finally {
      saveLoading.value = false
    }
  }

  /** 打开数据源驱动的模拟数据表单。 */
  function openSamplePreview(): void {
    if (designerReady.value)
      samplePreviewVisible.value = true
  }

  /**
   * 使用当前未保存设计和用户填写的内存模拟数据调起浏览器打印。
   * @param sample 模拟数据弹窗输出的单对象或对象数组。
   * @returns 打印调用完成信号。
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
      toast.danger((error as Error).message || t('setting.print_template.preview_failed'))
    }
    finally {
      previewLoading.value = false
    }
  }

  /** 先保存最新设计，再通过公共 FIFO 直打服务交给本地打印客户端。 */
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
      const sample = await createDefaultPrintSampleData(saved.dataSourceCode)
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
        toast.danger((error as Error).message || t('setting.print_template.direct_failed'))
    }
    finally {
      directLoading.value = false
    }
  }

  /** 加载或刷新客户端打印机列表；客户端不可用时保留编辑能力。 */
  async function loadPrinters(forceRefresh: boolean): Promise<void> {
    if (printerLoading.value)
      return
    printerLoading.value = true
    try {
      const values = forceRefresh ? await refreshPrinters() : await listPrinters()
      printers.value = values.map(printer => ({ label: printer.name, value: printer.name }))
    }
    catch (error) {
      printers.value = []
      if (forceRefresh)
        toast.danger((error as Error).message || t('setting.print_template.printer_load_failed'))
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
      toast.danger((error as Error).message)
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
  }
}

/** 创建新模板安全默认元数据。 */
function createDefaultForm(): PrintTemplateFormModel {
  return {
    templateCode: '',
    dataSourceCode: null,
    templateName: '',
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
    allowTenantUse: detail.allowTenantUse,
    status: detail.status,
    sort: detail.sort,
    remark: detail.remark ?? '',
  }
}
