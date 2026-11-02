import { registerModuleLocales } from '~/locales'
import { registerShellExtension } from '~/stores'
import { createChatApi } from './api'
import { setChatApi } from './api-contract'
import { chatShellExtension } from './integration'

/**
 * 聊天模块的启动钩子（照 4.x 的 setupChat）：登记本模块文案，用生成的操作客户端组装聊天 API 契约注入本模块的注入缝，
 * 再把壳层扩展（顶栏聊天按钮与未读角标、全局浮动面板、实时集成钩子）挂进布局。
 * 只登记、不发请求：按钮与实时链路按会话读权限（chat.chat-conversations.read）显隐与启停，
 * 后端关掉聊天模块（BasicApp:Modules:Chat:Enabled）时权限与页面都不下发，界面随之不出现。
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  setChatApi(createChatApi())
  registerShellExtension(chatShellExtension)
}
