/**
 * AI 助手提供方扩展点。
 * 职责：助手能力（可用助手/开会话/触发回复）由 AI 模块注册进来，聊天壳层按注册与否
 * 显隐助手入口——聊天不依赖 AI，删除 AI 模块后助手入口消失、其余聊天照常。
 */
import type { ChatAssistantConversationResult, ChatAssistantOption, ChatAssistantReplyResult } from './types'

/** 助手提供方契约（增量与完成经聊天 Hub 推送，不在此契约内） */
export interface ChatAssistantProvider {
  /** 当前用户可用的助手列表 */
  availableAssistants: () => Promise<ChatAssistantOption[]>
  /** 打开（或复用）与指定助手的会话 */
  openConversation: (assistantId: string) => Promise<ChatAssistantConversationResult>
  /** 触发一轮助手回复（replyId 关联后续增量推送） */
  reply: (conversationId: string, replyId: string) => Promise<ChatAssistantReplyResult>
}

let provider: ChatAssistantProvider | null = null

/**
 * 注册助手提供方；AI 模块 setup 钩子调用。
 * @param impl 提供方实现。
 * @returns 无返回值。
 */
export function registerChatAssistantProvider(impl: ChatAssistantProvider): void {
  provider = impl
}

/**
 * 获取助手提供方。
 * @returns 已注册的提供方；未注册返回 null（助手入口应据此隐藏）。
 */
export function getChatAssistantProvider(): ChatAssistantProvider | null {
  return provider
}

/**
 * 助手提供方是否已注册。
 * @returns 已注册返回 true。
 */
export function hasChatAssistantProvider(): boolean {
  return provider !== null
}
