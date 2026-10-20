/**
 * 打印模板编辑会话（use-print-template-editor.ts）的单元测试。
 *
 * 锁定：设计器只打开已有模板，没有详情时提示找不到并关掉；本机草稿键是模板 id；
 * 保存只更新这份模板，设计取设计器里的，元数据与行版本沿用详情，存好后让设计器记为已保存；
 * 未保存跟随设计器的判定，改了又撤销回原样即复位，只有旧格式模板换成空白时在存好前一直算未保存。
 */
import type { PrintTemplateDetailDto } from '../../../../api/print-template.types'
import type { PrintDocument } from '~/printing/designer'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, reactive } from 'vue'
import { dialog, toast } from '~/composables'
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

const printing = vi.hoisted(() => {
  class PrintTemplateFormatError extends Error {}
  return { PrintTemplateFormatError, parsePrintTemplateJson: vi.fn((): unknown => ({})) }
})

vi.mock('vue-i18n', () => ({ useI18n: () => ({ t: (key: string) => key }) }))
vi.mock('@/api', () => ({ EnableStatus: { Enabled: 1, Disabled: 0 } }))
vi.mock('~/composables', () => ({ dialog: { confirm: vi.fn() }, toast: { success: vi.fn(), warning: vi.fn(), danger: vi.fn() } }))
vi.mock('~/printing', () => ({
  createDefaultPrintSampleData: vi.fn(),
  directPrintByCode: vi.fn(),
  getPreferredPrinter: () => null,
  listPrinters: vi.fn(async () => []),
  parsePrintTemplateJson: printing.parsePrintTemplateJson,
  PrintTemplateFormatError: printing.PrintTemplateFormatError,
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

describe('未保存', () => {
  const doc = {} as PrintDocument

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('跟随设计器的判定：改了又撤销回原样即复位，返回列表不再确认', async () => {
    const emit = vi.fn()
    const { editor, props } = setup({ ...saved } as unknown as PrintTemplateDetailDto, emit)
    props.show = true
    await flushPromises()
    editor.onDesignerReady()

    editor.onDesignerDirtyChange(true)
    editor.onDesignerChanged(doc)
    expect(editor.dirty.value).toBe(true)

    // 撤销回原样：设计器先报干净，随后才报模板变了
    editor.onDesignerDirtyChange(false)
    editor.onDesignerChanged(doc)
    expect(editor.dirty.value).toBe(false)

    await editor.requestVisible(false)
    expect(dialog.confirm).not.toHaveBeenCalled()
    expect(emit).toHaveBeenCalledWith('update:show', false)
  })

  it('重新打开：上一次的未保存不带过来', async () => {
    const { editor, props } = setup({ ...saved } as unknown as PrintTemplateDetailDto)
    props.show = true
    await flushPromises()
    editor.onDesignerDirtyChange(true)

    props.show = false
    await flushPromises()
    props.show = true
    await flushPromises()

    expect(editor.dirty.value).toBe(false)
  })

  it('旧格式模板换成空白：设计器判定干净也算未保存，存好后才复位', async () => {
    printing.parsePrintTemplateJson.mockImplementationOnce(() => {
      throw new printing.PrintTemplateFormatError('旧格式')
    })
    const { editor, props } = setup({ ...saved } as unknown as PrintTemplateDetailDto)
    props.show = true
    await flushPromises()
    editor.onDesignerReady()

    expect(toast.warning).toHaveBeenCalledWith('setting.print_template.legacy_format_reset')
    expect(editor.dirty.value).toBe(true)

    editor.onDesignerDirtyChange(true)
    editor.onDesignerDirtyChange(false)
    expect(editor.dirty.value).toBe(true)

    // 存好后设计器记为已保存，随之报干净
    editor.setDesignerRef({ validate: () => [], getJson: () => ({ pages: [] }), markSaved: () => editor.onDesignerDirtyChange(false) })
    await editor.save()
    expect(editor.dirty.value).toBe(false)
  })
})
