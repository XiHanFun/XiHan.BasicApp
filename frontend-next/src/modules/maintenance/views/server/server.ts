import type { Tone } from '@xihan-ui/core'
import type { Translate } from '../jobs/job'
import type { DriveType, LifecycleState, ResponseOf } from '~/contracts'
import { ref } from 'vue'
import { server as serverApi } from '~/contracts'
import { formatDate } from '~/utils'

/** 服务器的运行信息：运行时、处理器、内存、磁盘、网卡、显卡、主板与健康项 */
export type ServerInfo = ResponseOf<'server.get'>
export type RuntimeInfo = ServerInfo['runtime']
export type ProcessorInfo = ServerInfo['processor']
export type MemoryInfo = ServerInfo['memory']
export type DiskInfo = ServerInfo['disks'][number]
export type NetworkInterfaceInfo = ServerInfo['networkInterfaces'][number]
export type GpuInfo = ServerInfo['gpus'][number]
export type BoardInfo = ServerInfo['board']
export type HealthItem = ServerInfo['health'][number]

/** 自动刷新间隔（4.x 同为 15 秒） */
export const REFRESH_INTERVAL_MS = 15_000

/** 读不到的字段：后端给 null（与 0 区分），页面一律显示 - */
export const UNKNOWN = '-'

type MaybeNumber = null | number | undefined
type MaybeText = null | string | undefined

/** 取运行信息 */
export function loadServerInfo(): Promise<ServerInfo> {
  return serverApi.get({})
}

// ── 数值与文本 ──────────────────────────────────────────────────

/** 读得到的数值；null、缺省与非有限数都算读不到（int64 统一按 Number 读） */
export function knownNumber(value: MaybeNumber): null | number {
  if (value === null || value === undefined)
    return null
  const number = Number(value)
  return Number.isFinite(number) ? number : null
}

/** 文本：读不到或空串为 - */
export function textOrDash(value: MaybeText): string {
  return value === null || value === undefined || value === '' ? UNKNOWN : value
}

/** 计数（核心数、进程 ID 等）：读不到为 -，0 照常显示 */
export function countText(value: MaybeNumber): string {
  const number = knownNumber(value)
  return number === null ? UNKNOWN : String(number)
}

/** 大计数（包数等）按千分位写出（4.x 同用 toLocaleString） */
export function formatCount(value: MaybeNumber): string {
  const number = knownNumber(value)
  return number === null ? UNKNOWN : number.toLocaleString()
}

const BYTE_UNITS = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'] as const

/** 字节（4.x 同）：按 1024 进位，B 取整、其余保留两位；读不到为 -，0 写作 0 B（4.x 把 0 也写成 -） */
export function formatBytes(value: MaybeNumber): string {
  const bytes = knownNumber(value)
  if (bytes === null || bytes < 0)
    return UNKNOWN
  if (bytes === 0)
    return '0 B'
  const index = Math.min(Math.max(Math.floor(Math.log(bytes) / Math.log(1024)), 0), BYTE_UNITS.length - 1)
  return `${(bytes / 1024 ** index).toFixed(index === 0 ? 0 : 2)} ${BYTE_UNITS[index]}`
}

/** 字节速率（网卡收发）：字节写法加 /s */
export function formatByteRate(value: MaybeNumber): string {
  const text = formatBytes(value)
  return text === UNKNOWN ? UNKNOWN : `${text}/s`
}

const BIT_UNITS = ['bps', 'Kbps', 'Mbps', 'Gbps', 'Tbps'] as const

/** 网卡速率（bit/s）：按 1000 进位，去掉多余的零，如 1 Gbps、2.5 Gbps、100 Mbps */
export function formatBitRate(value: MaybeNumber): string {
  const bits = knownNumber(value)
  if (bits === null || bits < 0)
    return UNKNOWN
  if (bits === 0)
    return '0 bps'
  const index = Math.min(Math.max(Math.floor(Math.log10(bits) / 3), 0), BIT_UNITS.length - 1)
  return `${Number.parseFloat((bits / 1000 ** index).toFixed(2))} ${BIT_UNITS[index]}`
}

/** 标称频率（MHz）：满 1000 按 GHz 保留两位（4.x 的基础频率写作 GHz），否则按 MHz 取整 */
export function formatFrequency(mhz: MaybeNumber): string {
  const value = knownNumber(mhz)
  if (value === null || value <= 0)
    return UNKNOWN
  return value >= 1000 ? `${(value / 1000).toFixed(2)} GHz` : `${Math.round(value)} MHz`
}

/** 温度（℃），保留一位 */
export function formatTemperature(celsius: MaybeNumber): string {
  const value = knownNumber(celsius)
  return value === null ? UNKNOWN : `${Number.parseFloat(value.toFixed(1))}°C`
}

/** 检查耗时（毫秒，带小数）：不足 10 ms 保留两位、不足一秒取整，否则按秒保留两位 */
export function formatDurationMs(ms: MaybeNumber): string {
  const value = knownNumber(ms)
  if (value === null || value < 0)
    return UNKNOWN
  if (value >= 1000)
    return `${(value / 1000).toFixed(2)} s`
  return `${Number.parseFloat(value.toFixed(value < 10 ? 2 : 0))} ms`
}

// ── 百分比与使用率 ──────────────────────────────────────────────

/** 百分比保留一位（4.x 磁盘同） */
export function roundPercent(value: MaybeNumber): null | number {
  const number = knownNumber(value)
  return number === null ? null : Math.round(number * 10) / 10
}

/** 部分占总量的百分比；任一读不到或总量不为正为 null */
export function ratioPercent(part: MaybeNumber, total: MaybeNumber): null | number {
  const numerator = knownNumber(part)
  const denominator = knownNumber(total)
  if (numerator === null || denominator === null || denominator <= 0)
    return null
  return roundPercent((numerator / denominator) * 100)
}

/** 百分比文本：读不到为 -（不写成 0%） */
export function formatPercent(value: MaybeNumber): string {
  const percent = roundPercent(value)
  return percent === null ? UNKNOWN : `${percent}%`
}

/** 使用率的语气（4.x 同：低于 50 为绿、低于 80 为黄、否则为红）；读不到为中性 */
export function usageTone(percent: MaybeNumber): Tone {
  const value = knownNumber(percent)
  if (value === null)
    return 'neutral'
  if (value < 50)
    return 'success'
  if (value < 80)
    return 'warning'
  return 'danger'
}

/** 系统 CPU 使用率：后台采样，启动后第一个采样间隔内为 null */
export function cpuUsage(processor: Pick<ProcessorInfo, 'usagePercent'> | null | undefined): null | number {
  return roundPercent(processor?.usagePercent)
}

/** 内存使用率：取后端的使用率，没有时按已用 / 总量算 */
export function memoryUsage(memory: Pick<MemoryInfo, 'totalBytes' | 'usagePercent' | 'usedBytes'> | null | undefined): null | number {
  return roundPercent(memory?.usagePercent) ?? ratioPercent(memory?.usedBytes, memory?.totalBytes)
}

/** 内存可用率：可用 / 总量 */
export function availableRate(memory: Pick<MemoryInfo, 'availableBytes' | 'totalBytes'> | null | undefined): null | number {
  return ratioPercent(memory?.availableBytes, memory?.totalBytes)
}

/** 磁盘使用率：取后端的使用率，没有时按已用（缺省为总量减空闲）/ 总量算 */
export function diskUsage(disk: Pick<DiskInfo, 'freeBytes' | 'totalBytes' | 'usagePercent' | 'usedBytes'>): null | number {
  const used = disk.usedBytes ?? Number(disk.totalBytes) - Number(disk.freeBytes)
  return roundPercent(disk.usagePercent) ?? ratioPercent(used, disk.totalBytes)
}

// ── 运行时长 ────────────────────────────────────────────────────

export interface DurationParts {
  days: number
  hours: number
  minutes: number
  seconds: number
}

/** .NET TimeSpan 的常规写法：[d.]hh:mm:ss[.fffffff] */
const TIME_SPAN = /^(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.\d+)?$/

/** 解析运行时长；读不到或写法不认得为 null */
export function parseDuration(raw: MaybeText): DurationParts | null {
  const match = raw ? TIME_SPAN.exec(raw) : null
  if (!match)
    return null
  const [, days = '0', hours = '0', minutes = '0', seconds = '0'] = match
  return { days: Number(days), hours: Number(hours), minutes: Number(minutes), seconds: Number(seconds) }
}

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

/** 运行时长（4.x 同）：满一天带天数，时分秒补足两位；读不到为 -，写法不认得时原样 */
export function formatUptime(t: Translate, raw: MaybeText): string {
  const parts = parseDuration(raw)
  if (!parts)
    return textOrDash(raw)
  const values = { hour: pad(parts.hours), minute: pad(parts.minutes), second: pad(parts.seconds) }
  return parts.days > 0
    ? t('maintenance.server.uptime_with_day', { day: parts.days, ...values })
    : t('maintenance.server.uptime', values)
}

// ── 卡片内容（4.x 各区块的行，字段换成 6.0 的） ──────────────────

export interface OverviewItem {
  icon: string
  title: string
  value: string
  sub: string
}

export interface DetailRow {
  label: string
  value: string
}

export interface IconDetailRow extends DetailRow {
  icon: string
}

/** 系统概览的五格：处理器、内存、操作系统、运行框架、系统运行 */
export function overviewItems(t: Translate, info: ServerInfo | null): OverviewItem[] {
  const processor = info?.processor
  const memory = info?.memory
  const runtime = info?.runtime
  return [
    {
      icon: 'lucide:cpu',
      title: t('maintenance.server.processor'),
      value: textOrDash(processor?.name),
      sub: t('maintenance.server.cores_threads', { physical: countText(processor?.physicalCores), logical: countText(processor?.logicalCores) }),
    },
    {
      icon: 'lucide:memory-stick',
      title: t('maintenance.server.memory'),
      value: formatBytes(memory?.totalBytes),
      sub: t('maintenance.server.usage_rate_value', { pct: formatPercent(memoryUsage(memory)) }),
    },
    {
      icon: 'lucide:monitor',
      title: t('maintenance.server.os'),
      value: textOrDash(runtime?.osDescription),
      sub: t('maintenance.server.arch_suffix', { arch: textOrDash(runtime?.osArchitecture) }),
    },
    {
      icon: 'lucide:box',
      title: t('maintenance.server.framework'),
      value: textOrDash(runtime?.framework),
      sub: textOrDash(runtime?.machineName),
    },
    {
      icon: 'lucide:timer',
      title: t('maintenance.server.system_uptime'),
      value: formatUptime(t, runtime?.systemUptime),
      sub: t('maintenance.server.process_uptime', { uptime: formatUptime(t, runtime?.processUptime) }),
    },
  ]
}

/** CPU 明细：4.x 的五行加上本进程的 CPU 占用 */
export function cpuDetails(t: Translate, info: ServerInfo | null): DetailRow[] {
  const processor = info?.processor
  return [
    { label: t('maintenance.server.processor_arch'), value: textOrDash(processor?.architecture) },
    { label: t('maintenance.server.base_clock'), value: formatFrequency(processor?.frequencyMhz) },
    { label: t('maintenance.server.cache_size'), value: formatBytes(processor?.cacheBytes) },
    { label: t('maintenance.server.physical_cores'), value: countText(processor?.physicalCores) },
    { label: t('maintenance.server.logical_cores'), value: countText(processor?.logicalCores) },
    { label: t('maintenance.server.process_cpu'), value: formatPercent(info?.runtime.processCpuUsagePercent) },
  ]
}

/** 内存明细：4.x 的四行加上空闲与文件缓存（读不到的平台为 -） */
export function memoryDetails(t: Translate, memory: MemoryInfo | null | undefined): DetailRow[] {
  return [
    { label: t('maintenance.server.total_memory'), value: formatBytes(memory?.totalBytes) },
    { label: t('maintenance.server.used'), value: formatBytes(memory?.usedBytes) },
    { label: t('maintenance.server.available'), value: formatBytes(memory?.availableBytes) },
    { label: t('maintenance.server.available_rate'), value: formatPercent(availableRate(memory)) },
    { label: t('maintenance.server.free'), value: formatBytes(memory?.freeBytes) },
    { label: t('maintenance.server.cached'), value: formatBytes(memory?.cachedBytes) },
  ]
}

/** 主板信息：4.x 的厂商、型号、版本、序列号，加上整机与 BIOS */
export function boardDetails(t: Translate, board: BoardInfo | null | undefined): IconDetailRow[] {
  return [
    { icon: 'lucide:factory', label: t('maintenance.server.manufacturer'), value: textOrDash(board?.manufacturer) },
    { icon: 'lucide:tag', label: t('maintenance.server.model'), value: textOrDash(board?.product) },
    { icon: 'lucide:git-branch', label: t('maintenance.server.version'), value: textOrDash(board?.version) },
    { icon: 'lucide:barcode', label: t('maintenance.server.serial_number'), value: textOrDash(board?.serialNumber) },
    { icon: 'lucide:building-2', label: t('maintenance.server.system_manufacturer'), value: textOrDash(board?.systemManufacturer) },
    { icon: 'lucide:laptop', label: t('maintenance.server.system_model'), value: textOrDash(board?.systemProduct) },
    { icon: 'lucide:microchip', label: t('maintenance.server.bios_vendor'), value: textOrDash(board?.biosVendor) },
    { icon: 'lucide:binary', label: t('maintenance.server.bios_version'), value: textOrDash(board?.biosVersion) },
    { icon: 'lucide:calendar-days', label: t('maintenance.server.bios_date'), value: textOrDash(board?.biosDate) },
  ]
}

/** 系统信息：4.x 的十二项，加上平台、运行时标识、进程架构、私有内存、托管堆、线程、句柄与 GC 次数 */
export function systemDetails(t: Translate, runtime: RuntimeInfo | null | undefined): IconDetailRow[] {
  const gc = runtime ? `${runtime.gen0Collections} / ${runtime.gen1Collections} / ${runtime.gen2Collections}` : UNKNOWN
  const interactive = runtime ? (runtime.interactive ? t('common.statuses.yes') : t('common.statuses.no')) : UNKNOWN
  return [
    { icon: 'lucide:monitor', label: t('maintenance.server.os'), value: textOrDash(runtime?.osDescription) },
    { icon: 'lucide:hash', label: t('maintenance.server.os_version'), value: textOrDash(runtime?.osVersion) },
    { icon: 'lucide:layers', label: t('maintenance.server.os_arch'), value: textOrDash(runtime?.osArchitecture) },
    { icon: 'lucide:app-window', label: t('maintenance.server.os_platform'), value: textOrDash(runtime?.osPlatform) },
    { icon: 'lucide:box', label: t('maintenance.server.framework'), value: textOrDash(runtime?.framework) },
    { icon: 'lucide:package', label: t('maintenance.server.runtime_identifier'), value: textOrDash(runtime?.runtimeIdentifier) },
    { icon: 'lucide:server', label: t('maintenance.server.machine_name'), value: textOrDash(runtime?.machineName) },
    { icon: 'lucide:user', label: t('maintenance.server.current_user'), value: textOrDash(runtime?.userName) },
    { icon: 'lucide:calendar', label: t('maintenance.server.system_start'), value: formatDate(runtime?.systemStartedAt) },
    { icon: 'lucide:play', label: t('maintenance.server.process_start'), value: formatDate(runtime?.processStartedAt) },
    { icon: 'lucide:fingerprint', label: t('maintenance.server.process_id'), value: countText(runtime?.processId) },
    { icon: 'lucide:terminal', label: t('maintenance.server.process_name'), value: textOrDash(runtime?.processName) },
    { icon: 'lucide:layers-2', label: t('maintenance.server.process_arch'), value: textOrDash(runtime?.processArchitecture) },
    { icon: 'lucide:hard-drive', label: t('maintenance.server.working_set'), value: formatBytes(runtime?.workingSetBytes) },
    { icon: 'lucide:lock', label: t('maintenance.server.private_bytes'), value: formatBytes(runtime?.privateBytes) },
    { icon: 'lucide:database', label: t('maintenance.server.managed_heap'), value: formatBytes(runtime?.managedHeapBytes) },
    { icon: 'lucide:git-fork', label: t('maintenance.server.thread_count'), value: countText(runtime?.threadCount) },
    { icon: 'lucide:link', label: t('maintenance.server.handle_count'), value: countText(runtime?.handleCount) },
    { icon: 'lucide:recycle', label: t('maintenance.server.gc_collections'), value: gc },
    { icon: 'lucide:toggle-right', label: t('maintenance.server.interactive_mode'), value: interactive },
  ]
}

// ── 磁盘 ────────────────────────────────────────────────────────

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function driveTypeLabel(t: Translate, type: DriveType): string {
  switch (type) {
    case 'Fixed': return t('maintenance.server.drive_types.fixed')
    case 'Removable': return t('maintenance.server.drive_types.removable')
    case 'Network': return t('maintenance.server.drive_types.network')
    case 'CDRom': return t('maintenance.server.drive_types.cd_rom')
    case 'Ram': return t('maintenance.server.drive_types.ram')
    case 'NoRootDirectory': return t('maintenance.server.drive_types.no_root_directory')
    default: return t('maintenance.server.drive_types.unknown')
  }
}

/** 磁盘标签：类型，读得到文件系统时带上（如「本地磁盘 · NTFS」） */
export function diskTag(t: Translate, disk: Pick<DiskInfo, 'format' | 'type'>): string {
  const type = driveTypeLabel(t, disk.type)
  return disk.format ? `${type} · ${disk.format}` : type
}

/** 一格磁盘：4.x 的名称、类型、使用率、进度与已用 / 总量 / 可用 */
export interface DiskCard {
  key: string
  name: string
  /** 悬停提示：卷标与设备，都没有时为名称 */
  hint: string
  tag: string
  percent: null | number
  percentText: string
  tone: Tone
  used: string
  total: string
  available: string
}

export function diskCard(t: Translate, disk: DiskInfo): DiskCard {
  const percent = diskUsage(disk)
  return {
    key: disk.name,
    name: disk.name,
    hint: [disk.label, disk.device].filter(Boolean).join(' · ') || disk.name,
    tag: diskTag(t, disk),
    percent,
    percentText: formatPercent(percent),
    tone: usageTone(percent),
    used: formatBytes(disk.usedBytes ?? Number(disk.totalBytes) - Number(disk.freeBytes)),
    total: formatBytes(disk.totalBytes),
    available: formatBytes(disk.availableBytes),
  }
}

// ── 显卡 ────────────────────────────────────────────────────────

/** PCI 厂商与设备 ID（如 10de:2684）；都读不到为 null */
export function pciId(gpu: Pick<GpuInfo, 'deviceId' | 'vendorId'>): null | string {
  if (gpu.vendorId && gpu.deviceId)
    return `${gpu.vendorId}:${gpu.deviceId}`
  return gpu.vendorId || gpu.deviceId || null
}

/** 显卡明细：只列读得到的项（4.x 同样按有无显示）；读不到的平台差异（如 NVIDIA 的显存与温度）不占行 */
export function gpuDetails(t: Translate, gpu: GpuInfo): DetailRow[] {
  const rows: DetailRow[] = []
  if (knownNumber(gpu.memoryBytes) !== null)
    rows.push({ label: t('maintenance.server.gpu_memory'), value: formatBytes(gpu.memoryBytes) })
  if (gpu.driver)
    rows.push({ label: t('maintenance.server.driver'), value: gpu.driver })
  if (gpu.driverVersion)
    rows.push({ label: t('maintenance.server.driver_version'), value: gpu.driverVersion })
  if (gpu.vendor)
    rows.push({ label: t('maintenance.server.vendor'), value: gpu.vendor })
  const pci = pciId(gpu)
  if (pci)
    rows.push({ label: t('maintenance.server.pci_id'), value: pci })
  if (knownNumber(gpu.temperatureCelsius) !== null)
    rows.push({ label: t('maintenance.server.temperature'), value: formatTemperature(gpu.temperatureCelsius) })
  return rows
}

// ── 网卡 ────────────────────────────────────────────────────────

/** 4.x 不列的虚拟过滤网卡（Windows 的 WFP / QoS / 筛选器与 Hyper-V 交换机） */
const VIRTUAL_ADAPTER_MARKS = ['WFP', 'QoS', 'Filter', 'vSwitch'] as const

/** 活跃网卡（4.x 同）：已连通、且不是虚拟过滤网卡 */
export function activeNetworkInterfaces(list: readonly NetworkInterfaceInfo[]): NetworkInterfaceInfo[] {
  return list.filter(nic => nic.status === 'Up' && !VIRTUAL_ADAPTER_MARKS.some(mark => nic.name.includes(mark)))
}

export interface AddressGroup {
  label: string
  values: string[]
}

/** 网卡的地址组：IPv4、IPv6、网关与 DNS，没有的组不列 */
export function networkAddresses(t: Translate, nic: Pick<NetworkInterfaceInfo, 'dnsServers' | 'gateways' | 'iPv4Addresses' | 'iPv6Addresses'>): AddressGroup[] {
  const groups: AddressGroup[] = [
    { label: 'IPv4', values: nic.iPv4Addresses ?? [] },
    { label: 'IPv6', values: nic.iPv6Addresses ?? [] },
    { label: t('maintenance.server.gateways'), values: nic.gateways ?? [] },
    { label: t('maintenance.server.dns_servers'), values: nic.dnsServers ?? [] },
  ]
  return groups.filter(group => group.values.length > 0)
}

/** 网卡流量：速率（后台采样，启动后第一个采样间隔内为 -）与累计收发 */
export function networkTraffic(t: Translate, nic: NetworkInterfaceInfo): DetailRow[] {
  return [
    { label: t('maintenance.server.receive_rate'), value: formatByteRate(nic.receiveBytesPerSecond) },
    { label: t('maintenance.server.send_rate'), value: formatByteRate(nic.sendBytesPerSecond) },
    { label: t('maintenance.server.received'), value: formatBytes(nic.bytesReceived) },
    { label: t('maintenance.server.sent'), value: formatBytes(nic.bytesSent) },
    { label: t('maintenance.server.received_packets'), value: formatCount(nic.packetsReceived) },
    { label: t('maintenance.server.sent_packets'), value: formatCount(nic.packetsSent) },
  ]
}

// ── 健康项（各 Provider 自带，这里只展示） ───────────────────────

export function healthStateLabel(t: Translate, state: LifecycleState): string {
  switch (state) {
    case 'Declared': return t('maintenance.server.health_states.declared')
    case 'Resolved': return t('maintenance.server.health_states.resolved')
    case 'Configured': return t('maintenance.server.health_states.configured')
    case 'Started': return t('maintenance.server.health_states.started')
    case 'Ready': return t('maintenance.server.health_states.ready')
    case 'Degraded': return t('maintenance.server.health_states.degraded')
    case 'Draining': return t('maintenance.server.health_states.draining')
    case 'Stopped': return t('maintenance.server.health_states.stopped')
    default: return t('maintenance.server.health_states.failed')
  }
}

/** 状态的语气：就绪为绿，降级与排空为黄，故障为红，停止为中性，其余（启动途中）为信息 */
export function healthStateTone(state: LifecycleState): Tone {
  switch (state) {
    case 'Ready': return 'success'
    case 'Degraded':
    case 'Draining': return 'warning'
    case 'Failed': return 'danger'
    case 'Stopped': return 'neutral'
    default: return 'info'
  }
}

function severity(tone: Tone): number {
  switch (tone) {
    case 'danger': return 3
    case 'warning': return 2
    case 'success': return 0
    default: return 1
  }
}

export interface HealthSummary {
  tone: Tone
  label: string
}

/**
 * 概览角标：取最差的健康项。全部就绪（或没有健康项）为「运行正常」（4.x 恒为运行正常），
 * 有故障、有降级、有未就绪的分别写出。
 */
export function healthSummary(t: Translate, items: readonly Pick<HealthItem, 'state'>[]): HealthSummary {
  let worst: Tone = 'success'
  for (const item of items) {
    const tone = healthStateTone(item.state)
    if (severity(tone) > severity(worst))
      worst = tone
  }
  switch (worst) {
    case 'danger': return { tone: 'danger', label: t('maintenance.server.health_failed') }
    case 'warning': return { tone: 'warning', label: t('maintenance.server.health_degraded') }
    case 'success': return { tone: 'success', label: t('maintenance.server.running_normal') }
    default: return { tone: 'info', label: t('maintenance.server.health_pending') }
  }
}

/** 健康项明细（名字到值，按后端给的顺序）；值为空写 - */
export function healthData(item: Pick<HealthItem, 'data'>): DetailRow[] {
  return Object.entries(item.data).map(([label, value]) => ({ label, value: textOrDash(value) }))
}

/** 一格健康项：名字、状态、耗时、检查时间、说明、错误与明细 */
export interface HealthCard {
  name: string
  stateLabel: string
  tone: Tone
  duration: string
  checkedAt: string
  description: null | string
  errorCode: null | string
  errorMessage: null | string
  data: DetailRow[]
}

export function healthCard(t: Translate, item: HealthItem): HealthCard {
  return {
    name: item.name,
    stateLabel: healthStateLabel(t, item.state),
    tone: healthStateTone(item.state),
    duration: t('maintenance.server.duration_value', { value: formatDurationMs(item.durationMs) }),
    checkedAt: t('maintenance.server.checked_at_value', { value: formatDate(item.checkedAt) }),
    description: item.description || null,
    errorCode: item.errorCode || null,
    errorMessage: item.errorMessage || null,
    data: healthData(item),
  }
}

// ── 取数与自动刷新 ──────────────────────────────────────────────

/**
 * 运行信息的状态：取一次替换整份；失败时保留上一次的数据并抛给调用方。
 * 上一次还在途时不再发（自动刷新遇上慢响应不堆积请求）。
 */
export function useServerInfo() {
  const info = ref<ServerInfo | null>(null)
  const loading = ref(false)
  /** 第一次取数结束（成败都算）：之前显示骨架 */
  const initialized = ref(false)

  /** 取一次；在途时不发并返回 false */
  async function refresh(): Promise<boolean> {
    if (loading.value)
      return false
    loading.value = true
    try {
      info.value = await loadServerInfo()
      return true
    }
    finally {
      loading.value = false
      initialized.value = true
    }
  }

  return { info, loading, initialized, refresh }
}

export interface Polling {
  start: () => void
  stop: () => void
}

/** 定时刷新：重复 start 不叠加计时器 */
export function createPolling(tick: () => void, intervalMs = REFRESH_INTERVAL_MS): Polling {
  let timer: ReturnType<typeof setInterval> | null = null
  return {
    start() {
      if (timer !== null)
        return
      timer = setInterval(tick, intervalMs)
    },
    stop() {
      if (timer === null)
        return
      clearInterval(timer)
      timer = null
    },
  }
}
