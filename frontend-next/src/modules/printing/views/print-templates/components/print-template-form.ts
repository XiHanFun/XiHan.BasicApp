/**
 * 打印模板元数据表单与保存入参：列表页的新增、编辑弹窗与设计器的保存共用
 */
import type { PrintTemplateCreateInput, PrintTemplateDetail, PrintTemplateFormModel, PrintTemplateUpdateInput } from './models'

/**
 * 保存时由页面决定的上下文：是否在维护全局模板（只有全局模板能开放给租户）
 * 写入的一侧由登录上下文决定（平台即全局模板），6.0 不再随请求传作用域
 */
export interface PrintTemplateSaveContext {
  globalMode: boolean
}

/** 新模板的默认元数据 */
export function createDefaultPrintTemplateForm(): PrintTemplateFormModel {
  return {
    templateCode: '',
    dataSourceCode: null,
    templateName: '',
    allowTenantUse: false,
    status: 'Active',
    order: 100,
    remark: '',
  }
}

/** 从详情回填一份独立的表单对象 */
export function toPrintTemplateForm(detail: PrintTemplateDetail): PrintTemplateFormModel {
  return {
    templateCode: detail.templateCode,
    dataSourceCode: detail.dataSourceCode ?? null,
    templateName: detail.templateName,
    allowTenantUse: detail.allowTenantUse,
    status: detail.status,
    order: detail.order,
    remark: detail.remark ?? '',
  }
}

/** 模板编码与名称都填了 */
export function isPrintTemplateFormComplete(form: PrintTemplateFormModel): boolean {
  return Boolean(form.templateCode.trim() && form.templateName.trim())
}

/** 新增模板的入参 */
export function toPrintTemplateCreateInput(
  form: PrintTemplateFormModel,
  templateJson: string,
  context: PrintTemplateSaveContext,
): PrintTemplateCreateInput {
  return {
    templateCode: form.templateCode.trim(),
    templateName: form.templateName.trim(),
    dataSourceCode: form.dataSourceCode?.trim() || null,
    templateJson,
    allowTenantUse: context.globalMode && form.allowTenantUse,
    status: form.status,
    order: form.order,
    remark: form.remark.trim() || null,
  }
}

/** 更新模板的入参；编码与状态不在这里改，并发戳取自最后读到的详情 */
export function toPrintTemplateUpdateInput(
  detail: PrintTemplateDetail,
  form: PrintTemplateFormModel,
  templateJson: string,
  context: PrintTemplateSaveContext,
): PrintTemplateUpdateInput {
  return {
    id: detail.id,
    concurrencyStamp: detail.concurrencyStamp,
    templateName: form.templateName.trim(),
    dataSourceCode: form.dataSourceCode?.trim() || null,
    templateJson,
    allowTenantUse: context.globalMode && form.allowTenantUse,
    order: form.order,
    remark: form.remark.trim() || null,
  }
}
