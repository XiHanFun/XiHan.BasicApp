/**
 * 打印模板编辑会话的本机草稿键（use-print-template-editor.ts 的 draftKey 与保存顺序）。
 *
 * 锁定：编辑已有模板时草稿键是模板 id，新建时不给（设计器按页面地址区分）；新建模板第一次保存，
 * 先在原来的键下记为已保存（设计器删掉页面地址下的那份草稿），再换上模板 id，不留下孤儿草稿。
 */
import type { PrintTemplateDetailDto } from '../../../../api/print-template.types'
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { defineComponent, reactive } from 'vue'
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
  printTemplateApi: { create: vi.fn(async () => saved), update: vi.fn(async () => saved) },
}))

function setup(detail: PrintTemplateDetailDto | null) {
  let editor!: ReturnType<typeof usePrintTemplateEditor>
  const props = reactive({ detail, globalMode: false, scope: PrintTemplateScope.Tenant, show: false })
  mount(defineComponent({
    setup() {
      editor = usePrintTemplateEditor(props, () => {})
      return () => null
    },
  }))
  return { editor, props }
}

describe('本机草稿键', () => {
  it('编辑已有模板时是模板 id，新建时不给', async () => {
    const existing = setup({ ...saved, basicId: 'tpl-9' } as unknown as PrintTemplateDetailDto)
    existing.props.show = true
    await flushPromises()
    expect(existing.editor.draftKey.value).toBe('tpl-9')

    const created = setup(null)
    created.props.show = true
    await flushPromises()
    expect(created.editor.draftKey.value).toBeUndefined()
  })

  it('新建模板第一次保存：先在原来的键下记为已保存，再换上模板 id', async () => {
    const { editor, props } = setup(null)
    props.show = true
    await flushPromises()
    const keysAtMarkSaved: (string | undefined)[] = []
    editor.setDesignerRef({
      validate: () => [],
      getJson: () => ({}),
      markSaved: () => keysAtMarkSaved.push(editor.draftKey.value),
    })
    editor.form.value.templateCode = 'invoice'
    editor.form.value.templateName = '发票'

    await editor.save()

    expect(keysAtMarkSaved).toEqual([undefined])
    expect(editor.draftKey.value).toBe('tpl-1')
  })
})
