/**
 * hiprint 设计器公共封装。
 * 职责：按需校验可选数据源、创建全屏设计器句柄并为字段素材启用拖拽。
 */
import type {
  PrintDesignerHandle,
  PrintDesignerOptions,
  PrintElementAlignAction,
  PrintElementSpacingDirection,
} from './types'
import { requirePrintDataSource } from './data-source-registry'
import { getPrintingAdapter } from './hiprint-adapter'

const MIN_ALIGN_SELECTION_COUNT = 2
const MIN_DISTRIBUTE_SELECTION_COUNT = 3
const MAX_ELEMENT_SPACING = 1000
const SUPPORTED_ALIGN_ACTIONS = new Set<PrintElementAlignAction>([
  'left',
  'vertical',
  'right',
  'top',
  'horizontal',
  'bottom',
  'distributeHor',
  'distributeVer',
])
const DISTRIBUTE_ACTIONS = new Set<PrintElementAlignAction>(['distributeHor', 'distributeVer'])

/**
 * 创建 hiprint 设计器。
 * @param options 画布、属性面板、模板和数据源配置。
 * @returns 可读取 JSON、撤销、重做与清空的控制句柄。
 * @throws 非空数据源未注册、DOM 容器不存在或 hiprint 初始化失败。
 */
export async function createPrintDesigner(options: PrintDesignerOptions): Promise<PrintDesignerHandle> {
  if (options.dataSourceCode?.trim())
    requirePrintDataSource(options.dataSourceCode)
  const adapter = await getPrintingAdapter()
  let suppressDataChanged = false
  const template = adapter.createTemplate(options.template, {
    settingContainer: options.settingContainer,
    paginationContainer: options.paginationContainer,
    history: true,
    onDataChanged: (_type: string, json: unknown) => {
      // JSON 整体替换期间会先清空再重建；抑制中间空模板，避免父组件短暂接收到无效设计。
      if (!suppressDataChanged)
        options.onDataChanged?.(json)
    },
    onUpdateError: (error: unknown) => {
      throw error instanceof Error ? error : new Error('hiprint 设计器更新失败。')
    },
  })
  template.design(options.canvas)

  /** 获取 0.0.60 模板更新方法；测试适配器缺少能力时给出明确错误。 */
  function requireUpdateMethod(): NonNullable<typeof template.update> {
    if (typeof template.update !== 'function')
      throw new Error('当前 hiprint 适配器不支持更新 JSON 模板。')
    return template.update.bind(template)
  }

  /** 获取 0.0.60 纸张旋转方法；测试适配器缺少能力时给出明确错误。 */
  function requireRotateMethod(): NonNullable<typeof template.rotatePaper> {
    if (typeof template.rotatePaper !== 'function')
      throw new Error('当前 hiprint 适配器不支持旋转纸张。')
    return template.rotatePaper.bind(template)
  }

  /** 获取 0.0.60 当前选择集读取方法，并隔离测试适配器能力差异。 */
  function requireSelectionMethod(): NonNullable<typeof template.getSelectEls> {
    if (typeof template.getSelectEls !== 'function')
      throw new Error('当前 hiprint 适配器不支持读取已选元素。')
    return template.getSelectEls.bind(template)
  }

  /** 获取 0.0.60 全选当前面板元素方法。 */
  function requireSelectAllMethod(): NonNullable<typeof template.selectAllElements> {
    if (typeof template.selectAllElements !== 'function')
      throw new Error('当前 hiprint 适配器不支持全选画布元素。')
    return template.selectAllElements.bind(template)
  }

  /** 获取 0.0.60 多选元素对齐方法。 */
  function requireAlignMethod(): NonNullable<typeof template.setElsAlign> {
    if (typeof template.setElsAlign !== 'function')
      throw new Error('当前 hiprint 适配器不支持元素对齐。')
    return template.setElsAlign.bind(template)
  }

  /** 获取 0.0.60 多选元素固定间距方法。 */
  function requireSpacingMethod(): NonNullable<typeof template.setElsSpace> {
    if (typeof template.setElsSpace !== 'function')
      throw new Error('当前 hiprint 适配器不支持元素间距调整。')
    return template.setElsSpace.bind(template)
  }

  /** 返回活动面板当前选择集数量。 */
  function getSelectedElementCount(): number {
    return requireSelectionMethod()().length
  }

  /**
   * 发布元素位置变化，使保存脏状态与 hiprint 历史记录同时更新。
   * @param reason 用于上游历史事件的操作说明，不包含模板或打印数据。
   */
  function notifyElementLayoutChanged(reason: string): void {
    const eventBus = typeof window !== 'undefined' ? window.hinnn?.event : undefined
    if (typeof eventBus?.trigger === 'function') {
      eventBus.trigger(`hiprintTemplateDataChanged_${template.id}`, reason)
      return
    }
    // 无浏览器事件总线的测试适配器仍应收到最终模板快照，避免测试与宿主行为分叉。
    options.onDataChanged?.(template.getJson())
  }

  /**
   * 对当前选择集执行官方对齐或等距分布命令。
   * @param action hiprint 0.0.60 的对齐命令。
   * @throws 命令非法、选择数量不足或适配器缺少能力时抛出友好错误。
   */
  function alignElements(action: PrintElementAlignAction): void {
    if (!SUPPORTED_ALIGN_ACTIONS.has(action))
      throw new TypeError('不支持的元素对齐命令。')

    const minimumSelectionCount = DISTRIBUTE_ACTIONS.has(action)
      ? MIN_DISTRIBUTE_SELECTION_COUNT
      : MIN_ALIGN_SELECTION_COUNT
    if (getSelectedElementCount() < minimumSelectionCount) {
      throw new RangeError(
        DISTRIBUTE_ACTIONS.has(action)
          ? '等距分布至少需要选择 3 个元素。'
          : '元素对齐至少需要选择 2 个元素。',
      )
    }

    // 命令同步作用于当前设计器实例，不读写跨实例共享状态；浏览器事件循环已串行化点击，无需额外互斥锁。
    requireAlignMethod()(action)
    notifyElementLayoutChanged('元素对齐')
  }

  /**
   * 设置当前选择集在指定方向上的固定间距。
   * @param spacing 相邻元素间距，使用 hiprint 内部点值。
   * @param direction 水平或垂直方向。
   * @throws 间距或方向非法、选择数量不足、适配器缺少能力时抛出友好错误。
   */
  function setElementSpacing(spacing: number, direction: PrintElementSpacingDirection): void {
    if (!Number.isFinite(spacing) || spacing < 0 || spacing > MAX_ELEMENT_SPACING)
      throw new RangeError(`元素间距必须在 0～${MAX_ELEMENT_SPACING} 之间。`)
    if (direction !== 'horizontal' && direction !== 'vertical')
      throw new TypeError('元素间距方向无效。')
    if (getSelectedElementCount() < MIN_ALIGN_SELECTION_COUNT)
      throw new RangeError('元素间距调整至少需要选择 2 个元素。')

    requireSpacingMethod()(spacing, direction === 'horizontal')
    notifyElementLayoutChanged('元素间距')
  }

  /**
   * 使用完整 JSON 更新设计器，并在上游更新异常时恢复原设计。
   * @param nextTemplate 已通过业务层根对象与 panels 校验的模板。
   * @param panelIndex 更新完成后选中的面板索引。
   * @throws hiprint 更新失败，或更新失败后连原模板也无法恢复。
   */
  function updateTemplate(nextTemplate: unknown, panelIndex = 0): void {
    const update = requireUpdateMethod()
    const previousTemplate = template.getJson()
    const normalizedPanelIndex = Number.isInteger(panelIndex) && panelIndex >= 0 ? panelIndex : 0
    suppressDataChanged = true

    try {
      // 先清空可确保从多面板 JSON 更新为较少面板时，不残留旧面板和旧元素。
      template.clear()
      update(nextTemplate, normalizedPanelIndex)
    }
    catch (error) {
      try {
        template.clear()
        update(previousTemplate, 0)
      }
      catch (rollbackError) {
        throw new Error('JSON 模板更新失败，且无法恢复更新前设计，请重新打开设计器。', {
          cause: rollbackError,
        })
      }
      throw error instanceof Error ? error : new Error('JSON 模板更新失败。')
    }
    finally {
      suppressDataChanged = false
    }

    // 上游 update 不稳定触发 dataChanged，这里只发布最终归一后的模板快照。
    options.onDataChanged?.(template.getJson())
  }

  return {
    template,
    alignElements,
    getJson: () => template.getJson(),
    getPanelCount: () => template.getPaneltotal(),
    getSelectedElementCount,
    undo: () => template.undo(),
    redo: () => template.redo(),
    clear: () => template.clear(),
    rotatePaper: () => {
      requireRotateMethod()()
      // 上游 rotatePaper 不触发模板变更事件，必须主动通知保存层更新 dirty 状态。
      options.onDataChanged?.(template.getJson())
    },
    selectPanel: index => template.selectPanel(index),
    selectAllElements: () => requireSelectAllMethod()(),
    setPaperPreset: paperType => template.setPaper(paperType),
    setElementSpacing,
    setCustomPaper: (width, height) => template.setPaper(width, height),
    // 缩放属于设计器工作状态；持久化 scale 可让再次编辑时延续用户最后确认的视图比例。
    setZoom: scale => template.zoom(scale, true),
    updateTemplate,
  }
}

/**
 * 为指定字段素材元素启用 hiprint 拖拽。
 * @param elements 带 tid 属性的 .ep-draggable-item 元素集合。
 * @returns 完成信号。
 * @throws hiprint 动态加载或拖拽运行时初始化失败。
 */
export async function enablePrintFieldDragging(elements: Iterable<Element>): Promise<void> {
  const adapter = await getPrintingAdapter()
  adapter.enableFieldDragging(elements)
}
