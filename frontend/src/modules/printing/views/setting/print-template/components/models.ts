/**
 * 打印模板管理页内部表单模型。
 * 职责：在元数据表单、全屏编辑器和列表页之间共享强类型状态，不作为后端 API 契约导出。
 */
import type { EnableStatus } from '@/api'

/** 打印模板全屏编辑器表单。 */
export interface PrintTemplateFormModel {
  allowTenantUse: boolean
  /** null 表示自由模板，不绑定代码注册的数据契约。 */
  dataSourceCode: null | string
  remark: string
  sort: number
  status: EnableStatus
  templateCode: string
  templateName: string
}

/** 打印模板页的设置键：列设置、搜索设置与打印设计器的设置同一条用户设置记录。 */
export const PRINT_TEMPLATE_PAGE_CODE = 'setting.print-template'
