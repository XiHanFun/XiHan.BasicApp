<script setup lang="ts">
import { XhAccordionContent, XhAccordionHeader, XhAccordionItem, XhAccordionRoot, XhAccordionTrigger, XhButton, XhButtonIndicator, XhButtonPrefix, XhCardContent, XhCardHeader, XhCardRoot, XhGridItem, XhGridRoot, XhProgress, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onActivated, onBeforeUnmount, onDeactivated, onMounted, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XTooltip } from '~/components'
import { toast } from '~/composables'
import { errorText } from '../jobs/job'
import {
  activeNetworkInterfaces,
  boardDetails,
  cpuDetails,
  cpuUsage,
  createPolling,
  diskCard,
  formatBitRate,
  gpuDetails,
  healthCard,
  healthSummary,
  memoryDetails,
  memoryUsage,
  networkAddresses,
  networkTraffic,
  overviewItems,
  systemDetails,
  textOrDash,
  usageTone,
  useServerInfo,
} from './server'

defineOptions({ name: 'MaintenanceServerView' })

const { t } = useI18n()
// 两只环形仪表的读屏名取各自卡片标题
const cpuTitleId = useId()
const memTitleId = useId()

const { info, loading, initialized, refresh } = useServerInfo()

const overview = computed(() => overviewItems(t, info.value))
/** 概览角标取最差的健康项；还没取到过数据时不显示 */
const summary = computed(() => (info.value ? healthSummary(t, info.value.health) : null))

/** CPU 使用率由后台采样，启动后第一个采样间隔内读不到（显示 -，不当作 0%） */
const cpuPct = computed(() => cpuUsage(info.value?.processor))
const memPct = computed(() => memoryUsage(info.value?.memory))
const cpuTone = computed(() => usageTone(cpuPct.value))
const memTone = computed(() => usageTone(memPct.value))
const cpuRows = computed(() => cpuDetails(t, info.value))
const memRows = computed(() => memoryDetails(t, info.value?.memory))

const disks = computed(() => (info.value?.disks ?? []).map(disk => diskCard(t, disk)))
/** 显卡按序号展开（刷新后展开的那块不变）；明细只列读得到的项 */
const gpus = computed(() => (info.value?.gpus ?? []).map((gpu, index) => ({
  key: String(index),
  title: gpu.name || `GPU ${index}`,
  rows: gpuDetails(t, gpu),
})))
/** 活跃网卡按网卡 ID 展开 */
const networks = computed(() => activeNetworkInterfaces(info.value?.networkInterfaces ?? []).map(nic => ({
  nic,
  addresses: networkAddresses(t, nic),
  traffic: networkTraffic(t, nic),
})))
const boardRows = computed(() => boardDetails(t, info.value?.board))
const systemRows = computed(() => systemDetails(t, info.value?.runtime))
const health = computed(() => (info.value?.health ?? []).map(item => healthCard(t, item)))

const showSkeleton = computed(() => !initialized.value)

async function fetchData() {
  try {
    await refresh()
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.server.fetch_failed'))
  }
}

// 4.x 每 15 秒刷新一次；6.0 的页面缓存在标签里，切到别的标签即停、切回来先刷一次
const polling = createPolling(() => void fetchData())
let deactivated = false
onMounted(() => {
  void fetchData()
  polling.start()
})
onActivated(() => {
  polling.start()
  if (deactivated) {
    deactivated = false
    void fetchData()
  }
})
onDeactivated(() => {
  deactivated = true
  polling.stop()
})
onBeforeUnmount(polling.stop)
</script>

<template>
  <div class="sv-page">
    <template v-if="showSkeleton">
      <!-- 骨架直接复用真实内容的布局类，底色、列数与断点自然一致 -->
      <div class="sv-banner">
        <div class="sv-banner-head">
          <span class="xh-skeleton-bone" style="inline-size: 6rem; block-size: 0.9375rem" />
          <span class="xh-skeleton-bone" style="inline-size: 6.75rem; block-size: 1.5rem" />
        </div>
        <div class="sv-overview-grid">
          <div v-for="i in 5" :key="`sk-ov-${i}`" class="sv-overview-item">
            <span class="xh-skeleton-bone xh-skeleton-bone--circle" style="inline-size: 2.25rem; block-size: 2.25rem" />
            <div class="sv-overview-body space-y-1">
              <span class="xh-skeleton-bone" style="inline-size: 48%; block-size: 0.75rem" />
              <span class="xh-skeleton-bone" style="inline-size: 86%; block-size: 0.8125rem" />
              <span class="xh-skeleton-bone" style="inline-size: 62%; block-size: 0.6875rem" />
            </div>
          </div>
        </div>
      </div>

      <!-- CPU 与内存各 6 行明细 -->
      <XhGridRoot :cols="{ base: 1, md: 2 }" gap="md">
        <XhGridItem v-for="i in 2" :key="`sk-perf-${i}`">
          <XhCardRoot variant="ghost" class="sv-card">
            <XhCardHeader>
              <span class="xh-skeleton-bone" style="inline-size: 5.5rem; block-size: 0.875rem" />
            </XhCardHeader>
            <XhCardContent>
              <div class="sv-perf">
                <div class="sv-gauge">
                  <span class="xh-skeleton-bone xh-skeleton-bone--circle" style="inline-size: 10rem; block-size: 10rem" />
                </div>
                <div class="sv-details">
                  <div v-for="r in 6" :key="r" class="sv-detail-row">
                    <span class="xh-skeleton-bone" style="inline-size: 3.5rem; block-size: 0.8125rem" />
                    <span class="xh-skeleton-bone" style="inline-size: 4rem; block-size: 0.8125rem" />
                  </div>
                </div>
              </div>
            </XhCardContent>
          </XhCardRoot>
        </XhGridItem>
      </XhGridRoot>

      <!-- 磁盘：3 列，每格 标题行 + 进度条 + 统计行 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <span class="xh-skeleton-bone" style="inline-size: 5.5rem; block-size: 0.875rem" />
        </XhCardHeader>
        <XhCardContent>
          <XhGridRoot :cols="{ base: 1, sm: 2, lg: 3 }" gap="md">
            <XhGridItem v-for="i in 3" :key="`sk-disk-${i}`">
              <div class="sv-disk-item">
                <div class="sv-disk-head">
                  <span class="xh-skeleton-bone" style="inline-size: 4.5rem; block-size: 0.875rem" />
                  <span class="xh-skeleton-bone" style="inline-size: 5.5rem; block-size: 0.875rem" />
                </div>
                <span class="xh-skeleton-bone" style="block-size: 0.375rem" />
                <div class="sv-disk-stats">
                  <span v-for="n in 3" :key="n" class="xh-skeleton-bone" style="inline-size: 4rem; block-size: 0.75rem" />
                </div>
              </div>
            </XhGridItem>
          </XhGridRoot>
        </XhCardContent>
      </XhCardRoot>

      <!-- 显卡 2 行、网络 3 行：折叠列表画成等高的触发行 -->
      <XhCardRoot v-for="(rows, i) in [2, 3]" :key="`sk-acc-${i}`" variant="ghost" class="sv-card">
        <XhCardHeader>
          <span class="xh-skeleton-bone" style="inline-size: 5.5rem; block-size: 0.875rem" />
        </XhCardHeader>
        <XhCardContent>
          <div class="space-y-1">
            <div v-for="r in rows" :key="r" class="sv-collapse-title sv-skeleton-trigger">
              <span class="xh-skeleton-bone xh-skeleton-bone--circle" style="inline-size: 0.875rem; block-size: 0.875rem" />
              <span class="xh-skeleton-bone" style="inline-size: 11.25rem; block-size: 0.8125rem" />
              <span class="xh-skeleton-bone" style="inline-size: 3rem; block-size: 1.125rem" />
            </div>
          </div>
        </XhCardContent>
      </XhCardRoot>

      <!-- 主板 9 项、系统信息 20 项：同一种格子，只是数量与网格类不同 -->
      <XhCardRoot v-for="g in [{ cls: 'sv-board-grid', n: 9 }, { cls: 'sv-sys-grid', n: 20 }]" :key="g.cls" variant="ghost" class="sv-card">
        <XhCardHeader>
          <span class="xh-skeleton-bone" style="inline-size: 5.5rem; block-size: 0.875rem" />
        </XhCardHeader>
        <XhCardContent>
          <div :class="g.cls">
            <div v-for="i in g.n" :key="i" class="sv-sys-item">
              <span class="xh-skeleton-bone" style="inline-size: 1.625rem; block-size: 1.625rem; border-radius: var(--xh-shape-control)" />
              <span class="xh-skeleton-bone" style="inline-size: 3.5rem; block-size: 0.75rem" />
              <span class="xh-skeleton-bone" style="inline-size: 6rem; block-size: 0.8125rem; margin-inline-start: auto" />
            </div>
          </div>
        </XhCardContent>
      </XhCardRoot>
    </template>

    <template v-else>
      <!-- 系统概览 -->
      <div class="sv-banner">
        <div class="sv-banner-head">
          <div class="sv-banner-title">
            <Icon icon="lucide:server" width="18" />
            <span>{{ t('maintenance.server.system_overview') }}</span>
          </div>
          <div class="sv-banner-actions">
            <XhTagRoot v-if="summary" variant="subtle" size="sm" :tone="summary.tone">
              <Icon icon="lucide:activity" width="12" />
              <XhTagLabel>{{ summary.label }}</XhTagLabel>
            </XhTagRoot>
            <XTooltip :content="t('common.actions.refresh')">
              <XhButton
                icon-only
                size="sm"
                variant="ghost"
                :aria-label="t('common.actions.refresh')"
                :loading="loading"
                class="sv-refresh-btn"
                @click="fetchData"
              >
                <XhButtonIndicator />
                <XhButtonPrefix><Icon icon="lucide:refresh-cw" width="14" /></XhButtonPrefix>
              </XhButton>
            </XTooltip>
          </div>
        </div>
        <div class="sv-overview-grid">
          <div v-for="item in overview" :key="item.icon" class="sv-overview-item">
            <div class="sv-overview-icon">
              <Icon :icon="item.icon" width="18" />
            </div>
            <div class="sv-overview-body">
              <div class="sv-overview-label">
                {{ item.title }}
              </div>
              <div class="sv-overview-value" :title="item.value">
                {{ item.value }}
              </div>
              <div class="sv-overview-sub" :title="item.sub">
                {{ item.sub }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- CPU & 内存 -->
      <XhGridRoot :cols="{ base: 1, md: 2 }" gap="md">
        <XhGridItem>
          <XhCardRoot variant="ghost" class="sv-card">
            <XhCardHeader>
              <div class="sv-card-header">
                <Icon icon="lucide:cpu" width="16" />
                <span :id="cpuTitleId">{{ t('maintenance.server.cpu_info') }}</span>
              </div>
            </XhCardHeader>
            <XhCardContent>
              <div class="sv-perf">
                <div class="sv-gauge">
                  <XhProgress
                    variant="circle"
                    semantics="meter"
                    size="lg"
                    :aria-labelledby="cpuTitleId"
                    :value="cpuPct ?? 0"
                    :value-text="cpuPct === null ? t('common.no_data') : undefined"
                    :tone="cpuTone"
                    :stroke-width="8"
                  >
                    <div class="sv-gauge-inner">
                      <div class="sv-gauge-pct" :class="`sv-tone-${cpuTone}`">
                        <template v-if="cpuPct !== null">
                          {{ cpuPct }}<small>%</small>
                        </template>
                        <template v-else>
                          -
                        </template>
                      </div>
                      <div class="sv-gauge-label">
                        {{ t('maintenance.server.usage_rate') }}
                      </div>
                    </div>
                  </XhProgress>
                </div>
                <div class="sv-details">
                  <div v-for="d in cpuRows" :key="d.label" class="sv-detail-row">
                    <span class="sv-detail-label">
                      {{ d.label }}
                    </span>
                    <span class="sv-detail-value" :title="d.value">
                      {{ d.value }}
                    </span>
                  </div>
                </div>
              </div>
            </XhCardContent>
          </XhCardRoot>
        </XhGridItem>
        <XhGridItem>
          <XhCardRoot variant="ghost" class="sv-card">
            <XhCardHeader>
              <div class="sv-card-header">
                <Icon icon="lucide:memory-stick" width="16" />
                <span :id="memTitleId">{{ t('maintenance.server.memory_info') }}</span>
              </div>
            </XhCardHeader>
            <XhCardContent>
              <div class="sv-perf">
                <div class="sv-gauge">
                  <XhProgress
                    variant="circle"
                    semantics="meter"
                    size="lg"
                    :aria-labelledby="memTitleId"
                    :value="memPct ?? 0"
                    :value-text="memPct === null ? t('common.no_data') : undefined"
                    :tone="memTone"
                    :stroke-width="8"
                  >
                    <div class="sv-gauge-inner">
                      <div class="sv-gauge-pct" :class="`sv-tone-${memTone}`">
                        <template v-if="memPct !== null">
                          {{ memPct }}<small>%</small>
                        </template>
                        <template v-else>
                          -
                        </template>
                      </div>
                      <div class="sv-gauge-label">
                        {{ t('maintenance.server.usage_rate') }}
                      </div>
                    </div>
                  </XhProgress>
                </div>
                <div class="sv-details">
                  <div v-for="d in memRows" :key="d.label" class="sv-detail-row">
                    <span class="sv-detail-label">
                      {{ d.label }}
                    </span>
                    <span class="sv-detail-value" :title="d.value">
                      {{ d.value }}
                    </span>
                  </div>
                </div>
              </div>
            </XhCardContent>
          </XhCardRoot>
        </XhGridItem>
      </XhGridRoot>

      <!-- 磁盘 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <div class="sv-card-header">
            <Icon icon="lucide:hard-drive" width="16" />
            <span>{{ t('maintenance.server.disk_info') }}</span>
          </div>
        </XhCardHeader>
        <XhCardContent>
          <div v-if="!disks.length" class="sv-empty">
            {{ t('common.no_data') }}
          </div>
          <XhGridRoot v-else :cols="{ base: 1, sm: 2, lg: 3 }" gap="md">
            <XhGridItem v-for="disk in disks" :key="disk.key">
              <div class="sv-disk-item">
                <div class="sv-disk-head">
                  <span class="sv-disk-name" :title="disk.hint">
                    {{ disk.name }}
                  </span>
                  <div class="sv-disk-head-right">
                    <XhTagRoot variant="subtle" size="sm">
                      <XhTagLabel>
                        {{ disk.tag }}
                      </XhTagLabel>
                    </XhTagRoot>
                    <span class="sv-disk-pct" :class="`sv-tone-${disk.tone}`">
                      {{ disk.percentText }}
                    </span>
                  </div>
                </div>
                <XhProgress
                  variant="line"
                  semantics="meter"
                  :aria-label="disk.name"
                  :value="disk.percent ?? 0"
                  :value-text="disk.percent === null ? t('common.no_data') : undefined"
                  :tone="disk.tone"
                />
                <div class="sv-disk-stats">
                  <span>{{ t('maintenance.server.used_space', { value: disk.used }) }}</span>
                  <span>{{ t('maintenance.server.total_space', { value: disk.total }) }}</span>
                  <span>{{ t('maintenance.server.free_space', { value: disk.available }) }}</span>
                </div>
              </div>
            </XhGridItem>
          </XhGridRoot>
        </XhCardContent>
      </XhCardRoot>

      <!-- 显卡 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <div class="sv-card-header">
            <Icon icon="lucide:monitor" width="16" />
            <span>{{ t('maintenance.server.gpu_info') }}</span>
            <XhTagRoot v-if="gpus.length" variant="subtle" size="sm" tone="info" class="sv-pkg-count">
              <XhTagLabel>
                {{ t('maintenance.server.count_unit', { count: gpus.length }) }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </XhCardHeader>
        <XhCardContent>
          <div v-if="!gpus.length" class="sv-empty">
            {{ t('common.no_data') }}
          </div>
          <XhAccordionRoot v-else collapsible>
            <XhAccordionItem v-for="gpu in gpus" :key="gpu.key" :value="gpu.key">
              <XhAccordionHeader>
                <XhAccordionTrigger>
                  <div class="sv-collapse-title">
                    <Icon icon="lucide:monitor" width="14" class="sv-collapse-title-icon" />
                    <span class="sv-collapse-title-text">
                      {{ gpu.title }}
                    </span>
                  </div>
                </XhAccordionTrigger>
              </XhAccordionHeader>
              <XhAccordionContent>
                <div class="sv-collapse-body">
                  <div v-for="row in gpu.rows" :key="row.label" class="sv-kv">
                    <span class="sv-kv-label">{{ row.label }}</span>
                    <span class="sv-kv-value" :title="row.value">{{ row.value }}</span>
                  </div>
                  <div v-if="!gpu.rows.length" class="sv-kv">
                    <span class="sv-kv-label">{{ t('common.no_data') }}</span>
                  </div>
                </div>
              </XhAccordionContent>
            </XhAccordionItem>
          </XhAccordionRoot>
        </XhCardContent>
      </XhCardRoot>

      <!-- 网络信息 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <div class="sv-card-header">
            <Icon icon="lucide:network" width="16" />
            <span>{{ t('maintenance.server.network_info') }}</span>
            <XhTagRoot v-if="networks.length" variant="subtle" size="sm" tone="info" class="sv-pkg-count">
              <XhTagLabel>
                {{ t('maintenance.server.active_count', { count: networks.length }) }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </XhCardHeader>
        <XhCardContent>
          <div v-if="!networks.length" class="sv-empty">
            {{ t('common.no_data') }}
          </div>
          <XhAccordionRoot v-else collapsible>
            <XhAccordionItem v-for="{ nic, addresses, traffic } in networks" :key="nic.id" :value="nic.id">
              <XhAccordionHeader>
                <XhAccordionTrigger>
                  <div class="sv-collapse-title">
                    <Icon
                      icon="lucide:wifi"
                      width="14"
                      class="sv-collapse-title-icon sv-collapse-title-icon--success"
                    />
                    <span class="sv-collapse-title-text">
                      {{ nic.name }}
                    </span>
                    <XhTagRoot variant="subtle" size="sm" class="sv-collapse-title-tag">
                      <XhTagLabel>
                        {{ nic.type }}
                      </XhTagLabel>
                    </XhTagRoot>
                  </div>
                </XhAccordionTrigger>
              </XhAccordionHeader>
              <XhAccordionContent>
                <XhGridRoot :cols="{ base: 1, md: 2 }" gap="sm">
                  <XhGridItem>
                    <div class="sv-collapse-body">
                      <div class="sv-kv">
                        <span class="sv-kv-label">{{ t('maintenance.server.description') }}</span>
                        <span class="sv-kv-value" :title="textOrDash(nic.description)">{{ textOrDash(nic.description) }}</span>
                      </div>
                      <div class="sv-kv">
                        <span class="sv-kv-label">{{ t('maintenance.server.physical_address') }}</span>
                        <span class="sv-kv-value">{{ textOrDash(nic.physicalAddress) }}</span>
                      </div>
                      <div class="sv-kv">
                        <span class="sv-kv-label">{{ t('maintenance.server.speed') }}</span>
                        <span class="sv-kv-value">{{ formatBitRate(nic.speedBitsPerSecond) }}</span>
                      </div>
                      <div v-for="group in addresses" :key="group.label" class="sv-kv">
                        <span class="sv-kv-label">{{ group.label }}</span>
                        <span class="sv-kv-tags">
                          <XhTagRoot
                            v-for="address in group.values"
                            :key="address"
                            variant="subtle"
                            size="sm"
                          ><XhTagLabel>
                            {{ address }}
                          </XhTagLabel></XhTagRoot>
                        </span>
                      </div>
                    </div>
                  </XhGridItem>
                  <XhGridItem>
                    <div class="sv-collapse-body">
                      <div v-for="row in traffic" :key="row.label" class="sv-kv">
                        <span class="sv-kv-label">{{ row.label }}</span>
                        <span class="sv-kv-value">{{ row.value }}</span>
                      </div>
                    </div>
                  </XhGridItem>
                </XhGridRoot>
              </XhAccordionContent>
            </XhAccordionItem>
          </XhAccordionRoot>
        </XhCardContent>
      </XhCardRoot>

      <!-- 主板信息 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <div class="sv-card-header">
            <Icon icon="lucide:circuit-board" width="16" />
            <span>{{ t('maintenance.server.board_info') }}</span>
          </div>
        </XhCardHeader>
        <XhCardContent>
          <div class="sv-board-grid">
            <div v-for="d in boardRows" :key="d.label" class="sv-sys-item">
              <div class="sv-sys-icon">
                <Icon :icon="d.icon" width="14" />
              </div>
              <span class="sv-sys-label">
                {{ d.label }}
              </span>
              <span class="sv-sys-value" :title="d.value">
                {{ d.value }}
              </span>
            </div>
          </div>
        </XhCardContent>
      </XhCardRoot>

      <!-- 系统信息 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <div class="sv-card-header">
            <Icon icon="lucide:settings" width="16" />
            <span>{{ t('maintenance.server.system_info') }}</span>
          </div>
        </XhCardHeader>
        <XhCardContent>
          <div class="sv-sys-grid">
            <div v-for="d in systemRows" :key="d.label" class="sv-sys-item">
              <div class="sv-sys-icon">
                <Icon :icon="d.icon" width="14" />
              </div>
              <span class="sv-sys-label">
                {{ d.label }}
              </span>
              <span class="sv-sys-value" :title="d.value">
                {{ d.value }}
              </span>
            </div>
          </div>
        </XhCardContent>
      </XhCardRoot>

      <!-- 健康检查：健康项由各 Provider 自带，这里只展示 -->
      <XhCardRoot variant="ghost" class="sv-card">
        <XhCardHeader>
          <div class="sv-card-header">
            <Icon icon="lucide:heart-pulse" width="16" />
            <span>{{ t('maintenance.server.health_info') }}</span>
            <XhTagRoot v-if="health.length" variant="subtle" size="sm" tone="info" class="sv-pkg-count">
              <XhTagLabel>
                {{ t('maintenance.server.count_unit', { count: health.length }) }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </XhCardHeader>
        <XhCardContent>
          <div v-if="!health.length" class="sv-empty">
            {{ t('common.no_data') }}
          </div>
          <XhGridRoot v-else :cols="{ base: 1, md: 2, lg: 3 }" gap="md">
            <XhGridItem v-for="(item, index) in health" :key="`${index}:${item.name}`">
              <div class="sv-health-item">
                <div class="sv-health-head">
                  <span class="sv-health-name" :title="item.name">
                    {{ item.name }}
                  </span>
                  <XhTagRoot variant="subtle" size="sm" :tone="item.tone">
                    <XhTagLabel>
                      {{ item.stateLabel }}
                    </XhTagLabel>
                  </XhTagRoot>
                </div>
                <div class="sv-health-meta">
                  <span>{{ item.duration }}</span>
                  <span>{{ item.checkedAt }}</span>
                </div>
                <p v-if="item.description" class="sv-health-text">
                  {{ item.description }}
                </p>
                <p v-if="item.errorCode || item.errorMessage" class="sv-health-text sv-health-error">
                  <code v-if="item.errorCode" class="sv-health-code">{{ item.errorCode }}</code>
                  <span v-if="item.errorMessage">{{ item.errorMessage }}</span>
                </p>
                <div v-if="item.data.length" class="sv-collapse-body sv-health-data">
                  <div v-for="row in item.data" :key="row.label" class="sv-kv">
                    <span class="sv-kv-label">{{ row.label }}</span>
                    <span class="sv-kv-value" :title="row.value">{{ row.value }}</span>
                  </div>
                </div>
              </div>
            </XhGridItem>
          </XhGridRoot>
        </XhCardContent>
      </XhCardRoot>
    </template>
  </div>
</template>

<style scoped>
.sv-page {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  padding: var(--xh-space-3);
}

/* ========== Banner ========== */
.sv-banner {
  padding: var(--xh-space-4) var(--xh-space-5);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-subtle);
  border: 1px solid var(--xh-border-default);
}

.sv-banner-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--xh-space-3);
}

.sv-banner-title {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.sv-banner-actions {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1_5);
}

.sv-refresh-btn {
  color: var(--xh-fg-muted);
}

.sv-overview-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: var(--xh-space-2_5);
}

.sv-overview-item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  padding: var(--xh-space-3);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  min-width: 0;
  transition: border-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.sv-overview-item:hover {
  border-color: color-mix(in srgb, var(--xh-fg-brand) 30%, var(--xh-border-default));
}

.sv-overview-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  inline-size: 2.25rem;
  block-size: 2.25rem;
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
}

.sv-overview-body {
  flex: 1;
  min-width: 0;
}

.sv-overview-label {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  line-height: var(--xh-leading-normal);
}

.sv-overview-value {
  overflow: hidden;
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
  line-height: var(--xh-leading-normal);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sv-overview-sub {
  overflow: hidden;
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-subtle);
  line-height: var(--xh-leading-normal);
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ========== Card header ========== */
.sv-card-header {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.sv-pkg-count {
  margin-inline-start: auto;
  font-variant-numeric: tabular-nums;
}

/* ========== Performance (CPU/MEM) ========== */
.sv-perf {
  display: flex;
  align-items: center;
  gap: var(--xh-space-4);
}

/* 环形仪表取组件库的 lg 直径（10rem，即 4.x 的 160px） */
.sv-gauge {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  inline-size: 10rem;
  block-size: 10rem;
}

.sv-gauge-inner {
  text-align: center;
  white-space: nowrap;
}

.sv-gauge-pct {
  font-size: var(--xh-font-size-2xl);
  font-weight: var(--xh-font-weight-bold);
  line-height: var(--xh-leading-none);
  letter-spacing: -0.02em;
  font-variant-numeric: tabular-nums;
  transition: color var(--xh-motion-duration-slide) var(--xh-motion-ease-continuous);
}

.sv-gauge-pct small {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-medium);
  opacity: 0.65;
}

.sv-gauge-label {
  margin-top: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

/* 使用率的字色与仪表的语气同族（4.x：低于 50 绿、低于 80 黄、否则红），读不到为次级色 */
.sv-tone-success {
  color: var(--xh-fg-success);
}

.sv-tone-warning {
  color: var(--xh-fg-warning);
}

.sv-tone-danger {
  color: var(--xh-fg-danger);
}

.sv-tone-neutral {
  color: var(--xh-fg-muted);
}

.sv-details {
  flex: 1;
  min-width: 0;
  max-width: 11.25rem;
  margin-inline: auto;
}

.sv-detail-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-1_5) 0;
  font-size: var(--xh-font-size-sm);
  border-bottom: 1px dashed var(--xh-border-subtle);
}

.sv-detail-row:first-child {
  padding-top: 0;
}

.sv-detail-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.sv-detail-label {
  flex-shrink: 0;
  color: var(--xh-fg-muted);
}

.sv-detail-value {
  min-width: 0;
  overflow: hidden;
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-default);
  font-variant-numeric: tabular-nums;
  text-align: right;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ========== Disk ========== */
.sv-disk-item {
  padding: var(--xh-space-3);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-subtle);
  transition: background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.sv-disk-item:hover {
  background: var(--xh-bg-subtle-hover);
}

.sv-disk-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
  margin-bottom: var(--xh-space-2);
}

.sv-disk-head-right {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: var(--xh-space-2);
}

.sv-disk-name {
  min-width: 0;
  overflow: hidden;
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sv-disk-pct {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-bold);
  font-variant-numeric: tabular-nums;
}

.sv-disk-stats {
  display: flex;
  justify-content: space-between;
  gap: var(--xh-space-2);
  margin-top: var(--xh-space-1_5);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

/* ========== Collapse shared (GPU / Network) ========== */
.sv-collapse-title {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  min-width: 0;
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-medium);
}

.sv-skeleton-trigger {
  padding-block: var(--xh-space-2_5);
}

.sv-collapse-title-icon {
  flex-shrink: 0;
  color: var(--xh-fg-brand);
}

.sv-collapse-title-icon--success {
  color: var(--xh-fg-success);
}

.sv-collapse-title-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sv-collapse-title-tag {
  margin-inline-start: var(--xh-space-2);
}

.sv-collapse-body {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-0_5);
}

.sv-kv {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--xh-space-3);
  padding: var(--xh-space-1) 0;
  font-size: var(--xh-font-size-xs);
  border-bottom: 1px dashed var(--xh-border-subtle);
}

.sv-kv:last-child {
  border-bottom: none;
}

.sv-kv-label {
  flex-shrink: 0;
  color: var(--xh-fg-muted);
}

.sv-kv-value {
  min-width: 0;
  overflow: hidden;
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-default);
  text-align: right;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 地址可能有好几个（IPv6 尤其长）：标签换行排开，不截断 */
.sv-kv-tags {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: var(--xh-space-1);
  min-width: 0;
}

/* ========== Board / System Info items ========== */
.sv-board-grid,
.sv-sys-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(16.25rem, 1fr));
  gap: var(--xh-space-1_5);
}

.sv-sys-item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-1_5) var(--xh-space-2_5);
  overflow: hidden;
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-subtle);
  transition: background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.sv-sys-item:hover {
  background: var(--xh-bg-subtle-hover);
}

.sv-sys-icon {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  inline-size: 1.625rem;
  block-size: 1.625rem;
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
}

.sv-sys-label {
  flex-shrink: 0;
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  white-space: nowrap;
}

.sv-sys-value {
  max-width: 55%;
  margin-inline-start: auto;
  overflow: hidden;
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-default);
  text-align: right;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ========== Health ========== */
.sv-health-item {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
  block-size: 100%;
  padding: var(--xh-space-3);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-subtle);
}

.sv-health-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
}

.sv-health-name {
  min-width: 0;
  overflow: hidden;
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sv-health-meta {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: var(--xh-space-1) var(--xh-space-3);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  font-variant-numeric: tabular-nums;
}

.sv-health-text {
  margin: 0;
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  word-break: break-word;
}

.sv-health-error {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1_5);
  color: var(--xh-fg-danger);
}

.sv-health-code {
  font-family: var(--xh-font-family-mono);
}

.sv-health-data {
  padding-top: var(--xh-space-1);
  border-top: 1px dashed var(--xh-border-subtle);
}

/* ========== Empty state ========== */
.sv-empty {
  padding: var(--xh-space-5) 0;
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-subtle);
  text-align: center;
}

/* ========== Responsive ========== */
@media (max-width: 80rem) {
  .sv-overview-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

/* 两栏并排变窄时仪表缩一档（4.x 同为 140px） */
@media (max-width: 56.25rem) {
  .sv-overview-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .sv-gauge {
    --xh-progress-size: 8.75rem;

    inline-size: 8.75rem;
    block-size: 8.75rem;
  }

  .sv-gauge-pct {
    font-size: var(--xh-font-size-xl);
  }

  .sv-gauge-pct small {
    font-size: var(--xh-font-size-xs);
  }
}

/* 单栏时仪表在上、明细在下，仪表回到 lg 直径 */
@media (max-width: 40rem) {
  .sv-banner {
    padding: var(--xh-space-3);
  }

  .sv-overview-grid {
    grid-template-columns: minmax(0, 1fr);
    gap: var(--xh-space-2);
  }

  .sv-perf {
    flex-direction: column;
    align-items: stretch;
  }

  .sv-gauge {
    --xh-progress-size: 10rem;

    inline-size: 10rem;
    block-size: 10rem;
    margin: 0 auto;
  }

  .sv-gauge-pct {
    font-size: var(--xh-font-size-2xl);
  }

  .sv-gauge-pct small {
    font-size: var(--xh-font-size-sm);
  }

  .sv-details {
    max-width: none;
    margin-inline: 0;
  }

  .sv-sys-grid,
  .sv-board-grid {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
