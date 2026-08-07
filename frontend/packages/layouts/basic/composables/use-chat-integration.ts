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
} from '~/types'
import { onMounted, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { islandStart, useSignalR } from '~/composables'
import { CHAT_HUB_PATH, CHAT_PERMISSIONS, CHAT_REALTIME_METHODS } from '~/constants'
import { useAccessStore, useChatStore, useUserStore } from '~/stores'
import { useAppContext } from '~/stores/app-context'

const CHAT_RECONNECT_INTERVAL_MS = 15000

/**
 * 在 BasicLayout 中初始化聊天实时链路（/hubs/chat 独立连接）：
 * 订阅四类推送回灌 chat store，并预取会话列表供顶栏未读角标。
 * 无 saas:chat:read 权限的用户整条链路静默关闭。
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
   * 不在聊天页时把他人发来的消息弹进灵动岛（点击直达会话）。
   * 已在聊天页则不弹——消息就在眼前，再弹一次是噪音。
   */
  function notifyIncomingMessage(payload: ChatMessagePushPayload) {
    const { message, conversation } = payload
    if (message.senderUserId === userStore.userInfo?.basicId || message.messageType === 'System') {
      return
    }
    if (chatPath && route.path.startsWith(chatPath)) {
      return
    }
    const preview = conversation.lastMessagePreview?.trim() || message.content?.trim()
    const label = preview
      ? t('chat.island_new_message', { name: message.senderUserName || conversation.conversationName || '', preview })
      : t('chat.island_new_message_fallback')
    islandStart(`chat:msg:${message.conversationId}`, label, {
      icon: 'lucide:message-circle',
      state: 'info',
      link: chatPath,
    })
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
