<script lang="ts" setup>
import { XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle } from '@xihan-ui/vue'
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
  /**
   * 弹窗内表单的 id。
   *
   * 保存钮在按钮行里、表单在正文里，两者是兄弟节点——不关联的话点保存会绕过整表校验。
   * 给了这个 id，保存钮就成为该表单的提交钮（HTML 的 form 属性允许跨节点关联），
   * 校验通过才会走到表单的 @submit；此时本组件不再发 save 事件。
   */
  formId?: string
}>(), {
  title: undefined,
  width: 640,
  loading: false,
  saveDisabled: false,
  saveText: undefined,
  cancelText: undefined,
  formId: undefined,
})

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'save'): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const modalStyle = computed(() => ({
  '--xh-dialog-max-w': typeof props.width === 'number' ? `${props.width}px` : props.width,
}))

function handleCancel() {
  emit('update:show', false)
  emit('cancel')
}
</script>

<template>
  <!-- 新增/编辑弹窗统一外壳（以用户页为基准）：
       表单内容配合全局 .xh-edit-form-grid 网格（两列/行距 10px/紧凑标签），跨整行字段加 .xh-span-2。
       点遮罩不关：编辑到一半误点外面就丢内容，只能由取消/保存/Esc 收场 -->
  <XhDialogRoot
    :open="show"
    :close-on-interact-outside="false"
    @update:open="(value: boolean) => emit('update:show', value)"
  >
    <XhDialogContent class="xh-edit-modal" :style="modalStyle">
      <XhDialogTitle v-if="title">
        {{ title }}
      </XhDialogTitle>
      <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>

      <div class="xh-edit-modal__body">
        <slot />
      </div>

      <div class="xh-edit-modal__footer">
        <slot name="footer-extra" />
        <XhButton size="sm" variant="outline" @click="handleCancel">
          {{ cancelText ?? t('common.actions.cancel') }}
        </XhButton>
        <XhButton
          size="sm"
          variant="solid"
          :type="formId ? 'submit' : 'button'"
          :form="formId"
          :loading="loading"
          :disabled="saveDisabled"
          @click="formId ? undefined : emit('save')"
        >
          {{ saveText ?? t('common.actions.save') }}
        </XhButton>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 表单区超高时在弹窗内部滚动，标题与按钮行留在原地 */
.xh-edit-modal__body {
  max-block-size: calc(100vh - 220px);
  overflow: auto;
}

.xh-edit-modal__footer {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}
</style>
