<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatContextMenuItem } from './ChatContextMenu.vue'
import type { AppDropdownOption } from '~/types'
import { watchDebounced } from '@vueuse/core'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import XDropdown from '~/components/common/XDropdown.vue'
import XIconButton from '~/components/common/XIconButton.vue'
import XInput from '~/components/common/XInput.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { getChatAssistantProvider, useUserStore } from '~/stores'
import { useChatGlobalSearch } from '../composables/useChatGlobalSearch'
import {
  CHAT_PERMISSIONS,
} from '../constants'
import { useChatStore } from '../store'
import { chatErrorText, formatConversationTime, messageBodyLabel, previewText } from './chat-helpers'
import ChatContextMenu from './ChatContextMenu.vue'
import ChatConversationAvatar from './ChatConversationAvatar.vue'
import ChatPresenceBadge from './ChatPresenceBadge.vue'

defineOptions({ name: 'ChatConversationList' })

const emit = defineEmits<{
  select: [conversationId: string]
  start: [mode: 'assistant' | 'department' | 'group' | 'single']
  /** 点了聊天记录里的一条命中：打开它所在的会话并定位过去 */
  locate: [conversationId: string, messageId: string]
}>()

const { t } = useI18n()
const chatStore = useChatStore()
const userStore = useUserStore()

const keyword = ref('')

const filteredConversations = computed(() => {
  const key = keyword.value.trim().toLowerCase()
  if (!key) {
    return chatStore.conversations
  }
  return chatStore.conversations.filter(c => c.displayName.toLowerCase().includes(key))
})

// 有关键字时另在全部会话的聊天记录里找：停手 300 毫秒再发，命中带上它所在的会话
const searching = computed(() => keyword.value.trim().length > 0)
const globalSearch = useChatGlobalSearch()
const messageHits = computed(() => globalSearch.hits.value.flatMap((hit) => {
  const conversation = chatStore.conversations.find(c => c.conversationId === hit.conversationId)
  return conversation
    ? [
        { hit, conversation },
      ]
    : []
}))

/** 聊天记录的结果还没跟上输入框里的词：防抖等着发，或请求还没回来 */
const hitsPending = computed(() => globalSearch.searched.value !== keyword.value.trim())

watchDebounced(keyword, (value) => {
  globalSearch.search(value).catch(notifyFailure)
}, { debounce: 300 })

function handleLoadMoreHits() {
  globalSearch.loadMore().catch(notifyFailure)
}

const startOptions = computed<AppDropdownOption[]>(() => {
  const options: AppDropdownOption[] = [
    {
      key: 'single',
      label: t('chat.start.single'),
    },
    {
      key: 'department',
      label: t('chat.start.department'),
    },
  ]
  // AI 助手入口仅在助手提供方已登记（AI 模块在位）、后端也没有关掉 AI 时出现
  if (getChatAssistantProvider() && !chatStore.assistantsUnavailable) {
    options.push({
      key: 'assistant',
      label: t('chat.start.assistant'),
    })
  }
  // 建群需要会话管理权限（后端 chat.chat-conversations.manage 门控）
  if (userStore.hasPermission(CHAT_PERMISSIONS.manage)) {
    options.splice(1, 0, {
      key: 'group',
      label: t('chat.start.group'),
    })
  }
  return options
})

/**
 * 首次展开「新建」菜单时探测可用助手
 * 只有「助手不可用」才隐藏入口，别的错误照常报
 */
function handleStartMenuOpen(details: { open: boolean }) {
  if (details.open) {
    chatStore.probeAssistants().catch(notifyFailure)
  }
}

function handleStartSelect(key: string | number) {
  emit('start', String(key) as 'assistant' | 'department' | 'group' | 'single')
}

/** 用户操作的失败由界面提示（请求层不弹统一提示） */
function notifyFailure(error: unknown) {
  toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
}

function handleRefresh() {
  chatStore.loadConversations().catch(notifyFailure)
}

// ===== 会话右键菜单（QQ 式） =====

const ctxShow = ref(false)
const ctxX = ref(0)
const ctxY = ref(0)
const ctxConversationId = ref<null | string>(null)

const ctxItems = computed<ChatContextMenuItem[]>(() => {
  const conv = chatStore.conversations.find(c => c.conversationId === ctxConversationId.value)
  if (!conv) {
    return []
  }
  return [
    conv.isPinned
      ? {
          key: 'pin',
          label: t('chat.list.unpin'),
          icon: 'lucide:pin-off',
        }
      : {
          key: 'pin',
          label: t('chat.list.pin'),
          icon: 'lucide:pin',
        },
    conv.isMuted
      ? {
          key: 'mute',
          label: t('chat.list.unmute'),
          icon: 'lucide:bell',
        }
      : {
          key: 'mute',
          label: t('chat.list.mute'),
          icon: 'lucide:bell-off',
        },
  ]
})

function openItemContextMenu(event: MouseEvent, conversationId: string) {
  event.preventDefault()
  ctxConversationId.value = conversationId
  ctxX.value = event.clientX
  ctxY.value = event.clientY
  ctxShow.value = true
}

function handleItemAction(key: string) {
  const id = ctxConversationId.value
  if (!id) {
    return
  }
  if (key === 'pin') {
    chatStore.togglePinConversation(id).catch(notifyFailure)
  }
  else if (key === 'mute') {
    chatStore.toggleMuteConversation(id).catch(notifyFailure)
  }
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col">
    <!-- 头部：搜索 + 发起聊天（固定高度与消息区会话头对齐，底部分割线） -->
    <div class="flex h-[56px] shrink-0 items-center gap-2 border-b border-border px-3">
      <XInput
        v-model:value="keyword"
        size="sm"
        clearable
        :placeholder="t('chat.list.search_placeholder')"
        :aria-label="t('chat.list.search_placeholder')"
      >
        <template #prefix>
          <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
        </template>
      </XInput>
      <XIconButton icon="lucide:refresh-cw" :label="t('chat.list.refresh')" @click="handleRefresh" />
      <!-- 菜单触发器合到纯图标钮上；名字由 aria-label 给，菜单本身已说明这颗钮做什么 -->
      <XDropdown :options="startOptions" @select="handleStartSelect" @open-change="handleStartMenuOpen">
        <XhButton variant="ghost" size="sm" icon-only class="x-icon-btn" :aria-label="t('chat.start.button')">
          <Icon icon="lucide:message-square-plus" />
        </XhButton>
      </XDropdown>
    </div>

    <!-- 会话列表 -->
    <div class="xh-loading-stage min-h-0 flex-1" :class="{ 'is-loading': chatStore.conversationsLoading && !chatStore.conversations.length }">
      <div class="xh-loading-stage__veil">
        <XhSpinner size="md" />
      </div>
      <div class="xh-scroll-area h-full">
        <div v-if="searching && filteredConversations.length" class="chat-conv-section">
          {{ t('chat.list.section_conversations') }}
        </div>
        <div v-if="!filteredConversations.length && !searching" class="py-10">
          <XhEmptyStateRoot size="sm">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('chat.list.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>
        <div
          v-for="conv in filteredConversations"
          :key="conv.conversationId"
          class="chat-conv-item"
          :class="{
            'chat-conv-item--active': conv.conversationId === chatStore.activeConversationId,
            'chat-conv-item--pinned': conv.isPinned,
          }"
          @click="emit('select', conv.conversationId)"
          @contextmenu="openItemContextMenu($event, conv.conversationId)"
        >
          <div class="chat-conv-avatar">
            <!-- 单聊的头像带对端的在线点；群、部门群与助手不出点 -->
            <ChatPresenceBadge :user-id="conv.conversationType === 'Single' ? conv.peerUserId : null">
              <ChatConversationAvatar :conversation-id="conv.conversationId" :conversation-type="conv.conversationType" :avatar="conv.avatar" :name="conv.displayName" :size="38" />
            </ChatPresenceBadge>
            <span v-if="!conv.isMuted && conv.unreadCount > 0" class="chat-conv-avatar__badge">
              {{ conv.unreadCount > 99 ? '99+' : conv.unreadCount }}
            </span>
          </div>
          <div class="min-w-0 flex-1">
            <div class="flex items-center justify-between gap-2">
              <span class="flex min-w-0 items-center gap-1">
                <Icon
                  v-if="conv.isPinned"
                  icon="lucide:pin"
                  width="11"
                  height="11"
                  class="shrink-0 text-primary/70"
                />
                <span class="truncate text-[13px] font-semibold text-foreground">{{ conv.displayName }}</span>
              </span>
              <span class="shrink-0 text-[11px] text-muted-foreground">
                {{ formatConversationTime(t, conv.lastMessageAt) }}
              </span>
            </div>
            <div class="mt-0.5 flex items-center justify-between gap-2">
              <span class="truncate text-xs text-muted-foreground">
                <span v-if="chatStore.mentionsPending[conv.conversationId]" class="text-destructive">
                  {{ t('chat.list.mention_me') }}
                </span>
                {{ chatStore.typingIndicators[conv.conversationId]
                  ? t('chat.thread.typing', { name: chatStore.typingIndicators[conv.conversationId]?.userName ?? '' })
                  : previewText(conv.conversationType, conv.lastMessagePreview ?? '') }}
              </span>
              <span class="flex shrink-0 items-center gap-1">
                <Icon
                  v-if="conv.isMuted"
                  icon="lucide:bell-off"
                  width="12"
                  height="12"
                  class="text-muted-foreground/70"
                />
                <span
                  v-if="conv.isMuted && conv.unreadCount > 0"
                  class="text-[11px] text-muted-foreground/70"
                >{{ conv.unreadCount }}</span>
              </span>
            </div>
          </div>
        </div>

        <!-- 聊天记录里的命中：哪个会话、谁说的、说了什么，点了定位过去 -->
        <template v-if="searching">
          <div class="chat-conv-section">
            {{ t('chat.list.section_messages') }}
          </div>
          <div v-if="hitsPending && !messageHits.length" class="flex justify-center py-4">
            <XhSpinner />
          </div>
          <div v-else-if="!messageHits.length" class="px-3 py-4 text-center text-xs text-muted-foreground">
            {{ t('chat.list.messages_empty') }}
          </div>
          <div
            v-for="{ hit, conversation } in messageHits"
            :key="hit.messageId"
            class="chat-conv-item"
            @click="emit('locate', conversation.conversationId, hit.messageId)"
          >
            <ChatConversationAvatar :conversation-id="conversation.conversationId" :conversation-type="conversation.conversationType" :avatar="conversation.avatar" :name="conversation.displayName" :size="38" />
            <div class="min-w-0 flex-1">
              <div class="flex items-center justify-between gap-2">
                <span class="truncate text-[13px] font-semibold text-foreground">{{ conversation.displayName }}</span>
                <span class="shrink-0 text-[11px] text-muted-foreground">{{ formatConversationTime(t, hit.createdAt) }}</span>
              </div>
              <div class="mt-0.5 truncate text-xs text-muted-foreground">
                {{ hit.senderUserName }}: {{ messageBodyLabel(hit) }}
              </div>
            </div>
          </div>
          <div v-if="globalSearch.hasMore.value" class="flex justify-center py-2">
            <XhButton variant="subtle" size="sm" :loading="globalSearch.loading.value" @click="handleLoadMoreHits">
              {{ t('chat.thread.load_more') }}
            </XhButton>
          </div>
        </template>
      </div>
    </div>

    <!-- 会话右键菜单（QQ 式） -->
    <ChatContextMenu
      v-model:show="ctxShow"
      :x="ctxX"
      :y="ctxY"
      :items="ctxItems"
      @select="handleItemAction"
    />
  </div>
</template>

<style scoped>
/* 未读计数贴在头像右上角 */
.chat-conv-avatar {
  position: relative;
  flex-shrink: 0;
}

.chat-conv-avatar__badge {
  position: absolute;
  inset-block-start: -2px;
  inset-inline-end: -2px;
  min-inline-size: 16px;
  padding: 0 var(--xh-space-1);
  border-radius: var(--xh-shape-pill);
  background: var(--xh-color-danger-600);
  /* 实心语气底上的字恒是白的，与组件库徽标同取 */
  color: var(--xh-color-neutral-0);
  font-size: var(--xh-font-size-xs);
  line-height: 16px;
  text-align: center;
}

/* 搜索时的分区标题：会话、聊天记录 */
.chat-conv-section {
  padding: var(--xh-space-2) var(--xh-space-3) var(--xh-space-1);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-medium);
}

.chat-conv-item {
  display: flex;
  gap: var(--xh-space-2_5);
  align-items: center;
  padding: var(--xh-space-2) var(--xh-space-3);
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-conv-item:hover {
  background: color-mix(in srgb, var(--xh-bg-subtle-hover) 60%, transparent);
}

.chat-conv-item--active {
  background: color-mix(in srgb, var(--xh-fg-brand) 8%, transparent);
}

.chat-conv-item--pinned {
  background: color-mix(in srgb, var(--xh-bg-muted) 40%, transparent);
}

.chat-conv-item--pinned.chat-conv-item--active {
  background: color-mix(in srgb, var(--xh-fg-brand) 8%, transparent);
}
</style>
