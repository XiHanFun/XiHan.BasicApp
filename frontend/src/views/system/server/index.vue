<script setup lang="ts">
import type {
  SysBoardInfo,
  SysCpuInfo,
  SysDiskInfo,
  SysGpuInfo,
  SysMemoryInfo,
  SysNetworkInfo,
  SysRuntimeInfo,
} from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NCollapse,
  NCollapseItem,
  NGrid,
  NGridItem,
  NProgress,
  NSkeleton,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import {
  getBoardInfoApi,
  getCpuInfoApi,
  getDiskInfoApi,
  getGpuInfoApi,
  getMemoryInfoApi,
  getNetworkInfoApi,
  getRuntimeInfoApi,
} from '@/api'

defineOptions({ name: 'SystemServerPage' })

const message = useMessage()
const loading = ref(false)
const initialized = ref(false)
let timer: ReturnType<typeof setInterval> | null = null

const runtimeInfo = ref<SysRuntimeInfo | null>(null)
const cpuInfo = ref<SysCpuInfo | null>(null)
const memoryInfo = ref<SysMemoryInfo | null>(null)
const diskInfos = ref<SysDiskInfo[]>([])
const networkInfos = ref<SysNetworkInfo[]>([])
const boardInfo = ref<SysBoardInfo | null>(null)
const gpuInfos = ref<SysGpuInfo[]>([])

function fmtBytes(bytes: unknown) {
  const v = Number(bytes)
  if (!Number.isFinite(v) || v <= 0) return '-'
  const u = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(Math.floor(Math.log(v) / Math.log(1024)), u.length - 1)
  return `${(v / 1024 ** i).toFixed(i === 0 ? 0 : 2)} ${u[i]}`
}

function fmtUptime(raw: unknown) {
  const s = String(raw ?? '')
  const dm = s.match(/^(\d+)\.(\d+):(\d+):(\d+)/)
  if (dm) return `${dm[1]}天 ${dm[2]}时${dm[3]}分${dm[4]}秒`
  const hm = s.match(/^(\d+):(\d+):(\d+)/)
  if (hm) return `${hm[1]}时${hm[2]}分${hm[3].split('.')[0]}秒`
  return s || '-'
}

function usageColor(pct: number) {
  if (pct < 50) return 'var(--color-success)'
  if (pct < 80) return 'var(--color-warning)'
  return 'var(--color-error)'
}

function usagePct(used: number, total: number) {
  if (total <= 0) return 0
  return Math.round((used / total) * 1000) / 10
}

const rt = computed(() => runtimeInfo.value)
const cpu = computed(() => cpuInfo.value)
const mem = computed(() => memoryInfo.value)
const cpuPct = computed(() => cpu.value?.usagePercentage ?? 0)
const memPct = computed(() => mem.value?.usagePercentage ?? 0)

const overviewItems = computed(() => [
  {
    icon: 'lucide:cpu',
    title: '处理器',
    value: cpu.value?.processorName ?? '-',
    sub: `${cpu.value?.physicalCoreCount ?? '-'}核 ${cpu.value?.logicalCoreCount ?? '-'}线程`,
  },
  {
    icon: 'lucide:memory-stick',
    title: '内存',
    value: fmtBytes(mem.value?.totalBytes),
    sub: `使用率 ${memPct.value}%`,
  },
  {
    icon: 'lucide:monitor',
    title: '操作系统',
    value: rt.value?.osDescription ?? '-',
    sub: `${rt.value?.osArchitecture ?? '-'} 架构`,
  },
  {
    icon: 'lucide:box',
    title: '运行框架',
    value: rt.value?.frameworkDescription ?? '-',
    sub: rt.value?.machineName ?? '-',
  },
  {
    icon: 'lucide:timer',
    title: '系统运行',
    value: fmtUptime(rt.value?.systemUptime),
    sub: `进程 ${fmtUptime(rt.value?.processUptime)}`,
  },
])

const cpuDetails = computed(() => [
  { label: '处理器架构', value: cpu.value?.processorArchitecture ?? '-' },
  { label: '基础频率', value: cpu.value?.baseClockSpeed ? `${cpu.value.baseClockSpeed} GHz` : '-' },
  { label: '缓存大小', value: fmtBytes(cpu.value?.cacheBytes) },
  { label: '物理核心', value: cpu.value?.physicalCoreCount ?? '-' },
  { label: '逻辑核心', value: cpu.value?.logicalCoreCount ?? '-' },
])

const memDetails = computed(() => [
  { label: '总内存', value: fmtBytes(mem.value?.totalBytes) },
  { label: '已使用', value: fmtBytes(mem.value?.usedBytes) },
  { label: '可用', value: fmtBytes(mem.value?.availableBytes ?? mem.value?.freeBytes) },
  {
    label: '可用率',
    value: mem.value?.availablePercentage != null ? `${mem.value.availablePercentage}%` : '-',
  },
])

const boardDetails = computed(() => [
  { icon: 'lucide:factory', label: '制造商', value: boardInfo.value?.manufacturer },
  { icon: 'lucide:tag', label: '型号', value: boardInfo.value?.product },
  { icon: 'lucide:git-branch', label: '版本', value: boardInfo.value?.version },
  { icon: 'lucide:barcode', label: '序列号', value: boardInfo.value?.serialNumber },
])

const sysDetails = computed(() => [
  { icon: 'lucide:monitor', label: '操作系统', value: rt.value?.osDescription },
  { icon: 'lucide:hash', label: '系统版本', value: rt.value?.osVersion },
  { icon: 'lucide:layers', label: '系统架构', value: rt.value?.osArchitecture },
  { icon: 'lucide:box', label: '运行框架', value: rt.value?.frameworkDescription },
  { icon: 'lucide:server', label: '机器名称', value: rt.value?.machineName },
  { icon: 'lucide:user', label: '当前用户', value: rt.value?.userName },
  { icon: 'lucide:calendar', label: '系统启动', value: rt.value?.systemStartTime },
  { icon: 'lucide:play', label: '进程启动', value: rt.value?.processStartTime },
  { icon: 'lucide:fingerprint', label: '进程ID', value: rt.value?.processId },
  { icon: 'lucide:terminal', label: '进程名称', value: rt.value?.processName },
  { icon: 'lucide:hard-drive', label: '工作集', value: fmtBytes(rt.value?.workingSet) },
  { icon: 'lucide:toggle-right', label: '交互模式', value: rt.value?.interactiveMode },
])

const activeNetworks = computed(() =>
  networkInfos.value.filter(
    (n) =>
      n.operationalStatus === 'Up' &&
      !n.name.includes('WFP') &&
      !n.name.includes('QoS') &&
      !n.name.includes('Filter') &&
      !n.name.includes('vSwitch'),
  ),
)

const showSkeleton = computed(() => !initialized.value && loading.value)

async function fetchData() {
  try {
    loading.value = true
    const [rtRes, cpuRes, memRes, diskRes, netRes, boardRes, gpuRes] = await Promise.all([
      getRuntimeInfoApi(),
      getCpuInfoApi(),
      getMemoryInfoApi(),
      getDiskInfoApi(),
      getNetworkInfoApi(),
      getBoardInfoApi(),
      getGpuInfoApi(),
    ])
    runtimeInfo.value = rtRes ?? null
    cpuInfo.value = cpuRes ?? null
    memoryInfo.value = memRes ?? null
    diskInfos.value = diskRes ?? []
    networkInfos.value = netRes ?? []
    boardInfo.value = boardRes ?? null
    gpuInfos.value = gpuRes ?? []
  } catch {
    message.error('获取服务器信息失败')
  } finally {
    loading.value = false
    initialized.value = true
  }
}

onMounted(() => {
  fetchData()
  timer = setInterval(fetchData, 15000)
})

onUnmounted(() => {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
})
</script>

<template>
  <div class="sv-page">
    <template v-if="showSkeleton">
      <NCard :bordered="false" size="small" class="sv-card">
        <div class="space-y-3">
          <NSkeleton text :repeat="1" style="width: 180px" />
          <NGrid cols="1 s:2 l:5" responsive="screen" :x-gap="10" :y-gap="10">
            <NGridItem v-for="i in 5" :key="`ov-${i}`">
              <div class="sv-skeleton-panel">
                <NSkeleton circle :width="36" :height="36" />
                <div class="flex-1 space-y-2">
                  <NSkeleton text style="width: 60%" />
                  <NSkeleton text style="width: 85%" />
                  <NSkeleton text style="width: 45%" />
                </div>
              </div>
            </NGridItem>
          </NGrid>
        </div>
      </NCard>

      <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
        <NGridItem v-for="i in 2" :key="`perf-${i}`">
          <NCard :bordered="false" size="small" class="sv-card">
            <div class="sv-skeleton-panel-col">
              <div class="sv-skeleton-circle">
                <NSkeleton circle :width="160" :height="160" />
              </div>
              <div class="w-full space-y-2">
                <NSkeleton text :repeat="4" />
              </div>
            </div>
          </NCard>
        </NGridItem>
      </NGrid>

      <NCard v-for="i in 4" :key="`card-${i}`" :bordered="false" size="small" class="sv-card">
        <div class="space-y-3">
          <NSkeleton text style="width: 140px" />
          <NSkeleton text :repeat="3" />
        </div>
      </NCard>
    </template>

    <template v-else>
      <!-- 系统概览 -->
      <div class="sv-banner">
        <div class="sv-banner-head">
          <div class="sv-banner-title">
            <Icon icon="lucide:server" width="18" />
            <span>系统概览</span>
          </div>
          <div class="sv-banner-actions">
            <NTag size="small" type="success" :bordered="false" round>
              <template #icon>
                <Icon icon="lucide:activity" width="12" />
              </template>
              运行正常
            </NTag>
            <NButton
              size="tiny"
              quaternary
              :loading="loading"
              class="sv-refresh-btn"
              @click="fetchData"
            >
              <template #icon>
                <Icon icon="lucide:refresh-cw" width="14" />
              </template>
            </NButton>
          </div>
        </div>
        <div class="sv-overview-grid">
          <div v-for="item in overviewItems" :key="item.title" class="sv-overview-item">
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
              <div class="sv-overview-sub">
                {{ item.sub }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- CPU & 内存 -->
      <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
        <NGridItem>
          <NCard :bordered="false" size="small" class="sv-card">
            <template #header>
              <div class="sv-card-header">
                <Icon icon="lucide:cpu" width="16" />
                <span>CPU信息</span>
              </div>
            </template>
            <div class="sv-perf">
              <div class="sv-gauge">
                <NProgress
                  type="circle"
                  :percentage="cpuPct"
                  :color="usageColor(cpuPct)"
                  rail-color="var(--border-color)"
                  :stroke-width="8"
                  :offset-degree="0"
                >
                  <div class="sv-gauge-inner">
                    <div class="sv-gauge-pct" :style="{ color: usageColor(cpuPct) }">
                      {{ cpuPct }}
                      <small>%</small>
                    </div>
                    <div class="sv-gauge-label">使用率</div>
                  </div>
                </NProgress>
              </div>
              <div class="sv-details">
                <div v-for="d in cpuDetails" :key="d.label" class="sv-detail-row">
                  <span class="sv-detail-label">
                    {{ d.label }}
                  </span>
                  <span class="sv-detail-value">
                    {{ d.value }}
                  </span>
                </div>
              </div>
            </div>
          </NCard>
        </NGridItem>
        <NGridItem>
          <NCard :bordered="false" size="small" class="sv-card">
            <template #header>
              <div class="sv-card-header">
                <Icon icon="lucide:memory-stick" width="16" />
                <span>内存信息</span>
              </div>
            </template>
            <div class="sv-perf">
              <div class="sv-gauge">
                <NProgress
                  type="circle"
                  :percentage="memPct"
                  :color="usageColor(memPct)"
                  rail-color="var(--border-color)"
                  :stroke-width="8"
                  :offset-degree="0"
                >
                  <div class="sv-gauge-inner">
                    <div class="sv-gauge-pct" :style="{ color: usageColor(memPct) }">
                      {{ memPct }}
                      <small>%</small>
                    </div>
                    <div class="sv-gauge-label">使用率</div>
                  </div>
                </NProgress>
              </div>
              <div class="sv-details">
                <div v-for="d in memDetails" :key="d.label" class="sv-detail-row">
                  <span class="sv-detail-label">
                    {{ d.label }}
                  </span>
                  <span class="sv-detail-value">
                    {{ d.value }}
                  </span>
                </div>
              </div>
            </div>
          </NCard>
        </NGridItem>
      </NGrid>

      <!-- 磁盘 -->
      <NCard :bordered="false" size="small" class="sv-card">
        <template #header>
          <div class="sv-card-header">
            <Icon icon="lucide:hard-drive" width="16" />
            <span>磁盘信息</span>
          </div>
        </template>
        <div v-if="!diskInfos.length" class="sv-empty">暂无数据</div>
        <NGrid v-else cols="1 s:2 l:3" responsive="screen" :x-gap="10" :y-gap="10">
          <NGridItem v-for="disk in diskInfos" :key="disk.diskName">
            <div class="sv-disk-item">
              <div class="sv-disk-head">
                <span class="sv-disk-name">
                  {{ disk.diskName }}
                </span>
                <div class="sv-disk-head-right">
                  <NTag size="tiny" :bordered="false">
                    {{ disk.typeName }}
                  </NTag>
                  <span
                    class="sv-disk-pct"
                    :style="{ color: usageColor(usagePct(disk.usedSpace, disk.totalSpace)) }"
                  >
                    {{ usagePct(disk.usedSpace, disk.totalSpace) }}%
                  </span>
                </div>
              </div>
              <NProgress
                type="line"
                :percentage="usagePct(disk.usedSpace, disk.totalSpace)"
                :color="usageColor(usagePct(disk.usedSpace, disk.totalSpace))"
                rail-color="var(--border-color)"
                :height="6"
                :show-indicator="false"
              />
              <div class="sv-disk-stats">
                <span>已用 {{ fmtBytes(disk.usedSpace) }}</span>
                <span>共 {{ fmtBytes(disk.totalSpace) }}</span>
                <span>可用 {{ fmtBytes(disk.freeSpace) }}</span>
              </div>
            </div>
          </NGridItem>
        </NGrid>
      </NCard>

      <!-- 显卡 -->
      <NCard :bordered="false" size="small" class="sv-card">
        <template #header>
          <div class="sv-card-header">
            <Icon icon="lucide:monitor" width="16" />
            <span>显卡信息</span>
            <NTag v-if="gpuInfos.length" size="tiny" :bordered="false" class="ml-auto">
              {{ gpuInfos.length }} 个
            </NTag>
          </div>
        </template>
        <div v-if="!gpuInfos.length" class="sv-empty">暂无数据</div>
        <NCollapse v-else>
          <NCollapseItem v-for="(gpu, idx) in gpuInfos" :key="idx" :name="idx">
            <template #header>
              <div class="sv-collapse-title">
                <Icon icon="lucide:monitor" width="14" class="sv-collapse-title-icon" />
                <span class="sv-collapse-title-text">
                  {{ gpu.name || `GPU ${idx}` }}
                </span>
                <NTag
                  v-if="gpu.status"
                  size="tiny"
                  :type="gpu.status === 'OK' ? 'success' : 'error'"
                  :bordered="false"
                  class="ml-2"
                >
                  {{ gpu.status }}
                </NTag>
              </div>
            </template>
            <div class="sv-collapse-body">
              <div v-if="gpu.memoryBytes" class="sv-kv">
                <span class="sv-kv-label">显存</span>
                <span class="sv-kv-value">{{ fmtBytes(gpu.memoryBytes) }}</span>
              </div>
              <div v-if="gpu.driverVersion" class="sv-kv">
                <span class="sv-kv-label">驱动版本</span>
                <span class="sv-kv-value">{{ gpu.driverVersion }}</span>
              </div>
              <div v-if="gpu.videoModeDescription" class="sv-kv">
                <span class="sv-kv-label">分辨率</span>
                <span class="sv-kv-value">{{ gpu.videoModeDescription }}</span>
              </div>
              <div v-if="gpu.vendor" class="sv-kv">
                <span class="sv-kv-label">厂商</span>
                <span class="sv-kv-value">{{ gpu.vendor }}</span>
              </div>
              <div v-if="gpu.temperature != null" class="sv-kv">
                <span class="sv-kv-label">温度</span>
                <span class="sv-kv-value">{{ gpu.temperature }}°C</span>
              </div>
            </div>
          </NCollapseItem>
        </NCollapse>
      </NCard>

      <!-- 网络信息 -->
      <NCard :bordered="false" size="small" class="sv-card">
        <template #header>
          <div class="sv-card-header">
            <Icon icon="lucide:network" width="16" />
            <span>网络信息</span>
            <NTag v-if="activeNetworks.length" size="tiny" :bordered="false" class="ml-auto">
              {{ activeNetworks.length }} 个活跃
            </NTag>
          </div>
        </template>
        <div v-if="!activeNetworks.length" class="sv-empty">暂无数据</div>
        <NCollapse v-else>
          <NCollapseItem v-for="net in activeNetworks" :key="net.name" :name="net.name">
            <template #header>
              <div class="sv-collapse-title">
                <Icon
                  icon="lucide:wifi"
                  width="14"
                  class="sv-collapse-title-icon sv-collapse-title-icon--success"
                />
                <span class="sv-collapse-title-text">
                  {{ net.name }}
                </span>
                <NTag size="tiny" :bordered="false" class="ml-2">
                  {{ net.type }}
                </NTag>
              </div>
            </template>
            <NGrid cols="1 m:2" responsive="screen" :x-gap="10" :y-gap="8">
              <NGridItem>
                <div class="sv-collapse-body">
                  <div class="sv-kv">
                    <span class="sv-kv-label">描述</span>
                    <span class="sv-kv-value">{{ net.description || '-' }}</span>
                  </div>
                  <div class="sv-kv">
                    <span class="sv-kv-label">物理地址</span>
                    <span class="sv-kv-value">{{ net.physicalAddress || '-' }}</span>
                  </div>
                  <div class="sv-kv">
                    <span class="sv-kv-label">速度</span>
                    <span class="sv-kv-value">{{ net.speed }}</span>
                  </div>
                  <div v-if="net.iPv4Addresses?.length" class="sv-kv">
                    <span class="sv-kv-label">IPv4</span>
                    <span class="sv-kv-value">
                      <NTag
                        v-for="ip in net.iPv4Addresses"
                        :key="ip.address"
                        size="tiny"
                        :bordered="false"
                        class="mr-1"
                      >
                        {{ ip.address }}
                      </NTag>
                    </span>
                  </div>
                </div>
              </NGridItem>
              <NGridItem v-if="net.statistics">
                <div class="sv-collapse-body">
                  <div class="sv-kv">
                    <span class="sv-kv-label">接收</span>
                    <span class="sv-kv-value">{{ fmtBytes(net.statistics.bytesReceived) }}</span>
                  </div>
                  <div class="sv-kv">
                    <span class="sv-kv-label">发送</span>
                    <span class="sv-kv-value">{{ fmtBytes(net.statistics.bytesSent) }}</span>
                  </div>
                  <div class="sv-kv">
                    <span class="sv-kv-label">接收包</span>
                    <span class="sv-kv-value">
                      {{ net.statistics.packetsReceived?.toLocaleString() }}
                    </span>
                  </div>
                  <div class="sv-kv">
                    <span class="sv-kv-label">发送包</span>
                    <span class="sv-kv-value">
                      {{ net.statistics.packetsSent?.toLocaleString() }}
                    </span>
                  </div>
                </div>
              </NGridItem>
            </NGrid>
          </NCollapseItem>
        </NCollapse>
      </NCard>

      <!-- 主板信息 -->
      <NCard :bordered="false" size="small" class="sv-card">
        <template #header>
          <div class="sv-card-header">
            <Icon icon="lucide:circuit-board" width="16" />
            <span>主板信息</span>
          </div>
        </template>
        <div class="sv-board-grid">
          <div v-for="d in boardDetails" :key="d.label" class="sv-sys-item">
            <div class="sv-sys-icon">
              <Icon :icon="d.icon" width="14" />
            </div>
            <span class="sv-sys-label">
              {{ d.label }}
            </span>
            <span class="sv-sys-value" :title="String(d.value ?? '-')">
              {{ d.value || '-' }}
            </span>
          </div>
        </div>
      </NCard>

      <!-- 系统信息 -->
      <NCard :bordered="false" size="small" class="sv-card">
        <template #header>
          <div class="sv-card-header">
            <Icon icon="lucide:settings" width="16" />
            <span>系统信息</span>
          </div>
        </template>
        <div class="sv-sys-grid">
          <div v-for="d in sysDetails" :key="d.label" class="sv-sys-item">
            <div class="sv-sys-icon">
              <Icon :icon="d.icon" width="14" />
            </div>
            <span class="sv-sys-label">
              {{ d.label }}
            </span>
            <span class="sv-sys-value" :title="String(d.value ?? '-')">
              {{ d.value ?? '-' }}
            </span>
          </div>
        </div>
      </NCard>
    </template>
  </div>
</template>

<style scoped>
.sv-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.sv-skeleton-panel {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  border-radius: var(--radius);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
}

.sv-skeleton-panel-col {
  display: flex;
  align-items: center;
  gap: 16px;
}

.sv-skeleton-circle {
  flex-shrink: 0;
  width: 160px;
  height: 160px;
  border-radius: 50%;
  overflow: hidden;
}

.sv-skeleton-circle .n-skeleton {
  width: 160px !important;
  height: 160px !important;
  min-width: 160px !important;
  min-height: 160px !important;
  border-radius: 50% !important;
}

/* ========== Banner ========== */
.sv-banner {
  padding: 16px 20px;
  border-radius: var(--radius);
  background: hsl(var(--accent));
  border: 1px solid var(--border-color);
}

.sv-banner-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.sv-banner-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.sv-banner-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.sv-refresh-btn {
  color: var(--text-secondary) !important;
}

.sv-overview-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 10px;
}

.sv-overview-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px;
  border-radius: var(--radius);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  min-width: 0;
  transition: border-color 0.2s;
}

.sv-overview-item:hover {
  border-color: hsl(var(--primary) / 30%);
}

.sv-overview-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.sv-overview-body {
  flex: 1;
  min-width: 0;
}

.sv-overview-label {
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.4;
}

.sv-overview-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  line-height: 1.4;
}

.sv-overview-sub {
  font-size: 11px;
  color: var(--text-disabled);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  line-height: 1.4;
}

/* ========== Card header ========== */
.sv-card-header {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

/* ========== Performance (CPU/MEM) ========== */
.sv-perf {
  display: flex;
  align-items: center;
  gap: 16px;
}

.sv-gauge {
  flex-shrink: 0;
  width: 160px;
  height: 160px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.sv-gauge-inner {
  text-align: center;
  white-space: nowrap;
}

.sv-gauge-pct {
  font-size: 24px;
  font-weight: 700;
  line-height: 1;
  letter-spacing: -0.5px;
  transition: color 0.4s;
}

.sv-gauge-pct small {
  font-size: 13px;
  font-weight: 500;
  opacity: 0.65;
}

.sv-gauge-label {
  font-size: 12px;
  color: var(--text-secondary);
  margin-top: 4px;
}

.sv-details {
  flex: 1;
  min-width: 0;
  max-width: 180px;
  margin-inline: auto;
}

.sv-detail-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 0;
  font-size: 13px;
  border-bottom: 1px dashed hsl(var(--border) / 60%);
  gap: 8px;
}

.sv-detail-row:first-child {
  padding-top: 0;
}

.sv-detail-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.sv-detail-label {
  color: var(--text-secondary);
  flex-shrink: 0;
}

.sv-detail-value {
  flex-shrink: 0;
  font-weight: 500;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ========== Disk ========== */
.sv-disk-item {
  padding: 12px;
  border-radius: 8px;
  background: hsl(var(--muted));
  transition: background 0.2s;
}

.sv-disk-item:hover {
  background: hsl(var(--accent));
}

.sv-disk-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.sv-disk-head-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.sv-disk-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.sv-disk-pct {
  font-size: 13px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.sv-disk-stats {
  display: flex;
  justify-content: space-between;
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-secondary);
}

/* ========== Collapse shared (GPU / Network) ========== */
.sv-collapse-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 500;
  min-width: 0;
}

.sv-collapse-title-icon {
  color: hsl(var(--primary));
  flex-shrink: 0;
}

.sv-collapse-title-icon--success {
  color: var(--color-success);
}

.sv-collapse-title-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sv-collapse-body {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.sv-kv {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 12px;
  gap: 12px;
  padding: 4px 0;
  border-bottom: 1px dashed hsl(var(--border) / 40%);
}

.sv-kv:last-child {
  border-bottom: none;
}

.sv-kv-label {
  color: var(--text-secondary);
  flex-shrink: 0;
}

.sv-kv-value {
  color: var(--text-primary);
  font-weight: 500;
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ========== Board grid ========== */
.sv-board-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 6px;
}

/* ========== System Info items ========== */
.sv-sys-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 6px;
}

.sv-sys-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 10px;
  border-radius: 6px;
  background: hsl(var(--muted));
  overflow: hidden;
  transition: background 0.2s;
}

.sv-sys-item:hover {
  background: hsl(var(--accent));
}

.sv-sys-icon {
  width: 26px;
  height: 26px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--primary) / 8%);
  color: hsl(var(--primary));
  font-size: 12px;
}

.sv-sys-label {
  font-size: 12px;
  color: var(--text-secondary);
  flex-shrink: 0;
  white-space: nowrap;
}

.sv-sys-value {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-primary);
  margin-left: auto;
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 55%;
}

/* ========== Empty state ========== */
.sv-empty {
  padding: 20px 0;
  text-align: center;
  font-size: 13px;
  color: var(--text-tertiary);
}

/* ========== Responsive ========== */
@media (max-width: 1280px) {
  .sv-overview-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 900px) {
  .sv-overview-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .sv-gauge {
    width: 140px;
    height: 140px;
  }

  .sv-gauge-pct {
    font-size: 22px;
  }

  .sv-gauge-pct small {
    font-size: 11px;
  }
}

@media (max-width: 640px) {
  .sv-banner {
    padding: 12px 14px;
  }

  .sv-overview-grid {
    grid-template-columns: 1fr;
    gap: 8px;
  }

  .sv-perf {
    flex-direction: column;
    align-items: stretch;
  }

  .sv-gauge {
    width: 160px;
    height: 160px;
    margin: 0 auto;
  }

  .sv-sys-grid {
    grid-template-columns: 1fr;
  }
}
</style>
