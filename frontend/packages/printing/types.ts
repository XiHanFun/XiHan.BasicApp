/**
 * 打印公共能力类型契约。
 * 职责：描述代码数据源、模板解析、浏览器预览、本机直打和可替换适配器边界。
 */

/** 与后端 PrintTemplateScope 字符串枚举保持一致的作用域。 */
export type PrintTemplateScope = 'Auto' | 'Global' | 'Tenant'

/** 打印字段支持的 hiprint 元素类型。 */
export type PrintFieldKind = 'barcode' | 'image' | 'qrcode' | 'table' | 'text'

/** 模拟打印数据表单支持的输入控件类型。 */
export type PrintSampleInputType = 'boolean' | 'date' | 'datetime' | 'number' | 'text' | 'textarea'

/** hiprint 0.0.60 支持的多选元素对齐与分布命令。 */
export type PrintElementAlignAction
  = | 'bottom'
    | 'distributeHor'
    | 'distributeVer'
    | 'horizontal'
    | 'left'
    | 'right'
    | 'top'
    | 'vertical'

/** 多选元素固定间距调整方向。 */
export type PrintElementSpacingDirection = 'horizontal' | 'vertical'

/** 明细表列定义。 */
export interface PrintTableColumn {
  /** 数据字段路径。 */
  field: string
  /** 设计器与表头标题。 */
  title: string
  /** 可选列宽，单位为 pt。 */
  width?: number
  /** 模拟数据表格单元格类型；省略时根据样例值推断。 */
  inputType?: PrintSampleInputType
  /** 模拟数据单元格占位文案。 */
  placeholder?: string
}

/** 数据源字段定义。 */
export interface PrintFieldDefinition {
  /** 数据字段路径；同一数据源内唯一。 */
  key: string
  /** 设计器素材标题。 */
  label: string
  /** 字段元素类型，默认 text。 */
  kind?: PrintFieldKind
  /** table 类型的明细列；其它类型忽略。 */
  columns?: readonly PrintTableColumn[]
  /** 模拟数据表单控件类型；图片、条码、二维码和表格仍优先遵循 kind。 */
  inputType?: PrintSampleInputType
  /** 模拟数据表单占位文案。 */
  placeholder?: string
}

/** 从模板标准字段绑定生成的模拟数据表单项。 */
export interface PrintSampleFormField {
  /** 明细表列定义；仅 table 类型存在。 */
  columns?: readonly PrintTableColumn[]
  /** 数据字段路径。 */
  key: string
  /** 字段在打印模板中的元素类型。 */
  kind: PrintFieldKind
  /** 表单标签；未注册字段回退为原始字段路径。 */
  label: string
  /** 可选显式输入控件；省略时由运行时样例值推断。 */
  inputType?: PrintSampleInputType
  /** 表单占位文案。 */
  placeholder?: string
  /** 字段是否来自当前代码数据源注册表。 */
  registered: boolean
}

/** 模板驱动的模拟数据表单结构。 */
export interface PrintSampleFormSchema {
  /** 已按面板和视觉坐标排序并去重的字段。 */
  fields: readonly PrintSampleFormField[]
  /** 不阻断预览但应在表单中向用户展示的结构提示。 */
  warnings: readonly string[]
}

/** 归一化后的模拟打印记录集合。 */
export interface PrintSampleDataSet {
  /** createSampleData 原始返回是否为顶层数组。 */
  isCollection: boolean
  /** 至少包含一条、彼此独立的可编辑记录。 */
  records: Record<string, unknown>[]
}

/** 代码注册的可选打印数据契约，用于提供字段素材、类型元数据和默认样例。 */
export interface PrintDataSourceDefinition<T = Record<string, unknown>> {
  /** 与后端模板 DataSourceCode 一致的稳定编码。 */
  code: string
  /** 数据源显示名称。 */
  name: string
  /** 设计器字段目录。 */
  fields: readonly PrintFieldDefinition[]
  /** 设计阶段生成样例数据；返回值仅保存在浏览器内存。 */
  createSampleData: () => Promise<T | T[]> | T | T[]
}

/** 后端按编码解析后的模板。 */
export interface ResolvedPrintTemplate {
  basicId: string
  /** 可选代码数据源；null 表示模板按自身标准 field 绑定自由接收数据。 */
  dataSourceCode: null | string
  engineVersion: string
  requestedScope: PrintTemplateScope
  resolvedScope: PrintTemplateScope
  rowVersion: string
  templateCode: string
  templateJson: string
  templateName: string
}

/** 浏览器预览选项。 */
export interface PreviewPrintOptions {
  /** 默认 Auto。 */
  scope?: PrintTemplateScope
  /** 浏览器打印任务标题。 */
  title?: string
  /** 打开模拟数据表单时读取的行版本；不一致时拒绝使用旧表单预览新模板。 */
  expectedRowVersion?: string
}

/** 本机直打选项。 */
export interface DirectPrintOptions extends PreviewPrintOptions {
  /** 打印份数，范围 1～99。 */
  copies?: number
  /** 调用方明确指定的打印机名称。 */
  printerName?: string
  /** 超时时间，默认 30 秒。 */
  timeoutMs?: number
  /** 调用组件卸载时可通过 AbortController 取消尚未完成的等待。 */
  signal?: AbortSignal
}

/** electron-hiprint 返回的打印机摘要。 */
export interface PrintDevice {
  clientId?: string
  description?: string
  displayName?: string
  isDefault?: boolean
  name: string
  status?: number | string
  [key: string]: unknown
}

/** 直打任务完成结果。 */
export interface PrintJobResult {
  /** 打印份数。 */
  copies: number
  /** 客户端事件原始载荷，仅在当前内存中返回。 */
  payload?: unknown
  /** 实际传给客户端的打印机；空字符串表示客户端默认打印机。 */
  printerName: string
  /** 后端实际命中的模板作用域。 */
  resolvedScope: PrintTemplateScope
  /** 模板编码。 */
  templateCode: string
}

/** 打印包运行配置，由应用启动层注入后端解析函数。 */
export interface PrintingConfiguration {
  /** electron-hiprint 或中转服务地址。 */
  host?: string
  /** 客户端鉴权 token；禁止写入日志或异常消息。 */
  token?: string
  /** 调用后端按编码解析模板。 */
  resolveTemplate: (templateCode: string, scope: PrintTemplateScope) => Promise<ResolvedPrintTemplate>
  /** 拉取后端数据源目录（可选；未注入时仅本地注册的数据源可用）。 */
  listDataSources?: () => Promise<RemotePrintDataSourceDto[]>
}

/** 后端数据源目录项（与 Printing 模块 PrintDataSourceDto 一一对应）。 */
export interface RemotePrintDataSourceDto {
  /** 数据源编码。 */
  code: string
  /** 设计器展示名称。 */
  name: string
  /** 字段清单。 */
  fields: RemotePrintDataSourceFieldDto[]
  /** 静态样例数据 JSON。 */
  sampleDataJson: string
}

/** 后端数据源字段。 */
export interface RemotePrintDataSourceFieldDto {
  key: string
  label: string
  kind: PrintFieldKind
  columns?: null | { field: string, title: string, width?: null | number, inputType?: null | string }[]
  inputType?: null | string
  placeholder?: null | string
}

/** hiprint 模板实例最小接口，隔离无官方 TypeScript 声明的第三方实现。 */
export interface HiprintTemplateInstance {
  id: string
  clientIsOpened: () => boolean
  clear: () => void
  design: (container: string | HTMLElement) => void
  getJson: () => unknown
  /** 返回模板当前拥有的面板数量。 */
  getPaneltotal: () => number
  /** 返回当前活动面板已选中的元素；vue-plugin-hiprint 0.0.60 提供，但测试适配器可省略。 */
  getSelectEls?: () => unknown[]
  getPrinterList: () => PrintDevice[]
  on: (eventName: 'printError' | 'printSuccess', callback: (payload: unknown) => void) => void
  print: (data: unknown, options?: Record<string, unknown>, styleOptions?: Record<string, unknown>) => void
  print2: (data: unknown, options?: Record<string, unknown>) => void
  redo: () => void
  /** 旋转当前活动面板；vue-plugin-hiprint 0.0.60 提供，但测试适配器可省略。 */
  rotatePaper?: () => void
  /** 按零基索引选择活动面板。 */
  selectPanel: (index: number) => void
  /** 选择当前活动面板全部非表格元素；测试适配器可省略。 */
  selectAllElements?: () => void
  /** 对当前多选元素执行边缘、中心或等距分布；测试适配器可省略。 */
  setElsAlign?: (action: PrintElementAlignAction) => void
  /** 设置当前多选元素之间的固定间距；第二个参数为 true 时调整水平方向。 */
  setElsSpace?: (spacing: number, horizontal: boolean) => void
  /** 切换标准纸张，或使用宽高数值设置自定义纸张。 */
  setPaper: (paperTypeOrWidth: number | string, height?: number) => void
  undo: () => void
  /** 用新的根模板对象更新设计器，并可指定更新后的活动面板。 */
  update?: (template: unknown, panelIndex?: number) => void
  /** 设置当前活动面板的设计视图缩放比例。 */
  zoom: (scale: number, persist?: boolean) => void
}

/** 可替换 hiprint 适配器，便于无物理打印机环境进行单元测试。 */
export interface PrintingAdapter {
  /** 创建模板实例。 */
  createTemplate: (template: unknown, options?: Record<string, unknown>) => HiprintTemplateInstance
  /** 激活字段素材拖拽。 */
  enableFieldDragging: (elements: Iterable<Element>) => void
  /** 当前客户端是否连接。 */
  isClientConnected: () => boolean
  /** 读取当前打印机快照。 */
  listPrinters: () => PrintDevice[]
  /** 请求客户端刷新打印机并等待列表事件。 */
  refreshPrinters: (timeoutMs?: number) => Promise<PrintDevice[]>
  /** 清理某个模板实例的打印完成/失败监听器。 */
  removePrintListeners: (template: HiprintTemplateInstance) => void
}

/** 设计器创建选项。 */
export interface PrintDesignerOptions {
  canvas: string | HTMLElement
  /** 可选代码数据源；为空时只启用 hiprint 通用素材与自由字段。 */
  dataSourceCode: null | string
  onDataChanged?: (template: unknown) => void
  paginationContainer?: string
  settingContainer: string
  template: unknown
}

/** 设计器控制句柄。 */
export interface PrintDesignerHandle {
  /** 对当前多选元素执行边缘、中心或等距分布。 */
  alignElements: (action: PrintElementAlignAction) => void
  clear: () => void
  getJson: () => unknown
  /** 返回模板当前面板数量。 */
  getPanelCount: () => number
  /** 返回当前活动面板的已选元素数量。 */
  getSelectedElementCount: () => number
  redo: () => void
  /** 旋转当前活动面板，并同步触发设计变更回调。 */
  rotatePaper: () => void
  /** 按零基索引选择活动面板。 */
  selectPanel: (index: number) => void
  /** 选择当前活动面板全部非表格元素，供对齐与间距快捷操作使用。 */
  selectAllElements: () => void
  /** 使用毫米宽高设置活动面板的自定义纸张。 */
  setCustomPaper: (width: number, height: number) => void
  /** 使用 hiprint 内置纸张编码调整活动面板。 */
  setPaperPreset: (paperType: string) => void
  /** 按指定方向设置当前多选元素的固定间距。 */
  setElementSpacing: (spacing: number, direction: PrintElementSpacingDirection) => void
  /** 设置活动面板缩放比例并写入设计 JSON。 */
  setZoom: (scale: number) => void
  template: HiprintTemplateInstance
  undo: () => void
  /** 使用完整 hiprint 根对象替换当前设计；失败时尝试恢复替换前内容。 */
  updateTemplate: (template: unknown, panelIndex?: number) => void
}
