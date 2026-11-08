/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { chatConversations, chatMessages } from '~/contracts'
import { registerModuleLocales } from '~/locales'
import { registerChatAssistantProvider } from '~/stores'

/**
 * AI 模块的启动钩子：登记本模块文案与聊天的助手能力
 * 页面由后端菜单下发（BasicApp:Modules:AI:Enabled 关掉即不下发），模块关着也无副作用；
 * 助手操作在聊天一侧，后端关掉 AI 时它们报「助手不可用」，聊天列表据此隐藏入口
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  registerChatAssistantProvider({
    availableAssistants: async () => (await chatConversations.assistants({})).items,
    openConversation: assistantId => chatConversations.openAssistant({ assistantId }),
    reply: (conversationId, replyId) => chatMessages.replyAssistant({ conversationId, replyId }),
  })
}
