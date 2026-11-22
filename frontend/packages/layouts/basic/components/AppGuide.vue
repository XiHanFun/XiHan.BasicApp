<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { TourStep } from '@xihan-ui/headless'
import {
  XhTourArrow,
  XhTourBackdrop,
  XhTourCloseTrigger,
  XhTourContent,
  XhTourDescription,
  XhTourNextTrigger,
  XhTourPositioner,
  XhTourPrevTrigger,
  XhTourProgressText,
  XhTourRoot,
  XhTourSkipTrigger,
  XhTourSpotlight,
  XhTourTitle,
} from '@xihan-ui/vue'
import { computed, onBeforeUnmount, onMounted, ref, shallowRef, watch, watchEffect } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { isLockedState, SESSION_LOCK_CHANGED_EVENT } from '~/composables/session-lock'
import { useGuideStore, useWelcomeStore } from '~/stores'

/**
 * 界面引导的播放器：壳层常驻一份，播 guide store 里排好的那串章节
 * 也管自动播放的时机：欢迎层看完、登录后的公告处理完、页面数据回来并进场之后，没看过的章节才开始
 */
defineOptions({ name: 'AppGuide' })

const { t } = useI18n()
const route = useRoute()
const guide = useGuideStore()
const welcome = useWelcomeStore()

/** 触发条件常连着变：稍等一会儿合成一次，这时进场的过渡与动画也都已经开始 */
const SETTLE_MS = 150
/** 量目标前等进场动画播完的上限：还在淡入的元素是透明的，会被当成看不见而去掉 */
const MOTION_WAIT_MS = 1000

/** 等页面上有尽头的过渡与动画播完（循环的加载动画不等），最多等 MOTION_WAIT_MS */
async function motionSettled(): Promise<void> {
  if (typeof document.getAnimations !== 'function')
    return
  const running = document.getAnimations().filter((animation) => {
    const end = animation.effect?.getComputedTiming().endTime
    return animation.playState === 'running' && typeof end === 'number' && Number.isFinite(end)
  })
  if (running.length === 0)
    return
  await Promise.race([
    Promise.allSettled(running.map(animation => animation.finished)),
    new Promise(resolve => setTimeout(resolve, MOTION_WAIT_MS)),
  ])
}

const steps = computed<TourStep[]>(() => (guide.chapter?.steps ?? []).map(step => ({
  id: step.id,
  target: step.target,
  title: t(step.titleKey),
  description: t(step.descriptionKey),
  placement: step.placement,
})))

// 收起要播完退场动画，这期间气泡还在：步骤、步序与章节名停在收起前那一刻，不跟着清空的 store 跳回第一步
const shownSteps = shallowRef<TourStep[]>([])
const shownValue = ref(0)
const shownTitle = ref('')
const shownFinal = ref(false)

/**
 * 高亮框的圆角：组件库照抄目标的圆角，按钮这类自带圆角的保持原轮廓；
 * 菜单、标签栏、表头这类本身是直角的区域取承载面的圆角，整套引导的高亮框都是圆角
 */
const SURFACE_RADIUS = { '--xh-tour-spotlight-radius': 'var(--xh-shape-surface)' }
const squareTarget = ref(false)

watchEffect(() => {
  if (!guide.active)
    return
  shownSteps.value = steps.value
  shownValue.value = guide.stepIndex
  shownTitle.value = guide.chapter?.title() ?? ''
  shownFinal.value = guide.lastChapter
  const target = guide.chapter?.steps[guide.stepIndex]?.target?.()
  squareTarget.value = !!target && getComputedStyle(target).borderRadius === '0px'
})

const translations = computed(() => ({
  close: t('guide.close'),
  progress: (step: number, count: number) => t('guide.progress', { step, count }),
  next: t('guide.next'),
  finish: t('guide.finish'),
}))

/** 末步再按下一步时组件先发 complete、再发关闭：据此把这次关闭认成「这一章看完」而不是放弃 */
let completing = false

function onValue(value: number): void {
  guide.stepIndex = value
}

function onComplete(): void {
  completing = true
}

function onOpenChange(details: { open: boolean }): void {
  if (details.open)
    return
  if (completing) {
    completing = false
    guide.advance()
    return
  }
  guide.dismiss()
}

let timer: ReturnType<typeof setTimeout> | undefined
/** 每次排期的序号：等动画的那段时间里又排了一次，旧的这次作废 */
let scheduled = 0

/** 自动播放：锁屏、欢迎层、还没判完要不要出欢迎层时都等着 */
function schedule(): void {
  clearTimeout(timer)
  const current = ++scheduled
  timer = setTimeout(async () => {
    await motionSettled()
    if (current !== scheduled || !welcome.checked || welcome.open || isLockedState())
      return
    guide.autoplay()
  }, SETTLE_MS)
}

// 换了页面：播到一半的收起（目标已不在），跳过的效力也到此为止
watch(() => route.path, () => {
  guide.stop()
  guide.resetQuiet()
})

// 时机变了就再看一眼：一串播完时当前页的章节刚就绪，也在这里接着播
watch(
  () => [
    route.fullPath,
    welcome.checked,
    welcome.open,
    guide.loaded,
    guide.held,
    guide.active,
    guide.quiet,
    guide.page,
    guide.page?.ready() ?? false,
  ],
  schedule,
  { immediate: true },
)

onMounted(() => {
  void guide.load()
  window.addEventListener(SESSION_LOCK_CHANGED_EVENT, schedule)
})

onBeforeUnmount(() => {
  clearTimeout(timer)
  scheduled += 1
  window.removeEventListener(SESSION_LOCK_CHANGED_EVENT, schedule)
  guide.stop()
})
</script>

<template>
  <XhTourRoot
    v-slot="{ lastStep }"
    :open="guide.active"
    :steps="shownSteps"
    :value="shownValue"
    :translations="translations"
    :spotlight-padding="4"
    @update:value="onValue"
    @complete="onComplete"
    @open-change="onOpenChange"
  >
    <XhTourBackdrop />
    <XhTourSpotlight :style="squareTarget ? SURFACE_RADIUS : undefined" />
    <XhTourPositioner>
      <XhTourContent class="app-guide">
        <div class="app-guide__meta">
          <span class="app-guide__chapter">{{ shownTitle }}</span>
          <XhTourProgressText />
        </div>
        <XhTourTitle />
        <XhTourDescription />
        <div class="app-guide__actions">
          <XhTourSkipTrigger>{{ t('guide.skip') }}</XhTourSkipTrigger>
          <span class="app-guide__spacer" />
          <XhTourPrevTrigger>{{ t('guide.prev') }}</XhTourPrevTrigger>
          <XhTourNextTrigger>{{ lastStep && shownFinal ? t('guide.finish') : t('guide.next') }}</XhTourNextTrigger>
        </div>
        <XhTourCloseTrigger />
        <XhTourArrow />
      </XhTourContent>
    </XhTourPositioner>
  </XhTourRoot>
</template>

<style scoped>
/* 章节名与进度同一行：章节名用品牌色小字，告诉人这是哪一部分的介绍 */
.app-guide__meta {
  display: flex;
  gap: var(--xh-space-2);
  align-items: baseline;
  padding-inline-end: calc(var(--xh-control-h-sm) + var(--xh-surface-action-inset));
  font-size: var(--xh-text-caption-size);
}

.app-guide__chapter {
  font-weight: 600;
  color: var(--xh-fg-brand);
}

.app-guide__actions {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  margin-block-start: var(--xh-space-1);
}

.app-guide__spacer {
  flex: 1;
}
</style>
