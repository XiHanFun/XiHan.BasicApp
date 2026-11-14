<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { IslandAccessory } from '~/composables/useDynamicIsland'
import { computed } from 'vue'
import { Icon } from '~/iconify'
import { formatElapsed } from './dynamic-island-view'
import XUserAvatar from './UserAvatar.vue'

/**
 * 灵动岛两侧小件：图标、头像、进度环、短文本、计时，或调用方自己的组件
 * 颜色缺省继承外层（状态色由外层给），tone 显式指定时覆盖
 */
defineOptions({ name: 'DynamicIslandAccessory' })

const props = withDefaults(defineProps<{
  accessory: IslandAccessory
  /** sm 胶囊与面板行，md 卡片与列表行，lg 数值块 */
  size?: 'sm' | 'md' | 'lg'
  /** 计时起点与当前时刻（毫秒），由岛统一走时钟 */
  startedAt?: number
  now?: number
}>(), {
  size: 'sm',
  startedAt: 0,
  now: 0,
})

const RING_RADIUS = 8
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS
/** 头像与图标的像素边长（组件参数，不是样式），与环的盒子同档：20 / 32 / 56 */
const AVATAR_SIZE = { sm: 20, md: 32, lg: 56 } as const
const ICON_SIZE = { sm: 15, md: 18, lg: 28 } as const
const RING_ICON_SIZE = { sm: 9, md: 14, lg: 24 } as const

/** 小档的圆只放得下一个字：取名字首字，两个字留给 md / lg */
const avatarName = computed(() => {
  const accessory = props.accessory
  if (accessory.type !== 'avatar') {
    return ''
  }
  return props.size === 'sm'
    ? Array.from(accessory.name.trim())[0] ?? ''
    : accessory.name
})

const toneClass = computed(() => {
  const accessory = props.accessory
  return 'tone' in accessory && accessory.tone
    ? `is-tone-${accessory.tone}`
    : undefined
})

const ringOffset = computed(() => {
  const accessory = props.accessory
  if (accessory.type !== 'ring') {
    return 0
  }
  // 不确定态取固定弧长旋转
  if (accessory.value == null) {
    return RING_CIRCUMFERENCE * 0.72
  }
  const ratio = Math.max(0, Math.min(1, accessory.value / (accessory.max ?? 100)))
  return RING_CIRCUMFERENCE * (1 - ratio)
})
</script>

<template>
  <span class="di-acc" :class="[`di-acc--${size}`, `di-acc--${accessory.type}`, toneClass]">
    <Icon v-if="accessory.type === 'icon'" :icon="accessory.icon" :width="ICON_SIZE[size]" :height="ICON_SIZE[size]" />
    <!-- 头像只是标识，名字在旁边的标题里，对读屏隐藏 -->
    <XUserAvatar v-else-if="accessory.type === 'avatar'" :avatar="accessory.avatar" :name="avatarName" :size="AVATAR_SIZE[size]" aria-hidden="true" />
    <template v-else-if="accessory.type === 'ring'">
      <svg class="di-acc__ring" :class="{ 'is-spinning': accessory.value == null }" viewBox="0 0 20 20" aria-hidden="true">
        <circle class="di-acc__ring-track" cx="10" cy="10" :r="RING_RADIUS" />
        <circle
          class="di-acc__ring-bar"
          cx="10"
          cy="10"
          :r="RING_RADIUS"
          :stroke-dasharray="RING_CIRCUMFERENCE"
          :stroke-dashoffset="ringOffset"
        />
      </svg>
      <Icon v-if="accessory.icon" :icon="accessory.icon" :width="RING_ICON_SIZE[size]" :height="RING_ICON_SIZE[size]" class="di-acc__ring-icon" />
    </template>
    <span v-else-if="accessory.type === 'text'" class="di-acc__text">{{ accessory.text }}</span>
    <span v-else-if="accessory.type === 'elapsed'" class="di-acc__text">{{ formatElapsed(now - startedAt) }}</span>
    <component :is="accessory.component" v-else v-bind="accessory.props" />
  </span>
</template>

<style scoped>
.di-acc {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--di-acc-tone);
}

/* 环与图标的盒子：20 / 32 / 56，取字形尺寸档 */
.di-acc--sm.di-acc--ring,
.di-acc--sm.di-acc--icon {
  inline-size: var(--xh-glyph-size-lg);
  block-size: var(--xh-glyph-size-lg);
}

.di-acc--md.di-acc--ring,
.di-acc--md.di-acc--icon {
  inline-size: var(--xh-glyph-size-xl);
  block-size: var(--xh-glyph-size-xl);
}

.di-acc--lg.di-acc--ring,
.di-acc--lg.di-acc--icon {
  inline-size: var(--xh-glyph-size-3xl);
  block-size: var(--xh-glyph-size-3xl);
}

/* 语气与岛上其余彩色同一兑法：往深底上的前景色兑一点，不刺眼 */
.is-tone-brand {
  --di-acc-tone: color-mix(in srgb, var(--xh-fg-brand) 60%, var(--xh-fg-on-media-chrome));
}

.is-tone-success {
  --di-acc-tone: color-mix(in srgb, var(--xh-fg-success) 75%, var(--xh-fg-on-media-chrome));
}

.is-tone-warning {
  --di-acc-tone: color-mix(in srgb, var(--xh-fg-warning) 80%, var(--xh-fg-on-media-chrome));
}

.is-tone-danger {
  --di-acc-tone: color-mix(in srgb, var(--xh-fg-danger) 85%, var(--xh-fg-on-media-chrome));
}

.is-tone-info {
  --di-acc-tone: color-mix(in srgb, var(--xh-fg-info) 70%, var(--xh-fg-on-media-chrome));
}

.is-tone-neutral {
  --di-acc-tone: color-mix(in srgb, var(--xh-fg-on-media-chrome) 85%, transparent);
}

.di-acc__ring {
  inline-size: 100%;
  block-size: 100%;
  transform: rotate(-90deg);
}

.di-acc__ring-track {
  fill: none;
  stroke: color-mix(in srgb, var(--xh-fg-on-media-chrome) 16%, transparent);
  stroke-width: 2.5;
}

.di-acc__ring-bar {
  fill: none;
  stroke: var(--di-acc-tone, color-mix(in srgb, var(--xh-fg-brand) 70%, var(--xh-fg-on-media-chrome)));
  stroke-width: 2.5;
  stroke-linecap: round;
  transition: stroke-dashoffset var(--xh-motion-duration-slide) var(--xh-motion-ease-enter-strong);
}

/* 不确定态：固定弧长旋转 */
.di-acc__ring.is-spinning {
  animation: di-acc-ring-rotate 1s var(--xh-motion-ease-loop) infinite;
}

.di-acc__ring.is-spinning .di-acc__ring-bar {
  transition: none;
}

@keyframes di-acc-ring-rotate {
  to {
    transform: rotate(270deg);
  }
}

.di-acc__ring-icon {
  position: absolute;
  inset: 0;
  margin: auto;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 80%, transparent);
}

.di-acc__text {
  font-size: var(--xh-font-size-xs);
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.di-acc--lg .di-acc__text {
  font-size: var(--xh-font-size-lg);
}
</style>
