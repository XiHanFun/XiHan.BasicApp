import type {
  ChatAssistantCompletedPushPayload,
  ChatAssistantDeltaPushPayload,
  ChatConversationChangedPushPayload,
  ChatMessageEditedPushPayload,
  ChatMessagePushPayload,
  ChatReactionChangedPushPayload,
  ChatReadPositionChangedPushPayload,
  ChatRecalledPushPayload,
  ChatTypingPushPayload,
} from '../types'
import { onMounted, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { islandStart, playNotificationSound, useSignalR } from '~/composables'
import { useAccessStore, useUserStore } from '~/stores'
import { useAppContext } from '~/stores/app-context'
import {
  CHAT_HUB_PATH,
  CHAT_PERMISSIONS,
  CHAT_REALTIME_METHODS,
} from '../constants'

import { useChatStore } from '../store'

const CHAT_RECONNECT_INTERVAL_MS = 15000

/**
 * 在 BasicLayout 中初始化聊天实时链路（/hubs/chat 独立连接）：
 * 订阅四类推送回灌 chat store，并预取会话列表供顶栏未读角标。
 * 无 chat:read 权限的用户整条链路静默关闭。
 */
export function useChatIntegration() {
  const accessStore = useAccessStore()
  const userStore = useUserStore()
  const chatStore = useChatStore()
  const signalR = useSignalR(CHAT_HUB_PATH)
  const route = useRoute()
  const { t } = useI18n()
  const chatPath = useAppContext().shellRoutes.chat

  /**
   * 他人发来的消息的提醒。岛提示与提示音的静音条件不同，分开判断：
   * - 岛提示：只要在聊天页就不弹，消息就在眼前，再弹一次是噪音
   * - 提示音：只有「正开着这条会话且窗口在前台」才不响。在聊天页但开着别的会话、
   *   或窗口切到后台，都还是要出声，否则会漏掉
   */
  function notifyIncomingMessage(payload: ChatMessagePushPayload) {
    const { message, conversation } = payload
    if (message.senderUserId === userStore.userInfo?.basicId || message.messageType === 'System') {
      return
    }

    const onChatPage = !!chatPath && route.path.startsWith(chatPath)
    const watchingThisConversation = onChatPage
      && chatStore.activeConversationId === message.conversationId
      && typeof document !== 'undefined' && document.hasFocus()

    if (!watchingThisConversation) {
      playNotificationSound('chat')
    }

    if (onChatPage) {
      return
    }

    // 卡片形态：标题给发送人、副文本给消息正文，与手机通知一致
    const sender = message.senderUserName || conversation.conversationName || t('chat.island_new_message_fallback')
    const preview = conversation.lastMessagePreview?.trim() || message.content?.trim() || ''
    // islandStart 只建条目，要靠句柄置终态才会安排移除；此处随即 info() 收尾，
    // 否则这条通知会一直挂在岛上不消失。
    // id 带上 messageId：按会话取 id 会让同一会话的后续消息复用同一条目（更新而非新增），
    // 岛上永远只有一条，既排不了队也堆不起来
    islandStart(`chat:msg:${message.conversationId}:${message.messageId}`, sender, {
      icon: 'lucide:message-circle',
      detail: preview,
      link: chatPath,
    }).info()
  }

  let isListenersBound = false
  let reconnectTimer: ReturnType<typeof setInterval> | null = null

  function canUseChat() {
    return Boolean(accessStore.accessToken) && userStore.hasPermission(CHAT_PERMISSIONS.read)
  }

  function setupListeners() {
    if (isListenersBound) {
      return
    }
    signalR.on(CHAT_REALTIME_METHODS.receiveChatMessage, (payload) => {
      chatStore.applyIncomingMessage(payload as ChatMessagePushPayload)
      notifyIncomingMessage(payload as ChatMessagePushPayload)
    })
    signalR.on(CHAT_REALTIME_METHODS.chatMessageRecalled, payload =>
      chatStore.applyMessageRecalled(payload as ChatRecalledPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatConversationChanged, payload =>
      chatStore.applyConversationChanged(payload as ChatConversationChangedPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatTyping, payload =>
      chatStore.applyTyping(payload as ChatTypingPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatMessageEdited, payload =>
      chatStore.applyMessageEdited(payload as ChatMessageEditedPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatReactionChanged, payload =>
      chatStore.applyReactionChanged(payload as ChatReactionChangedPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatReadPositionChanged, payload =>
      chatStore.applyReadPositionChanged(payload as ChatReadPositionChangedPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatAssistantDelta, payload =>
      chatStore.applyAssistantDelta(payload as ChatAssistantDeltaPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatAssistantCompleted, payload =>
      chatStore.applyAssistantCompleted(payload as ChatAssistantCompletedPushPayload))
    isListenersBound = true
  }

  function clearReconnectTimer() {
    if (reconnectTimer) {
      clearInterval(reconnectTimer)
      reconnectTimer = null
    }
  }

  function ensureReconnectTimer() {
    if (reconnectTimer || !canUseChat()) {
      return
    }
    reconnectTimer = setInterval(() => {
      void connect()
    }, CHAT_RECONNECT_INTERVAL_MS)
  }

  async function connect() {
    if (!canUseChat()) {
      return
    }
    setupListeners()
    await signalR.start()
    if (signalR.connected.value) {
      clearReconnectTimer()
    }
    else {
      ensureReconnectTimer()
    }
  }

  const stopTokenWatch = watch(
    () => accessStore.accessToken,
    async (token) => {
      if (token) {
        await connect()
        chatStore.ensureConversations().catch(() => {})
      }
      else {
        clearReconnectTimer()
        await signalR.destroy()
        isListenersBound = false
        chatStore.$reset()
      }
    },
  )

  // 窗口重新聚焦/回到页面：消费活跃会话在失焦期间积累的未读
  // （收消息时仅在「活跃且聚焦」才自动已读，失焦期间的未读需要这个触发点）
  function handleWindowFocus() {
    if (canUseChat()) {
      chatStore.consumeActiveUnread()
    }
  }

  function handleVisibilityChange() {
    if (document.visibilityState === 'visible') {
      handleWindowFocus()
    }
  }

  onMounted(() => {
    void connect()
    if (canUseChat()) {
      chatStore.ensureConversations().catch(() => {})
    }
    window.addEventListener('focus', handleWindowFocus)
    document.addEventListener('visibilitychange', handleVisibilityChange)
  })

  onUnmounted(() => {
    stopTokenWatch()
    clearReconnectTimer()
    isListenersBound = false
    window.removeEventListener('focus', handleWindowFocus)
    document.removeEventListener('visibilitychange', handleVisibilityChange)
    void signalR.destroy()
  })
}
