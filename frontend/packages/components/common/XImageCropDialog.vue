<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { ImageCropperRect, ImageCropperShape } from '@xihan-ui/headless'
import type { ImageCropSource } from './image-crop'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhImageCropperCropArea, XhImageCropperCropHandle, XhImageCropperImage, XhImageCropperRoot, XhImageCropperViewport, XhImageCropperZoomSlider } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { exportCroppedImage, initialCropRect } from './image-crop'

defineOptions({ name: 'XImageCropDialog' })

/**
 * 选好图片后的裁剪弹窗：source 有值即打开，确认后交出裁好的那张
 * loading 为上传中：裁切器与取消都停用，关不掉
 */
const props = withDefaults(defineProps<{
  source: ImageCropSource | null
  title: string
  confirmText: string
  loading?: boolean
  /** 锁定的宽高比；不传即自由裁切 */
  aspectRatio?: number
  shape?: ImageCropperShape
  /** 出图宽度的上限（像素） */
  maxWidth: number
  /** 一律出 JPEG（照片类图片控制体积） */
  opaque?: boolean
  size?: 'sm' | 'md'
}>(), {
  shape: 'rect',
  opaque: false,
  size: 'sm',
})
const emit = defineEmits<{ confirm: [file: File], cancel: [] }>()

const { t } = useI18n()

/** 锁了比例只给四个角的把手：边上的把手与角等效 */
const CORNER_HANDLES = ['nw', 'ne', 'se', 'sw'] as const
const ALL_HANDLES = [
  'nw',
  'n',
  'ne',
  'e',
  'se',
  's',
  'sw',
  'w',
] as const
/** 裁切框的最小边长（原图像素） */
const MIN_CROP_SIZE = 32

const zoomId = useId()
const rect = ref<ImageCropperRect>()
const exporting = ref(false)

const handles = computed(() => props.aspectRatio
  ? CORNER_HANDLES
  : ALL_HANDLES)

watch(() => props.source, (source) => {
  rect.value = source
    ? initialCropRect(source.image, props.aspectRatio)
    : undefined
}, { immediate: true })

/** 原图比这还小时，最小边长跟着降到原图短边 */
const minSize = computed(() => props.source
  ? Math.min(MIN_CROP_SIZE, props.source.image.naturalWidth, props.source.image.naturalHeight)
  : 0)

/** 视口的高由图片撑出来：竖长图按半屏高反推视口的宽度，免得弹窗被撑出屏幕 */
const viewportWidth = computed(() => {
  if (!props.source)
    return '100%'
  const ratio = props.source.image.naturalWidth / props.source.image.naturalHeight
  return `min(100%, calc(50vh * ${ratio}))`
})

function cancel() {
  if (!props.loading && !exporting.value)
    emit('cancel')
}

async function confirm() {
  if (!props.source || !rect.value)
    return
  exporting.value = true
  try {
    const file = await exportCroppedImage(props.source, rect.value, { maxWidth: props.maxWidth, opaque: props.opaque })
    if (file)
      emit('confirm', file)
    else
      toast.danger(t('component.image_crop.failed'))
  }
  finally {
    exporting.value = false
  }
}
</script>

<template>
  <XhDialogRoot
    :open="source !== null"
    :size="size"
    @update:open="(open: boolean) => !open && cancel()"
  >
    <XhDialogContent>
      <XhDialogHeader>
        <XhDialogTitle>{{ title }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <XhImageCropperRoot
          v-if="source"
          v-model:value="rect"
          :src="source.url"
          :alt="t('component.image_crop.alt')"
          :shape="shape"
          :aspect-ratio="aspectRatio"
          :min-width="minSize"
          :min-height="minSize"
          :disabled="loading"
        >
          <XhImageCropperViewport class="x-image-crop__viewport" :style="{ inlineSize: viewportWidth }">
            <XhImageCropperImage />
            <XhImageCropperCropArea>
              <XhImageCropperCropHandle v-for="position in handles" :key="position" :position="position" />
            </XhImageCropperCropArea>
          </XhImageCropperViewport>
          <div class="x-image-crop__zoom">
            <label :for="zoomId">{{ t('component.image_crop.zoom') }}</label>
            <XhImageCropperZoomSlider :id="zoomId" />
          </div>
        </XhImageCropperRoot>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton size="sm" variant="outline" :disabled="loading" @click="cancel">
          {{ t('common.actions.cancel') }}
        </XhButton>
        <XhButton size="sm" variant="solid" tone="brand" :loading="loading || exporting" @click="confirm">
          <XhButtonIndicator />
          <XhButtonLabel>{{ confirmText }}</XhButtonLabel>
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 竖长图的视口比弹窗窄，居中放；缩放那一行仍占满弹窗的宽 */
.x-image-crop__viewport {
  margin-inline: auto;
}

.x-image-crop__zoom {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  align-items: center;
  gap: var(--xh-space-3);
  padding-block-start: var(--xh-space-3);
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
}
</style>
