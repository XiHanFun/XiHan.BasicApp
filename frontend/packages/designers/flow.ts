import type { XihanWorkflowTexts } from '@xihan-flow/core/presets/xihan-workflow'
import type { XhConfig } from '@xihan-ui/web-components'
import {
  xihanWorkflowDeDE,
  xihanWorkflowEnUS,
  xihanWorkflowEsES,
  xihanWorkflowFrFR,
  xihanWorkflowJaJP,
  xihanWorkflowKoKR,
  xihanWorkflowPtBR,
  xihanWorkflowRuRU,
  xihanWorkflowZhCN,
  xihanWorkflowZhTW,
} from '@xihan-flow/core/presets/xihan-workflow'
import { defineFlowViewer } from '@xihan-flow/runtime'
import { flowDeDE, flowEnUS, flowEsES, flowFrFR, flowJaJP, flowKoKR, flowPtBR, flowRuRU, flowZhCN, flowZhTW } from '@xihan-flow/runtime/locale'
import { registerXhElementsLocale } from './elements-config'
import '@xihan-flow/runtime/style.css'

/**
 * 流程查看器（@xihan-flow）的注册、文案与应用要用的接口。
 *
 * 应用层经本模块取 @xihan-flow 的接口，不直接依赖它。设计器另在 ./flow-designer：
 * 只看流程图的页面不带上设计器的代码与样式。
 */

export type { FlowDocument, FlowEdge, FlowNode } from '@xihan-flow/core'
export {
  createXihanWorkflow,
  fromWorkflowDefinition,
  toWorkflowDefinition,
  XIHAN_WORKFLOW_ASSIGNEE_RULES,
} from '@xihan-flow/core/presets/xihan-workflow'
export type { XihanWorkflowDefinition, XihanWorkflowNode } from '@xihan-flow/core/presets/xihan-workflow'
export type {
  FlowDesignerEventMap,
  FlowDesignerExtension,
  FlowPropertyEditor,
  FlowPropertyEditorContext,
  XhFlowDesigner,
} from '@xihan-flow/designer'
export type { FlowNodeStatus, FlowViewerEventMap, XhFlowViewer } from '@xihan-flow/runtime'
export type { SchemaRule } from '@xihan-ui/designer-kit'
export type { SchemaIssue } from '@xihan-ui/designer-kit/schema'
export type { DesignerSettings } from '@xihan-ui/designer-shell'

/** 语言标签 → 流程的语言包（设计器零件与查看器、设计器的内建文案）。 */
const FLOW_LOCALES: Readonly<Record<string, XhConfig>> = {
  'de-DE': flowDeDE,
  'en-US': flowEnUS,
  'es-ES': flowEsES,
  'fr-FR': flowFrFR,
  'ja-JP': flowJaJP,
  'ko-KR': flowKoKR,
  'pt-BR': flowPtBR,
  'ru-RU': flowRuRU,
  'zh-CN': flowZhCN,
  'zh-TW': flowZhTW,
}

/** 语言标签 → XiHan 工作流引擎预设的文案。 */
const XIHAN_WORKFLOW_TEXTS: Readonly<Record<string, XihanWorkflowTexts>> = {
  'de-DE': xihanWorkflowDeDE,
  'en-US': xihanWorkflowEnUS,
  'es-ES': xihanWorkflowEsES,
  'fr-FR': xihanWorkflowFrFR,
  'ja-JP': xihanWorkflowJaJP,
  'ko-KR': xihanWorkflowKoKR,
  'pt-BR': xihanWorkflowPtBR,
  'ru-RU': xihanWorkflowRuRU,
  'zh-CN': xihanWorkflowZhCN,
  'zh-TW': xihanWorkflowZhTW,
}

/** 流程的语言包；没有这种语言时为 undefined（元素缺省英文）。 */
export function flowLocale(locale: string): XhConfig | undefined {
  return FLOW_LOCALES[locale]
}

/** 注册 <xh-flow-viewer>，并让它跟随应用语言；可重复调用。 */
export function ensureFlowViewer(): void {
  defineFlowViewer()
  registerXhElementsLocale(flowLocale)
}

/** XiHan 工作流引擎预设的文案；没有对应语言包时为 undefined（预设缺省英文）。 */
export function xihanWorkflowTexts(locale: string): XihanWorkflowTexts | undefined {
  return XIHAN_WORKFLOW_TEXTS[locale]
}
