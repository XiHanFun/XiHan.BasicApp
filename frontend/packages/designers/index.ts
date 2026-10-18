/**
 * @xihan/designers —— 接入曦寒设计器（Web Components）的公共接线
 *
 * 这里只放各款设计器共用、没有副作用的部分：跟随应用语言的全局配置、把 Vue 组件做成属性编辑器。
 * 各款设计器的注册与接口经子路径取用（如 ~/designers/flow），带样式副作用，只在用到的页面引入。
 */

export { registerXhElementsLocale } from './elements-config'
export type { XhElementsLocale } from './elements-config'
export { createVuePropertyEditor } from './vue-editor'
