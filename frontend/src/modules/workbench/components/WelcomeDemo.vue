<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhNumberAnimation } from '@xihan-ui/vue'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSequence } from '~/composables'
import { Icon } from '~/iconify'

/**
 * 欢迎页演示：工作台。几块小组件被依次拎起来换位置，循环播放
 */
defineOptions({ name: 'WorkbenchWelcomeDemo' })

type Tile = 'feed' | 'schedule' | 'shortcuts' | 'todo' | 'visits'
/** 一块小组件占的位置：左、上、宽、高，都是占画板的百分比 */
type Box = readonly [number, number, number, number]

/** 三种排法，轮流换 */
const LAYOUTS: ReadonlyArray<Record<Tile, Box>> = [
  {
    visits: [0, 0, 58, 47],
    todo: [61, 0, 39, 47],
    schedule: [0, 52, 31, 48],
    shortcuts: [34.5, 52, 31, 48],
    feed: [69, 52, 31, 48],
  },
  {
    visits: [42, 0, 58, 47],
    todo: [0, 0, 39, 47],
    schedule: [69, 52, 31, 48],
    shortcuts: [0, 52, 31, 48],
    feed: [34.5, 52, 31, 48],
  },
  {
    visits: [0, 0, 100, 40],
    todo: [0, 45, 31, 55],
    schedule: [34.5, 45, 31, 55],
    shortcuts: [69, 45, 31, 25],
    feed: [69, 75, 31, 25],
  },
]

const { t, locale } = useI18n()
const layout = ref(0)
/** 正被拎起来的那一块 */
const lifted = ref<null | Tile>(null)

function box(tile: Tile) {
  const [x, y, w, h] = LAYOUTS[layout.value]![tile]
  return {
    insetInlineStart: `${x}%`,
    insetBlockStart: `${y}%`,
    inlineSize: `${w}%`,
    blockSize: `${h}%`,
  }
}

const loop = useSequence(async (wait) => {
  const moves: Array<[Tile, number]> = [
    ['visits', 1],
    ['todo', 2],
    ['visits', 0],
  ]
  for (const [tile, next] of moves) {
    await wait(1500)
    lifted.value = tile
    await wait(360)
    layout.value = next
    await wait(950)
    lifted.value = null
  }
})

onMounted(() => loop.start())
</script>

<template>
  <div class="demo">
    <div class="demo__bar">
      <span class="demo__editing">
        <Icon icon="lucide:move" />
        {{ t('workbench.welcome.demo_editing') }}
      </span>
    </div>
    <div class="board">
      <div class="tile" :class="{ 'is-lifted': lifted === 'visits' }" :style="box('visits')">
        <small>{{ t('workbench.welcome.demo_visits') }}</small>
        <strong>
          <XhNumberAnimation :to="2846" :locale="locale" :format-options="{ useGrouping: true }" />
          <span>↑ 12.4%</span>
        </strong>
        <svg viewBox="0 0 200 60" preserveAspectRatio="none">
          <path class="tile__area" d="M0 46 C20 40 30 20 50 26 S 80 50 100 34 S 140 8 160 18 S 190 30 200 12 V60 H0Z" />
          <path class="tile__line" pathLength="100" d="M0 46 C20 40 30 20 50 26 S 80 50 100 34 S 140 8 160 18 S 190 30 200 12" />
        </svg>
      </div>

      <div class="tile" :class="{ 'is-lifted': lifted === 'todo' }" :style="box('todo')">
        <small>{{ t('workbench.welcome.demo_todo') }}</small>
        <ul class="tile__todo">
          <li class="is-done">
            <b><Icon icon="lucide:check" /></b><i />
          </li>
          <li class="is-done">
            <b><Icon icon="lucide:check" /></b><i />
          </li>
          <li><b /><i /></li>
          <li><b /><i /></li>
        </ul>
      </div>

      <div class="tile" :style="box('schedule')">
        <small>{{ t('workbench.welcome.demo_schedule') }}</small>
        <div class="tile__calendar">
          <i v-for="n in 28" :key="n" :class="{ 'is-today': n === 17, 'is-busy': n === 10 || n === 23 }" />
        </div>
      </div>

      <div class="tile" :style="box('shortcuts')">
        <small>{{ t('workbench.welcome.demo_shortcuts') }}</small>
        <div class="tile__apps">
          <b /><b /><b /><b />
        </div>
      </div>

      <div class="tile" :style="box('feed')">
        <small>{{ t('workbench.welcome.demo_messages') }}</small>
        <div class="tile__feed">
          <p><b /><i /></p>
          <p><b /><i /></p>
          <p><b /><i /></p>
        </div>
      </div>
    </div>
    <p class="demo__caption">
      {{ t('workbench.welcome.demo_caption') }}
    </p>
  </div>
</template>

<style scoped>
.demo {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-3);
  block-size: 100%;
  padding: var(--xh-space-4) var(--xh-space-6);
}

.demo__bar {
  display: flex;
  justify-content: flex-end;
  inline-size: 100%;
}

.demo__editing {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  padding: var(--xh-space-0_5) var(--xh-space-3);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-semibold);
}

.demo__caption {
  margin: 0;
  padding: var(--xh-space-0_5) var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-surface);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
  white-space: nowrap;
}

.board {
  position: relative;
  flex: 1;
  inline-size: 100%;
  min-block-size: 0;
}

/* 小组件：位置与大小由脚本按排法给，换排法时滑过去 */
.tile {
  position: absolute;
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  overflow: hidden;
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-surface);
  box-shadow: var(--xh-shadow-md);
  transition:
    inset-inline-start var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    inset-block-start var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    inline-size var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    block-size var(--xh-motion-duration-reveal) var(--xh-motion-ease-enter-strong),
    border-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    box-shadow var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-slide) var(--xh-motion-ease-settle);
}

.tile.is-lifted {
  z-index: 1;
  border-color: var(--xh-bg-brand);
  box-shadow: var(--xh-shadow-xl);
  transform: scale(1.045);
}

.tile > small {
  flex: none;
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
}

.tile > strong {
  display: flex;
  align-items: baseline;
  gap: var(--xh-space-2);
  font-size: var(--xh-font-size-3xl);
  font-variant-numeric: tabular-nums;
  line-height: var(--xh-leading-none);
}

.tile > strong span {
  color: var(--xh-fg-success);
  font-size: var(--xh-font-size-xs);
}

.tile > svg {
  flex: 1;
  inline-size: 100%;
  min-block-size: 0;
}

.tile__area {
  fill: var(--xh-bg-brand-subtle);
}

.tile__line {
  fill: none;
  stroke: var(--xh-bg-brand);
  stroke-width: 2.4;
  stroke-linecap: round;
  stroke-dasharray: 100;
  animation: tile-draw calc(var(--xh-motion-duration-reveal) * 2) var(--xh-motion-ease-enter-strong) backwards;
}

@keyframes tile-draw {
  from {
    stroke-dashoffset: 100;
  }
}

.tile__todo {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2_5);
  margin: 0;
  padding: 0;
  list-style: none;
}

.tile__todo li {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}

.tile__todo b {
  flex: none;
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-4);
  block-size: var(--xh-space-4);
  border: 1px solid var(--xh-border-control);
  border-radius: var(--xh-shape-inset);
  color: var(--xh-fg-on-brand);
  font-size: var(--xh-font-size-xs);
}

.tile__todo .is-done b {
  border-color: var(--xh-bg-brand);
  background-color: var(--xh-bg-brand);
}

.tile__todo i {
  flex: 1;
  block-size: var(--xh-space-2);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
}

.tile__todo li:nth-child(even) i {
  flex: 0.7;
}

.tile__todo .is-done i {
  opacity: 0.5;
}

.tile__calendar {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  align-content: start;
  gap: var(--xh-space-1);
}

.tile__calendar i {
  aspect-ratio: 1;
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-subtle-active);
}

.tile__calendar i.is-busy {
  background-color: var(--xh-bg-brand-subtle);
}

.tile__calendar i.is-today {
  background-color: var(--xh-bg-brand);
}

.tile__apps {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--xh-space-2);
}

.tile__apps b {
  aspect-ratio: 1;
  border-radius: var(--xh-shape-control);
  background-color: var(--xh-bg-brand);
}

.tile__apps b:nth-child(2) {
  background-color: var(--xh-fg-success);
}

.tile__apps b:nth-child(3) {
  background-color: var(--xh-fg-warning);
}

.tile__apps b:nth-child(4) {
  background-color: var(--xh-fg-info);
}

.tile__feed {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.tile__feed p {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  margin: 0;
}

.tile__feed b {
  flex: none;
  inline-size: var(--xh-space-5);
  block-size: var(--xh-space-5);
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-bg-brand);
}

.tile__feed p:nth-child(2) b {
  background-color: var(--xh-fg-warning);
}

.tile__feed p:nth-child(3) b {
  background-color: var(--xh-fg-success);
}

.tile__feed i {
  flex: 1;
  block-size: var(--xh-space-2);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
}
</style>
