<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { Component } from 'vue'
import type { WelcomeHighlight, WelcomeTone } from '~/stores'
import { resolveMotionPreference } from '@xihan-ui/motion'
import { XhButton, XhKbd } from '@xihan-ui/vue'
import { computed, defineAsyncComponent, markRaw, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XArtboard } from '~/components'
import { useSequence } from '~/composables'
import { welcomeGreetings } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useSiteStore, useWelcomeStore } from '~/stores'
import { SAND_DISPERSE_SECONDS, useWelcomeSand } from './welcome/use-welcome-sand'

/**
 * 新版欢迎页：登录后首次进入一条新的版本线时出现一次的全屏欢迎层
 * 三幕：各国问候由沙粒依次聚散 → 欢迎主画面（标志与版本号由沙粒循环聚散）→ 亮点导览 → 收尾
 * 开合与「看过没有」在 useWelcomeStore；这里只管呈现与编排，由基础布局在第一次要出现时按需加载
 */
defineOptions({ name: 'AppWelcome' })

type Scene = 'final' | 'intro' | 'tour'

/** 一句问候从开始聚拢到开始吹散、最后一句多停一会儿、两句之间的间隔（毫秒） */
const GREETING_HOLD = 2150
const GREETING_LAST_HOLD = 2500
const GREETING_GAP = 150
/** 主画面里一个形状从开始聚拢到开始吹散、两个形状之间的间隔（毫秒） */
const HERO_HOLD = 4400
const HERO_GAP = 350
/** 收尾画面等转场落定再聚出标志（毫秒） */
const FINAL_DELAY = 1000
/** 退场的圆洞迟迟没有开完时的兜底（毫秒）：过渡被打断也要把层摘掉 */
const EXIT_FALLBACK = 2400

/** 各语气取哪个语义令牌的颜色 */
const TONE_COLORS: Record<WelcomeTone, string> = {
  brand: 'var(--xh-bg-brand)',
  info: 'var(--xh-fg-info)',
  success: 'var(--xh-fg-success)',
  warning: 'var(--xh-fg-warning)',
  danger: 'var(--xh-fg-danger)',
}

const welcome = useWelcomeStore()
const router = useRouter()
const { t, locale } = useI18n()
const { isDark } = useTheme()
const sand = useWelcomeSand(() => isDark.value)

/** 层在不在文档里（退场动画期间还在） */
const mounted = ref(false)
const scene = ref<Scene>('intro')
/** 开场的两段：沙粒问候、欢迎主画面 */
const phase = ref<'hello' | 'hero'>('hello')
const index = ref(0)
const direction = ref<'back' | 'forward'>('forward')
const exiting = ref(false)
const reduced = ref(false)
const exitOrigin = ref({ x: '50%', y: '50%' })
const root = ref<HTMLElement | null>(null)
const heroArt = ref<HTMLElement | null>(null)
const finalArt = ref<HTMLElement | null>(null)
const stage = ref<HTMLElement | null>(null)
/** 层摘掉之后要做的事（「去看看」的跳转） */
let afterClose: (() => void) | null = null
let exitTimer: null | ReturnType<typeof setTimeout> = null
/** 问候落在哪张画面上收的尾：主画面的第一个形状用另一张，两者才能交叠 */
let lastGreetingLayer: 0 | 1 = 0

const site = useSiteStore()
const appTitle = computed(() => site.name)
const appLogo = computed(() => site.logo)
/** 版本线的主版本号：主画面聚成的那个数字 */
const major = computed(() => welcome.release?.split('.')[0] ?? '')
const slides = computed(() => welcome.slides)
const slide = computed<undefined | WelcomeHighlight>(() => slides.value[index.value])
const demos = computed<Component[]>(() => slides.value.map(item => markRaw(defineAsyncComponent(item.demo))))
/** 沙画不出来或标志取不成沙时，主画面与收尾改放一张静止的标志 */
const staticLogo = computed(() => !sand.supported.value || !sand.hasLogo.value)

/** 背景光斑的三种颜色：缺省取自标志；导览里第一种换成这一屏的语气色，后两种在标志配色里轮转 */
const rootStyle = computed(() => {
  const colors = sand.palette.value.map(([r, g, b]) => `rgb(${Math.round(r)} ${Math.round(g)} ${Math.round(b)})`)
  const style: Record<string, string> = {
    '--app-welcome-x': exitOrigin.value.x,
    '--app-welcome-y': exitOrigin.value.y,
  }
  if (colors.length >= 3) {
    const turn = scene.value === 'tour'
      ? index.value
      : 0
    style['--app-welcome-a1'] = colors[turn % colors.length]!
    style['--app-welcome-a2'] = colors[(turn + Math.floor(colors.length / 2)) % colors.length]!
    style['--app-welcome-a3'] = colors[(turn + colors.length - 1) % colors.length]!
  }
  if (scene.value === 'tour' && slide.value)
    style['--app-welcome-a1'] = TONE_COLORS[slide.value.tone ?? 'brand']
  return style
})

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

/** 问候摆在画面中央偏上，宽高都有上限 */
function greetingBox() {
  const width = root.value?.clientWidth ?? window.innerWidth
  const height = root.value?.clientHeight ?? window.innerHeight
  return {
    centerX: width / 2,
    centerY: height * 0.45,
    maxWidth: Math.min(width * 0.8, 980),
    maxHeight: Math.min(height * 0.26, 210),
  }
}

/**
 * 形状摆进一块占位元素里；shrink 让它比占位小一圈
 * 位置按排版量（offset*），不按画出来的样子量：换幕时这一幕还缩着、正在放大，量画出来的会偏小
 */
function artBox(element: HTMLElement | null, shrink = 1) {
  return () => {
    const base = root.value
    if (!element || !base)
      return greetingBox()
    let left = 0
    let top = 0
    for (let node: HTMLElement | null = element; node && node !== base; node = node.offsetParent as HTMLElement | null) {
      left += node.offsetLeft
      top += node.offsetTop
    }
    return {
      centerX: left + element.offsetWidth / 2,
      centerY: top + element.offsetHeight / 2,
      maxWidth: element.offsetWidth * shrink,
      maxHeight: element.offsetHeight * shrink,
    }
  }
}

/** 各国问候依次聚散，落到当前界面语言那一句之后进欢迎主画面 */
const intro = useSequence(async (wait) => {
  await wait(500)
  const greetings = welcomeGreetings(locale.value)
  for (let n = 0; n < greetings.length; n++) {
    const layer = (n % 2) as 0 | 1
    lastGreetingLayer = layer
    sand.gather(layer, sand.word(greetings[n]!), greetingBox)
    await wait(n === greetings.length - 1
      ? GREETING_LAST_HOLD
      : GREETING_HOLD)
    sand.disperse(layer)
    await wait(GREETING_GAP)
  }
  showHero()
  await wait(Number.POSITIVE_INFINITY)
})

/** 主画面中央：沙粒聚成标志，吹散，再聚成版本号，如此往复；减弱动效下聚成即止 */
const heroLoop = useSequence(async (wait) => {
  const logoLayer = lastGreetingLayer === 0
    ? 1
    : 0
  const numeralLayer = lastGreetingLayer
  if (sand.hasLogo.value) {
    sand.gather(logoLayer, sand.logo(), artBox(heroArt.value))
    if (reduced.value)
      return wait(Number.POSITIVE_INFINITY)
    await wait(HERO_HOLD)
    sand.disperse(logoLayer)
    await wait(HERO_GAP)
  }
  sand.gather(numeralLayer, sand.word(major.value, 800), artBox(heroArt.value, 0.8))
  if (reduced.value)
    return wait(Number.POSITIVE_INFINITY)
  await wait(HERO_HOLD)
  sand.disperse(numeralLayer)
  // 没有标志可换时，等这一遍吹散得差不多再聚回来
  await wait(sand.hasLogo.value
    ? HERO_GAP
    : SAND_DISPERSE_SECONDS * 1000 * 0.6)
})

/** 收尾画面：等画面落定再聚出标志（没有标志就聚版本号），之后一直留着 */
const finalShow = useSequence(async (wait) => {
  await wait(reduced.value ? 0 : FINAL_DELAY)
  if (sand.hasLogo.value)
    sand.gather(0, sand.logo(), artBox(finalArt.value))
  else sand.gather(0, sand.word(major.value, 800), artBox(finalArt.value, 0.8))
  await wait(Number.POSITIVE_INFINITY)
})

function stopSequences(): void {
  intro.stop()
  heroLoop.stop()
  finalShow.stop()
}

/** 这一幕的主操作；底部那组操作还在交替、新的一组没进来时取不到 */
function primaryAction(): HTMLElement | null {
  return root.value?.querySelector<HTMLElement>(`[data-welcome-foot="${scene.value}"] [data-welcome-primary]`) ?? null
}

/** 换幕之后把焦点交给这一幕的主操作；新的一组操作还没进来时先由整层接着，免得焦点随旧按钮掉到页面上 */
function focusPrimary(): void {
  void nextTick(() => {
    (primaryAction() ?? root.value)?.focus({ preventScroll: true })
  })
}

/** 底部新的一组操作进来：焦点还停在整层上（没有被用户挪到别的控件）就交给主操作 */
function onFootEnter(): void {
  const active = document.activeElement
  if (active === root.value || active === document.body || !root.value?.contains(active))
    primaryAction()?.focus({ preventScroll: true })
}

function showHero(): void {
  intro.stop()
  if (phase.value === 'hero')
    return
  sand.scatter()
  phase.value = 'hero'
  if (sand.supported.value)
    heroLoop.start()
  focusPrimary()
}

/** 翻页时整块舞台摆一下 */
function swing(): void {
  const element = stage.value
  if (!element || reduced.value)
    return
  element.classList.remove('is-swing')
  void element.offsetWidth
  element.classList.add('is-swing')
}

function go(next: number): void {
  if (next === index.value || next < 0 || next >= slides.value.length)
    return
  direction.value = next > index.value
    ? 'forward'
    : 'back'
  index.value = next
  swing()
}

function enterFinal(): void {
  stopSequences()
  sand.scatter()
  scene.value = 'final'
  if (sand.supported.value)
    finalShow.start()
  focusPrimary()
}

function enterTour(at = 0, toward: 'back' | 'forward' = 'forward'): void {
  stopSequences()
  sand.scatter()
  phase.value = 'hero'
  // 这一版对当前用户一屏亮点都没有：欢迎完直接收尾
  if (slides.value.length === 0) {
    enterFinal()
    return
  }
  direction.value = toward
  index.value = at
  scene.value = 'tour'
  focusPrimary()
}

function next(): void {
  if (index.value < slides.value.length - 1)
    go(index.value + 1)
  else enterFinal()
}

function unmount(): void {
  if (exitTimer !== null) {
    clearTimeout(exitTimer)
    exitTimer = null
  }
  if (!mounted.value)
    return
  mounted.value = false
  exiting.value = false
  window.removeEventListener('keydown', onKeydown)
  welcome.finish()
  const action = afterClose
  afterClose = null
  action?.()
}

/**
 * 退场：沙被吹散，欢迎层从触发的那颗按钮处开出一个圆洞，洞越开越大，露出背后的系统
 * @param origin 触发退场的元素；不给即从画面正中开
 */
function close(origin?: EventTarget | null): void {
  if (exiting.value || !mounted.value)
    return
  stopSequences()
  sand.scatter()
  const rect = origin instanceof Element
    ? origin.getBoundingClientRect()
    : null
  const base = root.value?.getBoundingClientRect()
  exitOrigin.value = rect && base
    ? { x: `${rect.left - base.left + rect.width / 2}px`, y: `${rect.top - base.top + rect.height / 2}px` }
    : { x: '50%', y: '50%' }
  if (reduced.value) {
    unmount()
    return
  }
  exiting.value = true
  exitTimer = setTimeout(unmount, EXIT_FALLBACK)
}

function onExitEnd(event: TransitionEvent): void {
  if (exiting.value && event.target === root.value && event.propertyName === '--app-welcome-hole')
    unmount()
}

/** 「去看看」：先退场，层摘掉之后再去 */
function visit(item: WelcomeHighlight, event: Event): void {
  const action = item.action
  if (!action)
    return
  afterClose = () => {
    if (action.to)
      void router.push(action.to)
    else action.run?.()
  }
  close(event.currentTarget)
}

function onKeydown(event: KeyboardEvent): void {
  if (!mounted.value || exiting.value)
    return
  if (event.key === 'Escape') {
    event.preventDefault()
    close()
    return
  }
  const forward = event.key === 'Enter' || event.key === 'ArrowRight'
  const back = event.key === 'ArrowLeft'
  // 焦点在欢迎层自己的按钮上时回车交给按钮；焦点还留在背后壳层的按钮上（已经不接操作）时照常由这里处理
  if (event.key === 'Enter' && event.target instanceof Element && root.value?.contains(event.target) && event.target.closest('button'))
    return
  if (!forward && !back)
    return
  event.preventDefault()
  if (scene.value === 'intro') {
    if (!forward)
      return
    if (phase.value === 'hello')
      showHero()
    else enterTour()
  }
  else if (scene.value === 'tour') {
    if (forward)
      next()
    else go(index.value - 1)
  }
  else if (forward) {
    close(root.value?.querySelector('[data-welcome-foot="final"] [data-welcome-primary]'))
  }
  else {
    enterTour(slides.value.length - 1, 'back')
  }
}

/** 问候还在聚散时，点任意处直接到欢迎主画面 */
function onLayerClick(): void {
  if (scene.value === 'intro' && phase.value === 'hello' && !exiting.value)
    showHero()
}

function skippedHello(): boolean {
  return phase.value === 'hero'
}

async function start(): Promise<void> {
  reduced.value = resolveMotionPreference(document.documentElement) === 'reduce'
  scene.value = 'intro'
  phase.value = 'hello'
  index.value = 0
  direction.value = 'forward'
  exiting.value = false
  mounted.value = true
  // 键盘在这里接上、层摘掉时卸下：组件按需加载，创建时层往往已经要出现，不能等「挂上没有」变化了才接
  window.addEventListener('keydown', onKeydown)
  await nextTick()
  // 焦点从背后的壳层收进欢迎层：之后的按键都落在这一层上
  root.value?.focus({ preventScroll: true })
  await sand.load(appLogo.value)
  // 读标志的这一会儿里用户可能已经关掉了
  if (!mounted.value)
    return
  if (skippedHello()) {
    // 标志读进来之前就跳到了主画面：那一遍只聚得出版本号，有了标志从头再来
    if (scene.value === 'intro' && sand.supported.value && sand.hasLogo.value) {
      sand.scatter()
      heroLoop.start()
    }
    return
  }
  // 减弱动效、或沙画不出来：不演问候，直接到欢迎主画面
  if (reduced.value || !sand.supported.value)
    showHero()
  else intro.start()
}

watch(() => welcome.open, (open) => {
  if (open && !mounted.value)
    void start()
}, { immediate: true })

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeydown)
  if (exitTimer !== null)
    clearTimeout(exitTimer)
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="mounted"
      ref="root"
      class="app-welcome"
      :class="{ 'is-exit': exiting }"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
      :aria-label="t('welcome.aria_label', { name: appTitle })"
      :data-scene="scene"
      :data-phase="phase"
      :data-direction="direction"
      :data-dark="isDark"
      :data-reduced="reduced"
      :style="rootStyle"
      @click="onLayerClick"
      @transitionend="onExitEnd"
    >
      <div class="app-welcome__aurora" aria-hidden="true">
        <i /><i /><i /><i />
      </div>
      <div :ref="sand.attach[0]" class="app-welcome__sand" aria-hidden="true" />
      <div :ref="sand.attach[1]" class="app-welcome__sand" aria-hidden="true" />

      <header class="app-welcome__top" :inert="phase === 'hello'">
        <div class="app-welcome__brand">
          <img :src="appLogo" alt="" class="app-welcome__brand-logo">
          <span class="app-welcome__brand-name">{{ appTitle }}</span>
          <span class="app-welcome__brand-version">{{ welcome.release }}</span>
        </div>
        <XhButton v-if="scene !== 'final'" variant="ghost" @click.stop="close()">
          {{ t('welcome.skip') }}
        </XhButton>
      </header>

      <main class="app-welcome__scenes">
        <!-- 第一幕：问候由沙粒在整层上聚散，主画面的占位留给沙粒聚成标志与版本号 -->
        <section class="app-welcome__scene" data-of="intro" :inert="scene !== 'intro' || phase === 'hello'">
          <div class="app-welcome__hero">
            <div ref="heroArt" class="app-welcome__art app-welcome__art--hero">
              <img v-if="staticLogo" :src="appLogo" alt="" class="app-welcome__art-logo">
            </div>
            <h1 class="app-welcome__title">
              {{ t('welcome.intro.title', { name: appTitle }) }}
              <b>{{ major }}</b>
            </h1>
            <p class="app-welcome__lead">
              {{ t('welcome.intro.lead') }}
            </p>
          </div>
        </section>

        <!-- 第二幕：亮点导览，左边说明、右边演示 -->
        <section class="app-welcome__scene" data-of="tour" :inert="scene !== 'tour'">
          <div v-if="slide" class="app-welcome__tour">
            <div class="app-welcome__slides" aria-live="polite">
              <Transition name="app-welcome-slide">
                <article :key="slide.key" class="app-welcome__slide">
                  <div class="app-welcome__kicker">
                    <span>{{ pad(index + 1) }} / {{ pad(slides.length) }}</span>
                    <span class="app-welcome__category">
                      <Icon :icon="slide.icon" />
                      {{ t(slide.categoryKey) }}
                    </span>
                  </div>
                  <h2 class="app-welcome__heading">
                    {{ t(slide.titleKey) }}
                  </h2>
                  <p class="app-welcome__text">
                    {{ t(slide.descriptionKey) }}
                  </p>
                  <ul class="app-welcome__points">
                    <li v-for="point in slide.pointKeys" :key="point">
                      {{ t(point) }}
                    </li>
                  </ul>
                  <!-- 这一屏没有「去看看」时也留着这一行：各屏的说明才落在同一个高度上 -->
                  <div class="app-welcome__visit">
                    <XhButton v-if="slide.action" variant="outline" size="lg" class="app-welcome__plain" @click.stop="visit(slide, $event)">
                      {{ t(slide.action.labelKey) }}
                    </XhButton>
                  </div>
                </article>
              </Transition>
            </div>
            <div class="app-welcome__stage-wrap">
              <div ref="stage" class="app-welcome__stage-tilt">
                <div class="app-welcome__stage-glow" />
                <div class="app-welcome__stage">
                  <XArtboard>
                    <Transition name="app-welcome-demo">
                      <component :is="demos[index]" :key="slide.key" />
                    </Transition>
                  </XArtboard>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- 第三幕：收尾 -->
        <section class="app-welcome__scene" data-of="final" :inert="scene !== 'final'">
          <div class="app-welcome__hero">
            <div ref="finalArt" class="app-welcome__art app-welcome__art--final">
              <img v-if="staticLogo" :src="appLogo" alt="" class="app-welcome__art-logo">
            </div>
            <h2 class="app-welcome__title">
              {{ t('welcome.final.title') }}
            </h2>
            <p class="app-welcome__lead">
              {{ t('welcome.final.lead') }}
            </p>
          </div>
        </section>
      </main>

      <!-- 操作一律落在页面底部：三幕各一组，换幕时交替 -->
      <footer class="app-welcome__foot" :inert="phase === 'hello'">
        <Transition name="app-welcome-foot" mode="out-in" @enter="onFootEnter">
          <div v-if="scene === 'intro'" key="intro" class="app-welcome__actions" data-welcome-foot="intro">
            <div class="app-welcome__buttons">
              <XhButton variant="solid" size="lg" full-width data-welcome-primary @click.stop="enterTour()">
                {{ t('welcome.intro.start') }}
              </XhButton>
              <XhButton variant="outline" size="lg" full-width class="app-welcome__plain" @click.stop="close($event.currentTarget)">
                {{ t('welcome.intro.enter') }}
              </XhButton>
            </div>
            <p class="app-welcome__hints">
              <XhKbd :keys="['Enter']" />
              <span>{{ t('welcome.hint_continue') }}</span>
              <XhKbd :keys="['Escape']" />
              <span>{{ t('welcome.hint_skip') }}</span>
            </p>
          </div>
          <div v-else-if="scene === 'tour'" key="tour" class="app-welcome__nav" data-welcome-foot="tour">
            <XhButton variant="outline" class="app-welcome__plain" :class="{ 'is-hidden': index === 0 }" :disabled="index === 0" @click.stop="go(index - 1)">
              <Icon icon="lucide:arrow-left" />
              {{ t('welcome.tour.previous') }}
            </XhButton>
            <div class="app-welcome__dots">
              <button
                v-for="(item, n) in slides"
                :key="item.key"
                type="button"
                class="app-welcome__dot"
                :class="{ 'is-active': n === index, 'is-done': n < index }"
                :aria-label="t('welcome.tour.goto', { index: n + 1, title: t(item.categoryKey) })"
                :aria-current="n === index ? 'step' : undefined"
                @click.stop="go(n)"
              />
            </div>
            <XhButton variant="solid" data-welcome-primary @click.stop="next()">
              {{ index === slides.length - 1 ? t('welcome.tour.done') : t('welcome.tour.next') }}
              <!-- 箭头只给翻页的「上一步」「下一步」：最后一屏是「完成」，不带 -->
              <Icon v-if="index < slides.length - 1" icon="lucide:arrow-right" />
            </XhButton>
          </div>
          <div v-else key="final" class="app-welcome__actions" data-welcome-foot="final">
            <div class="app-welcome__buttons">
              <XhButton variant="solid" size="lg" full-width data-welcome-primary @click.stop="close($event.currentTarget)">
                {{ t('welcome.final.start') }}
              </XhButton>
              <XhButton v-if="slides.length > 0" variant="outline" size="lg" full-width class="app-welcome__plain" @click.stop="enterTour()">
                {{ t('welcome.final.again') }}
              </XhButton>
            </div>
          </div>
        </Transition>
      </footer>
    </div>
  </Teleport>
</template>

<style scoped>
/* 光斑的三种颜色登记成颜色类型：换幕、翻页时能平滑过渡 */
@property --app-welcome-a1 {
  syntax: '<color>';
  inherits: true;
  initial-value: transparent;
}

@property --app-welcome-a2 {
  syntax: '<color>';
  inherits: true;
  initial-value: transparent;
}

@property --app-welcome-a3 {
  syntax: '<color>';
  inherits: true;
  initial-value: transparent;
}

/* 退场圆洞的半径登记成长度类型才过渡得起来 */
@property --app-welcome-hole {
  syntax: '<length>';
  inherits: true;
  initial-value: 0px;
}

.app-welcome {
  /* 取不到标志配色时的三种颜色 */
  --app-welcome-a1: var(--xh-bg-brand);
  --app-welcome-a2: var(--xh-fg-danger);
  --app-welcome-a3: var(--xh-fg-warning);
  --app-welcome-glow: 0.42;

  position: fixed;
  inset: 0;
  z-index: var(--xh-z-modal);
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
  overflow: clip;
  background: radial-gradient(120% 90% at 50% -10%, var(--xh-bg-surface), var(--xh-bg-page) 62%);
  color: var(--xh-fg-default);
  /* 整层只是为了接住焦点才可聚焦，不画聚焦环 */
  outline: none;
  animation: app-welcome-in var(--xh-motion-duration-slide) var(--xh-motion-ease-enter) backwards;
  transition:
    --app-welcome-a1 var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter),
    --app-welcome-a2 var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter),
    --app-welcome-a3 var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter);
}

.app-welcome[data-dark='true'] {
  --app-welcome-glow: 0.6;
}

@keyframes app-welcome-in {
  from {
    opacity: 0;
  }
}

/* 退场：从触发的那颗按钮处开出一个圆洞，洞越开越大，露出背后的系统 */
.app-welcome.is-exit {
  --app-welcome-hole: 150vmax;

  pointer-events: none;
  mask-image: radial-gradient(
    circle at var(--app-welcome-x) var(--app-welcome-y),
    transparent var(--app-welcome-hole),
    var(--xh-fg-default) calc(var(--app-welcome-hole) + 1px)
  );
  transition: --app-welcome-hole calc(var(--xh-motion-duration-reveal) * 1.3) var(--xh-motion-ease-sweep);
}

/* 洞沿的一圈光 */
.app-welcome::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 3;
  opacity: 0;
  pointer-events: none;
  background: radial-gradient(
    circle at var(--app-welcome-x) var(--app-welcome-y),
    transparent calc(var(--app-welcome-hole) - 1px),
    var(--xh-bg-surface) var(--app-welcome-hole),
    color-mix(in oklab, var(--xh-bg-brand) 85%, transparent) calc(var(--app-welcome-hole) + var(--xh-space-2)),
    transparent calc(var(--app-welcome-hole) + var(--xh-space-8) * 4)
  );
}

.app-welcome.is-exit::before {
  opacity: 1;
}

/* 背景光斑：几团慢慢游走的色块，颜色取自标志 */
.app-welcome__aurora {
  position: absolute;
  inset: 0;
  overflow: clip;
  opacity: var(--app-welcome-glow);
  pointer-events: none;
}

.app-welcome__aurora i {
  position: absolute;
  inline-size: 72vmax;
  block-size: 72vmax;
  border-radius: var(--xh-shape-circle);
  will-change: transform;
}

.app-welcome__aurora i:nth-child(1) {
  inset-block-start: -34vmax;
  inset-inline-start: -24vmax;
  background: radial-gradient(closest-side, var(--app-welcome-a1), transparent);
  animation: app-welcome-drift-1 24s ease-in-out infinite;
}

.app-welcome__aurora i:nth-child(2) {
  inset-block-start: -22vmax;
  inset-inline-end: -28vmax;
  background: radial-gradient(closest-side, var(--app-welcome-a2), transparent);
  animation: app-welcome-drift-2 28s ease-in-out infinite;
}

.app-welcome__aurora i:nth-child(3) {
  inset-block-end: -46vmax;
  inset-inline-start: 10vmax;
  background: radial-gradient(closest-side, var(--app-welcome-a3), transparent);
  animation: app-welcome-drift-3 26s ease-in-out infinite;
}

.app-welcome__aurora i:nth-child(4) {
  inset-block-end: -44vmax;
  inset-inline-end: 2vmax;
  inline-size: 56vmax;
  block-size: 56vmax;
  opacity: 0.75;
  background: radial-gradient(closest-side, var(--app-welcome-a1), transparent);
  animation: app-welcome-drift-2 34s ease-in-out infinite reverse;
}

.app-welcome[data-reduced='true'] .app-welcome__aurora i {
  animation: none;
  will-change: auto;
}

@keyframes app-welcome-drift-1 {
  33% {
    transform: translate(18%, 12%) scale(1.12);
  }

  66% {
    transform: translate(6%, 26%) scale(0.92);
  }
}

@keyframes app-welcome-drift-2 {
  33% {
    transform: translate(-16%, 14%) scale(0.9);
  }

  66% {
    transform: translate(-26%, 30%) scale(1.1);
  }
}

@keyframes app-welcome-drift-3 {
  33% {
    transform: translate(16%, -20%) scale(1.08);
  }

  66% {
    transform: translate(30%, -8%) scale(0.94);
  }
}

/* 沙的两张画面铺满整层，只是画面，不接指针 */
.app-welcome__sand {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.app-welcome__top,
.app-welcome__scenes,
.app-welcome__foot {
  position: relative;
  z-index: 2;
}

.app-welcome__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  block-size: calc(var(--xh-space-8) * 2);
  padding-inline: var(--xh-space-6) var(--xh-space-5);
  transition: opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.app-welcome[data-phase='hello'] .app-welcome__top,
.app-welcome[data-phase='hello'] .app-welcome__foot {
  opacity: 0;
}

.app-welcome__brand {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
}

.app-welcome__brand-logo {
  inline-size: var(--xh-space-7);
  block-size: var(--xh-space-7);
  object-fit: contain;
}

.app-welcome__brand-version {
  padding: 0 var(--xh-space-2);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
  font-variant-numeric: tabular-nums;
}

/* 三幕叠在同一块区域：走过的一幕冲过镜头，还没到的一幕等在远处 */
.app-welcome__scenes {
  min-block-size: 0;
}

.app-welcome__scene {
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
  padding: var(--xh-space-2) var(--xh-space-7);
  opacity: 0;
  visibility: hidden;
  transform: scale(0.7);
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    visibility 0s var(--xh-motion-duration-slide);
}

.app-welcome[data-scene='tour'] .app-welcome__scene[data-of='intro'],
.app-welcome[data-scene='final'] .app-welcome__scene[data-of='intro'],
.app-welcome[data-scene='final'] .app-welcome__scene[data-of='tour'] {
  transform: scale(2.8);
}

.app-welcome[data-scene='intro'][data-phase='hero'] .app-welcome__scene[data-of='intro'],
.app-welcome[data-scene='tour'] .app-welcome__scene[data-of='tour'],
.app-welcome[data-scene='final'] .app-welcome__scene[data-of='final'] {
  opacity: 1;
  visibility: visible;
  transform: none;
  transition-delay: var(--xh-motion-duration-enter), var(--xh-motion-duration-enter), 0s;
}

.app-welcome__hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  max-inline-size: calc(var(--xh-space-8) * 24);
  text-align: center;
}

/* 沙粒聚成标志与版本号的占位 */
.app-welcome__art {
  display: grid;
  place-items: center;
  aspect-ratio: 1;
}

.app-welcome__art--hero {
  inline-size: min(34vh, 54vw, calc(var(--xh-space-8) * 9.5));
  margin-block-end: var(--xh-space-4);
}

.app-welcome__art--final {
  inline-size: min(24vh, 46vw, calc(var(--xh-space-8) * 6.25));
  margin-block-end: var(--xh-space-3);
}

.app-welcome__art-logo {
  inline-size: 72%;
  block-size: 72%;
  object-fit: contain;
}

.app-welcome__title {
  margin: 0;
  font-size: clamp(var(--xh-font-size-2xl), 3.5vw, calc(var(--xh-font-size-5xl) * 1.15));
  font-weight: var(--xh-font-weight-bold);
  line-height: var(--xh-leading-tight);
  text-wrap: balance;
}

.app-welcome__title b {
  background: linear-gradient(100deg, var(--app-welcome-a1), var(--app-welcome-a2) 55%, var(--app-welcome-a3));
  background-clip: text;
  color: transparent;
}

.app-welcome__lead {
  max-inline-size: 30em;
  margin: var(--xh-space-3) 0 0;
  color: var(--xh-fg-muted);
  font-size: clamp(var(--xh-font-size-md), 1.5vw, var(--xh-font-size-lg));
  line-height: var(--xh-leading-relaxed);
  /* 文案里的换行照着断：收尾那句「之后想再看一遍…」另起一行 */
  white-space: pre-line;
  text-wrap: balance;
}

/*
 * 亮点导览：整块随视口放大，宽占视口的四分之三上下，说明五份、演示七份
 * 演示舞台按 4:3 出高，所以宽还要受可用高度限制：视口高减去顶栏、底栏与留白，再折回整块的宽
 * 说明文字的字号跟着整块走，各级按 em 取
 */
.app-welcome__tour {
  display: grid;
  grid-template-columns: minmax(0, 5fr) minmax(0, 7fr);
  align-items: center;
  justify-content: center;
  gap: calc(var(--xh-space-8) * 2.5);
  inline-size: min(100%, 74vw, calc((100vh - var(--xh-space-8) * 9.5) * 2.25));
  font-size: clamp(var(--xh-font-size-md), 1.05vw, calc(var(--xh-font-size-md) * 1.5));
}

/* 说明栏与演示舞台等高：说明在「去看看」上方的空间里居中，「去看看」落在左下角、与舞台的下沿对齐 */
.app-welcome__slides {
  display: grid;
  align-self: stretch;
}

.app-welcome__slide {
  display: flex;
  flex-direction: column;
  grid-area: 1 / 1;
}

.app-welcome__slide > * {
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong);
  transition-delay: calc(var(--xh-motion-duration-micro) * var(--app-welcome-order, 0) * 0.5);
}

.app-welcome__slide > :nth-child(2) {
  --app-welcome-order: 1;
}

.app-welcome__slide > :nth-child(3) {
  --app-welcome-order: 2;
}

.app-welcome__slide > :nth-child(4) {
  --app-welcome-order: 3;
}

.app-welcome__slide > :nth-child(5) {
  --app-welcome-order: 4;
}

/* 往后翻：新的一屏从右边进、旧的一屏往左边出；往回翻相反 */
.app-welcome-slide-enter-from > *,
.app-welcome-slide-leave-to > * {
  opacity: 0;
}

.app-welcome-slide-enter-from > * {
  transform: translateX(var(--xh-space-8));
}

.app-welcome-slide-leave-to > * {
  transform: translateX(calc(var(--xh-space-8) * -1));
}

.app-welcome[data-direction='back'] .app-welcome-slide-enter-from > * {
  transform: translateX(calc(var(--xh-space-8) * -1));
}

.app-welcome[data-direction='back'] .app-welcome-slide-leave-to > * {
  transform: translateX(var(--xh-space-8));
}

.app-welcome-slide-leave-active > * {
  transition-delay: 0s;
}

.app-welcome__kicker {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  margin-block-start: auto;
  color: var(--xh-fg-subtle);
  font-size: 0.9em;
  font-variant-numeric: tabular-nums;
}

.app-welcome__category {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  padding: var(--xh-space-0_5) var(--xh-space-3) var(--xh-space-0_5) var(--xh-space-2);
  border-radius: var(--xh-shape-pill);
  background-color: color-mix(in oklab, var(--app-welcome-a1) 18%, transparent);
  color: color-mix(in oklab, var(--app-welcome-a1) 62%, var(--xh-fg-default));
  font-weight: var(--xh-font-weight-semibold);
}

.app-welcome__heading {
  margin: var(--xh-space-5) 0 0;
  font-size: 2.9em;
  font-weight: var(--xh-font-weight-bold);
  line-height: var(--xh-leading-tight);
  text-wrap: balance;
}

.app-welcome__text {
  margin: var(--xh-space-4) 0 0;
  color: var(--xh-fg-muted);
  font-size: 1.25em;
  line-height: var(--xh-leading-relaxed);
  text-wrap: pretty;
}

.app-welcome__points {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  margin: var(--xh-space-6) 0 auto;
  padding: 0;
  list-style: none;
}

.app-welcome__points li {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  padding: var(--xh-space-1) var(--xh-space-3) var(--xh-space-1) var(--xh-space-2_5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-surface);
  color: var(--xh-fg-muted);
  font-size: 0.9em;
}

.app-welcome__points li::before {
  content: '';
  inline-size: var(--xh-space-1_5);
  block-size: var(--xh-space-1_5);
  border-radius: var(--xh-shape-circle);
  background-color: var(--app-welcome-a1);
}

.app-welcome__visit {
  /* 没有按钮的一屏也占着一颗大号钮的高度 */
  min-block-size: calc(var(--xh-space-8) * 1.25);
  margin-block-start: var(--xh-space-6);
}

/* 欢迎层的底是带颜色的光斑：描边钮缺省是透明底，光斑会从钮里透出来，这里给它实心的面 */
.app-welcome__plain {
  --xh-button-bg: var(--xh-bg-surface);
  --xh-button-bg-hover: var(--xh-bg-subtle);
  --xh-button-bg-active: var(--xh-bg-subtle-hover);
}

/* 演示舞台：外圈一道流动的光，翻页时整块摆一下 */
.app-welcome__stage-wrap {
  min-inline-size: 0;
  perspective: 90rem;
}

.app-welcome__stage-tilt {
  position: relative;
}

.app-welcome__stage-tilt.is-swing {
  animation: app-welcome-swing calc(var(--xh-motion-duration-reveal) * 0.85) var(--xh-motion-ease-enter-strong);
}

@keyframes app-welcome-swing {
  30% {
    transform: rotateY(-20deg) scale(0.93);
  }
}

.app-welcome[data-direction='back'] .app-welcome__stage-tilt.is-swing {
  animation-name: app-welcome-swing-back;
}

@keyframes app-welcome-swing-back {
  30% {
    transform: rotateY(20deg) scale(0.93);
  }
}

.app-welcome__stage-glow {
  position: absolute;
  inset: calc(var(--xh-space-0_5) * -1);
  overflow: hidden;
  border-radius: var(--xh-shape-sheet);
  opacity: 0.5;
  filter: blur(var(--xh-space-6));
}

.app-welcome__stage-glow::before {
  content: '';
  position: absolute;
  inset: -60%;
  background: conic-gradient(
    var(--app-welcome-a1),
    var(--app-welcome-a2),
    var(--app-welcome-a3),
    var(--app-welcome-a1)
  );
  animation: app-welcome-spin 10s linear infinite;
}

.app-welcome[data-reduced='true'] .app-welcome__stage-glow::before {
  animation: none;
}

@keyframes app-welcome-spin {
  to {
    transform: rotate(360deg);
  }
}

.app-welcome__stage {
  position: relative;
  aspect-ratio: 4 / 3;
  overflow: clip;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-sheet);
  background-color: var(--xh-bg-page);
  box-shadow: var(--xh-shadow-xl);
}

.app-welcome-demo-enter-active,
.app-welcome-demo-leave-active {
  position: absolute;
  inset: 0;
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong);
}

.app-welcome-demo-enter-from {
  opacity: 0;
  transform: scale(1.05);
}

.app-welcome-demo-leave-to {
  opacity: 0;
  transform: scale(0.96);
}

/* 底部的操作：三幕的高度不一样（两行按钮、一行翻页），底栏按最高的一幕留足并贴底摆，换幕时上面的画面不跟着跳 */
.app-welcome__foot {
  display: grid;
  align-items: end;
  min-block-size: calc(var(--xh-space-8) * 5.5);
  padding: 0 var(--xh-space-7) var(--xh-space-5);
  transition: opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.app-welcome__actions {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-4);
}

/* 主操作与次操作各占一行、同宽 */
.app-welcome__buttons {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2_5);
  inline-size: min(100%, calc(var(--xh-space-8) * 10));
}

.app-welcome__hints {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  margin: 0;
  color: var(--xh-fg-subtle);
  font-size: var(--xh-font-size-xs);
}

.app-welcome__hints span {
  margin-inline-end: var(--xh-space-2);
}

.app-welcome__nav {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
}

.app-welcome__nav > :first-child {
  justify-self: start;
}

.app-welcome__nav > :last-child {
  justify-self: end;
}

.app-welcome__nav .is-hidden {
  visibility: hidden;
}

.app-welcome__dots {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}

.app-welcome__dot {
  inline-size: var(--xh-space-2);
  block-size: var(--xh-space-2);
  padding: 0;
  border: 0;
  border-radius: var(--xh-shape-pill);
  background-color: color-mix(in oklab, var(--xh-fg-default) 24%, transparent);
  cursor: pointer;
  transition:
    inline-size var(--xh-motion-duration-move) var(--xh-motion-ease-enter-strong),
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.app-welcome__dot:hover,
.app-welcome__dot.is-done {
  background-color: color-mix(in oklab, var(--xh-fg-default) 55%, transparent);
}

.app-welcome__dot.is-active {
  inline-size: var(--xh-space-7);
  background-color: var(--xh-fg-default);
}

.app-welcome__dot:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-space-0_5);
}

.app-welcome-foot-enter-active,
.app-welcome-foot-leave-active {
  transition:
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-move) var(--xh-motion-ease-enter);
}

.app-welcome-foot-enter-from,
.app-welcome-foot-leave-to {
  opacity: 0;
  transform: translateY(var(--xh-space-2));
}

/* 窄屏：演示在上、说明在下 */
@media (max-width: 53.75rem) {
  .app-welcome__tour {
    grid-template-columns: minmax(0, 1fr);
    gap: var(--xh-space-5);
    inline-size: 100%;
    max-inline-size: 32rem;
    font-size: var(--xh-font-size-md);
  }

  .app-welcome__stage-wrap {
    order: -1;
    justify-self: center;
    inline-size: min(100%, calc((100vh - 25rem) * 4 / 3));
    min-inline-size: 14rem;
  }

  .app-welcome__scene {
    padding-inline: var(--xh-space-4);
  }

  .app-welcome__heading {
    margin-block-start: var(--xh-space-2_5);
    font-size: 1.75em;
  }

  .app-welcome__text {
    margin-block-start: var(--xh-space-2);
    font-size: 1em;
  }

  .app-welcome__points {
    margin-block-start: var(--xh-space-3);
  }

  .app-welcome__visit {
    min-block-size: 0;
    margin-block-start: var(--xh-space-3);
  }

  .app-welcome__top {
    padding-inline: var(--xh-space-4) var(--xh-space-2_5);
  }

  .app-welcome__brand-name {
    display: none;
  }

  .app-welcome__foot {
    padding-inline: var(--xh-space-4);
  }
}
</style>
