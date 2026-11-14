<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatGalleryItem } from './chat-image-gallery'
import { XhImageViewerCloseTrigger, XhImageViewerContent, XhImageViewerCounter, XhImageViewerFlipHorizontalTrigger, XhImageViewerFlipVerticalTrigger, XhImageViewerImage, XhImageViewerNextTrigger, XhImageViewerPrevTrigger, XhImageViewerResetTrigger, XhImageViewerRoot, XhImageViewerRotateLeftTrigger, XhImageViewerRotateRightTrigger, XhImageViewerToolbar, XhImageViewerViewport, XhImageViewerZoomInTrigger, XhImageViewerZoomOutTrigger } from '@xihan-ui/vue'

/**
 * 一组图片的看片浮层：消息流的会话图片组与「图片与文件」里的图片都用它
 * 计数、翻页、工具条、关闭各自钉在浮层的一条边上，都是 content 的直接子件；只有一张时不出计数与翻页
 */
defineOptions({ name: 'ChatImageViewer' })

defineProps<{
  items: ChatGalleryItem[]
}>()

const open = defineModel<boolean>('open', { default: false })
const index = defineModel<number>('index', { default: 0 })
</script>

<template>
  <XhImageViewerRoot v-model:open="open" v-model:index="index" :collection="items">
    <XhImageViewerContent>
      <XhImageViewerViewport>
        <XhImageViewerImage />
      </XhImageViewerViewport>
      <template v-if="items.length > 1">
        <XhImageViewerCounter />
        <XhImageViewerPrevTrigger />
        <XhImageViewerNextTrigger />
      </template>
      <XhImageViewerToolbar>
        <XhImageViewerZoomOutTrigger />
        <XhImageViewerZoomInTrigger />
        <XhImageViewerRotateLeftTrigger />
        <XhImageViewerRotateRightTrigger />
        <XhImageViewerFlipHorizontalTrigger />
        <XhImageViewerFlipVerticalTrigger />
        <XhImageViewerResetTrigger />
      </XhImageViewerToolbar>
      <XhImageViewerCloseTrigger />
    </XhImageViewerContent>
  </XhImageViewerRoot>
</template>
