<script setup lang="ts">
import { XhDrawerContent, XhDrawerRoot } from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useIsMobile } from '~/composables'
import { Icon } from '~/iconify'
import ChatPanel from '../components/ChatPanel.vue'
import { CHAT_PAGE_PATH } from '../constants'
import { useChatStore } from '../store'

defineOptions({ name: 'AppChatDrawer' })

const { t } = useI18n()
const router = useRouter()
const chatStore = useChatStore()
// 小屏（<768）抽屉全宽，避免留缝
const { isMobile } = useIsMobile()

const show = ref(false)

// 顶栏按钮经 layout-bridge 版本计数器请求打开（同偏好抽屉模式）
watch(() => chatStore.chatDrawerVersion, () => {
  show.value = true
  chatStore.ensureConversations().catch(() => {})
})

function handleOpenFullPage() {
  show.value = false
  void router.push(CHAT_PAGE_PATH)
}
</script>

<template>
  <XhDrawerRoot v-model:open="show" side="right">
    <XhDrawerContent :style="{ inlineSize: isMobile ? '100%' : '440px', maxInlineSize: '100vw' }">
      <div class="flex h-full min-h-0 flex-col">
        <div class="flex items-center justify-between border-b border-border px-4 py-3">
          <span class="text-sm font-semibold text-foreground">{{ t('chat.drawer.title') }}</span>
          <div class="flex items-center gap-1">
            <button :title="t('chat.drawer.open_page')" type="button" class="chat-drawer-btn" @click="handleOpenFullPage">
              <Icon icon="lucide:expand" width="15" height="15" />
            </button>
            <button type="button" class="chat-drawer-btn" @click="show = false">
              <Icon icon="lucide:x" width="15" height="15" />
            </button>
          </div>
        </div>
        <div class="min-h-0 flex-1 p-2">
          <ChatPanel mode="drawer" />
        </div>
      </div>
    </XhDrawerContent>
  </XhDrawerRoot>
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
