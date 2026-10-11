<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useAppStore } from '~/stores'

/**
 * 图表进入视口才挂载：偏好「视口内动画」开着时，图表露出一半才挂上，入场动画在用户眼前播；
 * 关着就照常立刻挂。组件库不内置这条行为，由应用推迟挂载——组件库的图表首次挂上即播入场。
 *
 * - 挂上之前用占位撑出图表高度，挂上时版面不跳；挂过一次就一直挂着，偏好再打开也不撤；
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

const shown = computed(() =>
  mounted.value || printing.value || !appStore.chartAnimateInView || typeof IntersectionObserver === 'undefined',
)

watch(shown, (value) => {
  if (value)
    mounted.value = true
}, { immediate: true })

let observer: IntersectionObserver | null = null

function stopObserving() {
  observer?.disconnect()
  observer = null
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
  window.removeEventListener('beforeprint', onBeforePrint)
  window.removeEventListener('afterprint', onAfterPrint)
})
</script>

<template>
  <div v-if="shown" ref="shell" class="contents" :data-motion="printing ? 'reduce' : undefined">
    <slot />
  </div>
  <div v-else ref="placeholder" aria-hidden="true" :style="{ blockSize: props.placeholderSize }" />
</template>
