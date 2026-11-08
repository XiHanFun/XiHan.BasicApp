<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
defineOptions({ name: 'AppBrand' })

const props = withDefaults(defineProps<AppBrandProps>(), {
  collapsed: false,
  logoSize: 32,
})

const emit = defineEmits<{ click: [] }>()

interface AppBrandProps {
  appTitle: string
  appLogo: string
  collapsed?: boolean
  logoSize?: number
}
</script>

<template>
  <div
    :class="[props.collapsed ? 'justify-center' : 'px-3']"
    class="app-brand flex h-full w-full min-w-0 cursor-pointer items-center overflow-hidden"
    @click="emit('click')"
  >
    <div class="flex min-w-0 items-center gap-2 overflow-hidden">
      <span class="app-brand-logo-wrap flex-shrink-0">
        <img
          :src="props.appLogo"
          :alt="props.appTitle"
          class="app-brand-logo"
          :style="{ width: `${props.logoSize}px`, height: `${props.logoSize}px` }"
        >
      </span>
      <!-- 展开时随侧栏变宽淡入；收起即撤，不等淡出 -->
      <Transition name="app-brand-title">
        <span
          v-if="!props.collapsed"
          class="app-brand-title min-w-0 flex-1 truncate"
        >
          {{ props.appTitle }}
        </span>
      </Transition>
    </div>
  </div>
</template>

<style scoped>
.app-brand-title-enter-active {
  transition: opacity var(--xh-motion-duration-move) var(--xh-motion-ease-enter);
}

.app-brand-title-enter-from {
  opacity: 0;
}

.app-brand-logo-wrap {
  display: inline-flex;
  height: 32px;
  width: 32px;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  border-radius: var(--xh-shape-control);
}

.app-brand-logo {
  height: 100%;
  width: 100%;
  object-fit: cover;
}

.app-brand-title {
  font-size: var(--xh-font-size-lg);
  font-weight: 600;
  letter-spacing: 0;
  line-height: 1.2;
  color: var(--xh-fg-default);
  max-width: 100%;
}
</style>
