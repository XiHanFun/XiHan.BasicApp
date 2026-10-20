/**
 * 打印模板元数据表单与保存入参（print-template-form.ts）的单元测试。
 *
 * 锁定：新增时编码、名称、备注去掉首尾空白，空数据源记为自由模板，带上初始状态；只有维护全局模板时才能开放给租户；
 * 编辑时主键与行版本取自详情，不带编码与状态；编码与名称都填了才算填好。
 */
import type { PrintTemplateDetailDto } from '../../../../api/print-template.types'
import { describe, expect, it, vi } from 'vitest'
import { EnableStatus } from '@/api'
import { PrintTemplateScope } from '../../../../api/print-template.types'
import {
  createDefaultPrintTemplateForm,
  isPrintTemplateFormComplete,
  toPrintTemplateCreateInput,
  toPrintTemplateForm,
  toPrintTemplateUpdateInput,
} from './print-template-form'

vi.mock('@/api', () => ({ EnableStatus: { Enabled: 1, Disabled: 0 } }))

const detail = {
  basicId: 'tpl-9',
  templateCode: 'invoice',
  templateName: '发票',
  templateJson: '{"pages":[]}',
  dataSourceCode: 'sales-order',
  scope: PrintTemplateScope.Global,
  status: 1,
  allowTenantUse: true,
  sort: 5,
  remark: null,
  rowVersion: '7',
} as unknown as PrintTemplateDetailDto

describe('新增', () => {
  it('编码、名称、备注去掉首尾空白，空数据源记为自由模板，带上初始状态', () => {
    const form = {
      ...createDefaultPrintTemplateForm(),
      templateCode: '  invoice ',
      templateName: ' 发票 ',
      dataSourceCode: '  ',
      status: EnableStatus.Disabled,
      remark: '  ',
    }

    expect(toPrintTemplateCreateInput(form, '{}', { scope: PrintTemplateScope.Tenant, globalMode: false })).toEqual({
      scope: PrintTemplateScope.Tenant,
      templateCode: 'invoice',
      templateName: '发票',
      dataSourceCode: null,
      templateJson: '{}',
      allowTenantUse: false,
      status: EnableStatus.Disabled,
      sort: 100,
      remark: null,
    })
  })

  it('只有维护全局模板时才能开放给租户', () => {
    const form = { ...createDefaultPrintTemplateForm(), templateCode: 'a', templateName: 'A', allowTenantUse: true }

    expect(toPrintTemplateCreateInput(form, '{}', { scope: PrintTemplateScope.Tenant, globalMode: false }).allowTenantUse).toBe(false)
    expect(toPrintTemplateCreateInput(form, '{}', { scope: PrintTemplateScope.Global, globalMode: true }).allowTenantUse).toBe(true)
  })
})

describe('编辑', () => {
  it('主键与行版本取自详情，不带编码与状态', () => {
    const form = { ...toPrintTemplateForm(detail), templateName: ' 销售发票 ', dataSourceCode: null, sort: 8, remark: ' 季度版 ' }

    expect(toPrintTemplateUpdateInput(detail, form, detail.templateJson, { scope: PrintTemplateScope.Global, globalMode: true })).toEqual({
      basicId: 'tpl-9',
      rowVersion: '7',
      scope: PrintTemplateScope.Global,
      templateName: '销售发票',
      dataSourceCode: null,
      templateJson: '{"pages":[]}',
      allowTenantUse: true,
      sort: 8,
      remark: '季度版',
    })
  })

  it('详情回填成表单：没有备注时是空串', () => {
    expect(toPrintTemplateForm(detail)).toEqual({
      templateCode: 'invoice',
      templateName: '发票',
      dataSourceCode: 'sales-order',
      allowTenantUse: true,
      status: 1,
      sort: 5,
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
