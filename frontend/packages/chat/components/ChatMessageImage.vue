<script setup lang="ts">
import { XhImageImage, XhImageRoot, XhImageViewerTrigger, XhSpinner } from '@xihan-ui/vue'

defineOptions({ name: 'ChatMessageImage' })

defineProps<{
  /** 可显示的图片地址；还在换取预签名地址时为空 */
  url: string
  alt?: null | string
  /** 相册多图时用方形缩略图；单图用自适应大图 */
  thumb?: boolean
}>()

/** 点了这张：看片浮层从这张开始看。图片列表由消息统一交给 XhImageViewerRoot */
const emit = defineEmits<{ select: [] }>()
</script>

<template>
  <XhImageViewerTrigger v-if="url" @click="emit('select')">
    <XhImageRoot>
      <XhImageImage
        :src="url"
        :alt="alt ?? undefined"
        :style="thumb
          ? 'width: 88px; height: 88px; border-radius: 6px; display: block; object-fit: cover;'
          : 'max-width: 240px; max-height: 240px; border-radius: 6px; display: block; object-fit: cover;'"
      />
    </XhImageRoot>
  </XhImageViewerTrigger>
  <div
    v-else
    class="flex items-center justify-center rounded bg-muted/40"
    :class="thumb ? 'h-[88px] w-[88px]' : 'h-24 w-40'"
  >
    <XhSpinner />
  </div>
</template>
