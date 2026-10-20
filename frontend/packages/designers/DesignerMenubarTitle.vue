<!--
  设计器菜单栏右边一段里宿主的那一行：正在编辑的是哪一份（名称为主、编码为辅）、补充的标签与保存状态。
  流程、打印两款编辑器共用，长相一致；放进设计器元素的 slot="menubar-end" 容器里。
-->
<script setup lang="ts">
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'

defineOptions({ name: 'DesignerMenubarTitle' })

defineProps<{
  /** 名称：模板名、流程名；还没起名时由宿主给一个缺省叫法 */
  name: string
  /** 编码：等宽小字，不是状态，不套标签 */
  code?: string
  /** 有没有未保存的修改 */
  saved: boolean
  /** 保存状态的文字（已保存 / 未保存），各模块用自己的文案 */
  statusText: string
}>()
</script>

<template>
  <span class="designer-menubar-title">
    <span class="designer-menubar-title__name" :title="name">{{ name }}</span>
    <code v-if="code" class="designer-menubar-title__code" :title="code">{{ code }}</code>
    <!-- 补充的标签（如所绑数据源、基础信息待完善），排在保存状态之前 -->
    <slot />
    <XhTagRoot class="designer-menubar-title__status" variant="subtle" size="sm" :tone="saved ? 'success' : 'warning'">
      <XhTagLabel>{{ statusText }}</XhTagLabel>
    </XhTagRoot>
  </span>
</template>

<style scoped>
/* 放不下时名称与编码先收窄（省略号），标签与保存状态不收窄 */
.designer-menubar-title {
  display: inline-flex;
  min-inline-size: 0;
  align-items: center;
  gap: var(--xh-space-2);
}

.designer-menubar-title__name,
.designer-menubar-title__code {
  min-inline-size: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.designer-menubar-title__name {
  color: var(--xh-fg-default);
  font-weight: var(--xh-font-weight-semibold);
}

.designer-menubar-title__code {
  flex: 0 1 auto;
  color: var(--xh-fg-muted);
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
}

.designer-menubar-title__status,
.designer-menubar-title :slotted(*) {
  flex: none;
}
</style>
