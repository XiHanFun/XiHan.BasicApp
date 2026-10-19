import { defineFlowDesigner } from '@xihan-flow/designer'
import { registerXhElementsLocale } from './elements-config'
import { flowLocale } from './flow'
import '@xihan-ui/designer-shell/style.css'
import '@xihan-flow/designer/style.css'

/**
 * 流程设计器（@xihan-flow/designer）的注册与样式：设计器零件与设计器本身的样式随本模块引入。
 * 类型与查看器也用的接口仍从 ./flow 取；只有设计器才用的值从这里取。
 */

export { FLOW_SETTING_KEYS } from '@xihan-flow/designer'

/** 注册 <xh-flow-designer>（含它用到的零件与 UI 元素），并让它跟随应用语言；可重复调用。 */
export function ensureFlowDesigner(): void {
  defineFlowDesigner()
  registerXhElementsLocale(flowLocale)
}
