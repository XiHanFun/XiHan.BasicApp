<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { IslandAction, IslandTask } from '~/composables/useDynamicIsland'
import { XhButton, XhButtonLabel, XhButtonPrefix, XhProgress } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { defaultLeading, formatElapsed } from './dynamic-island-view'
import DynamicIslandAccessory from './DynamicIslandAccessory.vue'
import DynamicIslandBlocks from './DynamicIslandBlocks.vue'

/**
 * 灵动岛一条活动的展开视图：卡片与面板里的每一行都用它
 * 首行是 leading、标题与 trailing，其下副文本、内容块、进度、已用时与操作都铺满整宽，不给小件单留一列
 * 可点击时整条由一枚铺满的按钮承接（操作、移除钮与自定义内容浮在它上面，各管各的点击）
 */
defineOptions({ name: 'DynamicIslandActivity' })

const props = defineProps<{
  task: IslandTask
  /** card 自动展开的卡片，row 面板里的一行 */
  variant: 'card' | 'row'
  /** 整条可点 */
  clickable: boolean
  now: number
}>()

const emit = defineEmits<{
  open: []
  action: [action: IslandAction]
  dismiss: []
}>()

const { t } = useI18n()

const leading = computed(() => props.task.leading ?? defaultLeading(props.task))
/** 缺省指示器带状态色 */
const leadingIsState = computed(() => !props.task.leading)
const showProgress = computed(() => props.task.state === 'loading' && props.task.progress != null)
const showElapsed = computed(() => props.task.state === 'loading' && props.task.elapsed !== false)
const dismissible = computed(() => props.variant === 'row' && (props.task.dismissible ?? !props.task.persistent))
const barActions = computed(() => props.task.actionLayout === 'bar')
</script>

<template>
  <div class="dia" :class="[`dia--${variant}`, `is-${task.state}`, { 'is-clickable': clickable }]">
    <button
      v-if="clickable"
      type="button"
      class="dia__hit"
      :aria-label="task.label"
      @click="emit('open')"
    />

    <div class="dia__body">
      <div class="dia__head">
        <span class="dia__leading" :class="{ 'is-state': leadingIsState }">
          <DynamicIslandAccessory :accessory="leading" :started-at="task.startedAt" :now="now" />
        </span>
        <span class="dia__label">{{ task.label }}</span>
        <DynamicIslandAccessory v-if="task.trailing" :accessory="task.trailing" :started-at="task.startedAt" :now="now" />
      </div>
      <div v-if="task.detail" class="dia__detail">
        {{ task.detail }}
      </div>

      <DynamicIslandBlocks v-if="task.blocks?.length" class="dia__blocks" :blocks="task.blocks" :started-at="task.startedAt" :now="now" />

      <div v-if="showProgress" class="dia__progress">
        <XhProgress :value="task.progress" :aria-label="task.label" size="sm" class="dia__progress-bar" />
        <span class="dia__progress-pct">{{ Math.round(task.progress ?? 0) }}%</span>
      </div>
      <div v-if="showElapsed" class="dia__meta">
        {{ t('island.elapsed_prefix') }} {{ formatElapsed(now - task.startedAt) }}
      </div>

      <div v-if="task.actions?.length" class="dia__actions" :class="{ 'is-bar': barActions }">
        <!-- 图标栏：等宽的纯图标钮（没给图标的退回文字）；行内：品牌档是主操作（实心），其余走淡底 -->
        <template v-if="barActions">
          <XhButton
            v-for="action in task.actions"
            :key="action.key"
            variant="subtle"
            :tone="action.tone ?? 'neutral'"
            :icon-only="!!action.icon"
            :aria-label="action.label"
            @click.stop="emit('action', action)"
          >
            <Icon v-if="action.icon" :icon="action.icon" />
            <template v-else>
              {{ action.label }}
            </template>
          </XhButton>
        </template>
        <template v-else>
          <XhButton
            v-for="action in task.actions"
            :key="action.key"
            size="sm"
            :variant="action.tone === 'brand' ? 'solid' : 'subtle'"
            :tone="action.tone ?? 'neutral'"
            @click.stop="emit('action', action)"
          >
            <XhButtonPrefix v-if="action.icon">
              <Icon :icon="action.icon" />
            </XhButtonPrefix>
            <XhButtonLabel>{{ action.label }}</XhButtonLabel>
          </XhButton>
        </template>
      </div>
    </div>

    <!-- 悬停才露出：显隐与定位落在外层，不在组件库按钮的根上写过渡 -->
    <span v-if="dismissible" class="dia__dismiss">
      <XhButton variant="ghost" size="sm" icon-only class="x-icon-btn dia__dismiss-btn" :aria-label="t('island.dismiss')" @click.stop="emit('dismiss')">
        <Icon icon="lucide:x" />
      </XhButton>
    </span>
  </div>
</template>

<style scoped>
.dia {
  position: relative;
  text-align: start;
}

.dia--row {
  padding: var(--xh-space-2) var(--xh-space-6) var(--xh-space-2) var(--xh-space-2);
  border-radius: var(--xh-shape-sheet);
}

.dia--card {
  padding: var(--xh-space-3) var(--xh-space-4);
}

/* 整条点击：铺满的透明按钮垫在内容下面，内容不接指针，点哪都落到它；操作、移除钮与自定义内容再浮上来 */
.dia__hit {
  position: absolute;
  inset: 0;
  padding: 0;
  border: 0;
  border-radius: inherit;
  background: transparent;
  cursor: pointer;
}

.dia__hit:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.dia.is-clickable .dia__body {
  pointer-events: none;
}

.dia.is-clickable .dia__actions {
  pointer-events: auto;
}

.dia.is-clickable .dia__head :deep(.di-acc--custom) {
  position: relative;
  pointer-events: auto;
}

/* 自定义内容块自己管交互：定位后才压得住垫底的整条按钮 */
.dia.is-clickable .dia__blocks :deep(.dib-custom) {
  position: relative;
  pointer-events: auto;
}

.dia--row.is-clickable:hover {
  background: color-mix(in srgb, var(--xh-fg-on-media-chrome) 6%, transparent);
}

.dia__leading {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 85%, transparent);
}

.dia.is-success .dia__leading.is-state {
  color: color-mix(in srgb, var(--xh-fg-success) 75%, var(--xh-fg-on-media-chrome));
}

.dia.is-error .dia__leading.is-state {
  color: color-mix(in srgb, var(--xh-fg-danger) 85%, var(--xh-fg-on-media-chrome));
}

.dia.is-info .dia__leading.is-state {
  color: color-mix(in srgb, var(--xh-fg-brand) 70%, var(--xh-fg-on-media-chrome));
}

.dia__body {
  display: flex;
  flex-direction: column;
  min-inline-size: 0;
}

.dia__head {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  min-block-size: var(--xh-glyph-size-lg);
}

.dia__label {
  min-inline-size: 0;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: var(--xh-font-size-sm);
  font-weight: 500;
}

.dia--card .dia__label {
  font-weight: 600;
  color: var(--xh-fg-on-media-chrome);
}

.dia__detail {
  margin-block-start: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  line-height: 1.45;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 55%, transparent);
  word-break: break-word;
}

/* 卡片只负责一眼可读：正文最多两行，再多就该去面板看 */
.dia--card .dia__detail {
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  overflow: hidden;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 72%, transparent);
}

.dia__blocks {
  margin-block-start: var(--xh-space-2_5);
}

.dia__progress {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  margin-block-start: var(--xh-space-2);
}

.dia__progress-bar {
  flex: 1;
}

.dia__progress-pct {
  font-size: var(--xh-font-size-xs);
  font-variant-numeric: tabular-nums;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 70%, transparent);
}

.dia__meta {
  margin-block-start: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  font-variant-numeric: tabular-nums;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 40%, transparent);
}

.dia__actions {
  position: relative;
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1_5);
  margin-block-start: var(--xh-space-2_5);
}

/* 图标栏：等宽一排，像小组件底部的快捷操作 */
.dia__actions.is-bar {
  display: grid;
  grid-auto-columns: minmax(0, 1fr);
  grid-auto-flow: column;
}

.dia__actions.is-bar > * {
  inline-size: 100%;
}

/* 移除钮落在这一条右侧留出的 space-6 里，竖向对齐首行文字 */
.dia__dismiss {
  position: absolute;
  inset-block-start: var(--xh-space-2);
  inset-inline-end: var(--xh-space-1);
  display: inline-flex;
  opacity: 0;
  transition: opacity var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

/* 组件库的纯图标钮压小：盒取 lg 字形档、字形取 xs 档，经按钮的公开槽给 */
.dia__dismiss-btn {
  --xh-button-h: var(--xh-glyph-size-lg);
  --xh-button-icon-size: var(--xh-glyph-size-xs);
}

/* 悬停这一条或键盘聚焦到移除钮时露出 */
.dia:hover .dia__dismiss,
.dia__dismiss:focus-within {
  opacity: 1;
}
</style>
