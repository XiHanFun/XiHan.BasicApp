<script lang="ts" setup>
import type { UploadCustomRequestOptions } from 'naive-ui'
import { NButton, NUpload, useMessage } from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { fileApi, ResourceAccessLevel } from '@/api'
import { Icon } from '~/components'
import { useAvatarUrl } from '~/composables'

defineOptions({ name: 'XLogoUpload' })

const props = withDefaults(defineProps<{
  /** 已存储的引用：上传后为文件主键(fileId)，兼容直链；展示时按需解析（避免持久化会过期的签名地址） */
  modelValue?: null | string
  /** 上传目录（归档到该子目录，便于区分品牌资源） */
  directory?: string
  /** 最大体积（MB） */
  maxSizeMb?: number
  /** 接受的图片 MIME */
  accept?: string
  /** 预览方框边长（px） */
  previewSize?: number
  /** 是否禁用 */
  disabled?: boolean
}>(), {
  modelValue: null,
  directory: 'branding',
  maxSizeMb: 2,
  accept: 'image/png,image/jpeg,image/svg+xml,image/webp,image/x-icon',
  previewSize: 96,
  disabled: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: null | string]
}>()

const { t } = useI18n()
const message = useMessage()
const uploading = ref(false)

/** 引用(fileId)/直链统一经 useAvatarUrl 解析为可展示地址：fileId→预签名(带缓存)，直链→按源解析 */
const previewUrl = useAvatarUrl(computed(() => props.modelValue))
const boxStyle = computed(() => ({ width: `${props.previewSize}px`, height: `${props.previewSize}px` }))

async function handleUpload(options: UploadCustomRequestOptions) {
  const rawFile = options.file.file
  if (!rawFile) {
    options.onError()
    return
  }

  if (rawFile.size > props.maxSizeMb * 1024 * 1024) {
    message.error(t('component.logo_upload.too_large', { size: props.maxSizeMb }))
    options.onError()
    return
  }

  uploading.value = true
  try {
    // 公开访问级别 + 品牌目录：本地存储副本可匿名直链（登录页展示），对象存储由后端读取时按需签名
    const detail = await fileApi.upload({
      file: rawFile,
      accessLevel: ResourceAccessLevel.Public,
      directory: props.directory,
    })
    // 仅落库文件主键：展示地址由读取侧按需解析，避免持久化会过期的签名地址
    emit('update:modelValue', String(detail.basicId))
    options.onFinish()
    message.success(t('component.logo_upload.success'))
  }
  catch {
    options.onError()
    message.error(t('component.logo_upload.failed'))
  }
  finally {
    uploading.value = false
  }
}

function clear() {
  emit('update:modelValue', null)
}
</script>

<template>
  <div class="x-logo-upload">
    <div class="x-logo-upload__box" :style="boxStyle">
      <img v-if="previewUrl" class="x-logo-upload__img" :src="previewUrl" :alt="t('component.logo_upload.preview')">
      <div v-else class="x-logo-upload__empty">
        <Icon icon="lucide:image" />
      </div>
    </div>
    <div class="x-logo-upload__actions">
      <NUpload
        :show-file-list="false"
        :accept="accept"
        :disabled="disabled || uploading"
        :custom-request="handleUpload"
      >
        <NButton size="small" :loading="uploading" :disabled="disabled">
          <template #icon>
            <Icon icon="lucide:upload" />
          </template>
          {{ previewUrl ? t('component.logo_upload.change') : t('component.logo_upload.select') }}
        </NButton>
      </NUpload>
      <NButton
        v-if="previewUrl"
        size="small"
        quaternary
        :disabled="disabled || uploading"
        @click="clear"
      >
        <template #icon>
          <Icon icon="lucide:trash-2" />
        </template>
        {{ t('component.logo_upload.remove') }}
      </NButton>
      <div class="x-logo-upload__hint">
        {{ t('component.logo_upload.hint', { size: maxSizeMb }) }}
      </div>
    </div>
  </div>
</template>

<style scoped>
.x-logo-upload {
  display: flex;
  align-items: flex-start;
  gap: 16px;
}

.x-logo-upload__box {
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  border: 1px dashed hsl(var(--border));
  border-radius: 8px;
  background: hsl(var(--muted) / 40%);
  flex-shrink: 0;
}

.x-logo-upload__img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.x-logo-upload__empty {
  font-size: 24px;
  color: hsl(var(--muted-foreground));
}

.x-logo-upload__actions {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  padding-top: 4px;
}

.x-logo-upload__hint {
  font-size: 12px;
  line-height: 1.5;
  color: hsl(var(--muted-foreground));
}
</style>
