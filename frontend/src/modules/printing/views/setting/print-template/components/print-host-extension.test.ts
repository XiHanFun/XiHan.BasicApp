/**
 * 打印模板编辑器挂进设计器的宿主操作（print-host-extension.ts）的单元测试。
 *
 * 锁定：样例预览、直接打印进「文件」菜单并上工具栏；打印机是文件菜单里的单选子菜单，末尾一条刷新；
 * 各条按编辑会话此刻的状态可用，执行时交给编辑会话。回列表走设计器自带的关闭钮（closable），不再有「返回列表」宿主命令。
 */
import type { DesignerCommandMenuItem } from '~/designers'
import { describe, expect, it, vi } from 'vitest'
import { createPrintHostExtension, PRINT_HOST_COMMANDS } from './print-host-extension'

interface State {
  ready: boolean
  previewing: boolean
  printing: boolean
  canDirectPrint: boolean
  printers: { label: string, value: string }[]
  selectedPrinter: string | null
  loadingPrinters: boolean
}

function setup(overrides: Partial<State> = {}) {
  const state: State = {
    ready: true,
    previewing: false,
    printing: false,
    canDirectPrint: true,
    printers: [{ label: '客户端默认打印机', value: '' }, { label: 'HP', value: 'HP' }, { label: 'Zebra', value: 'Zebra' }],
    selectedPrinter: 'HP',
    loadingPrinters: false,
    ...overrides,
  }
  const actions = {
    openSamplePreview: vi.fn(),
    directPrint: vi.fn(),
    selectPrinter: vi.fn(),
    refreshPrinters: vi.fn(),
  }
  const extension = createPrintHostExtension({
    t: key => key,
    ready: () => state.ready,
    previewing: () => state.previewing,
    printing: () => state.printing,
    canDirectPrint: () => state.canDirectPrint,
    printers: () => state.printers,
    selectedPrinter: () => state.selectedPrinter,
    loadingPrinters: () => state.loadingPrinters,
    ...actions,
  })
  const command = (id: string) => {
    const found = extension.commands?.find(item => item.id === id)
    if (!found)
      throw new Error(`没有命令 ${id}`)
    return found
  }
  const enabled = (id: string) => command(id).enabled?.({} as never) ?? true
  const label = (id: string) => {
    const value = command(id).label
    return typeof value === 'function' ? value({} as never) : value
  }
  const menu = () => (command(PRINT_HOST_COMMANDS.printer).menu?.({} as never) ?? [])
  return { state, actions, extension, command, enabled, label, menu }
}

describe('落点与文案', () => {
  it('样例预览、直接打印、打印机依次进文件菜单，前两条也上工具栏；没有返回列表', () => {
    const { extension } = setup()

    expect(extension.commands?.map(item => [item.id, item.placement])).toEqual([
      [PRINT_HOST_COMMANDS.samplePreview, ['menubar:file', 'toolbar']],
      [PRINT_HOST_COMMANDS.directPrint, ['menubar:file', 'toolbar']],
      [PRINT_HOST_COMMANDS.printer, ['menubar:file']],
    ])
  })

  it('命令 id 以扩展 id 开头，文案取打印模板的文案键，每条都带图标', () => {
    const { extension, label } = setup()

    expect(extension.commands?.every(item => item.id.startsWith(`${extension.id}.`) && item.icon !== undefined)).toBe(true)
    expect(label(PRINT_HOST_COMMANDS.samplePreview)).toBe('setting.print_template.sample_preview')
    expect(label(PRINT_HOST_COMMANDS.directPrint)).toBe('setting.print_template.direct_print')
    expect(label(PRINT_HOST_COMMANDS.printer)).toBe('setting.print_template.printer')
  })
})

describe('可用', () => {
  it('设计器没装好时样例预览与直接打印不可用', () => {
    const { enabled } = setup({ ready: false })

    expect(enabled(PRINT_HOST_COMMANDS.samplePreview)).toBe(false)
    expect(enabled(PRINT_HOST_COMMANDS.directPrint)).toBe(false)
  })

  it('预览中不能再预览，直打中不能再直打；这份模板不能直打时直接打印不可用', () => {
    expect(setup({ previewing: true }).enabled(PRINT_HOST_COMMANDS.samplePreview)).toBe(false)
    expect(setup({ printing: true }).enabled(PRINT_HOST_COMMANDS.directPrint)).toBe(false)
    expect(setup({ canDirectPrint: false }).enabled(PRINT_HOST_COMMANDS.directPrint)).toBe(false)
    expect(setup().enabled(PRINT_HOST_COMMANDS.directPrint)).toBe(true)
  })
})

describe('打印机子菜单', () => {
  it('客户端默认与各台打印机同组单选，勾着选中的那台；末尾隔一条线是刷新', () => {
    const { menu } = setup()
    const entries = menu()
    const items = entries.filter((entry): entry is DesignerCommandMenuItem => typeof entry !== 'string')

    expect(items.slice(0, 3).map(item => [item.label, item.checked, item.group])).toEqual([
      ['客户端默认打印机', false, items[0]!.group],
      ['HP', true, items[0]!.group],
      ['Zebra', false, items[0]!.group],
    ])
    expect(items[0]!.group).toBeTruthy()
    expect(entries[3]).toBe('separator')
    expect(items[3]).toMatchObject({ label: 'setting.print_template.refresh_printers', disabled: false })
    expect(items[3]!.checked).toBeUndefined()
  })

  it('没选打印机时勾着客户端默认；列表加载中刷新不可用', () => {
    const items = setup({ selectedPrinter: null, loadingPrinters: true }).menu().filter((entry): entry is DesignerCommandMenuItem => typeof entry !== 'string')

    expect(items.filter(item => item.checked).map(item => item.label)).toEqual(['客户端默认打印机'])
    expect(items.at(-1)).toMatchObject({ label: 'setting.print_template.refresh_printers', disabled: true })
  })

  it('选一台打印机交给编辑会话，选客户端默认传 null；选刷新时强制刷新列表', () => {
    const { command, menu, actions } = setup()
    const items = menu().filter((entry): entry is DesignerCommandMenuItem => typeof entry !== 'string')
    const select = (label: string) => command(PRINT_HOST_COMMANDS.printer).select?.(items.find(item => item.label === label)!.value, {} as never)

    select('Zebra')
    select('客户端默认打印机')
    select('setting.print_template.refresh_printers')

    expect(actions.selectPrinter.mock.calls).toEqual([['Zebra'], [null]])
    expect(actions.refreshPrinters).toHaveBeenCalledTimes(1)
  })
})

describe('执行', () => {
  it('样例预览、直接打印交给编辑会话', () => {
    const { command, actions } = setup()

    for (const id of [PRINT_HOST_COMMANDS.samplePreview, PRINT_HOST_COMMANDS.directPrint])
      command(id).run?.({} as never)

    expect(actions.openSamplePreview).toHaveBeenCalledTimes(1)
    expect(actions.directPrint).toHaveBeenCalledTimes(1)
  })
})
