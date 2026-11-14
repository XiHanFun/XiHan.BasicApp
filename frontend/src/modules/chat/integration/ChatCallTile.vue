<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { ref, watch } from 'vue'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import { Icon } from '~/iconify'

/**
 * 通话窗里的一格：一个人的画面（没有画面时是头像），或一块共享的屏幕；左下角是名字与状态
 * 画面一律静音：声音走各人自己的音频元素
 */
defineOptions({ name: 'ChatCallTile' })

const props = withDefaults(defineProps<{
  name: string
  avatar?: null | string
  /** 要放的画面；没有即显示头像 */
  stream?: MediaStream | null
  /** 画面的读屏名 */
  videoLabel?: string
  /** 画面怎样填这一格：人像裁满，屏幕完整显示 */
  fit?: 'contain' | 'cover'
  /** 左右镜像：自己的摄像头照镜子的方向放 */
  mirrored?: boolean
  /** 头像的边长（px） */
  avatarSize?: number
  /** 名字后面的状态（响铃中、连接中、连接失败）；没有不显示 */
  status?: string
  /** 麦克风关着时给静音标记的读屏名；没关不给 */
  mutedLabel?: string
  /** 还没进来的人（在响铃、连不上）：头像压暗 */
  dimmed?: boolean
}>(), {
  avatar: null,
  stream: null,
  videoLabel: undefined,
  fit: 'cover',
  mirrored: false,
  avatarSize: 56,
  status: undefined,
  mutedLabel: undefined,
  dimmed: false,
})

const video = ref<HTMLVideoElement | null>(null)

watch([
  video,
  () => props.stream,
], ([element, stream]) => {
  if (element && element.srcObject !== stream) {
    element.srcObject = stream
  }
}, { flush: 'post' })
</script>

<template>
  <div class="chat-call-tile" :class="{ 'chat-call-tile--dimmed': props.dimmed }" role="group" :aria-label="props.name">
    <video
      v-show="props.stream"
      ref="video"
      class="chat-call-tile__video"
      :class="{ 'chat-call-tile__video--contain': props.fit === 'contain', 'chat-call-tile__video--mirrored': props.mirrored }"
      autoplay
      playsinline
      muted
      :aria-label="props.videoLabel"
    />
    <XUserAvatar v-if="!props.stream" class="chat-call-tile__avatar" :avatar="props.avatar" :name="props.name" :size="props.avatarSize" />
    <div class="chat-call-tile__caption">
      <!-- 图标自己对读屏是隐藏的：名字给在外面这一层 -->
      <span v-if="props.mutedLabel" class="inline-flex shrink-0" role="img" :aria-label="props.mutedLabel">
        <Icon icon="lucide:mic-off" width="12" height="12" />
      </span>
      <span class="truncate">{{ props.name }}</span>
      <span v-if="props.status" class="shrink-0" role="status">{{ props.status }}</span>
    </div>
  </div>
</template>

<style scoped>
.chat-call-tile {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-inline-size: 0;
  min-block-size: 0;
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-surface);
  overflow: hidden;
}

.chat-call-tile__video {
  position: absolute;
  inset: 0;
  inline-size: 100%;
  block-size: 100%;
  object-fit: cover;
}

/* 共享的屏幕要看全：不裁，留出的边露出格子的底色 */
.chat-call-tile__video--contain {
  object-fit: contain;
}

.chat-call-tile__video--mirrored {
  transform: scaleX(-1);
}

.chat-call-tile__avatar {
  transition: opacity var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-call-tile--dimmed .chat-call-tile__avatar {
  opacity: 0.5;
}

/* 名字与状态：压在画面上，垫一层实体底保证看得清 */
.chat-call-tile__caption {
  position: absolute;
  inset-block-end: var(--xh-space-1);
  inset-inline-start: var(--xh-space-1);
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
  max-inline-size: calc(100% - var(--xh-space-2));
  padding: var(--xh-space-0_5) var(--xh-space-1_5);
  border-radius: var(--xh-shape-pill);
  background: var(--xh-bg-surface);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
}
</style>
