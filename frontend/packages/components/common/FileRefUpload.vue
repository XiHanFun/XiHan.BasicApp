<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { FileUploadRequest } from '@xihan-ui/vue'
import { useFieldControl, XhAlertAction, XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhAlertTitle, XhButton, XhButtonIndicator, XhButtonLabel, XhFileUploadHiddenInput, XhFileUploadRoot, XhFileUploadTrigger, XhProgress } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, shallowRef, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { resolveAvatarUrl, toast, useAvatarUrl } from '~/composables'
import { ResumableUploadError } from '~/files'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores/app-context'

/**
 * 业务表单里的文件引用上传：上传到文件库，字段只存文件 ID
 *
 * 展示地址按需换取（签名地址会过期，不落库）；也兼容历史数据里的直链
 * image 显示预览图，file 显示原始文件名与打开入口。上传与查看走文件库的接口，
 * 使用者要有文件的上传与查看权限；上传与文件库同一条路径：先试秒传，大文件分片，中断了可以接着传
 */
defineOptions({ name: 'XFileRefUpload' })

const props = withDefaults(defineProps<{
  /** 文件引用：文件 ID，兼容直链 */
  value?: null | string
  /** image 显示预览图；file 显示文件名 */
  kind?: 'file' | 'image'
  /** 接受的 MIME / 扩展名；图片缺省只收图片 */
  accept?: string
  /** 最大体积（MB）；图片缺省 10，文件缺省不在前端限制，以文件库的上限为准 */
  maxSizeMb?: number
  /** 是否禁用 */
  disabled?: boolean
}>(), {
  value: null,
  kind: 'file',
  accept: undefined,
  maxSizeMb: undefined,
  disabled: false,
})

const emit = defineEmits<{
  'update:value': [value: null | string]
}>()

// 字段接线落在真正可聚焦的上传钮上，标签的 for 才指得到
const fieldControl = useFieldControl()

const { t } = useI18n()
const uploading = ref(false)
const progress = ref(0)
const progressLabelId = useId()

/** 中断了、还能接着传的那个文件：再传它时只补没传完的部分 */
const interrupted = shallowRef<{ file: File, message: string } | null>(null)
/** 「继续上传」不经上传组件，字段卸载时由这里中止 */
let resuming: AbortController | null = null

const isImage = computed(() => props.kind === 'image')
const acceptTypes = computed(() => props.accept ?? (isImage.value ? 'image/*' : undefined))
const sizeLimitMb = computed(() => props.maxSizeMb ?? (isImage.value ? 10 : undefined))
const maxFileSize = computed(() => (sizeLimitMb.value === undefined
  ? undefined
  : sizeLimitMb.value * 1024 * 1024))
const reference = computed(() => props.value?.trim() || null)

/** 图片预览地址：文件 ID 换签名地址（带缓存），直链按源解析 */
const previewUrl = useAvatarUrl(computed(() => (isImage.value ? reference.value : null)))

/** 文件名：刚上传的直接记下，回填的按 ID 查详情；直链取路径最后一段 */
const knownNames = new Map<string, string>()
const fileName = ref('')
const fileUnavailable = ref(false)

function isDirectUrl(value: string) {
  return /^(?:https?:\/\/|\/|data:|blob:)/i.test(value)
}

watch(
  () => (isImage.value ? null : reference.value),
  async (current) => {
    fileUnavailable.value = false
    if (!current) {
      fileName.value = ''
      return
    }
    const known = knownNames.get(current)
    if (known) {
      fileName.value = known
      return
    }
    if (isDirectUrl(current)) {
      fileName.value = decodeURIComponent(current.split(/[?#]/)[0]!.split('/').pop() || current)
      return
    }
    fileName.value = ''
    try {
      const detail = await useAppContext().apis.getFileApi(current)
      if (reference.value !== current) {
        return
      }
      if (detail) {
        knownNames.set(current, detail.originalName)
        fileName.value = detail.originalName
      }
      else {
        fileUnavailable.value = true
      }
    }
    catch {
      // 文件被删或没有查看权限：如实标出，而不是显示一个打不开的 ID
      if (reference.value === current) {
        fileUnavailable.value = true
      }
    }
  },
  { immediate: true },
)

const fileLabel = computed(() => {
  if (fileUnavailable.value) {
    return t('component.file_ref_upload.unavailable')
  }
  return fileName.value || reference.value || ''
})

const hint = computed(() => {
  if (sizeLimitMb.value === undefined) {
    return ''
  }
  return isImage.value
    ? t('component.file_ref_upload.hint_image', { size: sizeLimitMb.value })
    : t('component.file_ref_upload.hint_file', { size: sizeLimitMb.value })
})

/**
 * 传一个文件并回填它的 ID
 * 中断了可以续传的留下提示与「继续上传」，别的失败轻提示；中止（字段卸载）不报
 */
async function transfer(file: File, onProgress?: (percent: number) => void, signal?: AbortSignal) {
  uploading.value = true
  progress.value = 0
  interrupted.value = null
  try {
    const uploaded = await useAppContext().apis.uploadFileApi(file, {
      onProgress: (percent) => {
        progress.value = percent
        onProgress?.(percent)
      },
      signal,
    })
    knownNames.set(uploaded.id, uploaded.originalName)
    // 只落文件 ID：展示地址由读取侧按需换取，避免持久化会过期的签名地址
    emit('update:value', uploaded.id)
    toast.success(t('component.file_ref_upload.success'))
  }
  catch (error) {
    if (error instanceof ResumableUploadError) {
      interrupted.value = { file, message: error.message }
    }
    else if (!(error instanceof DOMException && error.name === 'AbortError')) {
      toast.danger((error as Error)?.message || t('component.file_ref_upload.failed'))
    }
    throw error
  }
  finally {
    uploading.value = false
  }
}

/** 抛回去让上传组件把这个文件记成失败态；组件卸载时由它的信号中止 */
function handleUpload(request: FileUploadRequest) {
  return transfer(request.file, request.onProgress, request.signal)
}

async function resume() {
  const pending = interrupted.value
  if (!pending) {
    return
  }
  const controller = new AbortController()
  resuming = controller
  try {
    await transfer(pending.file, undefined, controller.signal)
  }
  catch {
    // 失败已在 transfer 里报出或留下续传提示
  }
  finally {
    if (resuming === controller) {
      resuming = null
    }
  }
}

onBeforeUnmount(() => resuming?.abort())

/** 超限由上传组件判定后回调，这里只负责报出来 */
function handleReject() {
  toast.danger(t('component.file_ref_upload.too_large', { size: sizeLimitMb.value }))
}

async function open() {
  if (!reference.value) {
    return
  }
  const url = await resolveAvatarUrl(reference.value)
  if (!url) {
    toast.danger(t('component.file_ref_upload.open_failed'))
    return
  }
  window.open(url, '_blank', 'noopener')
}

function clear() {
  emit('update:value', null)
}
</script>

<template>
  <div class="x-file-ref-upload">
    <button
      v-if="isImage"
      type="button"
      class="x-file-ref-upload__preview"
      :disabled="!previewUrl"
      :aria-label="t('component.file_ref_upload.preview')"
      @click="open"
    >
      <img v-if="previewUrl" class="x-file-ref-upload__img" :src="previewUrl" alt="">
      <Icon v-else icon="lucide:image" />
    </button>
    <div class="x-file-ref-upload__body">
      <div v-if="!isImage && reference" class="x-file-ref-upload__file">
        <Icon icon="lucide:paperclip" />
        <span class="x-file-ref-upload__name" :title="fileLabel">{{ fileLabel }}</span>
        <XhButton v-if="!fileUnavailable" size="sm" variant="ghost" @click="open">
          {{ t('component.file_ref_upload.open') }}
        </XhButton>
      </div>
      <div class="x-file-ref-upload__actions">
        <XhFileUploadRoot
          :accept="acceptTypes"
          :max-file-size="maxFileSize"
          :disabled="disabled || uploading"
          :upload="handleUpload"
          @file-reject="handleReject"
        >
          <XhFileUploadHiddenInput />
          <XhFileUploadTrigger as-child>
            <XhButton v-bind="fieldControl" size="sm" variant="outline" :loading="uploading" :disabled="disabled">
              <XhButtonIndicator />
              <Icon icon="lucide:upload" />
              <XhButtonLabel>{{ reference ? t('component.file_ref_upload.change') : t('component.file_ref_upload.select') }}</XhButtonLabel>
            </XhButton>
          </XhFileUploadTrigger>
        </XhFileUploadRoot>
        <XhButton
          v-if="reference"
          size="sm"
          variant="ghost"
          :disabled="disabled || uploading"
          @click="clear"
        >
          <Icon icon="lucide:trash-2" />
          {{ t('component.file_ref_upload.remove') }}
        </XhButton>
      </div>
      <div v-if="uploading" class="x-file-ref-upload__progress">
        <XhProgress
          class="x-file-ref-upload__bar"
          :value="progress"
          variant="line"
          size="sm"
          :aria-labelledby="progressLabelId"
        />
        <span :id="progressLabelId" class="x-file-ref-upload__percent">
          {{ t('component.file_ref_upload.uploading', { percent: progress }) }}
        </span>
      </div>
      <XhAlertRoot v-else-if="interrupted" tone="warning" variant="subtle">
        <XhAlertIndicator>
          <Icon icon="lucide:triangle-alert" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertTitle>{{ t('component.file_ref_upload.interrupted', { name: interrupted.file.name }) }}</XhAlertTitle>
          <XhAlertDescription>{{ interrupted.message }}</XhAlertDescription>
        </XhAlertContent>
        <XhAlertAction>
          <XhButton size="sm" variant="ghost" :disabled="disabled" @click="resume">
            <Icon icon="lucide:rotate-cw" />
            <XhButtonLabel>{{ t('component.file_ref_upload.resume') }}</XhButtonLabel>
          </XhButton>
        </XhAlertAction>
      </XhAlertRoot>
      <div v-if="hint" class="x-file-ref-upload__hint">
        {{ hint }}
      </div>
    </div>
  </div>
</template>

<style scoped>
.x-file-ref-upload {
  display: flex;
  align-items: flex-start;
  gap: var(--xh-space-3);
  min-width: 0;
}

/* 预览框按承载面的圆角，边长取 3 个 space-8，与 Logo 上传的缺省预览同尺寸 */
.x-file-ref-upload__preview {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: calc(var(--xh-space-8) * 3);
  height: calc(var(--xh-space-8) * 3);
  padding: 0;
  overflow: hidden;
  border: var(--xh-stroke-thin) dashed var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-subtle);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xl);
  cursor: pointer;
}

.x-file-ref-upload__preview:disabled {
  cursor: default;
}

.x-file-ref-upload__preview:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.x-file-ref-upload__img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* 撑满字段宽，进度条与续传提示随字段铺开 */
.x-file-ref-upload__body {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-width: 0;
}

.x-file-ref-upload__file {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  min-width: 0;
  color: var(--xh-fg-default);
}

.x-file-ref-upload__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.x-file-ref-upload__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.x-file-ref-upload__progress {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}

.x-file-ref-upload__bar {
  flex: 1;
  min-width: 0;
}

.x-file-ref-upload__percent {
  flex-shrink: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  font-variant-numeric: tabular-nums;
}

.x-file-ref-upload__hint {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}
</style>
