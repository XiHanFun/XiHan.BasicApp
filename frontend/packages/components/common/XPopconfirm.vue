<script lang="ts" setup>
import {
  XhPopconfirmCancelTrigger,
  XhPopconfirmConfirmTrigger,
  XhPopconfirmContent,
  XhPopconfirmDescription,
  XhPopconfirmPositioner,
  XhPopconfirmRoot,
  XhPopconfirmTitle,
  XhPopconfirmTrigger,
} from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'

/** 就地确认：触发元素放 trigger 插槽，确认文案走 default 插槽或 description 属性 */
defineOptions({ name: 'XPopconfirm' })

withDefaults(defineProps<{
  /** 标题：直接问这个操作，同时是浮层（非模态 dialog）的名字 */
  title?: string
  /** 确认文案；也可以用 default 插槽给 */
  description?: string
  okText?: string
  cancelText?: string
}>(), {
  title: undefined,
  description: undefined,
  okText: undefined,
  cancelText: undefined,
})

const emit = defineEmits<{ confirm: [], cancel: [] }>()

const { t } = useI18n()
</script>

<template>
  <XhPopconfirmRoot @confirm="emit('confirm')" @cancel="emit('cancel')">
    <XhPopconfirmTrigger as-child>
      <slot name="trigger" />
    </XhPopconfirmTrigger>
    <XhPopconfirmPositioner>
      <!-- 浮层的名字取自 title 部件；没给标题时退回描述文案，再退回「确认」 -->
      <XhPopconfirmContent :aria-label="title ? undefined : (description || t('common.actions.confirm'))">
        <XhPopconfirmTitle v-if="title">
          {{ title }}
        </XhPopconfirmTitle>
        <XhPopconfirmDescription>
          <slot>{{ description }}</slot>
        </XhPopconfirmDescription>
        <XhPopconfirmCancelTrigger>{{ cancelText ?? t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
        <XhPopconfirmConfirmTrigger>{{ okText ?? t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
      </XhPopconfirmContent>
    </XhPopconfirmPositioner>
  </XhPopconfirmRoot>
</template>
