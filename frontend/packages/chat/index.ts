/**
 * XiHan 前端聊天功能包入口。
 * 职责：集中导出聊天面板组件、状态 store、类型契约、协议常量与 API 注入缝。
 */
export * from './api-contract'
export { default as ChatPanel } from './components/ChatPanel.vue'
export * from './constants'
export * from './store'
export * from './types'
