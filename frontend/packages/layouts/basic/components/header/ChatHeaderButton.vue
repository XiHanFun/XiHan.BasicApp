<script setup lang="ts">
import { NNumberAnimation, NTooltip } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useChatStore, useLayoutBridgeStore, useUserStore } from '~/stores'

defineOptions({ name: 'ChatHeaderButton' })

const { t } = useI18n()
const chatStore = useChatStore()
const userStore = useUserStore()
const layoutBridgeStore = useLayoutBridgeStore()

// 无查看权限直接不渲染（会话预取与实时链路也在集成层被同一权限关闭）
const visible = computed(() => userStore.hasPermission(CHAT_PERMISSIONS.read))
const unread = computed(() => chatStore.totalUnread)
</script>

<template>
  <NTooltip v-if="visible">
    <template #trigger>
      <button
        type="button"
        class="chat-header-btn mr-1"
        @click="layoutBridgeStore.requestOpenChatDrawer()"
      >
        <Icon icon="lucide:messages-square" width="16" height="16" />
        <span v-if="unread > 0" class="chat-header-btn__badge">
          <NNumberAnimation :to="Math.min(unread, 99)" :duration="500" :precision="0" />
          <span v-if="unread > 99">+</span>
        </span>
      </button>
    </template>
    {{ t('chat.bell') }}
  </NTooltip>
</template>

<style scoped>
/* 与通知铃铛同款按钮/徽标规格（14px 小圆 + 9px 字） */
.chat-header-btn {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--foreground) / 65%);
  cursor: pointer;
  outline: none;
  flex-shrink: 0;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.chat-header-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.chat-header-btn__badge {
  position: absolute;
  top: -1px;
  right: -1px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 14px;
  height: 14px;
  padding: 0 3px;
  border-radius: 9999px;
  background: hsl(var(--destructive, 0 84% 60%));
  color: #fff;
  font-size: 9px;
  font-weight: 600;
  line-height: 14px;
  text-align: center;
}
</style>
