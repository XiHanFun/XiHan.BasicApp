<script setup lang="ts">
import { XhFieldLabel } from '@xihan-ui/vue'
import XHelpTip from './XHelpTip.vue'

/**
 * 字段名，可带一段说明
 *
 * 弹窗、抽屉里针对单个字段的说明不占字段下方的位置，收进字段名后面的问号提示（与偏好设置各项同款）；
 * 针对整张表单的说明、校验报错不走这里
 * 问号是一枚按钮，不能放进 label 里（label 里不许再有可标注的元素），所以与字段名并排、由这里包一行；
 * 标签到控件的那段间距随之挪到这一行上
 */
defineOptions({ name: 'XFieldLabel' })

defineProps<{
  /** 这个字段的说明；不给或为空即只出字段名 */
  help?: null | string
}>()
</script>

<template>
  <span v-if="help" class="x-field-label">
    <XhFieldLabel><slot /></XhFieldLabel>
    <XHelpTip :content="help" />
  </span>
  <XhFieldLabel v-else>
    <slot />
  </XhFieldLabel>
</template>

<style scoped>
.x-field-label {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
  min-inline-size: 0;
  /* 与组件库给标签的那段补白同一个算式：容器的 gap 之上补差值 */
  margin-block-end: calc(var(--xh-field-label-gap-block, var(--xh-space-2)) - var(--xh-field-gap, var(--xh-space-0)));
}

.x-field-label :deep([data-scope='field'][data-part='label']) {
  margin-block-end: 0;
}
</style>
