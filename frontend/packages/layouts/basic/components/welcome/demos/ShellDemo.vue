<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSequence } from '~/composables'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'

/**
 * 欢迎页演示：界面。一扇缩小的窗口轮流换布局与亮暗
 * 暗色用的是真的主题令牌（窗口自己带暗色标记）；应用本身已是暗色时这一步跳过
 */
defineOptions({ name: 'WelcomeShellDemo' })

type Step = 'dark' | 'mini' | 'side' | 'top'

const { t } = useI18n()
const { isDark } = useTheme()
const step = ref<Step>('side')

const darkened = computed(() => !isDark.value && (step.value === 'dark' || step.value === 'mini'))
const captions = computed<Record<Step, string>>(() => ({
  side: t('welcome.shell.demo_side'),
  dark: t('welcome.shell.demo_dark'),
  mini: t('welcome.shell.demo_mini'),
  top: t('welcome.shell.demo_top'),
}))

const loop = useSequence(async (wait) => {
  step.value = 'side'
  await wait(1700)
  if (!isDark.value) {
    step.value = 'dark'
    await wait(1900)
  }
  step.value = 'mini'
  await wait(2200)
  step.value = 'top'
  await wait(2200)
})

onMounted(loop.start)
</script>

<template>
  <div class="demo">
    <div class="mw" :class="[`is-${step}`, { dark: darkened }]" :data-theme="darkened ? 'dark' : undefined">
      <aside class="mw__side">
        <div class="mw__logo">
          <b /><span />
        </div>
        <i v-for="n in 6" :key="n" class="mw__item" :class="{ 'is-on': n === 1 }"><b /><span /></i>
      </aside>
      <div class="mw__main">
        <div class="mw__head">
          <div class="mw__topnav">
            <span class="is-on" /><span /><span />
          </div>
          <span class="mw__crumb" />
          <em />
          <span class="mw__tool" />
          <span class="mw__tool mw__tool--theme">
            <Icon :icon="darkened || isDark ? 'lucide:moon' : 'lucide:sun'" />
          </span>
          <span class="mw__avatar" />
        </div>
        <div class="mw__tabs">
          <span class="is-on" /><span /><span />
        </div>
        <div class="mw__body">
          <div v-for="n in 3" :key="n" class="mw__card">
            <small /><strong />
          </div>
          <div class="mw__card mw__card--chart">
            <small />
            <svg viewBox="0 0 200 60" preserveAspectRatio="none">
              <path d="M0 46 C20 40 30 20 50 26 S 80 50 100 34 S 140 8 160 18 S 190 30 200 12" />
            </svg>
          </div>
          <div class="mw__card mw__card--list">
            <small /><i /><i /><i />
          </div>
        </div>
      </div>
    </div>
    <p class="demo__caption">
      {{ captions[step] }}
    </p>
  </div>
</template>

<style scoped>
.demo {
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

/* 缩小的窗口：侧栏 + 顶栏 + 标签 + 内容 */
.mw {
  --mw-side: 24%;

  flex: 1;
  display: flex;
  inline-size: 100%;
  min-block-size: 0;
  overflow: hidden;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-canvas);
  box-shadow: var(--xh-shadow-xl);
  transition:
    background-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw.is-mini {
  --mw-side: 10%;
}

.mw.is-top {
  --mw-side: 0%;
}

.mw__side {
  flex: 0 0 var(--mw-side);
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
  min-inline-size: 0;
  overflow: hidden;
  padding: var(--xh-space-3) var(--xh-space-2);
  border-inline-end: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
  transition:
    flex-basis var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    padding var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    background-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw.is-top .mw__side {
  padding-inline: 0;
  border-inline-end-color: transparent;
  opacity: 0;
}

.mw__logo,
.mw__item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  block-size: var(--xh-space-7);
  padding-inline: var(--xh-space-1_5);
  border-radius: var(--xh-shape-control);
}

.mw__logo {
  margin-block-end: var(--xh-space-2);
}

.mw__logo b {
  flex: none;
  inline-size: var(--xh-space-5);
  block-size: var(--xh-space-5);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-brand);
}

.mw__item b {
  flex: none;
  inline-size: var(--xh-space-3);
  block-size: var(--xh-space-3);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-subtle-active);
}

.mw__logo span,
.mw__item span {
  inline-size: 62%;
  block-size: var(--xh-space-1_5);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
  transition:
    inline-size var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw__logo span {
  background-color: var(--xh-fg-default);
  opacity: 0.8;
}

.mw__item:nth-child(odd) span {
  inline-size: 48%;
}

.mw__item.is-on {
  background-color: var(--xh-bg-brand-subtle);
}

.mw__item.is-on b,
.mw__item.is-on span {
  background-color: var(--xh-bg-brand);
}

.mw.is-mini .mw__logo span,
.mw.is-mini .mw__item span {
  inline-size: 0;
  opacity: 0;
}

.mw__main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-inline-size: 0;
}

.mw__head {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  block-size: calc(var(--xh-space-8) + var(--xh-space-2));
  padding-inline: var(--xh-space-3);
  border-block-end: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
  transition:
    background-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw__head em {
  flex: 1;
}

.mw__topnav {
  display: flex;
  gap: var(--xh-space-1_5);
  inline-size: 0;
  overflow: hidden;
  opacity: 0;
  transition:
    inline-size var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw.is-top .mw__topnav {
  inline-size: calc(var(--xh-space-8) * 5.5);
  opacity: 1;
}

.mw__topnav span {
  flex: none;
  inline-size: calc(var(--xh-space-8) * 1.6);
  block-size: var(--xh-space-5);
  border-radius: var(--xh-shape-control);
  background-color: var(--xh-bg-subtle-active);
}

.mw__topnav span.is-on {
  background-color: var(--xh-bg-brand);
}

.mw__crumb {
  inline-size: 22%;
  block-size: var(--xh-space-1_5);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
  transition:
    inline-size var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw.is-top .mw__crumb {
  inline-size: 0;
  opacity: 0;
}

.mw__tool,
.mw__avatar {
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-5);
  block-size: var(--xh-space-5);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-subtle-active);
  font-size: var(--xh-font-size-xs);
}

.mw__tool--theme {
  background-color: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
}

.mw__avatar {
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-bg-brand);
}

.mw__tabs {
  display: flex;
  gap: var(--xh-space-1);
  padding: var(--xh-space-1_5) var(--xh-space-3) 0;
  border-block-end: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
  transition:
    background-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw__tabs span {
  inline-size: calc(var(--xh-space-8) * 2.2);
  block-size: var(--xh-space-5);
  border-start-start-radius: var(--xh-shape-inset);
  border-start-end-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-subtle);
}

.mw__tabs span.is-on {
  background-color: var(--xh-bg-canvas);
  box-shadow: inset 0 2px 0 var(--xh-bg-brand);
}

.mw__body {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  grid-template-rows: auto 1fr;
  gap: var(--xh-space-2_5);
  min-block-size: 0;
  padding: var(--xh-space-3);
}

.mw__card {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-block-size: 0;
  padding: var(--xh-space-2_5) var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-surface);
  transition:
    background-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.mw__card small {
  inline-size: 45%;
  block-size: var(--xh-space-1_5);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
}

.mw__card strong {
  inline-size: 62%;
  block-size: var(--xh-space-3);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-fg-default);
  opacity: 0.85;
}

.mw__card:first-child strong {
  background-color: var(--xh-bg-brand);
  opacity: 1;
}

.mw__card--chart {
  grid-column: span 2;
}

.mw__card--chart svg {
  flex: 1;
  inline-size: 100%;
  min-block-size: 0;
  overflow: visible;
}

.mw__card--chart path {
  fill: none;
  stroke: var(--xh-bg-brand);
  stroke-width: 2.5;
  stroke-linecap: round;
  vector-effect: non-scaling-stroke;
}

.mw__card--list i {
  block-size: var(--xh-space-1_5);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
}

.mw__card--list i:nth-of-type(2) {
  inline-size: 70%;
}

.mw__card--list i:nth-of-type(3) {
  inline-size: 85%;
}
</style>
