/**
 * 打印模板元数据表单与保存入参：列表页的新增、编辑弹窗与设计器的保存共用。
 */
import type { PrintTemplateCreateDto, PrintTemplateDetailDto, PrintTemplateScope, PrintTemplateUpdateDto } from '../../../../api/print-template.types'
import type { PrintTemplateFormModel } from './models'
import { EnableStatus } from '@/api'

/** 保存时由页面决定的上下文：作用域，以及是否在维护全局模板（只有全局模板能开放给租户） */
export interface PrintTemplateSaveContext {
  globalMode: boolean
  scope: PrintTemplateScope
}

/** 新模板的默认元数据。 */
export function createDefaultPrintTemplateForm(): PrintTemplateFormModel {
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

/** 从详情回填一份独立的表单对象。 */
export function toPrintTemplateForm(detail: PrintTemplateDetailDto): PrintTemplateFormModel {
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

/** 模板编码与名称都填了。 */
export function isPrintTemplateFormComplete(form: PrintTemplateFormModel): boolean {
  return Boolean(form.templateCode.trim() && form.templateName.trim())
}

/** 新增模板的入参。 */
export function toPrintTemplateCreateInput(
  form: PrintTemplateFormModel,
  templateJson: string,
  context: PrintTemplateSaveContext,
): PrintTemplateCreateDto {
  return {
    scope: context.scope,
    templateCode: form.templateCode.trim(),
    templateName: form.templateName.trim(),
    dataSourceCode: form.dataSourceCode?.trim() || null,
    templateJson,
    allowTenantUse: context.globalMode && form.allowTenantUse,
    status: form.status,
    sort: form.sort,
    remark: form.remark.trim() || null,
  }
}

/** 更新模板的入参；编码与状态不在这里改，行版本取自最后读到的详情。 */
export function toPrintTemplateUpdateInput(
  detail: PrintTemplateDetailDto,
  form: PrintTemplateFormModel,
  templateJson: string,
  context: PrintTemplateSaveContext,
): PrintTemplateUpdateDto {
  return {
    basicId: detail.basicId,
    rowVersion: detail.rowVersion,
    scope: context.scope,
    templateName: form.templateName.trim(),
    dataSourceCode: form.dataSourceCode?.trim() || null,
    templateJson,
    allowTenantUse: context.globalMode && form.allowTenantUse,
    sort: form.sort,
    remark: form.remark.trim() || null,
  }
}
