<script setup lang="ts">
import type { DropdownOption } from 'naive-ui'
import { NBadge, NDropdown, NEmpty, NInput, NScrollbar, NSpin, NTooltip } from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useChatStore, useUserStore } from '~/stores'
import XUserAvatar from '../common/UserAvatar.vue'
import { formatConversationTime } from './chat-helpers'

defineOptions({ name: 'ChatConversationList' })

const emit = defineEmits<{
  select: [conversationId: string]
  start: [mode: 'department' | 'group' | 'single']
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

const startOptions = computed<DropdownOption[]>(() => {
  const options: DropdownOption[] = [
    { key: 'single', label: t('chat.start.single') },
    { key: 'department', label: t('chat.start.department') },
  ]
  // 建群需要会话管理权限（后端 saas:chat:manage 门控）
  if (userStore.hasPermission(CHAT_PERMISSIONS.manage)) {
    options.splice(1, 0, { key: 'group', label: t('chat.start.group') })
  }
  return options
})

function handleStartSelect(key: string | number) {
  emit('start', String(key) as 'department' | 'group' | 'single')
}

function handleRefresh() {
  chatStore.loadConversations().catch(() => {})
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col">
    <!-- 头部：搜索 + 发起聊天 -->
    <div class="flex items-center gap-2 p-3 pb-2">
      <NInput
        v-model:value="keyword"
        size="small"
        clearable
        :placeholder="t('chat.list.search_placeholder')"
      >
        <template #prefix>
          <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
        </template>
      </NInput>
      <NTooltip>
        <template #trigger>
          <button type="button" class="chat-icon-btn" @click="handleRefresh">
            <Icon icon="lucide:refresh-cw" width="15" height="15" />
          </button>
        </template>
        {{ t('chat.list.refresh') }}
      </NTooltip>
      <NDropdown :options="startOptions" trigger="click" @select="handleStartSelect">
        <NTooltip>
          <template #trigger>
            <button type="button" class="chat-icon-btn">
              <Icon icon="lucide:message-square-plus" width="15" height="15" />
            </button>
          </template>
          {{ t('chat.start.button') }}
        </NTooltip>
      </NDropdown>
    </div>

    <!-- 会话列表 -->
    <NSpin
      :show="chatStore.conversationsLoading && !chatStore.conversations.length"
      class="min-h-0 flex-1"
      :content-style="{ height: '100%' }"
    >
      <NScrollbar class="h-full">
        <div v-if="!filteredConversations.length" class="py-10">
          <NEmpty :description="t('chat.list.empty')" size="small" />
        </div>
        <div
          v-for="conv in filteredConversations"
          :key="conv.conversationId"
          class="chat-conv-item"
          :class="{ 'chat-conv-item--active': conv.conversationId === chatStore.activeConversationId }"
          @click="emit('select', conv.conversationId)"
        >
          <NBadge :value="conv.isMuted ? 0 : conv.unreadCount" :max="99" :offset="[-2, 2]">
            <XUserAvatar :avatar="conv.avatar" :name="conv.displayName" :size="38" />
          </NBadge>
          <div class="min-w-0 flex-1">
            <div class="flex items-center justify-between gap-2">
              <span class="truncate text-[13px] font-medium text-foreground">{{ conv.displayName }}</span>
              <span class="shrink-0 text-[11px] text-muted-foreground">
                {{ formatConversationTime(t, conv.lastMessageTime) }}
              </span>
            </div>
            <div class="mt-0.5 flex items-center justify-between gap-2">
              <span class="truncate text-xs text-muted-foreground">
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
      </NScrollbar>
    </NSpin>
  </div>
</template>

<style scoped>
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
</style>
