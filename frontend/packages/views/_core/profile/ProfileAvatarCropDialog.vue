<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { ImageCropperRect } from '@xihan-ui/headless'
import type { AvatarCropSource } from './avatar-crop'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhImageCropperCropArea, XhImageCropperCropHandle, XhImageCropperImage, XhImageCropperRoot, XhImageCropperViewport, XhImageCropperZoomSlider } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { centeredSquare, exportAvatar } from './avatar-crop'

defineOptions({ name: 'ProfileAvatarCropDialog' })

/** source 有值即打开；loading 为上传中：裁切器与取消都停用，关不掉 */
const props = defineProps<{ source: AvatarCropSource | null, loading?: boolean }>()
const emit = defineEmits<{ confirm: [file: File], cancel: [] }>()

const { t } = useI18n()

/** 头像只给四个角的把手：锁了 1:1，边上的把手与角等效 */
const HANDLES = ['nw', 'ne', 'se', 'sw'] as const
/** 裁切框的最小边长（原图像素） */
const MIN_CROP_SIZE = 32

const zoomId = useId()
const rect = ref<ImageCropperRect>()
const exporting = ref(false)

watch(() => props.source, (source) => {
  rect.value = source
    ? centeredSquare(source.image)
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
    const file = await exportAvatar(props.source, rect.value)
    if (file)
      emit('confirm', file)
    else
      toast.danger(t('component.profile.info.err_avatar_crop_failed'))
  }
  finally {
    exporting.value = false
  }
}
</script>

<template>
  <XhDialogRoot
    :open="source !== null"
    size="sm"
    @update:open="(open: boolean) => !open && cancel()"
  >
    <XhDialogContent>
      <XhDialogHeader>
        <XhDialogTitle>{{ t('component.profile.info.crop_avatar_title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <XhImageCropperRoot
          v-if="source"
          v-model:value="rect"
          :src="source.url"
          :alt="t('component.profile.info.crop_avatar_alt')"
          shape="round"
          :aspect-ratio="1"
          :min-width="minSize"
          :min-height="minSize"
          :disabled="loading"
        >
          <XhImageCropperViewport class="pf-avatar-crop__viewport" :style="{ inlineSize: viewportWidth }">
            <XhImageCropperImage />
            <XhImageCropperCropArea>
              <XhImageCropperCropHandle v-for="position in HANDLES" :key="position" :position="position" />
            </XhImageCropperCropArea>
          </XhImageCropperViewport>
          <div class="pf-avatar-crop__zoom">
            <label :for="zoomId">{{ t('component.profile.info.crop_zoom') }}</label>
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
          <XhButtonLabel>{{ t('component.profile.info.btn_upload') }}</XhButtonLabel>
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 竖长图的视口比弹窗窄，居中放；缩放那一行仍占满弹窗的宽 */
.pf-avatar-crop__viewport {
  margin-inline: auto;
}

.pf-avatar-crop__zoom {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  align-items: center;
  gap: var(--xh-space-3);
  padding-block-start: var(--xh-space-3);
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
}
</style>
