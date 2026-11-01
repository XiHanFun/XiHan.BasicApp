/**
 * 打印模板管理页内部表单模型与契约投影。
 * 职责：在列表页的新增、编辑弹窗与元数据表单之间共享强类型状态；行、详情与入参都取自后端契约（~/contracts），这里不另写字段。
 */
import type { RequestOf, ResponseOf } from '~/contracts'

/** 列表行（不带模板 JSON）。 */
export type PrintTemplateRow = ResponseOf<'print-templates.list'>['items'][number]

/** 模板详情（带模板 JSON 与并发戳）。 */
export type PrintTemplateDetail = ResponseOf<'print-templates.get'>

/** 模板状态。 */
export type PrintTemplateStatus = PrintTemplateRow['status']

/** 作用域：平台只有 Global；租户在本租户（Tenant）与可用的全局模板（Global）之间切换，解析时另有 Auto。 */
export type PrintTemplateScope = ResponseOf<'print-templates.resolve'>['resolvedScope']

/** 新建模板的入参。 */
export type PrintTemplateCreateInput = RequestOf<'print-templates.create'>

/** 修改模板的入参。 */
export type PrintTemplateUpdateInput = RequestOf<'print-templates.update'>

/** 打印模板元数据表单。 */
export interface PrintTemplateFormModel {
  allowTenantUse: boolean
  /** null 表示自由模板，不绑定代码注册的数据契约。 */
  dataSourceCode: null | string
  remark: string
  order: number
  status: PrintTemplateStatus
  templateCode: string
  templateName: string
}

/** 打印模板页的设置键：列设置、搜索设置与打印设计器的设置同一条用户设置记录。 */
export const PRINT_TEMPLATE_PAGE_CODE = 'printing.print-templates'
