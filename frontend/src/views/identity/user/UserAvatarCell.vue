<script lang="ts" setup>
import { XhAvatarFallback, XhAvatarImage, XhAvatarRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useAvatarUrl } from '~/composables'

const props = defineProps<{
  /** 头像原始值（fileId / URL / 空） */
  avatar?: string | null
  /** 用于生成首字母与配色的名称来源 */
  name: string
  /** 文字头像背景色 */
  bg: string
  /** 文字头像前景色 */
  fg: string
  /** 尺寸 */
  size?: number
}>()

/** 经 useAvatarUrl 把 fileId 异步换取为可显示 URL；无图或换取中为空 */
const avatarUrl = useAvatarUrl(computed(() => props.avatar))

/** 首字母回退文案 */
const initials = computed(() => (props.name ? props.name.substring(0, 2) : '?'))
</script>

<template>
  <!-- 有图显示图片，无图/换取中/加载失败都落到首字母文字头像 -->
  <XhAvatarRoot
    :style="{
      '--xh-avatar-size': `${size ?? 32}px`,
      '--xh-avatar-bg': bg,
      '--xh-avatar-fg': fg,
      '--xh-avatar-font-size': '12px',
      '--xh-avatar-font-weight': '600',
    }"
  >
    <XhAvatarImage v-if="avatarUrl" :src="avatarUrl" :alt="name" />
    <XhAvatarFallback>{{ initials }}</XhAvatarFallback>
  </XhAvatarRoot>
</template>
