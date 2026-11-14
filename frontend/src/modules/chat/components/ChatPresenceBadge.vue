<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhBadge } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '../store'

/**
 * 在线状态点：包住一枚圆形头像，那个人在线时在右下角亮一颗绿点
 * 在线与否一律取聊天 store（读接口带回来的状态加之后推来的变化）；不给用户（群、助手的头像）即不出点
 * 壳恒在、只收放那颗点：上下线时头像不重挂，图片不会闪
 */
defineOptions({ name: 'ChatPresenceBadge' })

const props = defineProps<{
  userId?: null | string
}>()

const { t } = useI18n()
const chatStore = useChatStore()

const online = computed(() => chatStore.isUserOnline(props.userId))
</script>

<template>
  <XhBadge
    class="chat-presence-badge"
    dot
    size="lg"
    tone="success"
    placement="bottom-end"
    :count="online ? 1 : 0"
    :label="online ? t('chat.presence.online') : undefined"
  >
    <slot />
  </XhBadge>
</template>

<style scoped>
/* 圆形头像的外接方角上是留白：把点往里收到圆周上（方角到圆周 45° 处是边长的 14.6%），大小头像同一个比例 */
.chat-presence-badge {
  --xh-badge-offset-inline: -14.6%;
  --xh-badge-offset-block: -14.6%;
}
</style>
