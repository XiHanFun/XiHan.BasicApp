/**
 * 打印设计器（@xihan-print/designer）的注册、样式与新建模板。
 * 职责：编辑页用到时才引入设计器代码与它的样式；设计器跟随应用语言；新建模板是一张 A4 纵向的空白页面模板。
 * 只给编辑页用，经子路径 ~/printing/designer 取，不从包入口导出。
 */
import type { PrintDocument } from '@xihan-print/core'
import type { XhConfig } from '@xihan-ui/web-components'
import { createDocument } from '@xihan-print/core'
import { definePrintDesigner } from '@xihan-print/designer'
import { printDeDE, printEnUS, printEsES, printFrFR, printJaJP, printKoKR, printPtBR, printRuRU, printZhCN, printZhTW } from '@xihan-print/runtime/locale'
import { registerXhElementsLocale } from '~/designers'
import '@xihan-ui/designer-shell/style.css'
import '@xihan-print/runtime/style.css'
import '@xihan-print/designer/style.css'

export type { PrintDocument } from '@xihan-print/core'
export type { PrintDesignerEventMap, XhPrintDesigner } from '@xihan-print/designer'

/** 语言标签 → 打印的语言包（设计器零件与预览、设计器的内建文案）。 */
const PRINT_LOCALES: Readonly<Record<string, XhConfig>> = {
  'de-DE': printDeDE,
  'en-US': printEnUS,
  'es-ES': printEsES,
  'fr-FR': printFrFR,
  'ja-JP': printJaJP,
  'ko-KR': printKoKR,
  'pt-BR': printPtBR,
  'ru-RU': printRuRU,
  'zh-CN': printZhCN,
  'zh-TW': printZhTW,
}

/** 打印的语言包；没有这种语言时为 undefined（元素缺省英文）。 */
function printLocale(locale: string): XhConfig | undefined {
  return PRINT_LOCALES[locale]
}

/** 注册 <xh-print-designer>（含它用到的预览、零件与 UI 元素），并让它跟随应用语言；可重复调用。 */
export function ensurePrintDesigner(): void {
  definePrintDesigner()
  registerXhElementsLocale(printLocale)
}

/** 新建模板：一张 A4 纵向的空白页面模板。 */
export function createEmptyPrintDocument(): PrintDocument {
  return createDocument()
}
