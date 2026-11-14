<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhSpinner } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { resolveChatFileUrl } from '../composables/useChatFileUrl'
import { useChatStore } from '../store'
import { formatFileSize } from './chat-helpers'

/**
 * 视频附件：气泡里就地播放
 * 点了封面才换签名地址、开始取流，历史里的视频不会一进会话就各发一轮请求；
 * 地址放到一半过期就换一个接着放，换了也放不了（浏览器解不了这种编码）即交回上层按普通文件显示
 */
defineOptions({ name: 'ChatMessageVideo' })

const props = defineProps<{
  conversationId: string
  fileId: string
  fileName: string
  fileSize?: null | number
}>()

const emit = defineEmits<{
  /** 下载这个文件 */
  download: []
  /** 这个视频在浏览器里放不了 */
  unplayable: []
}>()

const { t } = useI18n()
const chatStore = useChatStore()

const videoRef = ref<HTMLVideoElement | null>(null)
const src = ref('')
const loading = ref(false)
/** 接着放的位置：地址过期换新之后从这里续 */
let resumeAt = 0

/** 同一时刻只有一段媒体出声：语音占位用消息 ID，视频用这个键 */
const mediaKey = computed(() => `video:${props.fileId}`)

async function load(): Promise<void> {
  if (loading.value) {
    return
  }
  loading.value = true
  try {
    src.value = await resolveChatFileUrl({ conversationId: props.conversationId, fileId: props.fileId })
  }
  catch {
    emit('unplayable')
  }
  finally {
    loading.value = false
  }
}

function onLoadedMetadata(): void {
  const video = videoRef.value
  if (video && resumeAt > 0) {
    video.currentTime = resumeAt
    resumeAt = 0
  }
}

/** 放不下去了：地址过期就换一个从原处接着放；地址没变说明是解不了，交回上层 */
async function onError(): Promise<void> {
  const failed = src.value
  const position = videoRef.value?.currentTime ?? 0
  try {
    const fresh = await resolveChatFileUrl({ conversationId: props.conversationId, fileId: props.fileId })
    if (fresh !== failed) {
      resumeAt = position
      src.value = fresh
      return
    }
  }
  catch {
    // 换不到新地址：同样当作放不了
  }
  release()
  emit('unplayable')
}

function onPlay(): void {
  chatStore.playingMediaId = mediaKey.value
}

function release(): void {
  if (chatStore.playingMediaId === mediaKey.value) {
    chatStore.playingMediaId = null
  }
}

// 别的语音或视频开始出声：自己让位
watch(() => chatStore.playingMediaId, (playing) => {
  if (playing !== mediaKey.value && videoRef.value && !videoRef.value.paused) {
    videoRef.value.pause()
  }
})

// 正放着被卸载（切会话、消息滚出）：占位还回去
onBeforeUnmount(release)
</script>

<template>
  <div class="chat-video">
    <video
      v-if="src"
      ref="videoRef"
      class="chat-video__stage"
      :src="src"
      :aria-label="fileName"
      controls
      autoplay
      playsinline
      preload="metadata"
      @loadedmetadata="onLoadedMetadata"
      @play="onPlay"
      @pause="release"
      @ended="release"
      @error="onError"
    />
    <button
      v-else
      type="button"
      class="chat-video__stage chat-video__cover"
      :aria-label="t('chat.thread.video_play', { name: fileName })"
      :disabled="loading"
      @click="load"
    >
      <span class="chat-video__play">
        <XhSpinner v-if="loading" />
        <Icon v-else icon="lucide:play" width="22" height="22" />
      </span>
    </button>
    <button type="button" class="chat-video__meta" @click="emit('download')">
      <Icon icon="lucide:video" width="16" height="16" class="shrink-0 text-primary" />
      <span class="min-w-0 flex-1 truncate text-left text-[13px] font-medium">{{ fileName }}</span>
      <span class="shrink-0 text-[11px] opacity-70">{{ formatFileSize(fileSize) }}</span>
      <Icon icon="lucide:download" width="14" height="14" class="shrink-0 opacity-70" />
    </button>
  </div>
</template>

<style scoped>
.chat-video {
  display: flex;
  flex-direction: column;
  inline-size: calc(var(--xh-space-8) * 9);
  max-inline-size: 100%;
  overflow: hidden;
  border: var(--xh-stroke-thin) solid var(--xh-border-default);
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-surface);
  color: var(--xh-fg-default);
}

/* 画面与封面同一块 16:9：点开前后气泡不跳 */
.chat-video__stage {
  display: block;
  inline-size: 100%;
  aspect-ratio: 16 / 9;
  background: var(--xh-bg-subtle);
}

.chat-video__cover {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-video__cover:hover:not(:disabled) {
  background: var(--xh-bg-subtle-hover);
}

.chat-video__play {
  display: flex;
  align-items: center;
  justify-content: center;
  inline-size: var(--xh-space-8);
  block-size: var(--xh-space-8);
  padding: var(--xh-space-3);
  border-radius: var(--xh-shape-circle);
  box-sizing: content-box;
  background: var(--xh-bg-surface);
  color: var(--xh-fg-brand);
  box-shadow: var(--xh-shadow-sm);
}

.chat-video__meta {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-2) var(--xh-space-2_5);
  border: none;
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-subtle);
  background: transparent;
  color: inherit;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-video__meta:hover {
  background: var(--xh-bg-subtle-hover);
}

.chat-video__cover:focus-visible,
.chat-video__meta:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: calc(var(--xh-ring-width) * -1);
}
</style>
