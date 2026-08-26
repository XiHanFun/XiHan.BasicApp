<script setup lang="ts">
import { XhNumberAnimation } from '@xihan-ui/vue'
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
      <Icon icon="lucide:messages-square" width="16" height="16" />
      <span v-if="unread > 0" class="chat-header-btn__badge">
        <XhNumberAnimation :to="Math.min(unread, 99)" :duration="500" :precision="0" />
        <span v-if="unread > 99">+</span>
      </span>
    </button>
  </XTooltip>
</template>

<style scoped>
/* 皮肤走全局 .xihan-icon-btn，这里只留徽标需要的定位（14px 小圆 + 9px 字） */
.chat-header-btn {
  position: relative;
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
  background: var(--xh-color-danger-600);
  color: #fff;
  font-size: 9px;
  font-weight: 600;
  line-height: 14px;
  text-align: center;
}
</style>
