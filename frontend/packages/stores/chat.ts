import type {
  ChatConversationChangedPushPayload,
  ChatConversationListItem,
  ChatMessageItem,
  ChatMessagePushPayload,
  ChatMessageSendInput,
  ChatRecalledPushPayload,
  ChatTypingPushPayload,
} from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { useSignalR } from '~/composables'
import { CHAT_HUB_METHODS, CHAT_HUB_PATH } from '~/constants'
import { useAppContext } from './app-context'
import { useUserStore } from './user'

/** 本地消息：在契约消息上叠加乐观上屏状态（仅前端可见） */
export interface ChatLocalMessage extends ChatMessageItem {
  /** 乐观上屏中（REST 尚未返回） */
  pending?: boolean
  /** 发送失败（可重发或移除） */
  failed?: boolean
}

/** 乐观消息的本地占位 ID 前缀（REST/推送回执到达后被正式 messageId 替换） */
const LOCAL_MESSAGE_ID_PREFIX = 'local:'
const HISTORY_PAGE_SIZE = 30
const MARK_READ_DEBOUNCE_MS = 800
const TYPING_DISPLAY_MS = 4000
const TYPING_SEND_THROTTLE_MS = 3000

function generateClientMessageId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID()
  }
  return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
}

/**
 * 聊天状态 store —— 会话列表（含未读合计供顶栏角标）+ 按会话的消息缓存 +
 * 乐观上屏/撤回/已读/typing。数据经 appContext.apis.chatApi，实时事件由
 * 布局层 use-chat-integration 订阅后回灌本 store。
 */
export const useChatStore = defineStore('chat', () => {
  const conversations = ref<ChatConversationListItem[]>([])
  const conversationsLoading = ref(false)
  const conversationsLoaded = ref(false)
  const activeConversationId = ref<null | string>(null)
  const messages = ref<Record<string, ChatLocalMessage[]>>({})
  const hasMoreOlder = ref<Record<string, boolean>>({})
  const historyLoading = ref<Record<string, boolean>>({})
  const typingIndicators = ref<Record<string, ChatTypingPushPayload>>({})

  const markReadTimers = new Map<string, ReturnType<typeof setTimeout>>()
  const typingClearTimers = new Map<string, ReturnType<typeof setTimeout>>()
  const typingSentAt = new Map<string, number>()

  function api() {
    return useAppContext().apis.chatApi
  }

  function currentUserId(): string {
    return useUserStore().userInfo?.basicId ?? ''
  }

  // 未读合计（免打扰会话不计入顶栏角标）
  const totalUnread = computed(() =>
    conversations.value.filter(c => !c.isMuted).reduce((sum, c) => sum + c.unreadCount, 0),
  )
  const activeConversation = computed(() =>
    conversations.value.find(c => c.conversationId === activeConversationId.value) ?? null,
  )
  const activeMessages = computed(() =>
    activeConversationId.value ? messages.value[activeConversationId.value] ?? [] : [],
  )
  const activeTyping = computed(() =>
    activeConversationId.value ? typingIndicators.value[activeConversationId.value] ?? null : null,
  )

  function sortConversations() {
    conversations.value.sort((a, b) => {
      const ta = a.lastMessageTime ? Date.parse(a.lastMessageTime) : 0
      const tb = b.lastMessageTime ? Date.parse(b.lastMessageTime) : 0
      return tb - ta
    })
  }

  async function loadConversations() {
    if (conversationsLoading.value) {
      return
    }
    conversationsLoading.value = true
    try {
      conversations.value = await api().myConversations()
      sortConversations()
      conversationsLoaded.value = true
      // 活跃会话已被移出/解散：收敛回列表态
      if (
        activeConversationId.value
        && !conversations.value.some(c => c.conversationId === activeConversationId.value)
      ) {
        activeConversationId.value = null
      }
    }
    finally {
      conversationsLoading.value = false
    }
  }

  async function ensureConversations() {
    if (!conversationsLoaded.value) {
      await loadConversations()
    }
  }

  /** 合并一页历史（服务端正序），与推送先行到达/乐观消息按 messageId 去重 */
  function mergeHistory(conversationId: string, items: ChatMessageItem[]) {
    const existing = messages.value[conversationId] ?? []
    const known = new Set(existing.map(m => m.messageId))
    const fresh = items.filter(m => !known.has(m.messageId))
    messages.value[conversationId] = [...fresh, ...existing]
  }

  async function loadHistory(conversationId: string) {
    if (historyLoading.value[conversationId]) {
      return
    }
    historyLoading.value[conversationId] = true
    try {
      const result = await api().messageHistory({ conversationId, take: HISTORY_PAGE_SIZE })
      mergeHistory(conversationId, result.items)
      hasMoreOlder.value[conversationId] = result.hasMore
    }
    finally {
      historyLoading.value[conversationId] = false
    }
  }

  async function loadOlder(conversationId: string) {
    const list = messages.value[conversationId]
    if (!list?.length || historyLoading.value[conversationId] || hasMoreOlder.value[conversationId] === false) {
      return
    }
    const firstPersisted = list.find(m => !m.messageId.startsWith(LOCAL_MESSAGE_ID_PREFIX))
    if (!firstPersisted) {
      return
    }
    historyLoading.value[conversationId] = true
    try {
      const result = await api().messageHistory({
        conversationId,
        beforeMessageId: firstPersisted.messageId,
        take: HISTORY_PAGE_SIZE,
      })
      mergeHistory(conversationId, result.items)
      hasMoreOlder.value[conversationId] = result.hasMore
    }
    finally {
      historyLoading.value[conversationId] = false
    }
  }

  /** Hub 组操作失败静默降级：消息仍按 userId 点发，仅 typing 组播不可用 */
  async function invokeHub(method: string, conversationId: string) {
    try {
      await useSignalR(CHAT_HUB_PATH).invoke(method, conversationId)
    }
    catch {
      // 未连接/断线中，忽略
    }
  }

  async function openConversation(conversationId: string) {
    if (activeConversationId.value === conversationId) {
      return
    }
    if (activeConversationId.value) {
      void invokeHub(CHAT_HUB_METHODS.leaveConversation, activeConversationId.value)
    }
    activeConversationId.value = conversationId
    void invokeHub(CHAT_HUB_METHODS.joinConversation, conversationId)
    if (!messages.value[conversationId]) {
      try {
        await loadHistory(conversationId)
      }
      catch {
        // 历史加载失败不阻塞打开，进入后可下拉重试
      }
    }
    markConversationRead(conversationId)
  }

  function closeActiveConversation() {
    if (activeConversationId.value) {
      void invokeHub(CHAT_HUB_METHODS.leaveConversation, activeConversationId.value)
    }
    activeConversationId.value = null
  }

  /** 本地立即清零 + 防抖上报（null 表示读到最新） */
  function markConversationRead(conversationId: string) {
    const conv = conversations.value.find(c => c.conversationId === conversationId)
    if (conv) {
      conv.unreadCount = 0
    }
    const pendingTimer = markReadTimers.get(conversationId)
    if (pendingTimer) {
      clearTimeout(pendingTimer)
    }
    markReadTimers.set(conversationId, setTimeout(() => {
      markReadTimers.delete(conversationId)
      api().markRead(conversationId, null).catch(() => {})
    }, MARK_READ_DEBOUNCE_MS))
  }

  function previewOf(message: ChatMessageItem): string {
    const text = message.content || message.fileName || ''
    return text.length > 60 ? `${text.slice(0, 60)}…` : text
  }

  /** REST 回执/推送到达后收敛乐观条目（clientMessageId 匹配替换，messageId 匹配去重） */
  function reconcileSaved(saved: ChatMessageItem) {
    const list = messages.value[saved.conversationId]
    if (!list) {
      return
    }
    const localIndex = saved.clientMessageId
      ? list.findIndex(m => m.clientMessageId === saved.clientMessageId && m.messageId !== saved.messageId)
      : -1
    const dupIndex = list.findIndex(m => m.messageId === saved.messageId)
    if (localIndex >= 0 && dupIndex >= 0) {
      list.splice(localIndex, 1)
    }
    else if (localIndex >= 0) {
      list.splice(localIndex, 1, { ...saved })
    }
    else if (dupIndex < 0) {
      list.push({ ...saved })
    }
    messages.value[saved.conversationId] = [...list]
  }

  function touchConversationBySend(saved: ChatMessageItem) {
    const conv = conversations.value.find(c => c.conversationId === saved.conversationId)
    if (conv) {
      conv.lastMessageTime = saved.createdTime
      conv.lastMessagePreview = previewOf(saved)
      sortConversations()
    }
  }

  async function sendMessage(input: Omit<ChatMessageSendInput, 'clientMessageId'>) {
    const userStore = useUserStore()
    const clientMessageId = generateClientMessageId()
    const local: ChatLocalMessage = {
      messageId: `${LOCAL_MESSAGE_ID_PREFIX}${clientMessageId}`,
      conversationId: input.conversationId,
      senderUserId: currentUserId(),
      senderUserName: userStore.nickname || userStore.username,
      messageType: input.messageType,
      content: input.content,
      fileId: input.fileId,
      fileName: input.fileName,
      fileSize: input.fileSize,
      isRecalled: false,
      clientMessageId,
      createdTime: new Date().toISOString(),
      pending: true,
    }
    messages.value[input.conversationId] = [...(messages.value[input.conversationId] ?? []), local]
    try {
      const saved = await api().sendMessage({ ...input, clientMessageId })
      reconcileSaved(saved)
      touchConversationBySend(saved)
    }
    catch (error) {
      const item = messages.value[input.conversationId]?.find(m => m.clientMessageId === clientMessageId)
      if (item) {
        item.pending = false
        item.failed = true
      }
      throw error
    }
  }

  async function retryMessage(conversationId: string, clientMessageId: string) {
    const item = messages.value[conversationId]?.find(m => m.clientMessageId === clientMessageId && m.failed)
    if (!item) {
      return
    }
    item.failed = false
    item.pending = true
    try {
      const saved = await api().sendMessage({
        conversationId,
        messageType: item.messageType,
        content: item.content,
        fileId: item.fileId,
        fileName: item.fileName,
        fileSize: item.fileSize,
        clientMessageId,
      })
      reconcileSaved(saved)
      touchConversationBySend(saved)
    }
    catch (error) {
      item.pending = false
      item.failed = true
      throw error
    }
  }

  function removeLocalMessage(conversationId: string, clientMessageId: string) {
    const list = messages.value[conversationId]
    if (!list) {
      return
    }
    messages.value[conversationId] = list.filter(
      m => !(m.clientMessageId === clientMessageId && (m.failed || m.pending)),
    )
  }

  async function recallMessage(conversationId: string, messageId: string) {
    await api().recallMessage(messageId)
    applyMessageRecalled({ conversationId, messageId })
  }

  // ===== SignalR 事件回灌（由布局层 use-chat-integration 订阅后调用） =====

  function applyIncomingMessage(payload: ChatMessagePushPayload) {
    const { message, conversation } = payload
    reconcileSaved(message)
    clearTyping(conversation.conversationId, message.senderUserId)

    const conv = conversations.value.find(c => c.conversationId === conversation.conversationId)
    if (!conv) {
      // 新会话（他人发起）：整表刷新拿完整列表项
      loadConversations().catch(() => {})
      return
    }
    conv.lastMessageTime = conversation.lastMessageTime ?? message.createdTime
    conv.lastMessagePreview = conversation.lastMessagePreview ?? previewOf(message)
    const isSelf = message.senderUserId === currentUserId()
    if (!isSelf) {
      const isActiveAndVisible = activeConversationId.value === conv.conversationId
        && typeof document !== 'undefined' && document.hasFocus()
      if (isActiveAndVisible) {
        markConversationRead(conv.conversationId)
      }
      else {
        conv.unreadCount += 1
      }
    }
    sortConversations()
  }

  function applyMessageRecalled(payload: ChatRecalledPushPayload) {
    const item = messages.value[payload.conversationId]?.find(m => m.messageId === payload.messageId)
    if (item) {
      item.isRecalled = true
      item.content = null
    }
  }

  function applyConversationChanged(_payload: ChatConversationChangedPushPayload) {
    // 成员增删/新建会话：整表刷新（含被移出时活跃会话收敛）
    loadConversations().catch(() => {})
  }

  function applyTyping(payload: ChatTypingPushPayload) {
    if (payload.userId === currentUserId()) {
      return
    }
    typingIndicators.value[payload.conversationId] = payload
    const oldTimer = typingClearTimers.get(payload.conversationId)
    if (oldTimer) {
      clearTimeout(oldTimer)
    }
    typingClearTimers.set(payload.conversationId, setTimeout(() => {
      typingClearTimers.delete(payload.conversationId)
      clearTyping(payload.conversationId)
    }, TYPING_DISPLAY_MS))
  }

  function clearTyping(conversationId: string, userId?: string) {
    const current = typingIndicators.value[conversationId]
    if (!current) {
      return
    }
    if (userId && current.userId !== userId) {
      return
    }
    delete typingIndicators.value[conversationId]
  }

  /** 输入中提示节流上报（组播给会话内其他成员） */
  function sendTyping(conversationId: string) {
    const last = typingSentAt.get(conversationId) ?? 0
    if (Date.now() - last < TYPING_SEND_THROTTLE_MS) {
      return
    }
    typingSentAt.set(conversationId, Date.now())
    void invokeHub(CHAT_HUB_METHODS.typing, conversationId)
  }

  // ===== 发起会话（成功后刷新列表并进入） =====

  async function startSingleConversation(peerUserId: string) {
    const result = await api().openSingleConversation(peerUserId)
    await loadConversations()
    await openConversation(result.conversationId)
    return result
  }

  async function startGroupConversation(conversationName: string, memberUserIds: string[]) {
    const result = await api().createGroupConversation(conversationName, memberUserIds)
    await loadConversations()
    await openConversation(result.conversationId)
    return result
  }

  async function startDepartmentConversation(departmentId: string) {
    const result = await api().openDepartmentConversation(departmentId)
    await loadConversations()
    await openConversation(result.conversationId)
    return result
  }

  function $reset() {
    conversations.value = []
    conversationsLoading.value = false
    conversationsLoaded.value = false
    activeConversationId.value = null
    messages.value = {}
    hasMoreOlder.value = {}
    historyLoading.value = {}
    typingIndicators.value = {}
    for (const timer of markReadTimers.values()) {
      clearTimeout(timer)
    }
    markReadTimers.clear()
    for (const timer of typingClearTimers.values()) {
      clearTimeout(timer)
    }
    typingClearTimers.clear()
    typingSentAt.clear()
  }

  return {
    conversations,
    conversationsLoading,
    conversationsLoaded,
    activeConversationId,
    messages,
    hasMoreOlder,
    historyLoading,
    typingIndicators,
    totalUnread,
    activeConversation,
    activeMessages,
    activeTyping,
    loadConversations,
    ensureConversations,
    loadHistory,
    loadOlder,
    openConversation,
    closeActiveConversation,
    markConversationRead,
    sendMessage,
    retryMessage,
    removeLocalMessage,
    recallMessage,
    sendTyping,
    applyIncomingMessage,
    applyMessageRecalled,
    applyConversationChanged,
    applyTyping,
    startSingleConversation,
    startGroupConversation,
    startDepartmentConversation,
    $reset,
  }
})
