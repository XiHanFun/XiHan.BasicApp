/**
 * 打印模板管理页内部表单模型。
 * 职责：在元数据表单、全屏编辑器和列表页之间共享强类型状态，不作为后端 API 契约导出。
 */
import type { EnableStatus } from '@/api'

/** 设计器工作模式；单面板模式隐藏 hiprint 的面板增删入口，多面板模式完整保留。 */
export type PrintDesignerMode = 'multi' | 'single'

/** 官网快捷工具栏提供的标准纸张编码。 */
export const PRINT_PAPER_PRESETS = ['A3', 'A4', 'A5', 'B3', 'B4', 'B5'] as const

/** 标准纸张编码。 */
export type PrintPaperPreset = typeof PRINT_PAPER_PRESETS[number]

/** 工具栏当前纸张类型；CUSTOM 表示由用户输入毫米尺寸。 */
export type PrintPaperType = 'CUSTOM' | PrintPaperPreset

/** 纸张尺寸值对象，单位统一为毫米。 */
export interface PrintPaperSize {
  height: number
  width: number
}

/** 打印模板全屏编辑器表单。 */
export interface PrintTemplateFormModel {
  allowTenantUse: boolean
  /** null 表示自由模板，不绑定代码注册的数据契约。 */
  dataSourceCode: null | string
  engineVersion: string
  remark: string
  sort: number
  status: EnableStatus
  templateCode: string
  templateName: string
}
