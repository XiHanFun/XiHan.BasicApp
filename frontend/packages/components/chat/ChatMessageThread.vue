<script setup lang="ts">
import type { ChatLocalMessage } from '~/stores'
import { NButton, NEmpty, NPopover, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useChatStore, useUserStore } from '~/stores'
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

const scrollRef = ref<HTMLDivElement>()

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
      <button v-if="isGroupLike" type="button" class="chat-thread-btn" @click="emit('members')">
        <Icon icon="lucide:users" width="16" height="16" />
      </button>
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

      <ChatMessageItemView
        v-for="item in chatStore.activeMessages"
        :key="item.messageId"
        :message="item"
        :is-self="item.senderUserId === currentUserId"
        :show-sender-name="isGroupLike"
        :can-recall="canRecall(item)"
        :can-edit="canEdit(item)"
        :can-pin="canPin"
        :conversation-type="conversation.conversationType"
        :read-count="readCountOf(item)"
        @recall="handleRecall(item)"
        @retry="item.clientMessageId && chatStore.retryMessage(conversation.conversationId, item.clientMessageId).catch(() => {})"
        @remove="item.clientMessageId && chatStore.removeLocalMessage(conversation.conversationId, item.clientMessageId)"
        @reply="handleReply(item)"
        @edit="handleEdit(item)"
        @pin="handlePin(item, true)"
        @unpin="handlePin(item, false)"
        @react="emoji => handleReact(item, emoji)"
      />
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
</style>
