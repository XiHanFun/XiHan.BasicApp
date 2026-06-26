<script lang="ts" setup>
import { NAvatar } from 'naive-ui'
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
  <!-- 有图：显示图片头像；无图/换取中：显示首字母文字头像（slot 与 src 互斥，避免 slot 覆盖图片） -->
  <NAvatar
    v-if="avatarUrl"
    round
    :size="size ?? 32"
    :src="avatarUrl"
    object-fit="cover"
  />
  <NAvatar
    v-else
    round
    :size="size ?? 32"
    :color="bg"
    :style="{ color: fg, fontSize: '12px', fontWeight: 600 }"
  >
    {{ initials }}
  </NAvatar>
</template>
