<script lang="ts" setup>
import { Icon } from '@iconify/vue'
import { NButton, NIcon } from 'naive-ui'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'

defineOptions({ name: 'QrCodeLoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const canvasRef = ref<HTMLCanvasElement | null>(null)

function drawQrPlaceholder(canvas: HTMLCanvasElement) {
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const size = 200
  canvas.width = size
  canvas.height = size

  const cellSize = 5
  const margin = 20
  const gridSize = size - margin * 2
  const cells = Math.floor(gridSize / cellSize)

  ctx.fillStyle = '#ffffff'
  ctx.fillRect(0, 0, size, size)

  ctx.fillStyle = isDark.value ? '#e2e8f0' : '#1a1a2e'

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
        (row < 7 && col < 7) ||
        (row < 7 && col >= cells - 7) ||
        (row >= cells - 7 && col < 7)

      if (isCorner || random() > 0.55) {
        ctx.fillRect(
          margin + col * cellSize,
          margin + row * cellSize,
          cellSize,
          cellSize,
        )
      }
    }
  }

  function drawFinderPattern(x: number, y: number) {
    ctx.fillStyle = isDark.value ? '#e2e8f0' : '#1a1a2e'
    ctx.fillRect(x, y, 7 * cellSize, 7 * cellSize)
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(x + cellSize, y + cellSize, 5 * cellSize, 5 * cellSize)
    ctx.fillStyle = isDark.value ? '#e2e8f0' : '#1a1a2e'
    ctx.fillRect(x + 2 * cellSize, y + 2 * cellSize, 3 * cellSize, 3 * cellSize)
  }

  drawFinderPattern(margin, margin)
  drawFinderPattern(margin + (cells - 7) * cellSize, margin)
  drawFinderPattern(margin, margin + (cells - 7) * cellSize)
}

onMounted(() => {
  if (canvasRef.value) drawQrPlaceholder(canvasRef.value)
})

onBeforeUnmount(() => {
  // cleanup if needed
})
</script>

<template>
  <div>
    <h1 class="mb-1 text-2xl font-bold">
      {{ t('page.auth.qrcode_login') }} 📱
    </h1>
    <p
      class="mb-5 text-sm"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.qrcode_subtitle') }}
    </p>

    <div class="flex flex-col items-center py-4">
      <div
        class="relative flex items-center justify-center rounded-xl border-2 p-3"
        :class="isDark ? 'border-white/15 bg-white/5' : 'border-[hsl(var(--border))] bg-white'"
      >
        <canvas ref="canvasRef" class="rounded-lg" style="width: 180px; height: 180px" />
        <div class="absolute inset-0 flex items-center justify-center">
          <div
            class="flex h-10 w-10 items-center justify-center rounded-lg"
            :class="isDark ? 'bg-[#0b1220]' : 'bg-white'"
          >
            <NIcon :size="22" :class="isDark ? 'text-white' : 'text-[hsl(var(--primary))]'">
              <Icon icon="lucide:scan" />
            </NIcon>
          </div>
        </div>
      </div>

      <p
        class="mt-4 text-center text-sm"
        :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.qrcode_prompt') }}
      </p>
    </div>

    <NButton class="mt-2 w-full" quaternary @click="router.push('/auth/login')">
      {{ t('page.auth.back_to_login') }}
    </NButton>
  </div>
</template>
