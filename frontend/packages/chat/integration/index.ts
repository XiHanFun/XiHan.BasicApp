/**
 * 聊天壳层扩展：顶栏按钮 + 全局浮动面板 + 实时集成钩子，经 registerShellExtension 挂进布局。
 */
import type { ShellExtension } from '~/stores'
import { registerNotificationSound } from '~/composables'
import AppChatPanel from './AppChatPanel.vue'
import ChatHeaderButton from './ChatHeaderButton.vue'
import { useChatIntegration } from './use-chat-integration'

/**
 * 注册聊天音色：
 * - chat：他人发来的消息，上行纯五度（A5→E6），短促明亮，与站内通知的下行音色区分
 * - chat-sent：自己的消息发送成功，上行纯四度（E6→A6）更高、更短、更轻，是对自己操作的确认，压在收消息之下
 */
function registerChatSound(): void {
  registerNotificationSound('chat', [
    { frequency: 880, offset: 0, duration: 0.14, gain: 1 },
    { frequency: 1318.51, offset: 0.1, duration: 0.2, gain: 0.8 },
  ])
  registerNotificationSound('chat-sent', [
    { frequency: 1318.51, offset: 0, duration: 0.07, gain: 0.5 },
    { frequency: 1760, offset: 0.045, duration: 0.1, gain: 0.35 },
  ])
}

/** 聊天的壳层扩展定义（模块 setup 注册） */
export const chatShellExtension: ShellExtension = {
  headerToolbarItems: [ChatHeaderButton],
  overlays: [AppChatPanel],
  integrations: [registerChatSound, useChatIntegration],
}
