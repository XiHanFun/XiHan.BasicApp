<script lang="ts" setup>
import { XhAvatarFallback, XhAvatarImage, XhAvatarRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useAvatarUrl } from '~/composables'

defineOptions({ name: 'XUserAvatar' })

const props = withDefaults(defineProps<{
  /** 头像原始值：文件主键(fileId) / 直链 / 空值 */
  avatar?: null | string
  /** 用于生成首字母回退的名称（昵称/用户名） */
  name?: null | string
  /** 尺寸（px） */
  size?: number
  /** 是否圆形 */
  round?: boolean
}>(), {
  avatar: null,
  name: null,
  size: 40,
  round: true,
})

/** fileId → 预签名 URL；直链原样；空值/失败为 ''，交由首字母回退 */
const resolvedUrl = useAvatarUrl(computed(() => props.avatar))

/** 首字母回退（取前两位，兼容中英文）；NAvatar 在无 src 或图片加载失败时渲染默认插槽 */
const initials = computed(() => {
  const n = (props.name ?? '').trim()
  return n ? n.slice(0, 2) : '?'
})

const fontSize = computed(() => `${Math.max(12, Math.round(props.size * 0.4))}px`)
</script>

<template>
  <!-- 头像根自带回退：图片没给/加载失败时自动显示 fallback，不必在外面分支渲染 -->
  <XhAvatarRoot
    class="xh-user-avatar"
    :src="resolvedUrl || undefined"
    :style="{ inlineSize: `${size}px`, blockSize: `${size}px`, borderRadius: round ? 'var(--xh-radius-full)' : 'var(--xh-shape-control)' }"
  >
    <XhAvatarImage :alt="initials" />
    <XhAvatarFallback class="xh-user-avatar__initials" :style="{ fontSize }">
      {{ initials }}
    </XhAvatarFallback>
  </XhAvatarRoot>
</template>

<style scoped>
/* 作为 flex 子项时不被横向压缩，保证任何布局下始终为正圆 */
.xh-user-avatar {
  flex-shrink: 0;
}

.xh-user-avatar__initials {
  font-weight: 700;
  line-height: 1;
  color: hsl(var(--primary));
}
</style>
