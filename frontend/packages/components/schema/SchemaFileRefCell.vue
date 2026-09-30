<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { resolveAvatarUrl, toast, useAvatarUrl } from '~/composables'
import { Icon } from '~/iconify'

/**
 * 列表单元格里的文件引用：image 显示缩略图，file 显示「打开」入口。
 *
 * 单元格值是文件主键（兼容直链），访问地址按需换取（预签名地址会过期，不落库）。
 * 点击只打开文件，不冒泡到行（行点击是速览）。
 */
defineOptions({ name: 'SchemaFileRefCell' })

const props = defineProps<{
  /** 文件主键或直链 */
  value: string
  /** image 显示缩略图；file 显示打开入口 */
  kind: 'file' | 'image'
}>()

const { t } = useI18n()

/** 缩略图地址只在图片模式换取，文件模式点开时才换 */
const thumbnailUrl = useAvatarUrl(computed(() => (props.kind === 'image' ? props.value : null)))

async function open() {
  const url = await resolveAvatarUrl(props.value)
  if (!url) {
    toast.danger(t('component.schema_table.file_open_failed'))
    return
  }
  window.open(url, '_blank', 'noopener')
}
</script>

<template>
  <button
    v-if="kind === 'image'"
    type="button"
    class="schema-file-ref schema-file-ref--image"
    :aria-label="t('component.schema_table.image_preview')"
    @click.stop="open"
  >
    <img v-if="thumbnailUrl" class="schema-file-ref__thumbnail" :src="thumbnailUrl" alt="">
    <Icon v-else icon="lucide:image" />
  </button>
  <button
    v-else
    type="button"
    class="schema-file-ref schema-file-ref--file"
    @click.stop="open"
  >
    <Icon icon="lucide:paperclip" />
    <span>{{ t('component.schema_table.file_open') }}</span>
  </button>
</template>

<style scoped>
.schema-file-ref {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
  padding: 0;
  border: 0;
  background: none;
  color: var(--xh-fg-brand);
  font-size: inherit;
  cursor: pointer;
}

.schema-file-ref--image {
  justify-content: center;
  width: var(--xh-space-7);
  height: var(--xh-space-7);
  overflow: hidden;
  border-radius: var(--xh-radius-sm);
  background: var(--xh-bg-subtle);
  color: var(--xh-fg-muted);
}

.schema-file-ref__thumbnail {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.schema-file-ref:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-space-0);
}
</style>
