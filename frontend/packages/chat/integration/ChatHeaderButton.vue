<script setup lang="ts">
import { XhBadge } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import XTooltip from '~/components/common/XTooltip.vue'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { CHAT_PERMISSIONS } from '../constants'
import { useChatStore } from '../store'

defineOptions({ name: 'ChatHeaderButton' })

const { t } = useI18n()
const chatStore = useChatStore()
const userStore = useUserStore()

// 无查看权限直接不渲染（会话预取与实时链路也在集成层被同一权限关闭）
const visible = computed(() => userStore.hasPermission(CHAT_PERMISSIONS.read))
const unread = computed(() => chatStore.totalUnread)
</script>

<template>
  <XTooltip :content="t('chat.bell')">
    <button
      v-if="visible"
      type="button"
      class="xihan-icon-btn chat-header-btn mr-1"
      @click="chatStore.requestOpenChatDrawer()"
    >
      <!-- 数字、99+、「零则收起」与贴角定位都归组件库算；与通知铃铛同一写法。
           不在角标里再嵌数字动画：它会继承角标语气的文字色（深色压在同色底上看不见），行高也和角标不是一档 -->
      <XhBadge
        size="sm"
        tone="danger"
        :count="unread"
        :label="t('chat.unread_label', { n: unread })"
      >
        <Icon icon="lucide:messages-square" width="16" height="16" />
      </XhBadge>
    </button>
  </XTooltip>
</template>

<style scoped>
/* 皮肤走全局 .xihan-icon-btn；角标的定位与尺寸归组件库的 Badge，这里不再另画一套 */
.chat-header-btn {
  position: relative;
}
</style>
