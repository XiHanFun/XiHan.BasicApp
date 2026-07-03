<script setup lang="ts">
import { onActivated, onBeforeUnmount, onMounted, ref } from 'vue'
import { ChatPanel } from '~/components'

// 路由名须与后端 PageRegistry 的 RouteName（MessageChat）一致，keepalive 按此匹配
defineOptions({ name: 'MessageChat' })

// 布局根是 min-h-full 的整页滚动设计，百分比高度链路（h-full）在内容超高时集体失效。
// 聊天页需要内部滚动：按「视口高 − 自身文档顶部偏移 − 布局页脚高」实时钉死高度，
// 滚动只发生在面板内部（消息流/会话列表）。ResizeObserver 跟踪横幅显隐/页签开关等动态偏移。
const rootRef = ref<HTMLElement>()
const panelHeight = ref('600px')
let bodyObserver: null | ResizeObserver = null
let calibrateTimer: null | ReturnType<typeof setTimeout> = null

function updateHeight() {
  const el = rootRef.value
  if (!el) {
    return
  }
  // 文档坐标顶部（不受当前滚动位置影响）
  const docTop = el.getBoundingClientRect().top + window.scrollY
  const footer = document.querySelector('footer')
  const footerHeight = footer instanceof HTMLElement ? footer.offsetHeight : 0
  const next = `${Math.max(360, window.innerHeight - docTop - footerHeight)}px`
  if (panelHeight.value !== next) {
    panelHeight.value = next
  }
}

function scheduleCalibrate() {
  // 路由切换动画（scale 变换）期间 rect 不可靠，动画结束后二次校准
  if (calibrateTimer) {
    clearTimeout(calibrateTimer)
  }
  calibrateTimer = setTimeout(() => {
    calibrateTimer = null
    updateHeight()
  }, 350)
}

onMounted(() => {
  updateHeight()
  scheduleCalibrate()
  window.addEventListener('resize', updateHeight)
  bodyObserver = new ResizeObserver(() => updateHeight())
  bodyObserver.observe(document.body)
})

onActivated(() => {
  updateHeight()
  scheduleCalibrate()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateHeight)
  bodyObserver?.disconnect()
  bodyObserver = null
  if (calibrateTimer) {
    clearTimeout(calibrateTimer)
  }
})
</script>

<template>
  <div ref="rootRef" class="overflow-hidden p-2 sm:p-4" :style="{ height: panelHeight }">
    <ChatPanel mode="page" />
  </div>
</template>
