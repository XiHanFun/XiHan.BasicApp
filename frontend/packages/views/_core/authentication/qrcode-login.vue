<script lang="ts" setup>
import { Icon } from '~/iconify'
import { NButton, NIcon } from 'naive-ui'
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTheme } from '~/hooks'

defineOptions({ name: 'QrCodeLoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const canvasRef = ref<HTMLCanvasElement | null>(null)

function drawQrPlaceholder(canvas: HTMLCanvasElement) {
  const ctx = canvas.getContext('2d')
  if (!ctx) return
  const context = ctx

  const size = 200
  canvas.width = size
  canvas.height = size

  const cellSize = 5
  const margin = 20
  const gridSize = size - margin * 2
  const cells = Math.floor(gridSize / cellSize)

  context.fillStyle = '#ffffff'
  context.fillRect(0, 0, size, size)

  context.fillStyle = isDark.value ? '#e2e8f0' : '#1a1a2e'

  const rng = (seed: number) => {
    let s = seed
    return () => {
      s = (s * 16807 + 0) % 2147483647
      return s / 2147483647
    }
  }
  const random = rng(42)

  for (let row = 0; row < cells; row++) {
    for (let col = 0; col < cells; col++) {
      const isCorner =
        (row < 7 && col < 7) || (row < 7 && col >= cells - 7) || (row >= cells - 7 && col < 7)

      if (isCorner || random() > 0.55) {
        context.fillRect(margin + col * cellSize, margin + row * cellSize, cellSize, cellSize)
      }
    }
  }

  function drawFinderPattern(x: number, y: number) {
    context.fillStyle = isDark.value ? '#e2e8f0' : '#1a1a2e'
    context.fillRect(x, y, 7 * cellSize, 7 * cellSize)
    context.fillStyle = '#ffffff'
    context.fillRect(x + cellSize, y + cellSize, 5 * cellSize, 5 * cellSize)
    context.fillStyle = isDark.value ? '#e2e8f0' : '#1a1a2e'
    context.fillRect(x + 2 * cellSize, y + 2 * cellSize, 3 * cellSize, 3 * cellSize)
  }

  drawFinderPattern(margin, margin)
  drawFinderPattern(margin + (cells - 7) * cellSize, margin)
  drawFinderPattern(margin, margin + (cells - 7) * cellSize)
}

onMounted(() => {
  if (canvasRef.value) drawQrPlaceholder(canvasRef.value)
})

watch(isDark, () => {
  if (canvasRef.value) drawQrPlaceholder(canvasRef.value)
})
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.qrcode_login') }}
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.qrcode_subtitle') }}
      </p>
    </div>

    <div class="flex flex-col items-center py-4">
      <div
        class="flex relative justify-center items-center p-4 rounded-2xl border-2"
        :class="isDark ? 'border-white/20 bg-white/5' : 'border-[hsl(var(--border))] bg-white/90'"
      >
        <canvas ref="canvasRef" class="rounded-xl" style="width: 216px; height: 216px" />
        <div class="flex absolute inset-0 justify-center items-center">
          <div
            class="flex justify-center items-center w-12 h-12 rounded-xl"
            :class="isDark ? 'bg-[#0b1220]' : 'bg-white'"
          >
            <NIcon :size="24" :class="isDark ? 'text-white' : 'text-[hsl(var(--primary))]'">
              <Icon icon="lucide:scan" />
            </NIcon>
          </div>
        </div>
      </div>

      <p
        class="mt-5 text-center text-[15px]"
        :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.qrcode_prompt') }}
      </p>
    </div>

    <NButton class="mt-5 !h-11 w-full !rounded-xl" quaternary @click="router.push('/auth/login')">
      {{ t('page.auth.back_to_login') }}
    </NButton>
  </div>
</template>
