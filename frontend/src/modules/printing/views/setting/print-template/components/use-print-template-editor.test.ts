/**
 * 打印模板编辑会话（use-print-template-editor.ts）的单元测试。
 *
 * 锁定：设计器只打开已有模板，没有详情时提示找不到并关掉；本机草稿键是模板 id；
 * 保存只更新这份模板，设计取设计器里的，元数据与行版本沿用详情，存好后让设计器记为已保存。
 */
import type { PrintTemplateDetailDto } from '../../../../api/print-template.types'
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { defineComponent, reactive } from 'vue'
import { toast } from '~/composables'
import { printTemplateApi } from '../../../../api/print-template'
import { PrintTemplateScope } from '../../../../api/print-template.types'
import { usePrintTemplateEditor } from './use-print-template-editor'

const saved = vi.hoisted(() => ({
  basicId: 'tpl-1',
  templateCode: 'invoice',
  templateName: '发票',
  templateJson: '{}',
  dataSourceCode: null,
  status: 1,
  allowTenantUse: false,
  sort: 0,
  remark: null,
  rowVersion: '1',
}))

vi.mock('vue-i18n', () => ({ useI18n: () => ({ t: (key: string) => key }) }))
vi.mock('@/api', () => ({ EnableStatus: { Enabled: 1, Disabled: 0 } }))
vi.mock('~/composables', () => ({ dialog: { confirm: vi.fn() }, toast: { success: vi.fn(), warning: vi.fn(), danger: vi.fn() } }))
vi.mock('~/printing', () => ({
  createDefaultPrintSampleData: vi.fn(),
  directPrintByCode: vi.fn(),
  getPreferredPrinter: () => null,
  listPrinters: vi.fn(async () => []),
  parsePrintTemplateJson: () => ({}),
  PrintTemplateFormatError: class extends Error {},
  refreshPrinters: vi.fn(async () => []),
  setPreferredPrinter: vi.fn(),
}))
vi.mock('~/printing/designer', () => ({ createEmptyPrintDocument: () => ({}) }))
vi.mock('../../../../api/print-template', () => ({
  printTemplateApi: { update: vi.fn(async () => saved) },
}))

function setup(detail: PrintTemplateDetailDto | null, emit: (...args: unknown[]) => void = () => {}) {
  let editor!: ReturnType<typeof usePrintTemplateEditor>
  const props = reactive({ detail, globalMode: false, scope: PrintTemplateScope.Tenant, show: false })
  mount(defineComponent({
    setup() {
      editor = usePrintTemplateEditor(props, emit as never)
      return () => null
    },
  }))
  return { editor, props }
}

describe('打开', () => {
  it('没有模板详情不打开：提示找不到并关掉', async () => {
    const emit = vi.fn()
    const { props } = setup(null, emit)
    props.show = true
    await flushPromises()

    expect(toast.danger).toHaveBeenCalledWith('setting.print_template.not_found')
    expect(emit).toHaveBeenCalledWith('update:show', false)
  })

  it('本机草稿键是模板 id', async () => {
    const { editor, props } = setup({ ...saved, basicId: 'tpl-9' } as unknown as PrintTemplateDetailDto)
    props.show = true
    await flushPromises()

    expect(editor.draftKey.value).toBe('tpl-9')
  })
})

describe('保存', () => {
  it('只更新这份模板：设计取设计器里的，元数据与行版本沿用详情；存好后设计器记为已保存', async () => {
    const { editor, props } = setup({ ...saved, basicId: 'tpl-9', remark: '季度版', rowVersion: '3' } as unknown as PrintTemplateDetailDto)
    props.show = true
    await flushPromises()
    const markSaved = vi.fn()
    editor.setDesignerRef({ validate: () => [], getJson: () => ({ pages: [] }), markSaved })

    await editor.save()

    expect(printTemplateApi.update).toHaveBeenCalledWith({
      basicId: 'tpl-9',
      rowVersion: '3',
      scope: PrintTemplateScope.Tenant,
      templateName: '发票',
      dataSourceCode: null,
      templateJson: '{"pages":[]}',
      allowTenantUse: false,
      sort: 0,
      remark: '季度版',
    })
    expect(markSaved).toHaveBeenCalledTimes(1)
    expect(editor.currentDetail.value).toEqual(saved)
  })
})
