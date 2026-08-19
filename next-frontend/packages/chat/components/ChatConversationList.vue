<script setup lang="ts">
import type { ChatContextMenuItem } from './ChatContextMenu.vue'
import type { AppDropdownOption } from '~/types'
import { XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XDropdown from '~/components/common/XDropdown.vue'
import XInput from '~/components/common/XInput.vue'
import XTooltip from '~/components/common/XTooltip.vue'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { hasChatAssistantProvider } from '../assistant-provider'
import {
  CHAT_PERMISSIONS,
} from '../constants'
import { useChatStore } from '../store'
import { formatConversationTime } from './chat-helpers'
import ChatContextMenu from './ChatContextMenu.vue'

defineOptions({ name: 'ChatConversationList' })

const emit = defineEmits<{
  select: [conversationId: string]
  start: [mode: 'assistant' | 'department' | 'group' | 'single']
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

const startOptions = computed<AppDropdownOption[]>(() => {
  const options: AppDropdownOption[] = [
    { key: 'single', label: t('chat.start.single') },
    { key: 'department', label: t('chat.start.department') },
  ]
  // AI 助手入口仅在助手提供方已注册（AI 模块在位）时出现
  if (hasChatAssistantProvider()) {
    options.push({ key: 'assistant', label: t('chat.start.assistant') })
  }
  // 建群需要会话管理权限（后端 chat:manage 门控）
  if (userStore.hasPermission(CHAT_PERMISSIONS.manage)) {
    options.splice(1, 0, { key: 'group', label: t('chat.start.group') })
  }
  return options
})

function handleStartSelect(key: string | number) {
  emit('start', String(key) as 'assistant' | 'department' | 'group' | 'single')
}

function handleRefresh() {
  chatStore.loadConversations().catch(() => {})
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
      ? { key: 'pin', label: t('chat.list.unpin'), icon: 'lucide:pin-off' }
      : { key: 'pin', label: t('chat.list.pin'), icon: 'lucide:pin' },
    conv.isMuted
      ? { key: 'mute', label: t('chat.list.unmute'), icon: 'lucide:bell' }
      : { key: 'mute', label: t('chat.list.mute'), icon: 'lucide:bell-off' },
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
    chatStore.togglePinConversation(id).catch(() => {})
  }
  else if (key === 'mute') {
    chatStore.toggleMuteConversation(id).catch(() => {})
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
      >
        <template #prefix>
          <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
        </template>
      </XInput>
      <XTooltip :content="t('chat.list.refresh')">
        <button type="button" class="chat-icon-btn" @click="handleRefresh">
          <Icon icon="lucide:refresh-cw" width="15" height="15" />
        </button>
      </XTooltip>
      <!-- 下拉与气泡叠在同一颗按钮上：菜单触发器借用它，说明文字走原生 title -->
      <XDropdown :options="startOptions" @select="handleStartSelect">
        <button type="button" class="chat-icon-btn" :title="t('chat.start.button')">
          <Icon icon="lucide:message-square-plus" width="15" height="15" />
        </button>
      </XDropdown>
    </div>

    <!-- 会话列表 -->
    <div class="xh-loading-stage min-h-0 flex-1" :class="{ 'is-loading': chatStore.conversationsLoading && !chatStore.conversations.length }">
      <div class="xh-loading-stage__veil">
        <XhSpinner size="md" />
      </div>
      <div class="xh-scroll-area h-full">
        <div v-if="!filteredConversations.length" class="py-10">
          <XhEmptyStateRoot size="sm">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIcon>
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
            <XUserAvatar :avatar="conv.avatar" :name="conv.displayName" :size="38" />
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
                {{ formatConversationTime(t, conv.lastMessageTime) }}
              </span>
            </div>
            <div class="mt-0.5 flex items-center justify-between gap-2">
              <span class="truncate text-xs text-muted-foreground">
                <span v-if="chatStore.mentionsPending[conv.conversationId]" class="text-destructive">
                  {{ t('chat.list.mention_me') }}
                </span>
                {{ chatStore.typingIndicators[conv.conversationId]
                  ? t('chat.thread.typing', { name: chatStore.typingIndicators[conv.conversationId]?.userName ?? '' })
                  : conv.lastMessagePreview ?? '' }}
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
  padding: 0 4px;
  border-radius: 999px;
  background: var(--xh-color-danger-500);
  color: #fff;
  font-size: 10px;
  line-height: 16px;
  text-align: center;
}

.chat-icon-btn {
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

.chat-icon-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.chat-conv-item {
  display: flex;
  gap: 10px;
  align-items: center;
  padding: 9px 12px;
  cursor: pointer;
  transition: background 0.15s ease;
}

.chat-conv-item:hover {
  background: hsl(var(--accent) / 60%);
}

.chat-conv-item--active {
  background: hsl(var(--primary) / 8%);
}

.chat-conv-item--pinned {
  background: hsl(var(--muted) / 40%);
}

.chat-conv-item--pinned.chat-conv-item--active {
  background: hsl(var(--primary) / 8%);
}
</style>
