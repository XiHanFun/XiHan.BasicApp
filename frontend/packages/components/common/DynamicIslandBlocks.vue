<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { IslandBlock } from '~/composables/useDynamicIsland'
import { XhProgress } from '@xihan-ui/vue'
import { Icon } from '~/iconify'
import DynamicIslandAccessory from './DynamicIslandAccessory.vue'

/**
 * 灵动岛内容块：文本、大号数值、线形进度、列表、网格、图片，或调用方自己的组件
 * 自上而下按数组顺序排，块与块之间同一档间距
 */
defineOptions({ name: 'DynamicIslandBlocks' })

defineProps<{
  blocks: IslandBlock[]
  startedAt: number
  now: number
}>()

function percentText(value: number, max = 100): string {
  return `${Math.round(Math.max(0, Math.min(1, value / max)) * 100)}%`
}
</script>

<template>
  <div class="dib">
    <template v-for="(block, index) in blocks" :key="`${block.type}:${index}`">
      <p v-if="block.type === 'text'" class="dib-text">
        {{ block.text }}
      </p>

      <div v-else-if="block.type === 'metric'" class="dib-metric">
        <div class="dib-metric__main">
          <div class="dib-metric__value">
            {{ block.value }}<span v-if="block.unit" class="dib-metric__unit">{{ block.unit }}</span>
          </div>
          <div v-if="block.caption" class="dib-metric__caption">
            {{ block.caption }}
          </div>
        </div>
        <DynamicIslandAccessory v-if="block.accessory" :accessory="block.accessory" size="lg" :started-at="startedAt" :now="now" />
      </div>

      <div v-else-if="block.type === 'progress'" class="dib-progress">
        <div v-if="block.label" class="dib-progress__head">
          <span class="dib-progress__label">{{ block.label }}</span>
          <span class="dib-progress__value">{{ block.valueText ?? percentText(block.value, block.max) }}</span>
        </div>
        <XhProgress
          :value="block.value"
          :max="block.max ?? 100"
          :tone="block.tone"
          :value-text="block.valueText"
          :aria-label="block.label"
          size="sm"
        />
      </div>

      <ul v-else-if="block.type === 'list'" class="dib-list">
        <li v-for="item in block.items" :key="item.key" class="dib-list__item">
          <DynamicIslandAccessory v-if="item.leading" :accessory="item.leading" size="md" :started-at="startedAt" :now="now" />
          <div class="dib-list__body">
            <div class="dib-list__row">
              <span class="dib-list__label">{{ item.label }}</span>
              <span v-if="item.value" class="dib-list__value">{{ item.value }}</span>
            </div>
            <XhProgress v-if="item.progress != null" :value="item.progress" :tone="item.tone" :aria-label="item.label" size="sm" />
          </div>
        </li>
      </ul>

      <div v-else-if="block.type === 'grid'" class="dib-grid" :style="{ '--dib-grid-columns': block.items.length }">
        <div v-for="item in block.items" :key="item.key" class="dib-grid__cell">
          <span class="dib-grid__label">{{ item.label }}</span>
          <Icon v-if="item.icon" :icon="item.icon" width="18" height="18" />
          <span class="dib-grid__value">{{ item.value }}</span>
        </div>
      </div>

      <img v-else-if="block.type === 'media'" class="dib-media" :src="block.src" :alt="block.alt" loading="lazy">

      <div v-else class="dib-custom">
        <component :is="block.component" v-bind="block.props" />
      </div>
    </template>
  </div>
</template>

<style scoped>
.dib {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2_5);
}

.dib-text {
  margin: 0;
  font-size: var(--xh-font-size-xs);
  line-height: 1.5;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 72%, transparent);
}

/* 大号数值：值与单位基线对齐，说明在下，小件贴右 */
.dib-metric {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-3);
}

.dib-metric__main {
  min-inline-size: 0;
}

.dib-metric__value {
  font-size: var(--xh-font-size-4xl);
  font-weight: 600;
  line-height: 1.1;
  font-variant-numeric: tabular-nums;
  color: var(--xh-fg-on-media-chrome);
}

.dib-metric__unit {
  margin-inline-start: var(--xh-space-1);
  font-size: var(--xh-font-size-sm);
  font-weight: 500;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 60%, transparent);
}

.dib-metric__caption {
  margin-block-start: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 55%, transparent);
}

.dib-progress {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
}

.dib-progress__head,
.dib-list__row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--xh-space-2);
  font-size: var(--xh-font-size-xs);
}

.dib-progress__label,
.dib-list__label {
  min-inline-size: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dib-progress__value,
.dib-list__value {
  flex-shrink: 0;
  font-variant-numeric: tabular-nums;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 55%, transparent);
}

.dib-list {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2_5);
  margin: 0;
  padding: 0;
  list-style: none;
}

.dib-list__item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
}

.dib-list__body {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
  min-inline-size: 0;
  flex: 1;
}

.dib-list__label {
  font-size: var(--xh-font-size-sm);
}

/* 网格：各格等宽，自上而下标题、图标、取值 */
.dib-grid {
  display: grid;
  grid-template-columns: repeat(var(--dib-grid-columns), minmax(0, 1fr));
  gap: var(--xh-space-1);
}

.dib-grid__cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-1);
  min-inline-size: 0;
  font-size: var(--xh-font-size-xs);
}

.dib-grid__label {
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 55%, transparent);
}

.dib-grid__label,
.dib-grid__value {
  max-inline-size: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dib-grid__value {
  font-variant-numeric: tabular-nums;
}

.dib-media {
  display: block;
  inline-size: 100%;
  aspect-ratio: 16 / 9;
  object-fit: cover;
  border-radius: var(--xh-shape-inset);
}
</style>
