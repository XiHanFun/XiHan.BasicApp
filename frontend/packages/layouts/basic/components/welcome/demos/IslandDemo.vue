<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSequence } from '~/composables'
import { Icon } from '~/iconify'

/**
 * 欢迎页演示：灵动岛。顶部的小岛轮流展开成导出进度、导出完成、通话与设置同步
 */
defineOptions({ name: 'WelcomeIslandDemo' })

type State = 'call' | 'done' | 'export' | 'idle' | 'sync'

const { t } = useI18n()
const state = ref<State>('idle')

const loop = useSequence(async (wait) => {
  state.value = 'idle'
  await wait(1000)
  state.value = 'export'
  await wait(3500)
  state.value = 'done'
  await wait(1900)
  state.value = 'idle'
  await wait(800)
  state.value = 'call'
  await wait(3300)
  state.value = 'idle'
  await wait(700)
  state.value = 'sync'
  await wait(2100)
})

onMounted(loop.start)
</script>

<template>
  <div class="demo">
    <div class="page">
      <div class="page__bar">
        <i /><i /><i />
      </div>
      <div class="page__body">
        <i /><i /><i /><i /><i /><i />
      </div>
    </div>

    <div class="island" :data-state="state">
      <div class="island__face" data-of="idle">
        <b /><i />
      </div>
      <div class="island__face" data-of="export">
        <span class="island__icon"><Icon icon="lucide:download" /></span>
        <span class="island__text">
          <strong>{{ t('welcome.island.demo_exporting') }}</strong>
          <small>{{ t('welcome.island.demo_file') }}</small>
        </span>
        <span class="island__ring"><em /></span>
      </div>
      <div class="island__face" data-of="done">
        <span class="island__icon is-ok"><Icon icon="lucide:check" /></span>
        <span class="island__text">
          <strong>{{ t('welcome.island.demo_done') }}</strong>
          <small>{{ t('welcome.island.demo_rows') }}</small>
        </span>
      </div>
      <div class="island__face" data-of="call">
        <span class="island__avatar">{{ t('welcome.island.demo_peer').slice(0, 1) }}</span>
        <span class="island__text">
          <strong>{{ t('welcome.island.demo_peer') }}</strong>
          <small>{{ t('welcome.island.demo_call') }}</small>
        </span>
        <span class="island__wave"><i /><i /><i /><i /><i /></span>
        <span class="island__hang"><Icon icon="lucide:phone-off" /></span>
      </div>
      <div class="island__face" data-of="sync">
        <span class="island__icon"><Icon icon="lucide:refresh-cw" /></span>
        <span class="island__text">
          <strong>{{ t('welcome.island.demo_synced') }}</strong>
          <small>{{ t('welcome.island.demo_synced_from') }}</small>
        </span>
      </div>
    </div>

    <p class="demo__caption">
      {{ t('welcome.island.demo_caption') }}
    </p>
  </div>
</template>

<style scoped>
/* 进度环的百分比与弧度登记成数值类型，才能在关键帧里过渡 */
@property --island-progress {
  syntax: '<integer>';
  inherits: true;
  initial-value: 0;
}

.demo {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-3);
  block-size: 100%;
  padding: var(--xh-space-6) var(--xh-space-6) var(--xh-space-4);
}

.demo__caption {
  margin: 0;
  padding: var(--xh-space-0_5) var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-surface);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
  white-space: nowrap;
}

/* 岛下面的页面，只是陪衬 */
.page {
  flex: 1;
  inline-size: 100%;
  min-block-size: 0;
  overflow: hidden;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-canvas);
  box-shadow: var(--xh-shadow-xl);
}

.page__bar {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  block-size: calc(var(--xh-space-8) * 1.6);
  padding-inline: var(--xh-space-4);
  border-block-end: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
}

.page__bar i {
  inline-size: var(--xh-space-5);
  block-size: var(--xh-space-5);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-subtle-active);
}

.page__bar i:first-child {
  inline-size: calc(var(--xh-space-8) * 2.5);
  block-size: var(--xh-space-2);
  margin-inline-end: auto;
  border-radius: var(--xh-shape-pill);
}

.page__body {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--xh-space-3);
  padding: calc(var(--xh-space-8) * 1.7) var(--xh-space-4) 0;
}

.page__body i {
  block-size: calc(var(--xh-space-8) * 2.2);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-surface);
}

.page__body i:nth-child(n + 4) {
  grid-column: span 3;
  block-size: var(--xh-space-2);
  border: 0;
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
}

.page__body i:nth-child(5) {
  inline-size: 72%;
}

.page__body i:nth-child(6) {
  inline-size: 86%;
}

/* 岛：一颗黑色胶囊，按内容伸缩；它在任何主题下都是深色的，颜色取媒体控件那一族 */
.island {
  position: absolute;
  inset-block-start: calc(var(--xh-space-6) + var(--xh-space-2));
  inset-inline-start: 50%;
  inline-size: calc(var(--xh-space-8) * 3.4);
  block-size: var(--xh-space-8);
  overflow: hidden;
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-media-chrome);
  box-shadow: var(--xh-shadow-xl);
  color: var(--xh-fg-on-media-chrome);
  transform: translateX(-50%);
  transition:
    inline-size var(--xh-motion-duration-reveal) var(--xh-motion-ease-settle),
    block-size var(--xh-motion-duration-reveal) var(--xh-motion-ease-settle);
}

.island[data-state='export'] {
  inline-size: calc(var(--xh-space-8) * 9.5);
  block-size: calc(var(--xh-space-8) * 1.7);
}

.island[data-state='done'],
.island[data-state='sync'] {
  inline-size: calc(var(--xh-space-8) * 8);
  block-size: calc(var(--xh-space-8) * 1.7);
}

.island[data-state='call'] {
  inline-size: calc(var(--xh-space-8) * 10.5);
  block-size: calc(var(--xh-space-8) * 2);
}

.island__face {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  gap: var(--xh-space-3);
  padding-inline: var(--xh-space-4);
  opacity: 0;
  white-space: nowrap;
  transform: scale(0.9);
  transition:
    opacity var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.island[data-state='idle'] [data-of='idle'],
.island[data-state='export'] [data-of='export'],
.island[data-state='done'] [data-of='done'],
.island[data-state='call'] [data-of='call'],
.island[data-state='sync'] [data-of='sync'] {
  opacity: 1;
  transform: none;
  transition-delay: var(--xh-motion-duration-enter);
}

.island__face[data-of='idle'] {
  justify-content: center;
  gap: var(--xh-space-1_5);
}

.island__face[data-of='idle'] b {
  inline-size: var(--xh-space-2);
  block-size: var(--xh-space-2);
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-fg-success);
}

.island__face[data-of='idle'] i {
  inline-size: var(--xh-space-6);
  block-size: var(--xh-space-1);
  border-radius: var(--xh-shape-pill);
  background-color: currentColor;
  opacity: 0.3;
}

.island__icon,
.island__avatar,
.island__hang {
  flex: none;
  display: grid;
  place-items: center;
  inline-size: calc(var(--xh-space-8) + var(--xh-space-1));
  block-size: calc(var(--xh-space-8) + var(--xh-space-1));
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-bg-media-chrome-hover);
  font-size: var(--xh-font-size-md);
}

.island__icon.is-ok {
  background-color: var(--xh-fg-success);
}

.island__avatar {
  background-color: var(--xh-bg-brand);
  color: var(--xh-fg-on-brand);
  font-weight: var(--xh-font-weight-semibold);
}

.island__hang {
  background-color: var(--xh-fg-danger);
}

.island__text {
  display: flex;
  flex-direction: column;
  line-height: var(--xh-leading-tight);
}

.island__text strong {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
}

.island__text small {
  font-size: var(--xh-font-size-xs);
  opacity: 0.6;
}

/* 进度环：一圈锥形渐变，中间的数字跟着同一个进度走 */
.island__ring {
  display: grid;
  place-items: center;
  inline-size: calc(var(--xh-space-8) + var(--xh-space-1));
  block-size: calc(var(--xh-space-8) + var(--xh-space-1));
  margin-inline-start: auto;
  border-radius: var(--xh-shape-circle);
  background: conic-gradient(var(--xh-fg-success) calc(var(--island-progress) * 1%), var(--xh-bg-media-chrome-hover) 0);
}

.island__ring em {
  display: grid;
  place-items: center;
  inline-size: 76%;
  block-size: 76%;
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-bg-media-chrome);
  font-size: var(--xh-font-size-xs);
  font-style: normal;
  font-variant-numeric: tabular-nums;
}

.island__ring em::after {
  counter-reset: island-progress var(--island-progress);
  content: counter(island-progress);
}

.island[data-state='export'] .island__ring {
  animation: island-progress 2.5s cubic-bezier(0.4, 0.1, 0.3, 1) 0.55s forwards;
}

@keyframes island-progress {
  to {
    --island-progress: 100;
  }
}

.island__wave {
  display: flex;
  align-items: center;
  gap: var(--xh-space-0_5);
  block-size: var(--xh-space-5);
  margin-inline-start: auto;
}

.island__wave i {
  inline-size: var(--xh-space-0_5);
  block-size: 30%;
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-fg-success);
  animation: island-wave 1s ease-in-out infinite;
}

.island__wave i:nth-child(2) {
  animation-delay: -0.2s;
}

.island__wave i:nth-child(3) {
  animation-delay: -0.45s;
}

.island__wave i:nth-child(4) {
  animation-delay: -0.7s;
}

.island__wave i:nth-child(5) {
  animation-delay: -0.3s;
}

@keyframes island-wave {
  50% {
    block-size: 100%;
  }
}
</style>
