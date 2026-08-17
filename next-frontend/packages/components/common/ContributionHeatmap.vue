<script setup lang="ts">
import { computed } from 'vue'

/**
 * 贡献热力图：按周分列、按星期分行的一年活跃度方格。
 *
 * 组件库在能力缺口评审里明确判定不做热力图，全站也只有个人中心这一处用，
 * 因此在应用侧自绘——它本质上就是一张 CSS 网格，不需要图表库。
 */
defineOptions({ name: 'ContributionHeatmap' })

const props = withDefaults(defineProps<{
  /** 逐日数据，按时间升序；缺的日子当作 0 */
  data: ReadonlyArray<{ timestamp: number, value: number }>
  /** 一周从周几起，0 = 周日 */
  firstDayOfWeek?: number
  /** 每格边长（px） */
  cellSize?: number
}>(), {
  firstDayOfWeek: 0,
  cellSize: 11,
})

const WEEK_LABELS = ['日', '一', '二', '三', '四', '五', '六']

/** 值域分五档：0 与四级由高到低。用分位而不是固定阈值，稀疏数据也能拉开层次 */
const thresholds = computed(() => {
  const positives = props.data.map(d => d.value).filter(v => v > 0).sort((a, b) => a - b)
  if (positives.length === 0) {
    return [1, 2, 3, 4]
  }
  const at = (ratio: number) => positives[Math.min(positives.length - 1, Math.floor(positives.length * ratio))] ?? 1
  return [at(0.25), at(0.5), at(0.75), at(0.95)]
})

function levelOf(value: number): number {
  if (value <= 0) {
    return 0
  }
  const [t1, t2, t3] = thresholds.value
  if (value <= (t1 ?? 1)) {
    return 1
  }
  if (value <= (t2 ?? 2)) {
    return 2
  }
  if (value <= (t3 ?? 3)) {
    return 3
  }
  return 4
}

interface Cell {
  key: string
  date: Date
  value: number
  level: number
}

/**
 * 按周切列。首列前面补空格，让每一行都落在固定的星期上；
 * 不补的话整张图会随起始日错位一格。
 */
const columns = computed<Array<Array<Cell | null>>>(() => {
  if (props.data.length === 0) {
    return []
  }
  const cells: Cell[] = props.data.map((point) => {
    const date = new Date(point.timestamp)
    return {
      key: String(point.timestamp),
      date,
      value: point.value,
      level: levelOf(point.value),
    }
  })

  const out: Array<Array<Cell | null>> = []
  let current: Array<Cell | null> = []
  const offset = (cells[0]!.date.getDay() - props.firstDayOfWeek + 7) % 7
  for (let i = 0; i < offset; i++) {
    current.push(null)
  }
  for (const cell of cells) {
    current.push(cell)
    if (current.length === 7) {
      out.push(current)
      current = []
    }
  }
  if (current.length > 0) {
    while (current.length < 7) {
      current.push(null)
    }
    out.push(current)
  }
  return out
})

/** 月份标注：某一列里出现该月 1 号就在这列上打标 */
const monthLabels = computed(() =>
  columns.value.map((week) => {
    const first = week.find(cell => cell && cell.date.getDate() <= 7)
    return first ? `${first.date.getMonth() + 1}月` : ''
  }),
)

/** 行首的星期标注，跟着 firstDayOfWeek 轮转 */
const weekLabels = computed(() =>
  Array.from({ length: 7 }, (_, i) => WEEK_LABELS[(i + props.firstDayOfWeek) % 7] ?? ''),
)

function titleOf(cell: Cell): string {
  const d = cell.date
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}：${cell.value}`
}
</script>

<template>
  <div class="heatmap" :style="{ '--heatmap-cell': `${cellSize}px` }">
    <div class="heatmap__months">
      <span v-for="(label, i) in monthLabels" :key="i" class="heatmap__month">{{ label }}</span>
    </div>
    <div class="heatmap__body">
      <div class="heatmap__weekdays">
        <span v-for="(label, i) in weekLabels" :key="i" class="heatmap__weekday">
          <!-- 只标奇数行，七行全标会挤成一团 -->
          {{ i % 2 === 1 ? label : '' }}
        </span>
      </div>
      <div class="heatmap__grid">
        <div v-for="(week, wi) in columns" :key="wi" class="heatmap__week">
          <span
            v-for="(cell, di) in week"
            :key="cell?.key ?? `${wi}-${di}`"
            class="heatmap__cell"
            :data-level="cell ? cell.level : undefined"
            :data-empty="cell ? undefined : ''"
            :title="cell ? titleOf(cell) : undefined"
          />
        </div>
      </div>
    </div>
    <div class="heatmap__legend">
      <span class="heatmap__legend-text">少</span>
      <span v-for="level in [0, 1, 2, 3, 4]" :key="level" class="heatmap__cell" :data-level="level" />
      <span class="heatmap__legend-text">多</span>
    </div>
  </div>
</template>

<style scoped>
.heatmap {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 10px;
  color: var(--xh-fg-muted);
}

.heatmap__body {
  display: flex;
  gap: 4px;
}

.heatmap__weekdays {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: none;
}

.heatmap__weekday {
  block-size: var(--heatmap-cell);
  line-height: var(--heatmap-cell);
}

.heatmap__grid {
  display: flex;
  gap: 2px;
}

.heatmap__week {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.heatmap__months {
  display: flex;
  gap: 2px;
  /* 与网格左侧的星期栏对齐 */
  padding-inline-start: calc(var(--heatmap-cell) + 4px);
}

.heatmap__month {
  inline-size: var(--heatmap-cell);
  white-space: nowrap;
}

.heatmap__cell {
  inline-size: var(--heatmap-cell);
  block-size: var(--heatmap-cell);
  border-radius: 2px;
  background: var(--xh-bg-subtle);
}

/* 四档由浅到深，取当前主色；0 档留作底色 */
.heatmap__cell[data-level='1'] {
  background: hsl(var(--primary) / 25%);
}

.heatmap__cell[data-level='2'] {
  background: hsl(var(--primary) / 45%);
}

.heatmap__cell[data-level='3'] {
  background: hsl(var(--primary) / 70%);
}

.heatmap__cell[data-level='4'] {
  background: hsl(var(--primary));
}

/* 补位的空格不画底，避免首尾多出一截方块 */
.heatmap__cell[data-empty] {
  background: transparent;
}

.heatmap__legend {
  display: flex;
  gap: 3px;
  align-items: center;
  align-self: flex-end;
}
</style>
