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

/** 首字母回退（取前两位，兼容中英文） */
const initials = computed(() => {
  const n = (props.name ?? '').trim()
  return n ? n.slice(0, 2) : '?'
})

/** 回退字随头像尺寸放大，两个汉字在小头像里也放得下 */
const fontSize = computed(() => `${Math.max(12, Math.round(props.size * 0.36))}px`)
</script>

<template>
  <!-- 全应用唯一的用户头像：顶栏、用户列表、聊天、锁屏都用它，同一个人处处同一副样子。
       回退字与底色走组件库的品牌语气，不在应用里另配颜色；尺寸与圆角经头像的公开槽给。
       src / alt 是根部件的属性：写在 image 部件上状态机拿不到地址；图片没给 / 加载失败时自动显示回退 -->
  <XhAvatarRoot
    class="xh-user-avatar"
    tone="brand"
    :src="resolvedUrl || undefined"
    :alt="name || initials"
    :style="{
      '--xh-avatar-size': `${size}px`,
      '--xh-avatar-font-size': fontSize,
      '--xh-avatar-radius': round ? undefined : 'var(--xh-shape-control)',
    }"
  >
    <XhAvatarImage />
    <XhAvatarFallback>{{ initials }}</XhAvatarFallback>
  </XhAvatarRoot>
</template>

<style scoped>
/* 作为 flex 子项时不被横向压缩，保证任何布局下始终为正圆 */
.xh-user-avatar {
  flex-shrink: 0;
}
</style>
