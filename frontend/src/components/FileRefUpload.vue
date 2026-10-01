<script lang="ts" setup>
import type { FileUploadRequest } from '@xihan-ui/vue'
import { useFieldControl, XhButton, XhButtonIndicator, XhButtonLabel, XhFileUploadHiddenInput, XhFileUploadRoot, XhFileUploadTrigger } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { fileApi } from '@/api'
import { Icon } from '~/components'
import { resolveAvatarUrl, toast, useAvatarUrl } from '~/composables'

/**
 * 业务表单里的文件引用上传：上传到文件中心，字段只存文件主键(fileId)。
 *
 * 展示地址按需换取（预签名地址会过期，不落库）；也兼容历史数据里的直链。
 * image 显示预览图，file 显示原始文件名与打开入口。上传与查看走文件中心接口，
 * 使用者需要文件的上传与查看权限。
 */
defineOptions({ name: 'XFileRefUpload' })

const props = withDefaults(defineProps<{
  /** 文件引用：文件主键(fileId)，兼容直链 */
  value?: null | string
  /** image 显示预览图；file 显示文件名 */
  kind?: 'file' | 'image'
  /** 接受的 MIME / 扩展名；图片缺省只收图片 */
  accept?: string
  /** 最大体积（MB） */
  maxSizeMb?: number
  /** 上传到文件中心的目录 */
  directory?: string
  /** 是否禁用 */
  disabled?: boolean
}>(), {
  value: null,
  kind: 'file',
  accept: undefined,
  maxSizeMb: 10,
  directory: undefined,
  disabled: false,
})

const emit = defineEmits<{
  'update:value': [value: null | string]
}>()

// 字段接线落在真正可聚焦的上传钮上，标签的 for 才指得到
const fieldControl = useFieldControl()

const { t } = useI18n()
const uploading = ref(false)

const isImage = computed(() => props.kind === 'image')
const acceptTypes = computed(() => props.accept ?? (isImage.value ? 'image/*' : undefined))
const maxFileSize = computed(() => props.maxSizeMb * 1024 * 1024)
const reference = computed(() => props.value?.trim() || null)

/** 图片预览地址：fileId 换预签名（带缓存），直链按源解析 */
const previewUrl = useAvatarUrl(computed(() => (isImage.value ? reference.value : null)))

/** 文件名：刚上传的直接记下，回填的按主键查详情；直链取路径最后一段 */
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
      const detail = await fileApi.detail(current)
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
      // 文件被删或无查看权限：如实标出，而不是显示一个打不开的主键
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

const hint = computed(() =>
  isImage.value
    ? t('component.file_ref_upload.hint_image', { size: props.maxSizeMb })
    : t('component.file_ref_upload.hint_file', { size: props.maxSizeMb }),
)

async function handleUpload(request: FileUploadRequest) {
  uploading.value = true
  try {
    const detail = await fileApi.upload({ file: request.file, directory: props.directory })
    const fileId = String(detail.basicId)
    knownNames.set(fileId, detail.originalName)
    // 只落文件主键：展示地址由读取侧按需换取，避免持久化会过期的签名地址
    emit('update:value', fileId)
    toast.success(t('component.file_ref_upload.success'))
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('component.file_ref_upload.failed'))
    throw error
  }
  finally {
    uploading.value = false
  }
}

/** 超限由上传组件判定后回调，这里只负责报出来 */
function handleReject() {
  toast.danger(t('component.file_ref_upload.too_large', { size: props.maxSizeMb }))
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
      <div class="x-file-ref-upload__hint">
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

/* 预览框按 surface 圆角，边长取 3 个 space-8，与 Logo 上传的缺省预览同尺寸 */
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
  border-radius: var(--xh-radius-md);
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
  outline-offset: var(--xh-space-0);
}

.x-file-ref-upload__img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.x-file-ref-upload__body {
  display: flex;
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

.x-file-ref-upload__hint {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}
</style>
