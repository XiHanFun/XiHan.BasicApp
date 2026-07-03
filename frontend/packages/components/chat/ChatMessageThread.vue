<script setup lang="ts">
import type { DropdownOption } from 'naive-ui'
import type { VNodeChild } from 'vue'
import type { ChatLocalMessage } from '~/stores'
import type { ChatMessageItem } from '~/types'
import { NButton, NDropdown, NEmpty, NInput, NPopover, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, h, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useAppContext, useChatStore, useUserStore } from '~/stores'
import { CHAT_EDIT_WINDOW_MINUTES, CHAT_RECALL_WINDOW_MINUTES } from '~/types'
import { ChatConversationType, ChatMemberRole, ChatMessageType } from '~/types/enums'
import XUserAvatar from '../common/UserAvatar.vue'
import { formatMessageTime } from './chat-helpers'
import ChatComposer from './ChatComposer.vue'
import ChatMessageItemView from './ChatMessageItemView.vue'

defineOptions({ name: 'ChatMessageThread' })

const props = withDefaults(defineProps<{
  /** 抽屉窄布局：显示返回按钮 */
  showBack?: boolean
}>(), {
  showBack: false,
})

const emit = defineEmits<{
  back: []
  members: []
}>()

const { t } = useI18n()
const message = useMessage()
const chatStore = useChatStore()
const userStore = useUserStore()
const appContext = useAppContext()

const scrollRef = ref<HTMLDivElement>()

// 会话内搜索
const showSearch = ref(false)
const searchKeyword = ref('')
const searchLoading = ref(false)
const searchResults = ref<ChatMessageItem[]>([])
const searchHasMore = ref(false)
let searchSeq = 0

// 撤回窗口是时间敏感 UI：30s 心跳重算 canRecall，避免按钮滞留
const nowTick = ref(Date.now())
let tickTimer: ReturnType<typeof setInterval> | null = null

const conversation = computed(() => chatStore.activeConversation)
const conversationId = computed(() => chatStore.activeConversationId)
const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')
const isGroupLike = computed(() =>
  conversation.value?.conversationType === ChatConversationType.Group
  || conversation.value?.conversationType === ChatConversationType.Department,
)

const conversationTypeLabel = computed(() => {
  switch (conversation.value?.conversationType) {
    case ChatConversationType.Single:
      return t('chat.type.single')
    case ChatConversationType.Group:
      return t('chat.type.group')
    case ChatConversationType.Department:
      return t('chat.type.department')
    default:
      return ''
  }
})

const historyLoading = computed(() =>
  conversationId.value ? chatStore.historyLoading[conversationId.value] ?? false : false,
)
const hasMoreOlder = computed(() =>
  conversationId.value ? chatStore.hasMoreOlder[conversationId.value] ?? false : false,
)

function canRecall(item: ChatLocalMessage): boolean {
  if (item.isRecalled || item.pending || item.failed) {
    return false
  }
  if (item.senderUserId !== currentUserId.value) {
    return false
  }
  if (!userStore.hasPermission(CHAT_PERMISSIONS.send)) {
    return false
  }
  const created = Date.parse(item.createdTime)
  return !Number.isNaN(created) && nowTick.value - created <= CHAT_RECALL_WINDOW_MINUTES * 60 * 1000
}

function canEdit(item: ChatLocalMessage): boolean {
  if (item.isRecalled || item.pending || item.failed || item.messageType !== ChatMessageType.Text) {
    return false
  }
  if (item.senderUserId !== currentUserId.value || !userStore.hasPermission(CHAT_PERMISSIONS.send)) {
    return false
  }
  const created = Date.parse(item.createdTime)
  return !Number.isNaN(created) && nowTick.value - created <= CHAT_EDIT_WINDOW_MINUTES * 60 * 1000
}

// Pin 权限：单聊双方皆可，群/部门群仅群主与管理员（与后端不变量一致）
const canPin = computed(() => {
  if (!userStore.hasPermission(CHAT_PERMISSIONS.send) || !conversation.value) {
    return false
  }
  if (conversation.value.conversationType === ChatConversationType.Single) {
    return true
  }
  return conversation.value.memberRole === ChatMemberRole.Owner
    || conversation.value.memberRole === ChatMemberRole.Admin
})

const pinnedList = computed(() =>
  conversationId.value ? chatStore.pinnedMessages[conversationId.value] ?? [] : [])

/** 本人消息已读回执：已读位加载前为 null（不显示） */
function readCountOf(item: ChatLocalMessage): null | number {
  const id = conversationId.value
  if (!id || item.senderUserId !== currentUserId.value) {
    return null
  }
  if (!chatStore.readPositions[id]) {
    return null
  }
  return chatStore.readCountFor(id, item.messageId)
}

function handleReply(item: ChatLocalMessage) {
  chatStore.editTarget = null
  chatStore.replyTarget = item
}

function handleEdit(item: ChatLocalMessage) {
  chatStore.replyTarget = null
  chatStore.editTarget = item
}

function handleReact(item: ChatLocalMessage, emoji: string) {
  if (!conversationId.value) {
    return
  }
  chatStore.toggleReaction(conversationId.value, item.messageId, emoji).catch(() => {})
}

function handlePin(item: ChatLocalMessage, pin: boolean) {
  if (!conversationId.value) {
    return
  }
  const action = pin
    ? chatStore.pinMessage(conversationId.value, item.messageId)
    : chatStore.unpinMessage(conversationId.value, item.messageId)
  action.catch(() => {})
}

// ===== 右键菜单（QQ 式：快捷表情行 + 操作项） =====

const QUICK_REACTIONS = ['👍', '❤️', '😂', '😮', '😢', '🎉']

const ctxShow = ref(false)
const ctxX = ref(0)
const ctxY = ref(0)
const ctxMessage = ref<ChatLocalMessage | null>(null)

function renderCtxIcon(icon: string) {
  return () => h(Icon, { icon, width: 14, height: 14 })
}

/** 菜单头：快捷表情回应行 */
function renderQuickReactions(): VNodeChild {
  const target = ctxMessage.value
  if (!target) {
    return null
  }
  return h(
    'div',
    { class: 'chat-ctx-reactions' },
    QUICK_REACTIONS.map(emoji =>
      h('button', {
        type: 'button',
        class: 'chat-ctx-react-btn',
        onClick: () => {
          ctxShow.value = false
          handleReact(target, emoji)
        },
      }, emoji)),
  )
}

const ctxOptions = computed<DropdownOption[]>(() => {
  const target = ctxMessage.value
  if (!target) {
    return []
  }
  const options: DropdownOption[] = [
    { key: 'quick-reactions', type: 'render', render: renderQuickReactions },
    { key: 'divider-reactions', type: 'divider' },
  ]
  if (target.messageType === ChatMessageType.Text && target.content) {
    options.push({ key: 'copy', label: t('chat.thread.copy'), icon: renderCtxIcon('lucide:copy') })
  }
  options.push({ key: 'reply', label: t('chat.thread.reply'), icon: renderCtxIcon('lucide:reply') })
  if (canEdit(target)) {
    options.push({ key: 'edit', label: t('chat.thread.edit'), icon: renderCtxIcon('lucide:pencil') })
  }
  if (canPin.value) {
    options.push(target.isPinned
      ? { key: 'unpin', label: t('chat.thread.unpin'), icon: renderCtxIcon('lucide:pin-off') }
      : { key: 'pin', label: t('chat.thread.pin'), icon: renderCtxIcon('lucide:pin') })
  }
  if (canRecall(target)) {
    options.push({ key: 'recall', label: t('chat.thread.recall'), icon: renderCtxIcon('lucide:undo-2') })
  }
  return options
})

function openContextMenu(event: MouseEvent, item: ChatLocalMessage) {
  // 系统提示/已撤回/未落库消息无可用操作
  if (item.messageType === ChatMessageType.System || item.isRecalled || item.pending || item.failed) {
    return
  }
  event.preventDefault()
  ctxShow.value = false
  void nextTick(() => {
    ctxMessage.value = item
    ctxX.value = event.clientX
    ctxY.value = event.clientY
    ctxShow.value = true
  })
}

async function handleCtxSelect(key: string | number) {
  ctxShow.value = false
  const target = ctxMessage.value
  if (!target) {
    return
  }
  switch (key) {
    case 'copy':
      try {
        await navigator.clipboard.writeText(target.content ?? '')
        message.success(t('chat.thread.copied'))
      }
      catch {
        message.error(t('chat.thread.copy_failed'))
      }
      break
    case 'reply':
      handleReply(target)
      break
    case 'edit':
      handleEdit(target)
      break
    case 'pin':
      handlePin(target, true)
      break
    case 'unpin':
      handlePin(target, false)
      break
    case 'recall':
      void handleRecall(target)
      break
  }
}

// ===== 会话内搜索 =====

const isDetached = computed(() =>
  conversationId.value ? Boolean(chatStore.detachedConversations[conversationId.value]) : false)

async function runSearch(loadMore = false) {
  const id = conversationId.value
  const keyword = searchKeyword.value.trim()
  if (!id || !keyword) {
    searchResults.value = []
    searchHasMore.value = false
    return
  }
  const seq = ++searchSeq
  searchLoading.value = true
  try {
    const before = loadMore ? searchResults.value.at(-1)?.messageId : null
    const result = await appContext.apis.chatApi.searchMessages({
      conversationId: id,
      keyword,
      beforeMessageId: before,
      take: 20,
    })
    if (seq !== searchSeq) {
      return
    }
    searchResults.value = loadMore ? [...searchResults.value, ...result.items] : result.items
    searchHasMore.value = result.hasMore
  }
  catch {
    if (seq === searchSeq) {
      searchResults.value = []
      searchHasMore.value = false
    }
  }
  finally {
    if (seq === searchSeq) {
      searchLoading.value = false
    }
  }
}

async function handleLocate(item: ChatMessageItem) {
  const id = conversationId.value
  if (!id) {
    return
  }
  showSearch.value = false
  try {
    await chatStore.jumpToMessage(id, item.messageId)
    await nextTick()
    document.getElementById(`chat-msg-${item.messageId}`)?.scrollIntoView({ block: 'center' })
  }
  catch {
    // 定位失败静默（消息可能已被清理）
  }
}

async function handleBackToLatest() {
  const id = conversationId.value
  if (!id) {
    return
  }
  try {
    await chatStore.reloadLatest(id)
    scrollToBottom()
  }
  catch {
    // 静默
  }
}

function toggleSearch() {
  showSearch.value = !showSearch.value
  if (!showSearch.value) {
    searchKeyword.value = ''
    searchResults.value = []
    searchHasMore.value = false
  }
}

function isNearBottom(): boolean {
  const el = scrollRef.value
  if (!el) {
    return true
  }
  return el.scrollHeight - el.scrollTop - el.clientHeight < 80
}

function scrollToBottom() {
  void nextTick(() => {
    const el = scrollRef.value
    if (el) {
      el.scrollTop = el.scrollHeight
    }
  })
}

async function handleLoadOlder() {
  const id = conversationId.value
  const el = scrollRef.value
  if (!id || !el) {
    return
  }
  const prevHeight = el.scrollHeight
  const prevTop = el.scrollTop
  try {
    await chatStore.loadOlder(id)
  }
  catch {
    return
  }
  // 保持视口停留在原消息处（prepend 不跳动）
  await nextTick()
  el.scrollTop = el.scrollHeight - prevHeight + prevTop
}

function handleScroll() {
  const el = scrollRef.value
  if (el && el.scrollTop < 40 && hasMoreOlder.value && !historyLoading.value) {
    void handleLoadOlder()
  }
}

async function handleRecall(item: ChatLocalMessage) {
  if (!conversationId.value) {
    return
  }
  try {
    await chatStore.recallMessage(conversationId.value, item.messageId)
  }
  catch {
    message.error(t('chat.thread.recall_failed'))
  }
}

// 会话切换：滚到底部
watch(conversationId, (id) => {
  if (id) {
    scrollToBottom()
  }
})

// 新消息追加：本人消息或视口在底部附近时跟随滚动
watch(() => chatStore.activeMessages.length, (len, prevLen) => {
  if (len > (prevLen ?? 0)) {
    const last = chatStore.activeMessages[len - 1]
    if (last?.senderUserId === currentUserId.value || isNearBottom()) {
      scrollToBottom()
    }
  }
})

onMounted(() => {
  tickTimer = setInterval(() => {
    nowTick.value = Date.now()
  }, 30000)
  scrollToBottom()
})

onBeforeUnmount(() => {
  if (tickTimer) {
    clearInterval(tickTimer)
    tickTimer = null
  }
})
</script>

<template>
  <!-- 未选择会话的空态 -->
  <div v-if="!conversation" class="flex h-full items-center justify-center">
    <NEmpty :description="t('chat.thread.select_conversation')" size="small">
      <template #icon>
        <Icon icon="lucide:messages-square" width="36" height="36" class="text-muted-foreground/50" />
      </template>
    </NEmpty>
  </div>

  <div v-else class="flex h-full min-h-0 flex-col">
    <!-- 会话头 -->
    <div class="flex items-center gap-2 border-b border-border px-3 py-2.5">
      <button v-if="props.showBack" type="button" class="chat-thread-btn" @click="emit('back')">
        <Icon icon="lucide:arrow-left" width="16" height="16" />
      </button>
      <XUserAvatar :avatar="conversation.avatar" :name="conversation.displayName" :size="30" />
      <div class="min-w-0 flex-1">
        <div class="flex items-center gap-1.5">
          <span class="truncate text-[13px] font-medium text-foreground">{{ conversation.displayName }}</span>
          <NTag size="tiny" :bordered="false" round>
            {{ conversationTypeLabel }}
          </NTag>
        </div>
        <div v-if="isGroupLike" class="text-[11px] text-muted-foreground">
          {{ t('chat.members.count', { n: conversation.memberCount }) }}
        </div>
      </div>
      <button type="button" class="chat-thread-btn" :title="t('chat.thread.search')" @click="toggleSearch">
        <Icon icon="lucide:search" width="16" height="16" />
      </button>
      <button v-if="isGroupLike" type="button" class="chat-thread-btn" @click="emit('members')">
        <Icon icon="lucide:users" width="16" height="16" />
      </button>
    </div>

    <!-- 会话内搜索面板 -->
    <div v-if="showSearch" class="border-b border-border bg-muted/20 px-3 py-2">
      <NInput
        v-model:value="searchKeyword"
        size="small"
        clearable
        :placeholder="t('chat.thread.search_placeholder')"
        @keydown.enter="runSearch(false)"
        @clear="searchResults = []"
      >
        <template #prefix>
          <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
        </template>
      </NInput>
      <div v-if="searchLoading" class="flex justify-center py-3">
        <NSpin size="small" />
      </div>
      <div v-else-if="searchResults.length" class="mt-1.5 max-h-56 overflow-y-auto">
        <button
          v-for="hit in searchResults"
          :key="hit.messageId"
          type="button"
          class="chat-search-hit"
          @click="handleLocate(hit)"
        >
          <span class="flex items-center justify-between gap-2">
            <span class="text-[11px] text-muted-foreground">{{ hit.senderUserName }} · {{ formatMessageTime(hit.createdTime) }}</span>
            <span class="text-[11px] text-primary">{{ t('chat.thread.locate') }}</span>
          </span>
          <span class="block truncate text-left text-xs text-foreground">{{ hit.content || hit.fileName || '' }}</span>
        </button>
        <div v-if="searchHasMore" class="flex justify-center py-1">
          <NButton text size="tiny" @click="runSearch(true)">
            {{ t('chat.thread.load_more') }}
          </NButton>
        </div>
      </div>
      <div v-else-if="searchKeyword.trim()" class="py-3 text-center text-xs text-muted-foreground">
        {{ t('chat.thread.search_empty') }}
      </div>
    </div>

    <!-- Pin 消息栏 -->
    <div v-if="pinnedList.length" class="flex items-center gap-2 border-b border-border bg-primary/5 px-3 py-1.5">
      <Icon icon="lucide:pin" width="13" height="13" class="shrink-0 text-primary" />
      <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">
        {{ pinnedList[0]?.content || pinnedList[0]?.fileName || '' }}
      </span>
      <NPopover trigger="click" placement="bottom-end" style="max-width: 340px">
        <template #trigger>
          <button type="button" class="chat-thread-btn h-6 w-auto px-1.5 text-[11px]">
            {{ t('chat.thread.pinned_count', { n: pinnedList.length }) }}
          </button>
        </template>
        <div class="flex max-h-64 flex-col gap-1 overflow-y-auto">
          <div
            v-for="pinnedItem in pinnedList"
            :key="pinnedItem.messageId"
            class="flex items-start gap-2 border-b border-border/50 py-1.5 last:border-b-0"
          >
            <div class="min-w-0 flex-1">
              <div class="text-[11px] text-muted-foreground">
                {{ pinnedItem.senderUserName }} · {{ formatMessageTime(pinnedItem.createdTime) }}
              </div>
              <div class="truncate text-xs text-foreground">
                {{ pinnedItem.content || pinnedItem.fileName || '' }}
              </div>
            </div>
            <button
              v-if="canPin"
              type="button"
              class="chat-meta-unpin shrink-0"
              @click="conversationId && chatStore.unpinMessage(conversationId, pinnedItem.messageId).catch(() => {})"
            >
              {{ t('chat.thread.unpin') }}
            </button>
          </div>
        </div>
      </NPopover>
    </div>

    <!-- 消息流 -->
    <div ref="scrollRef" class="min-h-0 flex-1 overflow-y-auto px-3 py-2" @scroll.passive="handleScroll">
      <div v-if="hasMoreOlder || historyLoading" class="flex justify-center py-1.5">
        <NSpin v-if="historyLoading" size="small" />
        <NButton v-else text size="tiny" @click="handleLoadOlder">
          {{ t('chat.thread.load_more') }}
        </NButton>
      </div>

      <div v-if="!chatStore.activeMessages.length && !historyLoading" class="py-12">
        <NEmpty :description="t('chat.thread.empty')" size="small" />
      </div>

      <div
        v-for="item in chatStore.activeMessages"
        :id="`chat-msg-${item.messageId}`"
        :key="item.messageId"
        @contextmenu="openContextMenu($event, item)"
      >
        <ChatMessageItemView
          :message="item"
          :is-self="item.senderUserId === currentUserId"
          :show-sender-name="isGroupLike"
          :conversation-type="conversation.conversationType"
          :read-count="readCountOf(item)"
          :highlighted="chatStore.highlightMessageId === item.messageId"
          @retry="item.clientMessageId && chatStore.retryMessage(conversation.conversationId, item.clientMessageId).catch(() => {})"
          @remove="item.clientMessageId && chatStore.removeLocalMessage(conversation.conversationId, item.clientMessageId)"
          @react="emoji => handleReact(item, emoji)"
        />
      </div>

      <!-- 消息右键菜单（QQ 式） -->
      <NDropdown
        placement="bottom-start"
        trigger="manual"
        :show="ctxShow"
        :x="ctxX"
        :y="ctxY"
        :options="ctxOptions"
        @select="handleCtxSelect"
        @clickoutside="ctxShow = false"
      />

      <!-- 视口分离态：回到最新 -->
      <div v-if="isDetached" class="sticky bottom-1 flex justify-center">
        <NButton size="tiny" round secondary type="primary" @click="handleBackToLatest">
          <template #icon>
            <Icon icon="lucide:arrow-down-to-line" width="13" height="13" />
          </template>
          {{ t('chat.thread.back_to_latest') }}
        </NButton>
      </div>
    </div>

    <!-- 输入中提示 -->
    <div v-if="chatStore.activeTyping" class="px-4 pb-1 text-[11px] text-muted-foreground">
      {{ t('chat.thread.typing', { name: chatStore.activeTyping.userName ?? '' }) }}
    </div>

    <!-- 输入区 -->
    <ChatComposer :conversation-id="conversation.conversationId" />
  </div>
</template>

<style scoped>
.chat-thread-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.15s ease;
}

.chat-thread-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.chat-meta-unpin {
  padding: 0;
  border: none;
  background: transparent;
  color: hsl(var(--primary));
  font-size: 11px;
  cursor: pointer;
}

.chat-meta-unpin:hover {
  text-decoration: underline;
}

.chat-search-hit {
  display: block;
  width: 100%;
  padding: 5px 8px;
  border: none;
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  transition: background 0.12s ease;
}

.chat-search-hit:hover {
  background: hsl(var(--accent));
}

/* 右键菜单头部的快捷表情行（NDropdown render 选项为全局弹层，样式需全局作用域） */
:global(.chat-ctx-reactions) {
  display: flex;
  gap: 2px;
  padding: 6px 8px 2px;
}

:global(.chat-ctx-react-btn) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  font-size: 17px;
  cursor: pointer;
  transition: background 0.12s ease;
}

:global(.chat-ctx-react-btn:hover) {
  background: hsl(var(--accent));
}
</style>
