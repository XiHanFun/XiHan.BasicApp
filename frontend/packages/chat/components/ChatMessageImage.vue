<script setup lang="ts">
import { NImage, NSpin } from 'naive-ui'
import { computed } from 'vue'
import { useAvatarUrl } from '~/composables'

defineOptions({ name: 'ChatMessageImage' })

const props = defineProps<{
  fileId: string
  alt?: null | string
  /** 相册多图时用方形缩略图；单图用自适应大图 */
  thumb?: boolean
}>()

// fileId → 预签名 URL（内存缓存 + 并发去重，复用头像解析链路）
const url = useAvatarUrl(computed(() => props.fileId || null))
</script>

<template>
  <NImage
    v-if="url"
    :src="url"
    :alt="alt ?? undefined"
    object-fit="cover"
    :img-props="{
      style: thumb
        ? 'width: 88px; height: 88px; border-radius: 6px; display: block;'
        : 'max-width: 240px; max-height: 240px; border-radius: 6px; display: block;',
    }"
  />
  <div
    v-else
    class="flex items-center justify-center rounded bg-muted/40"
    :class="thumb ? 'h-[88px] w-[88px]' : 'h-24 w-40'"
  >
    <NSpin size="small" />
  </div>
</template>
