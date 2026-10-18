import type { XihanWorkflowTextsOverride } from '@xihan-flow/core/presets/xihan-workflow'
import { xihanWorkflowZhCN } from '@xihan-flow/core/presets/xihan-workflow'
import { defineFlowViewer } from '@xihan-flow/runtime'
import { flowZhCN } from '@xihan-flow/runtime/locale'
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
  xihanWorkflowZhCN,
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

/** 流程的语言包：设计器零件与查看器、设计器的内建文案。 */
export function flowLocale(locale: string) {
  return locale === 'zh-CN' ? flowZhCN : undefined
}

/** 注册 <xh-flow-viewer>，并让它跟随应用语言；可重复调用。 */
export function ensureFlowViewer(): void {
  defineFlowViewer()
  registerXhElementsLocale(flowLocale)
}

/** XiHan 工作流引擎预设的文案；没有对应语言包时为 undefined（预设缺省英文）。 */
export function xihanWorkflowTexts(locale: string): XihanWorkflowTextsOverride | undefined {
  return locale === 'zh-CN' ? xihanWorkflowZhCN : undefined
}
