/**
 * 打印模板元数据表单与保存入参（print-template-form.ts）的单元测试
 *
 * 锁定：新增时编码、名称、备注去掉首尾空白，空数据源记为自由模板，带上初始状态；只有维护全局模板时才能开放给租户；
 * 编辑时主键与并发戳取自详情，不带编码与状态；编码与名称都填了才算填好。写入的一侧由登录上下文决定，入参里没有作用域
 */
import type { PrintTemplateDetail, PrintTemplateFormModel } from './models'
import { describe, expect, it } from 'vitest'
import {
  createDefaultPrintTemplateForm,
  isPrintTemplateFormComplete,
  toPrintTemplateCreateInput,
  toPrintTemplateForm,
  toPrintTemplateUpdateInput,
} from './print-template-form'

const detail = {
  id: 'tpl-9',
  isGlobal: true,
  templateCode: 'invoice',
  templateName: '发票',
  templateJson: '{"pages":[]}',
  dataSourceCode: 'sales-order',
  status: 'Active',
  allowTenantUse: true,
  order: 5,
  remark: null,
  concurrencyStamp: 'stamp-7',
  createdAt: '2026-11-01T00:00:00Z',
  modifiedAt: null,
} satisfies PrintTemplateDetail

describe('新增', () => {
  it('编码、名称、备注去掉首尾空白，空数据源记为自由模板，带上初始状态', () => {
    const form: PrintTemplateFormModel = {
      ...createDefaultPrintTemplateForm(),
      templateCode: '  invoice ',
      templateName: ' 发票 ',
      dataSourceCode: '  ',
      status: 'Disabled',
      remark: '  ',
    }

    expect(toPrintTemplateCreateInput(form, '{}', { globalMode: false })).toEqual({
      templateCode: 'invoice',
      templateName: '发票',
      dataSourceCode: null,
      templateJson: '{}',
      allowTenantUse: false,
      status: 'Disabled',
      order: 100,
      remark: null,
    })
  })

  it('只有维护全局模板时才能开放给租户', () => {
    const form = { ...createDefaultPrintTemplateForm(), templateCode: 'a', templateName: 'A', allowTenantUse: true }

    expect(toPrintTemplateCreateInput(form, '{}', { globalMode: false }).allowTenantUse).toBe(false)
    expect(toPrintTemplateCreateInput(form, '{}', { globalMode: true }).allowTenantUse).toBe(true)
  })
})

describe('编辑', () => {
  it('主键与并发戳取自详情，不带编码与状态', () => {
    const form = { ...toPrintTemplateForm(detail), templateName: ' 销售发票 ', dataSourceCode: null, order: 8, remark: ' 季度版 ' }

    expect(toPrintTemplateUpdateInput(detail, form, detail.templateJson, { globalMode: true })).toEqual({
      id: 'tpl-9',
      concurrencyStamp: 'stamp-7',
      templateName: '销售发票',
      dataSourceCode: null,
      templateJson: '{"pages":[]}',
      allowTenantUse: true,
      order: 8,
      remark: '季度版',
    })
  })

  it('详情回填成表单：没有备注时是空串', () => {
    expect(toPrintTemplateForm(detail)).toEqual({
      templateCode: 'invoice',
      templateName: '发票',
      dataSourceCode: 'sales-order',
      allowTenantUse: true,
      status: 'Active',
      order: 5,
      remark: '',
    })
  })
})

describe('必填', () => {
  it('编码与名称去掉空白后都不为空才算填好', () => {
    const form = createDefaultPrintTemplateForm()

    expect(isPrintTemplateFormComplete(form)).toBe(false)
    expect(isPrintTemplateFormComplete({ ...form, templateCode: 'a', templateName: '  ' })).toBe(false)
    expect(isPrintTemplateFormComplete({ ...form, templateCode: 'a', templateName: 'A' })).toBe(true)
  })
})
