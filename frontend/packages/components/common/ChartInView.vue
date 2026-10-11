<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useAppStore } from '~/stores'

/**
 * 图表进入视口才挂载：偏好「视口内动画」开着时，图表露出一半才挂上，入场动画在用户眼前播；
 * 关着就照常立刻挂。组件库不内置这条行为，由应用推迟挂载——组件库的图表首次挂上即播入场。
 *
 * - 挂上之前用占位撑出图表高度，挂上时版面不跳；挂过之后偏好再打开也不撤；
 * - 换图表配色时重播入场：露出一半的当场重挂，其余退回占位、滚进来再按新配色播；视口内动画关着就全部当场重挂；
 * - 打印时还没进过视口的一并挂上，打印期间外壳写 data-motion="reduce" 让图表直接画终态，纸上不留入场的第一帧；
 *   减弱动效下仍留着的淡入（如热力图格子填色）在挂上后一步播完。外壳是 display: contents，不在图表外多出一层盒子。
 */
defineOptions({ name: 'ChartInView' })

const props = withDefaults(defineProps<{
  /** 占位高度，取图表自己的高度 */
  placeholderSize?: string
}>(), {
  placeholderSize: 'var(--xh-chart-height)',
})

/**
 * 露出多少才开播：一露头就播，滚到整张图进入视口时入场已经播完，只看得到尾巴。
 * 占位与图表同高，图表不会高过视口两倍，一半总能露出来
 */
const VISIBLE_RATIO = 0.5

const appStore = useAppStore()

const placeholder = ref<HTMLElement | null>(null)
const shell = ref<HTMLElement | null>(null)
/** 已经挂上过 */
const mounted = ref(false)
/** 打印期间：没进过视口的图表一并挂上、直接画终态 */
const printing = ref(false)
/** 外壳的 key：加一即整张图重挂，组件库的图表重挂即重播入场 */
const generation = ref(0)
/** 挂上之后图表此刻是否露出一半，换配色时据此决定当场重播还是退回占位 */
let halfVisible = false

const observable = typeof IntersectionObserver !== 'undefined'

const shown = computed(() => mounted.value || printing.value || !appStore.chartAnimateInView || !observable)

watch(shown, (value) => {
  if (value)
    mounted.value = true
}, { immediate: true })

/** 占位上的观察器：露出一半就挂上 */
let observer: IntersectionObserver | null = null
/** 挂上之后盯着图表本身：只在视口内动画开着时需要 */
let tracker: IntersectionObserver | null = null

function stopObserving() {
  observer?.disconnect()
  observer = null
}

function stopTracking() {
  tracker?.disconnect()
  tracker = null
  halfVisible = false
}

watch(placeholder, (el) => {
  stopObserving()
  if (!el)
    return
  observer = new IntersectionObserver((entries) => {
    if (entries.some(entry => entry.intersectionRatio >= VISIBLE_RATIO))
      mounted.value = true
  }, { threshold: VISIBLE_RATIO })
  observer.observe(el)
}, { flush: 'post' })

watch([shell, () => appStore.chartAnimateInView], ([el, inView]) => {
  stopTracking()
  // 外壳是 display: contents，没有盒子可观察，盯它里面的第一个盒子（图表根）
  const target = el?.firstElementChild
  if (!target || !inView || !observable)
    return
  tracker = new IntersectionObserver((entries) => {
    const entry = entries.at(-1)
    if (entry)
      halfVisible = entry.intersectionRatio >= VISIBLE_RATIO
  }, { threshold: [0, VISIBLE_RATIO] })
  tracker.observe(target)
}, { flush: 'post' })

// 换配色重播入场；还在占位的不用管，滚进来时自然按新配色入场
watch(() => appStore.chartPalette, () => {
  if (!mounted.value)
    return
  if (halfVisible || !appStore.chartAnimateInView || !observable)
    generation.value++
  else
    mounted.value = false
})

async function onBeforePrint() {
  printing.value = true
  // 浏览器在 beforeprint 之后立刻排版截图，等不到动画播完：挂上之后把有限时长的动画一步播完，循环动画不动
  await nextTick()
  for (const animation of shell.value?.getAnimations({ subtree: true }) ?? []) {
    if (Number.isFinite(animation.effect?.getComputedTiming().endTime))
      animation.finish()
  }
}

function onAfterPrint() {
  printing.value = false
}

onMounted(() => {
  window.addEventListener('beforeprint', onBeforePrint)
  window.addEventListener('afterprint', onAfterPrint)
})
onBeforeUnmount(() => {
  stopObserving()
  stopTracking()
  window.removeEventListener('beforeprint', onBeforePrint)
  window.removeEventListener('afterprint', onAfterPrint)
})
</script>

<template>
  <div v-if="shown" :key="generation" ref="shell" class="contents" :data-motion="printing ? 'reduce' : undefined">
    <slot />
  </div>
  <div v-else ref="placeholder" aria-hidden="true" :style="{ blockSize: props.placeholderSize }" />
</template>
