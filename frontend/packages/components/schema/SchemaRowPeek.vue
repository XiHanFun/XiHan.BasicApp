<script setup lang="ts" generic="TRow extends object">
import type { ListFieldSchema } from './types'
import { computed, nextTick, ref, watch } from 'vue'
import { formatFieldText } from './renderer'

defineOptions({ name: 'SchemaRowPeek' })

const props = defineProps<{
  visible: boolean
  row: TRow | null
  /** 速览字段（全部可读字段，含表格隐藏列） */
  fields: ListFieldSchema<TRow>[]
  /** 光标坐标（视口系），卡片在其右下浮出并自动夹取 */
  x: number
  y: number
}>()

const cardRef = ref<HTMLElement | null>(null)
const pos = ref({ left: 0, top: 0 })

/** 字段条目：跳过空值（'-'），密集 DTO 不浪费版面 */
const entries = computed(() => {
  const row = props.row
  if (!row) {
    return []
  }
  return props.fields
    .map(field => ({ key: field.key, label: field.title, value: formatFieldText(field, row) }))
    .filter(item => item.value !== '-')
})

// 定位：光标右下偏移 16px；超出视口时左翻/上收，并留 12px 安全边距
watch(
  [() => props.visible, () => props.x, () => props.y, () => props.row],
  async ([visible]) => {
    if (!visible) {
      return
    }
    await nextTick()
    const el = cardRef.value
    const width = el?.offsetWidth ?? 380
    const height = el?.offsetHeight ?? 320
    const margin = 12
    let left = props.x + 16
    let top = props.y + 16
    if (left + width + margin > window.innerWidth) {
      left = Math.max(margin, props.x - width - 16)
    }
    if (top + height + margin > window.innerHeight) {
      top = Math.max(margin, window.innerHeight - height - margin)
    }
    pos.value = { left, top }
  },
)
</script>

<template>
  <Teleport to="body">
    <Transition name="row-peek">
      <div
        v-if="visible && row && entries.length"
        ref="cardRef"
        class="row-peek"
        :style="{ left: `${pos.left}px`, top: `${pos.top}px` }"
      >
        <div class="row-peek__grid">
          <template v-for="item in entries" :key="item.key">
            <span class="row-peek__label">{{ item.label }}</span>
            <span class="row-peek__value">{{ item.value }}</span>
          </template>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
/* 纯速览：pointer-events none，永不截获鼠标，不干扰行内按钮/拖拽 */
.row-peek {
  position: fixed;
  z-index: 2600;
  width: 400px;
  max-width: 90vw;
  max-height: 64vh;
  padding: 14px 16px;
  overflow: hidden;
  border-radius: var(--radius-card, 10px);
  background: hsl(var(--popover));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 16px 48px hsl(var(--foreground) / 18%);
  pointer-events: none;
}

.row-peek__grid {
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 7px 14px;
  align-items: baseline;
}

.row-peek__label {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  white-space: nowrap;
}

.row-peek__value {
  font-size: 13px;
  color: hsl(var(--foreground));
  word-break: break-all;
  line-height: 1.45;
}

/* iOS Peek 弹出：轻微上移 + 缩放 + 淡入 */
.row-peek-enter-active {
  transition:
    opacity 0.16s ease,
    transform 0.22s cubic-bezier(0.22, 1.2, 0.36, 1);
}

.row-peek-leave-active {
  transition: opacity 0.12s ease;
}

.row-peek-enter-from {
  opacity: 0;
  transform: translateY(6px) scale(0.96);
}

.row-peek-leave-to {
  opacity: 0;
}
</style>
