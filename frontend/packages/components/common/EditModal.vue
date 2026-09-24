<script lang="ts" setup>
import type { Tone } from '@xihan-ui/core'
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
   * 保存按钮语气：不传时不写 tone，按钮走实心形态的品牌配色。
   * 传 danger / warning 等，让终止、拒绝这类动作在按钮上保留视觉警示。
   */
  saveTone?: Tone
  /**
   * 弹窗内表单的 id。
   *
   * 保存钮在按钮行里、表单在正文里，两者是兄弟节点——不关联的话点保存会绕过整表校验。
   * 给了这个 id，保存钮就成为该表单的提交钮（HTML 的 form 属性允许跨节点关联），
   * 校验通过才会走到表单的 @submit；此时本组件不再发 save 事件。
   */
  formId?: string
  /**
   * 展开后先把焦点落到 content 内匹配此选择器的元素。
   * 不给即走组件库的默认可 tab 顺序——那会落在右上角的关闭钮上。
   * 组件库聚焦输入框时会连带全选其中的值，编辑既有记录的弹窗慎用。
   */
  initialFocus?: string
}>(), {
  title: undefined,
  width: 640,
  loading: false,
  saveDisabled: false,
  saveText: undefined,
  cancelText: undefined,
  saveTone: undefined,
  formId: undefined,
  initialFocus: undefined,
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
       表单内容配合全局 .xh-edit-form-grid 网格（两列/紧凑行距/紧凑标签），跨整行字段加 .xh-span-2。
       点遮罩不关：编辑到一半误点外面就丢内容，只能由取消/保存/Esc 收场 -->
  <XhDialogRoot
    :open="show"
    :initial-focus="initialFocus"
    :close-on-interact-outside="false"
    @update:open="(value: boolean) => emit('update:show', value)"
  >
    <XhDialogContent class="xh-edit-modal" :style="modalStyle">
      <XhDialogTitle v-if="title">
        {{ title }}
      </XhDialogTitle>
      <XhDialogCloseTrigger />

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
          :tone="saveTone"
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
/* 面板的行内内衬钉在组件库这一个槽上：正文的滚动沟槽按同一个值借位，两处不会各走各的 */
:global(.xh-edit-modal) {
  --xh-dialog-px: var(--xh-surface-px-md);
}

/* 表单区超高时在弹窗内部滚动，标题与按钮行留在原地。
   滚动口向两侧借走面板的内衬，再用同宽的内边距把表单推回原位：表单仍与标题、按钮行对齐，
   滚动口两侧则各多出一段沟槽。粗指针下框内按钮（数字框加减、清除、密码显隐）的 44px 命中区
   是往外扩的透明伪元素，贴着表单行尾的那颗会漫出几像素——没有这段沟槽，它就把滚动口撑出
   一条滚不出任何内容的横向滚动条，漫出的那截命中区也被滚动口裁掉、点不到 */
.xh-edit-modal__body {
  max-block-size: calc(100vh - 220px);
  margin-inline: calc(-1 * var(--xh-dialog-px));
  padding-inline: var(--xh-dialog-px);
  overflow: auto;
}

.xh-edit-modal__footer {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}
</style>
