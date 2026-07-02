import type {
  ChatConversationChangedPushPayload,
  ChatMessagePushPayload,
  ChatRecalledPushPayload,
  ChatTypingPushPayload,
} from '~/types'
import { onMounted, onUnmounted, watch } from 'vue'
import { useSignalR } from '~/composables'
import { CHAT_HUB_PATH, CHAT_PERMISSIONS, CHAT_REALTIME_METHODS } from '~/constants'
import { useAccessStore, useChatStore, useUserStore } from '~/stores'

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

  let isListenersBound = false
  let reconnectTimer: ReturnType<typeof setInterval> | null = null

  function canUseChat() {
    return Boolean(accessStore.accessToken) && userStore.hasPermission(CHAT_PERMISSIONS.read)
  }

  function setupListeners() {
    if (isListenersBound) {
      return
    }
    signalR.on(CHAT_REALTIME_METHODS.receiveChatMessage, payload =>
      chatStore.applyIncomingMessage(payload as ChatMessagePushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatMessageRecalled, payload =>
      chatStore.applyMessageRecalled(payload as ChatRecalledPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatConversationChanged, payload =>
      chatStore.applyConversationChanged(payload as ChatConversationChangedPushPayload))
    signalR.on(CHAT_REALTIME_METHODS.chatTyping, payload =>
      chatStore.applyTyping(payload as ChatTypingPushPayload))
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

  onMounted(() => {
    void connect()
    if (canUseChat()) {
      chatStore.ensureConversations().catch(() => {})
    }
  })

  onUnmounted(() => {
    stopTokenWatch()
    clearReconnectTimer()
    isListenersBound = false
    void signalR.destroy()
  })
}
