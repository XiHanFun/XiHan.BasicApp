/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { chatConversations, chatMessages } from '~/contracts'
import { registerModuleLocales } from '~/locales'
import { LONG_TIMEOUT_MS } from '~/request'
import { registerChatAssistantProvider } from '~/stores'

/**
 * 登记本模块文案与聊天的助手能力
 * 页面由后端菜单下发（BasicApp:Modules:AI:Enabled 关掉即不下发），登记无副作用
 * 后端关掉 AI 时助手操作报「助手不可用」，聊天列表据此隐藏入口
 * 回复在一次调用里等模型流式回完，放宽到长超时
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  registerChatAssistantProvider({
    availableAssistants: async () => (await chatConversations.assistants({})).items,
    openConversation: assistantId => chatConversations.openAssistant({ assistantId }),
    reply: (conversationId, replyId) => chatMessages.replyAssistant({ conversationId, replyId }, { timeoutMs: LONG_TIMEOUT_MS }),
  })
}
