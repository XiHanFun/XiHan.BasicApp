/**
 * 聊天 API 注入缝。
 * 职责：应用侧（src/modules/chat）组装 ChatApiContract 后注册进来，包内经 getChatApi 消费，
 * 使聊天壳层不依赖 src、删除聊天模块时无壳层残留。
 */
import type { ChatApiContract } from './types'

let contract: ChatApiContract | null = null

/**
 * 注册聊天 API 契约实现；应用启动时（模块 setup 钩子）调用。
 * @param api 契约实现。
 * @returns 无返回值。
 */
export function setChatApi(api: ChatApiContract): void {
  contract = api
}

/**
 * 获取已注册的聊天 API。
 * @returns 契约实现。
 * @throws 聊天模块未注册 API（聊天 UI 不应在未注册时被渲染）。
 */
export function getChatApi(): ChatApiContract {
  if (!contract)
    throw new Error('聊天 API 尚未注册，请确认 src/modules/chat 的 setup 已执行。')
  return contract
}
