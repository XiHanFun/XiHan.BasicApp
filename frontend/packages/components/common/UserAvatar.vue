<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

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

/** 回退字随头像尺寸放大，不小于 12px */
const fontPx = computed(() => Math.max(12, Math.round(props.size * 0.36)))
const fontSize = computed(() => `${fontPx.value}px`)

/** 东亚文字区（中日韩部首起）的第一个码位：从这里往后的字按全角算 */
const WIDE_FROM = 0x2E80

/** 一个字大约占的宽（字号的倍数）：中日韩等全角字占满一个字号，拉丁字母约六成 */
function charWidth(char: string): number {
  return (char.codePointAt(0) ?? 0) >= WIDE_FROM
    ? 1
    : 0.6
}

/**
 * 回退字：名字的前两个字，头像里放不下第二个就只放一个
 * 两个汉字在顶栏 24px 的头像里会撑出圆外，看着不居中
 */
const initials = computed(() => {
  const [first, second] = [
    ...(props.name ?? '').trim(),
  ]
  if (!first)
    return '?'
  const room = props.size * 0.9 / fontPx.value
  return second && charWidth(first) + charWidth(second) <= room
    ? first + second
    : first
})
</script>

<template>
  <!-- 全应用唯一的用户头像：顶栏、用户列表、聊天、锁屏都用它，同一个人处处同一副样子
       回退字与底色走组件库的品牌语气，不在应用里另配颜色；尺寸与圆角经头像的公开槽给
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
