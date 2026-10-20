/**
 * @xihan/designers —— 接入曦寒设计器（Web Components）的公共接线
 *
 * 这里只放各款设计器共用、没有副作用的部分：跟随应用语言的全局配置、把 Vue 组件做成属性编辑器、宿主命令用的图标与类型、
 * 菜单栏右边宿主那一行（DesignerMenubarTitle）。
 * 各款设计器的注册与接口经子路径取用（如 ~/designers/flow），带样式副作用，只在用到的页面引入。
 */

export { default as DesignerMenubarTitle } from './DesignerMenubarTitle.vue'
export { registerXhElementsLocale } from './elements-config'
export type { XhElementsLocale } from './elements-config'
export * from './host-icons'
export { createVuePropertyEditor } from './vue-editor'
export type { DesignerCommand, DesignerCommandMenuEntry, DesignerCommandMenuItem } from '@xihan-ui/designer-kit'
