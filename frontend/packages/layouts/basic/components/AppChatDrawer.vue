<script setup lang="ts">
import { NDrawer, NTooltip } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ChatPanel } from '~/components'
import { useIsMobile } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext, useChatStore, useLayoutBridgeStore } from '~/stores'

defineOptions({ name: 'AppChatDrawer' })

const { t } = useI18n()
const router = useRouter()
const appContext = useAppContext()
const chatStore = useChatStore()
const layoutBridgeStore = useLayoutBridgeStore()
// 小屏（<768）抽屉全宽，避免留缝
const { isMobile } = useIsMobile()

const show = ref(false)

// 顶栏按钮经 layout-bridge 版本计数器请求打开（同偏好抽屉模式）
watch(() => layoutBridgeStore.chatDrawerVersion, () => {
  show.value = true
  chatStore.ensureConversations().catch(() => {})
})

// 聊天全屏页的路由由应用注册（后端 PageRegistry 下发），未配置则不展示"展开"按钮
const chatFullPagePath = computed(() => appContext.shellRoutes.chat)

function handleOpenFullPage() {
  if (!chatFullPagePath.value) {
    return
  }
  show.value = false
  void router.push(chatFullPagePath.value)
}
</script>

<template>
  <NDrawer
    v-model:show="show"
    :width="isMobile ? '100%' : 440"
    style="max-width: 100vw;"
    placement="right"
  >
    <div class="flex h-full min-h-0 flex-col">
      <div class="flex items-center justify-between border-b border-border px-4 py-3">
        <span class="text-sm font-semibold text-foreground">{{ t('chat.drawer.title') }}</span>
        <div class="flex items-center gap-1">
          <NTooltip>
            <template #trigger>
              <button v-if="chatFullPagePath" type="button" class="chat-drawer-btn" @click="handleOpenFullPage">
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
