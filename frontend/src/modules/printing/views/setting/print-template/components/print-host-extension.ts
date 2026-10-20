/**
 * 打印模板编辑器挂进打印设计器的宿主操作：样例预览、直接打印、打印机、模板设置与返回列表写成一个扩展的命令，
 * 落在菜单栏的「文件」菜单里，常用的同时上工具栏；保存用设计器内置的 save 命令（Ctrl / ⌘ + S），不在这里。
 *
 * 命令只读编辑会话此刻的状态、执行时交回编辑会话；状态变了由宿主调设计器的 requestUpdate() 刷新可用与勾选，
 * 扩展对象本身建一次、引用不变（换引用设计器当作卸下再装上）。
 */
import type { DesignerCommandMenuEntry } from '~/designers'
import type { PrintDesignerExtension } from '~/printing/designer'
import { ArrowLeftIcon, PrinterIcon, ReceiptIcon, RefreshIcon, SendIcon, SlidersHorizontalIcon } from '~/designers'

/** 宿主扩展的 id；命令 id 都以它加点开头 */
export const PRINT_HOST_EXTENSION_ID = '@xihan/print-template'

export const PRINT_HOST_COMMANDS = {
  samplePreview: `${PRINT_HOST_EXTENSION_ID}.sample-preview`,
  directPrint: `${PRINT_HOST_EXTENSION_ID}.direct-print`,
  printer: `${PRINT_HOST_EXTENSION_ID}.printer`,
  templateSettings: `${PRINT_HOST_EXTENSION_ID}.template-settings`,
  backToList: `${PRINT_HOST_EXTENSION_ID}.back-to-list`,
} as const

/** 打印机子菜单里各台打印机条目的 value 前缀；客户端默认打印机的 value 是空串，加了前缀不与刷新撞 */
const PRINTER_VALUE_PREFIX = 'printer:'
const REFRESH_PRINTERS_VALUE = 'refresh'

export interface PrintHostOptions {
  t: (key: string) => string
  /** 设计器装好、模板装载完成 */
  ready: () => boolean
  /** 正在调起样例预览 */
  previewing: () => boolean
  /** 正在直接打印 */
  printing: () => boolean
  /** 这份模板此刻能直接打印（停用的模板不能） */
  canDirectPrint: () => boolean
  /** 正在保存或直打，不许离开 */
  busy: () => boolean
  /** 打印机选项：客户端默认打印机（value 为空串）在前 */
  printers: () => readonly { label: string, value: string }[]
  /** 选中的打印机；null 交给客户端默认 */
  selectedPrinter: () => string | null
  /** 正在加载打印机列表 */
  loadingPrinters: () => boolean
  openSamplePreview: () => void
  directPrint: () => void
  /** 选打印机；null 是客户端默认 */
  selectPrinter: (printer: string | null) => void
  /** 强制刷新打印机列表 */
  refreshPrinters: () => void
  openTemplateSettings: () => void
  backToList: () => void
}

export function createPrintHostExtension(options: PrintHostOptions): PrintDesignerExtension {
  const { t } = options

  function printerMenu(): DesignerCommandMenuEntry[] {
    const selected = options.selectedPrinter() ?? ''
    return [
      ...options.printers().map(printer => ({
        value: `${PRINTER_VALUE_PREFIX}${printer.value}`,
        label: printer.label,
        checked: printer.value === selected,
        group: 'printer',
      })),
      'separator',
      {
        value: REFRESH_PRINTERS_VALUE,
        label: t('setting.print_template.refresh_printers'),
        icon: RefreshIcon,
        disabled: options.loadingPrinters(),
      },
    ]
  }

  return {
    id: PRINT_HOST_EXTENSION_ID,
    commands: [
      {
        id: PRINT_HOST_COMMANDS.samplePreview,
        label: () => t('setting.print_template.sample_preview'),
        icon: ReceiptIcon,
        placement: ['menubar:file', 'toolbar'],
        enabled: () => options.ready() && !options.previewing(),
        run: () => options.openSamplePreview(),
      },
      {
        id: PRINT_HOST_COMMANDS.directPrint,
        label: () => t('setting.print_template.direct_print'),
        icon: SendIcon,
        placement: ['menubar:file', 'toolbar'],
        enabled: () => options.ready() && !options.printing() && options.canDirectPrint(),
        run: () => options.directPrint(),
      },
      {
        id: PRINT_HOST_COMMANDS.printer,
        label: () => t('setting.print_template.printer'),
        icon: PrinterIcon,
        placement: ['menubar:file'],
        menu: printerMenu,
        select: (value) => {
          if (value === REFRESH_PRINTERS_VALUE)
            options.refreshPrinters()
          else if (value.startsWith(PRINTER_VALUE_PREFIX))
            options.selectPrinter(value.slice(PRINTER_VALUE_PREFIX.length) || null)
        },
      },
      {
        id: PRINT_HOST_COMMANDS.templateSettings,
        label: () => t('setting.print_template.template_settings'),
        icon: SlidersHorizontalIcon,
        placement: ['menubar:file', 'toolbar'],
        run: () => options.openTemplateSettings(),
      },
      {
        id: PRINT_HOST_COMMANDS.backToList,
        label: () => t('setting.print_template.back_to_list'),
        icon: ArrowLeftIcon,
        placement: ['menubar:file', 'toolbar'],
        enabled: () => !options.busy(),
        run: () => options.backToList(),
      },
    ],
  }
}
