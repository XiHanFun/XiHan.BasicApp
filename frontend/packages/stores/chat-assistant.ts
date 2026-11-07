/**
 * 聊天助手登记点
 * 职责：助手能力（可用助手、开会话、触发回复）由 AI 模块登记进来，聊天壳层按登记与否显隐助手入口——
 * 聊天不依赖 AI，删除 AI 模块目录后助手入口消失、其余聊天照常（与工作台小组件同一个做法）
 * 后端关掉 AI、前端 AI 模块还在时，三个操作都返回「助手不可用」（CHAT_UNAVAILABLE_001），入口由聊天列表按这个码隐藏
 */
import type { ResponseOf } from '~/contracts'

/** 一个可用的助手 */
export type ChatAssistantOption = ResponseOf<'chat-conversations.assistants'>['items'][number]

/** 助手提供方（增量与完成经聊天 Hub 推送，不在此契约内） */
export interface ChatAssistantProvider {
  /** 当前用户可用的助手 */
  availableAssistants: () => Promise<ChatAssistantOption[]>
  /** 打开（没有即建）与指定助手的会话 */
  openConversation: (assistantId: string) => Promise<ResponseOf<'chat-conversations.open-assistant'>>
  /** 触发一轮助手回复（replyId 关联后续的增量推送）；失败时抛出 */
  reply: (conversationId: string, replyId: string) => Promise<ResponseOf<'chat-messages.reply-assistant'>>
}

let provider: ChatAssistantProvider | null = null

/**
 * 登记助手提供方；AI 模块 setup 钩子调用
 * @param impl 提供方实现
 */
export function registerChatAssistantProvider(impl: ChatAssistantProvider): void {
  provider = impl
}

/**
 * 读取助手提供方
 * @returns 已登记的提供方；没有登记返回 null（助手入口据此隐藏）
 */
export function getChatAssistantProvider(): ChatAssistantProvider | null {
  return provider
}

/**
 * 助手提供方是否已登记
 * @returns 已登记返回 true
 */
export function hasChatAssistantProvider(): boolean {
  return provider !== null
}
