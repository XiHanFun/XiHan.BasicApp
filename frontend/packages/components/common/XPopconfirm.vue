<script lang="ts" setup>
import {
  XhPopconfirmCancelTrigger,
  XhPopconfirmConfirmTrigger,
  XhPopconfirmContent,
  XhPopconfirmDescription,
  XhPopconfirmPositioner,
  XhPopconfirmRoot,
  XhPopconfirmTrigger,
} from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'

/** 就地确认：触发元素放 trigger 插槽，确认文案走 default 插槽或 description 属性 */
defineOptions({ name: 'XPopconfirm' })

withDefaults(defineProps<{
  /** 确认文案；也可以用 default 插槽给 */
  description?: string
  okText?: string
  cancelText?: string
}>(), {
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
      <XhPopconfirmContent>
        <XhPopconfirmDescription>
          <slot>{{ description }}</slot>
        </XhPopconfirmDescription>
        <XhPopconfirmCancelTrigger>{{ cancelText ?? t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
        <XhPopconfirmConfirmTrigger>{{ okText ?? t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
      </XhPopconfirmContent>
    </XhPopconfirmPositioner>
  </XhPopconfirmRoot>
</template>
