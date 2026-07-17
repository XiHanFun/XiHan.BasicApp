<script lang="ts" setup>
import { NButton, NModal, NSpace } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

defineOptions({ name: 'XEditModal' })

const props = withDefaults(defineProps<{
  /** 是否显示（v-model:show） */
  show: boolean
  /** 弹窗标题 */
  title?: string
  /** 弹窗宽度：数字为 px，字符串原样使用（默认 640px，小屏自动收窄） */
  width?: number | string
  /** 保存按钮 loading */
  loading?: boolean
  /** 保存按钮禁用 */
  saveDisabled?: boolean
  /** 保存按钮文案（默认 common.actions.save） */
  saveText?: string
  /** 取消按钮文案（默认 common.actions.cancel） */
  cancelText?: string
}>(), {
  title: undefined,
  width: 640,
  loading: false,
  saveDisabled: false,
  saveText: undefined,
  cancelText: undefined,
})

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'save'): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const modalStyle = computed(() => ({
  width: typeof props.width === 'number' ? `${props.width}px` : props.width,
  maxWidth: 'calc(100vw - 32px)',
}))

function handleCancel() {
  emit('update:show', false)
  emit('cancel')
}
</script>

<template>
  <!-- 新增/编辑弹窗统一外壳（以用户页为基准）：preset=card 使用 Naive 主题卡片背景避免暗色下透明；
       表单内容配合全局 .xh-edit-form-grid 网格（两列/行距 10px/紧凑标签），跨整行字段加 .xh-span-2 -->
  <NModal
    :show="show"
    :mask-closable="false"
    :auto-focus="false"
    :bordered="false"
    :title="title"
    preset="card"
    :style="modalStyle"
    @update:show="emit('update:show', $event)"
  >
    <slot />

    <template #footer>
      <NSpace justify="end">
        <slot name="footer-extra" />
        <NButton size="small" @click="handleCancel">
          {{ cancelText ?? t('common.actions.cancel') }}
        </NButton>
        <NButton size="small" type="primary" :loading="loading" :disabled="saveDisabled" @click="emit('save')">
          {{ saveText ?? t('common.actions.save') }}
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>
