/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 聊天 API 注入缝
 * 职责：本模块的 setup 用生成的操作客户端组装 ChatApiContract 后注册进来，store 与组件经 getChatApi 消费；
 * 测试注册替身即可，不碰网络层
 */
import type { ChatApiContract } from './types'

let contract: ChatApiContract | null = null

/**
 * 注册聊天 API 契约实现；应用启动时（模块 setup 钩子）调用
 * @param api 契约实现
 */
export function setChatApi(api: ChatApiContract): void {
  contract = api
}

/**
 * 获取已注册的聊天 API
 * @returns 契约实现
 * @throws 聊天模块未注册 API（聊天 UI 不应在未注册时被渲染）
 */
export function getChatApi(): ChatApiContract {
  if (!contract)
    throw new Error('聊天 API 尚未注册，请确认 src/modules/chat 的 setup 已执行。')
  return contract
}
