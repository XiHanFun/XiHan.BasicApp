/**
 * @xihan/designers —— 接入曦寒设计器（Web Components）的公共接线
 *
 * 这里只放各款设计器共用、没有副作用的部分：跟随应用语言的全局配置、把 Vue 组件做成属性编辑器、宿主命令用的图标与类型、
 * 宿主放进外框的标题（菜单栏正中，DesignerMenubarTitle）与状态（状态栏右端，DesignerStatusText）。
 * 各款设计器的注册与接口经各自的子路径取用（打印设计器在 ~/printing/designer，流程设计器随 Workflow 加 ~/designers/flow），带样式副作用，只在用到的页面引入。
 */

export { registerXhElementsLocale } from './elements-config'
export type { XhElementsLocale } from './elements-config'
export * from './host-icons'
export { DesignerMenubarTitle, DesignerStatusText } from './host-items'
export { createVuePropertyEditor } from './vue-editor'
export type { DesignerCommand, DesignerCommandMenuEntry, DesignerCommandMenuItem } from '@xihan-ui/designer-kit'
