<script setup lang="ts">
import type { ColumnSetting, TableDensity } from './useTableSettings'
import { NButton, NCheckbox, NDivider, NIcon, NPopover, NTooltip } from 'naive-ui'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaTableSettings' })

defineProps<{
  /** 列设置（来自 useTableSettings.columns） */
  columns: ColumnSetting[]
  /** 当前密度 */
  density: TableDensity
}>()

const emit = defineEmits<{
  'toggleVisible': [key: string, value: boolean]
  'setFixed': [key: string, fixed: 'left' | 'right' | undefined]
  'move': [fromIndex: number, toIndex: number]
  'setDensity': [value: TableDensity]
  'reset': []
}>()

const densityOptions: Array<{ label: string, value: TableDensity }> = [
  { label: '紧凑', value: 'small' },
  { label: '默认', value: 'medium' },
  { label: '宽松', value: 'large' },
]

/** 固定循环切换：无 → 左 → 右 → 无 */
function nextFixed(current?: 'left' | 'right'): 'left' | 'right' | undefined {
  if (current === undefined) {
    return 'left'
  }
  if (current === 'left') {
    return 'right'
  }
  return undefined
}

function fixedLabel(fixed?: 'left' | 'right'): string {
  if (fixed === 'left') {
    return '左'
  }
  if (fixed === 'right') {
    return '右'
  }
  return '－'
}
</script>

<template>
  <NPopover trigger="click" placement="bottom-end" :width="280">
    <template #trigger>
      <NTooltip>
        <template #trigger>
          <NButton circle quaternary size="small" aria-label="列设置">
            <template #icon>
              <NIcon><Icon icon="lucide:settings-2" /></NIcon>
            </template>
          </NButton>
        </template>
        列设置
      </NTooltip>
    </template>

    <div class="flex flex-col gap-2">
      <!-- 密度 -->
      <div class="flex gap-2 items-center justify-between">
        <span class="text-xs text-foreground/60">密度</span>
        <div class="flex gap-1">
          <NButton
            v-for="opt in densityOptions"
            :key="opt.value"
            size="tiny"
            :type="density === opt.value ? 'primary' : 'default'"
            @click="emit('setDensity', opt.value)"
          >
            {{ opt.label }}
          </NButton>
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 列显隐 / 固定 / 排序 -->
      <div class="flex items-center justify-between">
        <span class="text-xs text-foreground/60">列设置</span>
        <NButton size="tiny" quaternary @click="emit('reset')">
          恢复默认
        </NButton>
      </div>

      <div class="flex flex-col max-h-72 overflow-auto">
        <div
          v-for="(col, index) in columns"
          :key="col.key"
          class="flex gap-2 items-center py-1"
        >
          <NCheckbox
            :checked="col.visible"
            @update:checked="(value) => emit('toggleVisible', col.key, value)"
          >
            {{ col.title }}
          </NCheckbox>
          <div class="flex-1" />
          <NButton
            size="tiny"
            quaternary
            :title="`固定：${fixedLabel(col.fixed)}`"
            @click="emit('setFixed', col.key, nextFixed(col.fixed))"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:pin" /></NIcon>
            </template>
            {{ fixedLabel(col.fixed) }}
          </NButton>
          <NButton
            size="tiny"
            quaternary
            :disabled="index === 0"
            aria-label="上移"
            @click="emit('move', index, index - 1)"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:chevron-up" /></NIcon>
            </template>
          </NButton>
          <NButton
            size="tiny"
            quaternary
            :disabled="index === columns.length - 1"
            aria-label="下移"
            @click="emit('move', index, index + 1)"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:chevron-down" /></NIcon>
            </template>
          </NButton>
        </div>
      </div>
    </div>
  </NPopover>
</template>
