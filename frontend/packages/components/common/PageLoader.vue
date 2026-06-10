<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { buildPath, getDetailScale, getLoaderConfig, getParticle, getRotation } from './math-curve-loaders'

defineOptions({ name: 'PageLoader' })

const props = withDefaults(defineProps<{
  /** 加载器标识（与偏好 loadingName 对应） */
  name?: string
  /** 像素尺寸 */
  size?: number
  /** 预览态：选择器中同时渲染多个，降配以保证流畅 */
  preview?: boolean
  /** 固定颜色：用前景色（暗色主题亮、亮色主题暗），不跟随主题色 */
  fixedColor?: boolean
}>(), {
  name: 'lissajous-drift',
  size: 44,
  preview: false,
  fixedColor: false,
})

const config = computed(() => getLoaderConfig(props.name))

// 居中偏移：采样曲线包围盒中心，平移到 (50,50)，使各曲线（如心形波/心形线等非对称曲线）在卡片中上下左右居中
const centerOffset = computed(() => {
  const cfg = config.value
  let minX = Number.POSITIVE_INFINITY
  let maxX = Number.NEGATIVE_INFINITY
  let minY = Number.POSITIVE_INFINITY
  let maxY = Number.NEGATIVE_INFINITY
  const steps = 240
  for (let i = 0; i <= steps; i++) {
    const point = cfg.point(i / steps, 0.8, cfg)
    minX = Math.min(minX, point.x)
    maxX = Math.max(maxX, point.x)
    minY = Math.min(minY, point.y)
    maxY = Math.max(maxY, point.y)
  }
  return { dx: 50 - (minX + maxX) / 2, dy: 50 - (minY + maxY) / 2 }
})
// 预览态降低粒子数与路径采样（选择器一次渲染 20+ 个）；实际加载态全量还原
const particleCount = computed(() => props.preview ? Math.min(config.value.particleCount, 44) : config.value.particleCount)
const pathSteps = computed(() => props.preview ? 220 : 460)
const particleList = computed(() => Array.from({ length: particleCount.value }, (_, index) => index))

const groupEl = ref<SVGGElement | null>(null)
const pathEl = ref<SVGPathElement | null>(null)
let circleEls: SVGCircleElement[] = []

function collectCircle(el: unknown, index: number) {
  if (el) {
    circleEls[index] = el as SVGCircleElement
  }
}

let raf = 0
let startTime = 0

function frame(now: number) {
  const cfg = config.value
  const count = particleCount.value
  const time = now - startTime
  const progress = (time % cfg.durationMs) / cfg.durationMs
  const detailScale = getDetailScale(time, cfg, 0)
  const rotation = getRotation(time, cfg, 0)

  const offset = centerOffset.value
  groupEl.value?.setAttribute('transform', `translate(${offset.dx.toFixed(2)} ${offset.dy.toFixed(2)}) rotate(${rotation.toFixed(2)} 50 50)`)
  pathEl.value?.setAttribute('d', buildPath(cfg, detailScale, pathSteps.value))

  for (let index = 0; index < count; index++) {
    const node = circleEls[index]
    if (!node) {
      continue
    }
    const particle = getParticle(cfg, index, count, progress, detailScale)
    node.setAttribute('cx', particle.x.toFixed(2))
    node.setAttribute('cy', particle.y.toFixed(2))
    node.setAttribute('r', particle.radius.toFixed(2))
    node.setAttribute('opacity', particle.opacity.toFixed(3))
  }

  raf = requestAnimationFrame(frame)
}

function start() {
  cancelAnimationFrame(raf)
  startTime = typeof performance === 'undefined' ? 0 : performance.now()
  raf = requestAnimationFrame(frame)
}

watch(() => props.name, () => {
  circleEls = []
  void nextTick(start)
})

onMounted(start)
onBeforeUnmount(() => cancelAnimationFrame(raf))
</script>

<template>
  <svg
    class="xh-page-loader"
    :class="{ 'is-fixed-color': fixedColor }"
    :style="{ width: `${size}px`, height: `${size}px` }"
    viewBox="0 0 100 100"
    fill="none"
    aria-hidden="true"
  >
    <g ref="groupEl">
      <path
        ref="pathEl"
        stroke="currentColor"
        :stroke-width="config.strokeWidth"
        stroke-linecap="round"
        stroke-linejoin="round"
        opacity="0.1"
      />
      <circle
        v-for="index in particleList"
        :key="index"
        :ref="(el) => collectCircle(el, index)"
        fill="currentColor"
      />
    </g>
  </svg>
</template>

<style scoped>
.xh-page-loader {
  color: hsl(var(--primary));
  overflow: visible;
}
/* 固定颜色：前景色随主题取明/暗，但不跟随主题色 */
.xh-page-loader.is-fixed-color {
  color: hsl(var(--foreground));
}
</style>
