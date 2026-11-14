/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type {
  ChatAssistantCompletedPushPayload,
  ChatAssistantDeltaPushPayload,
  ChatCallItem,
  ChatCallSignalPushPayload,
  ChatConversationChangedPushPayload,
  ChatMessageEditedPushPayload,
  ChatMessagePushPayload,
  ChatPresenceChangedPushPayload,
  ChatReactionChangedPushPayload,
  ChatReadPositionChangedPushPayload,
  ChatRecalledPushPayload,
  ChatTypingPushPayload,
} from '../types'
import { onMounted, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { islandStart, playNotificationSound, useSignalR } from '~/composables'
import { RealtimeHubs, RealtimeMethods } from '~/contracts'
import { useAccessStore, useUserStore } from '~/stores'
import { isMissedCallRecord } from '../call/call-helpers'
import { useChatCallStore } from '../call/call-store'
import { previewText } from '../components/chat-helpers'
import { CHAT_PAGE_PATH, CHAT_PERMISSIONS } from '../constants'
import { useChatStore } from '../store'

const CHAT_RECONNECT_INTERVAL_MS = 15000

/**
 * 在 BasicLayout 中初始化聊天实时链路（RealtimeHubs.Chat 独立连接）
 * 推送回灌 chat store 与通话 store，并预取会话列表供顶栏未读角标
 * 没有会话读权限（chat.chat-conversations.read）时整条链路静默关闭
 */
export function useChatIntegration() {
  const accessStore = useAccessStore()
  const userStore = useUserStore()
  const chatStore = useChatStore()
  const callStore = useChatCallStore()
  const signalR = useSignalR(RealtimeHubs.Chat)
  const route = useRoute()
  const { t } = useI18n()

  /**
   * 提醒他人发来的消息，岛提示与提示音分开判断静音
   * 岛提示：在聊天页就不弹，消息就在眼前
   * 提示音：只有正开着这条会话且窗口在前台才不响，否则会漏掉
   */
  function notifyIncomingMessage(payload: ChatMessagePushPayload) {
    const { message, conversation } = payload
    if (message.senderUserId === userStore.userInfo?.basicId || message.messageType === 'System') {
      return
    }
    // 通话记录：只有没接到的（对方取消或没人接）才提醒，接通过的与自己拒的不提醒
    if (message.messageType === 'Call' && !isMissedCallRecord(message.content)) {
      return
    }

    const onChatPage = route.path.startsWith(CHAT_PAGE_PATH)
    const watchingThisConversation = onChatPage
      && chatStore.activeConversationId === message.conversationId
      && typeof document !== 'undefined' && document.hasFocus()

    if (!watchingThisConversation) {
      playNotificationSound('receive')
    }

    if (onChatPage) {
      return
    }

    // 卡片形态：标题给发送人、副文本给消息正文，与手机通知一致
    const sender = message.senderUserName || conversation.conversationName || t('chat.island_new_message_fallback')
    const preview = previewText(conversation.conversationType, conversation.lastMessagePreview?.trim() || message.content?.trim() || '')
    // islandStart 只建条目，要靠句柄置终态才会安排移除；此处随即 info() 收尾，
    // 否则这条通知会一直挂在岛上不消失
    // id 带上 messageId：按会话取 id 会让同一会话的后续消息复用同一条目（更新而非新增），
    // 岛上永远只有一条，既排不了队也堆不起来
    islandStart(`chat:msg:${message.conversationId}:${message.messageId}`, sender, {
      leading: { type: 'avatar', name: sender },
      detail: preview,
      link: CHAT_PAGE_PATH,
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
    signalR.on(RealtimeMethods.ReceiveChatMessage, (payload) => {
      chatStore.applyIncomingMessage(payload as ChatMessagePushPayload)
      notifyIncomingMessage(payload as ChatMessagePushPayload)
    })
    signalR.on(RealtimeMethods.ChatMessageRecalled, payload =>
      chatStore.applyMessageRecalled(payload as ChatRecalledPushPayload))
    signalR.on(RealtimeMethods.ChatConversationChanged, payload =>
      chatStore.applyConversationChanged(payload as ChatConversationChangedPushPayload))
    signalR.on(RealtimeMethods.ChatTyping, payload =>
      chatStore.applyTyping(payload as ChatTypingPushPayload))
    signalR.on(RealtimeMethods.ChatMessageEdited, payload =>
      chatStore.applyMessageEdited(payload as ChatMessageEditedPushPayload))
    signalR.on(RealtimeMethods.ChatReactionChanged, payload =>
      chatStore.applyReactionChanged(payload as ChatReactionChangedPushPayload))
    signalR.on(RealtimeMethods.ChatReadPositionChanged, payload =>
      chatStore.applyReadPositionChanged(payload as ChatReadPositionChangedPushPayload))
    signalR.on(RealtimeMethods.ChatPresenceChanged, payload =>
      chatStore.applyPresenceChanged(payload as ChatPresenceChangedPushPayload))
    signalR.on(RealtimeMethods.ChatAssistantDelta, payload =>
      chatStore.applyAssistantDelta(payload as ChatAssistantDeltaPushPayload))
    signalR.on(RealtimeMethods.ChatAssistantCompleted, payload =>
      chatStore.applyAssistantCompleted(payload as ChatAssistantCompletedPushPayload))
    signalR.on(RealtimeMethods.ChatCallChanged, payload =>
      callStore.applyCallChanged(payload as ChatCallItem))
    signalR.on(RealtimeMethods.ChatCallSignal, payload =>
      callStore.applyCallSignal(payload as ChatCallSignalPushPayload))
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

  // 连接断过又连上：断开期间的上下线没收到，把在线状态重取一遍（第一次连上不算，那时刚读过）
  // 通话每次连上都与服务端对一遍：还在响的来电认回来，断线期间结束了的收场，刷新前占着的那一通替自己挂断
  let everConnected = false
  const stopConnectedWatch = watch(signalR.connected, (connected) => {
    if (!connected) {
      return
    }
    if (everConnected) {
      chatStore.resyncPresence()
    }
    everConnected = true
    callStore.restore().catch(() => {})
  })

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
        everConnected = false
        callStore.reset()
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
    stopConnectedWatch()
    clearReconnectTimer()
    isListenersBound = false
    window.removeEventListener('focus', handleWindowFocus)
    document.removeEventListener('visibilitychange', handleVisibilityChange)
    void signalR.destroy()
  })
}
