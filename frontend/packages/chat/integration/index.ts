/**
 * 聊天壳层扩展：顶栏按钮 + 全局抽屉 + 实时集成钩子，经 registerShellExtension 挂进布局。
 */
import type { ShellExtension } from '~/stores'
import { registerNotificationSound } from '~/composables'
import AppChatDrawer from './AppChatDrawer.vue'
import ChatHeaderButton from './ChatHeaderButton.vue'
import { useChatIntegration } from './use-chat-integration'

/** 注册聊天音色：上行纯五度（A5→E6），短促明亮，与站内通知的下行音色区分 */
function registerChatSound(): void {
  registerNotificationSound('chat', [
    { frequency: 880, offset: 0, duration: 0.14, gain: 1 },
    { frequency: 1318.51, offset: 0.1, duration: 0.2, gain: 0.8 },
  ])
}

/** 聊天的壳层扩展定义（模块 setup 注册） */
export const chatShellExtension: ShellExtension = {
  headerToolbarItems: [ChatHeaderButton],
  overlays: [AppChatDrawer],
  integrations: [registerChatSound, useChatIntegration],
}
