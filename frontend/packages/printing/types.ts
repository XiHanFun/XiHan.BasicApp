/**
 * 打印公共能力类型契约。
 * 职责：描述代码数据源、模板解析、浏览器打印与本机客户端直打的边界。
 */

/** 与后端 PrintTemplateScope 字符串枚举保持一致的作用域。 */
export type PrintTemplateScope = 'Auto' | 'Global' | 'Tenant'

/** 打印字段的内容种类：文本、图片、条码、二维码或明细表。 */
export type PrintFieldKind = 'barcode' | 'image' | 'qrcode' | 'table' | 'text'

/** 模拟打印数据表单支持的输入控件类型。 */
export type PrintSampleInputType = 'boolean' | 'date' | 'datetime' | 'number' | 'text' | 'textarea'

/** 明细表列定义。 */
export interface PrintTableColumn {
  /** 数据字段路径。 */
  field: string
  /** 列标题。 */
  title: string
  /** 可选列宽；后端数据源目录带来的值原样保留。 */
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
  /** 字段显示名称。 */
  label: string
  /** 字段内容种类，默认 text。 */
  kind?: PrintFieldKind
  /** table 类型的明细列；其它类型忽略。 */
  columns?: readonly PrintTableColumn[]
  /** 模拟数据表单控件类型；图片、条码、二维码和表格仍优先遵循 kind。 */
  inputType?: PrintSampleInputType
  /** 模拟数据表单占位文案。 */
  placeholder?: string
}

/** 模拟数据表单项。 */
export interface PrintSampleFormField {
  /** 明细表列定义；仅 table 类型存在。 */
  columns?: readonly PrintTableColumn[]
  /** 数据字段路径。 */
  key: string
  /** 字段内容种类。 */
  kind: PrintFieldKind
  /** 表单标签。 */
  label: string
  /** 可选显式输入控件；省略时由运行时样例值推断。 */
  inputType?: PrintSampleInputType
  /** 表单占位文案。 */
  placeholder?: string
  /** 字段是否来自当前代码数据源注册表。 */
  registered: boolean
}

/** 模拟数据表单结构。 */
export interface PrintSampleFormSchema {
  /** 按数据源字段顺序排列的字段。 */
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

/** 代码注册的可选打印数据契约，用于提供可绑定字段、类型元数据和默认样例。 */
export interface PrintDataSourceDefinition<T = Record<string, unknown>> {
  /** 与后端模板 DataSourceCode 一致的稳定编码。 */
  code: string
  /** 数据源显示名称。 */
  name: string
  /** 设计器可绑定的字段目录。 */
  fields: readonly PrintFieldDefinition[]
  /** 设计阶段生成样例数据；返回值仅保存在浏览器内存。 */
  createSampleData: () => Promise<T | T[]> | T | T[]
}

/** 后端按编码解析后的模板。 */
export interface ResolvedPrintTemplate {
  basicId: string
  /** 可选代码数据源；null 表示模板按自身字段绑定自由接收数据。 */
  dataSourceCode: null | string
  requestedScope: PrintTemplateScope
  resolvedScope: PrintTemplateScope
  rowVersion: string
  templateCode: string
  templateJson: string
  templateName: string
}

/** 浏览器打印选项。 */
export interface PreviewPrintOptions {
  /** 默认 Auto。 */
  scope?: PrintTemplateScope
  /** 打印对话框与另存 PDF 时的缺省文件名，缺省为模板名称。 */
  title?: string
  /** 打开模拟数据表单时读取的行版本；不一致时拒绝使用旧表单打印新模板。 */
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

/** 本地打印客户端返回的打印机摘要。 */
export interface PrintDevice {
  name: string
  isDefault: boolean
  /** 打印机状态：ready、offline 等。 */
  status?: string
}

/** 直打任务完成结果。 */
export interface PrintJobResult {
  /** 打印份数。 */
  copies: number
  /** 实际交给客户端的打印机；空字符串表示客户端的默认打印机。 */
  printerName: string
  /** 后端实际命中的模板作用域。 */
  resolvedScope: PrintTemplateScope
  /** 模板编码。 */
  templateCode: string
}

/** 打印包运行配置，由应用启动层注入后端解析函数。 */
export interface PrintingConfiguration {
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
  columns?: null | { field: string, title: string, width?: null | number, inputType?: null | string, placeholder?: null | string }[]
  inputType?: null | string
  placeholder?: null | string
}
