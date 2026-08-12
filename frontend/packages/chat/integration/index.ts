/**
 * 聊天壳层扩展：顶栏按钮 + 全局抽屉 + 实时集成钩子，经 registerShellExtension 挂进布局。
 */
import type { ShellExtension } from '~/stores'
import AppChatDrawer from './AppChatDrawer.vue'
import ChatHeaderButton from './ChatHeaderButton.vue'
import { useChatIntegration } from './use-chat-integration'

/** 聊天的壳层扩展定义（模块 setup 注册） */
export const chatShellExtension: ShellExtension = {
  headerToolbarItems: [ChatHeaderButton],
  overlays: [AppChatDrawer],
  integrations: [useChatIntegration],
}
