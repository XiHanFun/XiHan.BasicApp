<script setup lang="ts">
import { NDrawer, NTooltip } from 'naive-ui'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ChatPanel } from '~/components'
import { Icon } from '~/iconify'
import { useChatStore, useLayoutBridgeStore } from '~/stores'

defineOptions({ name: 'AppChatDrawer' })

const { t } = useI18n()
const router = useRouter()
const chatStore = useChatStore()
const layoutBridgeStore = useLayoutBridgeStore()

const show = ref(false)

// 顶栏按钮经 layout-bridge 版本计数器请求打开（同偏好抽屉模式）
watch(() => layoutBridgeStore.chatDrawerVersion, () => {
  show.value = true
  chatStore.ensureConversations().catch(() => {})
})

function handleOpenFullPage() {
  show.value = false
  void router.push('/message/chat')
}
</script>

<template>
  <NDrawer
    v-model:show="show"
    :width="440"
    style="max-width: calc(100vw - 24px);"
    placement="right"
  >
    <div class="flex h-full min-h-0 flex-col">
      <div class="flex items-center justify-between border-b border-border px-4 py-3">
        <span class="text-sm font-semibold text-foreground">{{ t('chat.drawer.title') }}</span>
        <div class="flex items-center gap-1">
          <NTooltip>
            <template #trigger>
              <button type="button" class="chat-drawer-btn" @click="handleOpenFullPage">
                <Icon icon="lucide:expand" width="15" height="15" />
              </button>
            </template>
            {{ t('chat.drawer.open_page') }}
          </NTooltip>
          <button type="button" class="chat-drawer-btn" @click="show = false">
            <Icon icon="lucide:x" width="15" height="15" />
          </button>
        </div>
      </div>
      <div class="min-h-0 flex-1 p-2">
        <ChatPanel mode="drawer" />
      </div>
    </div>
  </NDrawer>
</template>

<style scoped>
.chat-drawer-btn {
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
  transition: all 0.15s ease;
}

.chat-drawer-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}
</style>
