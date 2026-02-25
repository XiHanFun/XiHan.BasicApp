<script lang="ts" setup>
import { Icon } from '@iconify/vue'
import { NCard, NGrid, NGridItem, NIcon, NNumberAnimation, NSelect } from 'naive-ui'
import { ref } from 'vue'

defineOptions({ name: 'AnalyticsPage' })

const period = ref('7d')
const periodOptions = [
  { label: '近7天', value: '7d' },
  { label: '近30天', value: '30d' },
  { label: '近90天', value: '90d' },
]

const summaryCards = ref([
  { label: '页面访问量 (PV)', value: 128450, icon: 'lucide:eye', color: '#18a058' },
  { label: '独立访客 (UV)', value: 36820, icon: 'lucide:users', color: '#2080f0' },
  { label: '平均访问时长', value: '3:42', icon: 'lucide:clock', color: '#f0a020', isText: true },
  { label: '跳出率', value: '38.2%', icon: 'lucide:log-out', color: '#d03050', isText: true },
])
</script>

<template>
  <div class="space-y-4">
    <!-- 页面标题 -->
    <div class="flex items-center justify-between">
      <h2 class="text-lg font-semibold">数据分析</h2>
      <NSelect v-model:value="period" :options="periodOptions" style="width: 120px" />
    </div>

    <!-- 汇总数据 -->
    <NGrid :x-gap="16" :y-gap="16" :cols="4" responsive="screen" :item-responsive="true">
      <NGridItem v-for="card in summaryCards" :key="card.label" span="4 s:2 m:1">
        <NCard :bordered="false" class="hover-card">
          <div class="flex items-center gap-3">
            <div
              class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
              :style="{ backgroundColor: `${card.color}20` }"
            >
              <NIcon size="20" :style="{ color: card.color }">
                <Icon :icon="card.icon" />
              </NIcon>
            </div>
            <div>
              <p class="text-xs text-gray-500">
                {{ card.label }}
              </p>
              <p class="mt-0.5 text-xl font-bold">
                <template v-if="card.isText">
                  {{ card.value }}
                </template>
                <NNumberAnimation v-else :from="0" :to="card.value as number" :duration="1200" />
              </p>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>

    <!-- 图表区域占位 -->
    <NGrid :x-gap="16" :y-gap="16" :cols="2" responsive="screen" :item-responsive="true">
      <NGridItem span="2 m:1">
        <NCard title="访问趋势" :bordered="false">
          <div class="flex h-48 items-center justify-center text-gray-400">
            <div class="text-center">
              <NIcon size="48" class="mb-2 opacity-30">
                <Icon icon="lucide:bar-chart-2" />
              </NIcon>
              <p class="text-sm">可集成 ECharts 等图表库</p>
            </div>
          </div>
        </NCard>
      </NGridItem>
      <NGridItem span="2 m:1">
        <NCard title="访问来源" :bordered="false">
          <div class="flex h-48 items-center justify-center text-gray-400">
            <div class="text-center">
              <NIcon size="48" class="mb-2 opacity-30">
                <Icon icon="lucide:pie-chart" />
              </NIcon>
              <p class="text-sm">可集成 ECharts 等图表库</p>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>
