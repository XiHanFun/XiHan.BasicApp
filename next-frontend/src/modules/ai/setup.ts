/**
 * AI 模块启动注册。
 * 职责：把聊天 AI 助手提供方注册进 @xihan/chat 的助手扩展点；删除本模块后聊天无助手入口、其余照常。
 */
import { registerChatAssistantProvider } from '~/chat'
import { chatAssistantProviderApi } from './api/chat-assistant'

/** 注册聊天助手提供方。 */
export default function setupAi(): void {
  registerChatAssistantProvider(chatAssistantProviderApi)
}
