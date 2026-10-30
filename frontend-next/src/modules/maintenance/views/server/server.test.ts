import type { DiskInfo, GpuInfo, HealthItem, NetworkInterfaceInfo, ServerInfo } from './server'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { configureRequest } from '~/request'
import {
  activeNetworkInterfaces,
  availableRate,
  boardDetails,
  countText,
  cpuDetails,
  cpuUsage,
  createPolling,
  diskCard,
  diskTag,
  diskUsage,
  driveTypeLabel,
  formatBitRate,
  formatByteRate,
  formatBytes,
  formatCount,
  formatDurationMs,
  formatFrequency,
  formatPercent,
  formatTemperature,
  formatUptime,
  gpuDetails,
  healthCard,
  healthData,
  healthStateLabel,
  healthStateTone,
  healthSummary,
  knownNumber,
  memoryDetails,
  memoryUsage,
  networkAddresses,
  networkTraffic,
  overviewItems,
  parseDuration,
  pciId,
  ratioPercent,
  REFRESH_INTERVAL_MS,
  roundPercent,
  systemDetails,
  textOrDash,
  usageTone,
  useServerInfo,
} from './server'

/** 假翻译：键原样返回，带参数时拼上参数便于断言 */
const t = (key: string, values?: Record<string, unknown>) => (values ? `${key}:${JSON.stringify(values)}` : key)

const GiB = 1024 ** 3

function info(): ServerInfo {
  return {
    runtime: {
      osDescription: 'Microsoft Windows 10.0.26300',
      osPlatform: 'windows',
      osVersion: '10.0.26300',
      osArchitecture: 'X64',
      framework: '.NET 10.0.0',
      runtimeIdentifier: 'win-x64',
      processArchitecture: 'X64',
      machineName: 'XIHAN-01',
      userName: 'svc',
      interactive: false,
      processId: 4242,
      processName: 'XiHan.BasicApp.Host',
      processStartedAt: null,
      processUptime: '01:02:03.4560000',
      systemStartedAt: null,
      systemUptime: '3.04:05:06.7890000',
      workingSetBytes: 512 * 1024 * 1024,
      privateBytes: null,
      managedHeapBytes: 64 * 1024 * 1024,
      threadCount: 37,
      handleCount: null,
      gen0Collections: 12,
      gen1Collections: 3,
      gen2Collections: 0,
      processCpuUsagePercent: null,
    },
    processor: { name: 'AMD Ryzen 9 7950X', vendor: 'AuthenticAMD', architecture: 'X64', physicalCores: null, logicalCores: 32, frequencyMhz: 4500, cacheBytes: 64 * 1024 * 1024, usagePercent: null },
    memory: { totalBytes: 64 * GiB, availableBytes: 48 * GiB, freeBytes: null, cachedBytes: null, usedBytes: 16 * GiB, usagePercent: null },
    disks: [],
    networkInterfaces: [],
    gpus: [],
    board: { manufacturer: 'ASUSTeK', product: 'ROG X670E', version: 'Rev 1.xx', serialNumber: null, systemManufacturer: null, systemProduct: null, biosVendor: 'AMI', biosVersion: '2401', biosDate: '08/13/2024' },
    health: [],
  }
}

function disk(patch: Partial<DiskInfo> = {}): DiskInfo {
  return { name: 'C:\\', device: null, label: 'System', format: 'NTFS', type: 'Fixed', totalBytes: 1000, freeBytes: 250, availableBytes: 200, usedBytes: 750, usagePercent: 75.04, ...patch }
}

function nic(patch: Partial<NetworkInterfaceInfo> = {}): NetworkInterfaceInfo {
  return {
    id: '{A1}',
    name: 'Ethernet',
    description: 'Intel(R) Ethernet',
    type: 'Ethernet',
    status: 'Up',
    speedBitsPerSecond: 1_000_000_000,
    physicalAddress: '00:1A:2B:3C:4D:5E',
    iPv4Addresses: ['192.168.1.5/24'],
    iPv6Addresses: [],
    gateways: ['192.168.1.1'],
    dnsServers: [],
    bytesReceived: 2048,
    bytesSent: 0,
    packetsReceived: 1234567,
    packetsSent: null,
    receiveBytesPerSecond: null,
    sendBytesPerSecond: 1536,
    ...patch,
  }
}

function health(patch: Partial<HealthItem> = {}): HealthItem {
  return { name: 'database', state: 'Ready', errorCode: null, errorMessage: null, description: null, checkedAt: null, durationMs: 1.234, data: {}, ...patch }
}

afterEach(() => {
  vi.useRealTimers()
})

describe('服务监控：读不到与为零', () => {
  it('null、缺省与非有限数都算读不到，0 照常是 0', () => {
    expect([knownNumber(null), knownNumber(undefined), knownNumber(Number.NaN), knownNumber(0), knownNumber(7)]).toEqual([null, null, null, 0, 7])
    expect([textOrDash(null), textOrDash(''), textOrDash('x')]).toEqual(['-', '-', 'x'])
    expect([countText(null), countText(0), countText(32)]).toEqual(['-', '0', '32'])
    expect(formatCount(null)).toBe('-')
    expect(formatCount(1234567)).toBe((1234567).toLocaleString())
  })

  it('字节按 1024 进位（4.x 同），读不到为 -、0 写作 0 B；速率加 /s', () => {
    expect([formatBytes(null), formatBytes(undefined), formatBytes(-1)]).toEqual(['-', '-', '-'])
    expect(formatBytes(0)).toBe('0 B')
    expect(formatBytes(512)).toBe('512 B')
    expect(formatBytes(1536)).toBe('1.50 KB')
    expect(formatBytes(16 * GiB)).toBe('16.00 GB')
    expect(formatByteRate(null)).toBe('-')
    expect(formatByteRate(0)).toBe('0 B/s')
    expect(formatByteRate(1536)).toBe('1.50 KB/s')
  })

  it('网卡速率按 1000 进位并去掉多余的零；频率满 1000 MHz 写 GHz；温度保留一位；检查耗时按量级取精度', () => {
    expect([formatBitRate(null), formatBitRate(0), formatBitRate(999)]).toEqual(['-', '0 bps', '999 bps'])
    expect([formatBitRate(100_000_000), formatBitRate(1_000_000_000), formatBitRate(2_500_000_000)]).toEqual(['100 Mbps', '1 Gbps', '2.5 Gbps'])
    expect([formatFrequency(null), formatFrequency(0), formatFrequency(800.4), formatFrequency(3600)]).toEqual(['-', '-', '800 MHz', '3.60 GHz'])
    expect([formatTemperature(null), formatTemperature(0), formatTemperature(65.26)]).toEqual(['-', '0°C', '65.3°C'])
    expect([formatDurationMs(null), formatDurationMs(0.4567), formatDurationMs(12.4), formatDurationMs(1500)]).toEqual(['-', '0.46 ms', '12 ms', '1.50 s'])
  })
})

describe('服务监控：百分比与使用率', () => {
  it('百分比保留一位；占比在任一读不到或总量不为正时为 null；读不到写 -、不写 0%', () => {
    expect(roundPercent(12.345)).toBe(12.3)
    expect(roundPercent(null)).toBeNull()
    expect(ratioPercent(1, 3)).toBe(33.3)
    expect([ratioPercent(1, 0), ratioPercent(null, 10), ratioPercent(1, null)]).toEqual([null, null, null])
    expect([formatPercent(null), formatPercent(0), formatPercent(12.34)]).toEqual(['-', '0%', '12.3%'])
  })

  it('语气同 4.x：低于 50 绿、低于 80 黄、否则红；读不到为中性', () => {
    expect([usageTone(null), usageTone(0), usageTone(49.9), usageTone(50), usageTone(79.9), usageTone(80), usageTone(100)])
      .toEqual(['neutral', 'success', 'success', 'warning', 'warning', 'danger', 'danger'])
  })

  it('处理器使用率在首个采样间隔内为 null；内存与磁盘取后端使用率，没有时按已用 / 总量算', () => {
    expect(cpuUsage({ usagePercent: null })).toBeNull()
    expect(cpuUsage({ usagePercent: 12.34 })).toBe(12.3)
    expect(cpuUsage(null)).toBeNull()

    expect(memoryUsage({ totalBytes: 64, usedBytes: 16, usagePercent: 30 })).toBe(30)
    expect(memoryUsage({ totalBytes: 64, usedBytes: 16, usagePercent: null })).toBe(25)
    expect(memoryUsage({ totalBytes: null, usedBytes: 16, usagePercent: null })).toBeNull()
    expect(availableRate({ availableBytes: 48, totalBytes: 64 })).toBe(75)
    expect(availableRate(undefined)).toBeNull()

    expect(diskUsage(disk())).toBe(75)
    expect(diskUsage(disk({ usagePercent: null }))).toBe(75)
    expect(diskUsage(disk({ usagePercent: null, usedBytes: undefined, freeBytes: 600 }))).toBe(40)
    expect(diskUsage(disk({ usagePercent: null, totalBytes: 0, freeBytes: 0, usedBytes: 0 }))).toBeNull()
  })
})

describe('服务监控：运行时长', () => {
  it('解析 .NET TimeSpan 的常规写法，不认得的为 null', () => {
    expect(parseDuration('3.04:05:06.7890000')).toEqual({ days: 3, hours: 4, minutes: 5, seconds: 6 })
    expect(parseDuration('01:02:03.4560000')).toEqual({ days: 0, hours: 1, minutes: 2, seconds: 3 })
    expect(parseDuration('00:00:05')).toEqual({ days: 0, hours: 0, minutes: 0, seconds: 5 })
    expect([parseDuration(null), parseDuration(''), parseDuration('P1D')]).toEqual([null, null, null])
  })

  it('满一天带天数、时分秒补两位（4.x 同）；读不到为 -，写法不认得时原样', () => {
    expect(formatUptime(t, '3.04:05:06.7890000')).toBe('maintenance.server.uptime_with_day:{"day":3,"hour":"04","minute":"05","second":"06"}')
    expect(formatUptime(t, '01:02:03.4560000')).toBe('maintenance.server.uptime:{"hour":"01","minute":"02","second":"03"}')
    expect(formatUptime(t, null)).toBe('-')
    expect(formatUptime(t, 'P1D')).toBe('P1D')
  })
})

describe('服务监控：各区块的行', () => {
  it('概览五格：没取到数据时全是 -；读不到的物理核心写 -，内存使用率按已用 / 总量补', () => {
    expect(overviewItems(t, null).map(item => item.value)).toEqual(['-', '-', '-', '-', '-'])

    const items = overviewItems(t, info())
    expect(items.map(item => item.icon)).toEqual(['lucide:cpu', 'lucide:memory-stick', 'lucide:monitor', 'lucide:box', 'lucide:timer'])
    expect(items[0]).toMatchObject({ value: 'AMD Ryzen 9 7950X', sub: 'maintenance.server.cores_threads:{"physical":"-","logical":"32"}' })
    expect(items[1]).toMatchObject({ value: '64.00 GB', sub: 'maintenance.server.usage_rate_value:{"pct":"25%"}' })
    expect(items[2]!.sub).toBe('maintenance.server.arch_suffix:{"arch":"X64"}')
    expect(items[3]).toMatchObject({ value: '.NET 10.0.0', sub: 'XIHAN-01' })
    expect(items[4]!.sub).toBe('maintenance.server.process_uptime:{"uptime":"maintenance.server.uptime:{\\"hour\\":\\"01\\",\\"minute\\":\\"02\\",\\"second\\":\\"03\\"}"}')
  })

  it('处理器明细带本进程占用（未采样为 -），内存明细读不到空闲与缓存的平台写 -', () => {
    expect(cpuDetails(t, info()).map(row => row.value)).toEqual(['X64', '4.50 GHz', '64.00 MB', '-', '32', '-'])
    expect(cpuDetails(t, null).every(row => row.value === '-')).toBe(true)
    expect(memoryDetails(t, info().memory).map(row => row.value)).toEqual(['64.00 GB', '16.00 GB', '48.00 GB', '75%', '-', '-'])
  })

  it('主板与系统信息：读不到为 -，交互模式写是 / 否，GC 写三代次数', () => {
    expect(boardDetails(t, info().board).map(row => row.value)).toEqual(['ASUSTeK', 'ROG X670E', 'Rev 1.xx', '-', '-', '-', 'AMI', '2401', '08/13/2024'])

    const rows = systemDetails(t, info().runtime)
    const value = (label: string) => rows.find(row => row.label === label)?.value
    expect(rows).toHaveLength(20)
    expect(value('maintenance.server.interactive_mode')).toBe('common.statuses.no')
    expect(value('maintenance.server.gc_collections')).toBe('12 / 3 / 0')
    expect(value('maintenance.server.process_id')).toBe('4242')
    expect(value('maintenance.server.private_bytes')).toBe('-')
    expect(value('maintenance.server.handle_count')).toBe('-')
    expect(value('maintenance.server.system_start')).toBe('-')
    expect(systemDetails(t, undefined).every(row => row.value === '-')).toBe(true)
  })
})

describe('服务监控：磁盘、显卡与网卡', () => {
  it('磁盘类型写出文案，标签带文件系统；一格磁盘的已用缺省按总量减空闲，总量为 0 时使用率为 -', () => {
    expect(driveTypeLabel(t, 'Fixed')).toBe('maintenance.server.drive_types.fixed')
    expect(driveTypeLabel(t, 'CDRom')).toBe('maintenance.server.drive_types.cd_rom')
    expect(driveTypeLabel(t, 'Unknown')).toBe('maintenance.server.drive_types.unknown')
    expect(diskTag(t, { type: 'Network', format: null })).toBe('maintenance.server.drive_types.network')
    expect(diskTag(t, { type: 'Fixed', format: 'NTFS' })).toBe('maintenance.server.drive_types.fixed · NTFS')

    expect(diskCard(t, disk({ device: '/dev/sda1' }))).toEqual({
      key: 'C:\\',
      name: 'C:\\',
      hint: 'System · /dev/sda1',
      tag: 'maintenance.server.drive_types.fixed · NTFS',
      percent: 75,
      percentText: '75%',
      tone: 'warning',
      used: '750 B',
      total: '1000 B',
      available: '200 B',
    })
    expect(diskCard(t, disk({ label: null, usedBytes: undefined })).used).toBe('750 B')
    expect(diskCard(t, disk({ label: null })).hint).toBe('C:\\')
    expect(diskCard(t, disk({ usagePercent: null, totalBytes: 0, freeBytes: 0, usedBytes: 0 }))).toMatchObject({ percent: null, percentText: '-', tone: 'neutral' })
  })

  it('显卡只列读得到的项（0 照常列出）；PCI ID 两段都有时用冒号连起', () => {
    const gpu: GpuInfo = { name: 'NVIDIA RTX 4090', vendor: 'NVIDIA', vendorId: '10de', deviceId: '2684', driver: 'nvidia', driverVersion: '560.70', memoryBytes: null, temperatureCelsius: 0 }
    expect(gpuDetails(t, gpu)).toEqual([
      { label: 'maintenance.server.driver', value: 'nvidia' },
      { label: 'maintenance.server.driver_version', value: '560.70' },
      { label: 'maintenance.server.vendor', value: 'NVIDIA' },
      { label: 'maintenance.server.pci_id', value: '10de:2684' },
      { label: 'maintenance.server.temperature', value: '0°C' },
    ])
    expect(gpuDetails(t, { name: 'Basic Display' })).toEqual([])
    expect([pciId({ vendorId: '8086' }), pciId({ deviceId: null, vendorId: null })]).toEqual(['8086', null])
  })

  it('活跃网卡同 4.x：已连通且不是虚拟过滤网卡；地址组只列有地址的，速率未采样为 -', () => {
    const list = [
      nic(),
      nic({ id: 'down', status: 'Down' }),
      nic({ id: 'wfp', name: 'Ethernet-WFP Native MAC Layer LightWeight Filter-0000' }),
      nic({ id: 'vswitch', name: 'vSwitch (Default Switch)' }),
    ]
    expect(activeNetworkInterfaces(list).map(item => item.id)).toEqual(['{A1}'])

    expect(networkAddresses(t, nic())).toEqual([
      { label: 'IPv4', values: ['192.168.1.5/24'] },
      { label: 'maintenance.server.gateways', values: ['192.168.1.1'] },
    ])
    expect(networkAddresses(t, {})).toEqual([])
    expect(networkTraffic(t, nic()).map(row => row.value)).toEqual(['-', '1.50 KB/s', '2.00 KB', '0 B', (1234567).toLocaleString(), '-'])
  })
})

describe('服务监控：健康项', () => {
  it('九种状态都有文案；就绪绿、降级与排空黄、故障红、停止中性，启动途中为信息', () => {
    expect(healthStateLabel(t, 'Draining')).toBe('maintenance.server.health_states.draining')
    expect(healthStateLabel(t, 'Failed')).toBe('maintenance.server.health_states.failed')
    expect((['Declared', 'Resolved', 'Configured', 'Started', 'Ready', 'Degraded', 'Draining', 'Stopped', 'Failed'] as const).map(healthStateTone))
      .toEqual(['info', 'info', 'info', 'info', 'success', 'warning', 'warning', 'neutral', 'danger'])
  })

  it('概览角标取最差的一项：全部就绪或没有健康项为运行正常', () => {
    expect(healthSummary(t, [])).toEqual({ tone: 'success', label: 'maintenance.server.running_normal' })
    expect(healthSummary(t, [{ state: 'Ready' }])).toEqual({ tone: 'success', label: 'maintenance.server.running_normal' })
    expect(healthSummary(t, [{ state: 'Ready' }, { state: 'Started' }])).toEqual({ tone: 'info', label: 'maintenance.server.health_pending' })
    expect(healthSummary(t, [{ state: 'Stopped' }, { state: 'Degraded' }, { state: 'Started' }])).toEqual({ tone: 'warning', label: 'maintenance.server.health_degraded' })
    expect(healthSummary(t, [{ state: 'Failed' }, { state: 'Degraded' }])).toEqual({ tone: 'danger', label: 'maintenance.server.health_failed' })
  })

  it('明细按后端顺序列出、空值写 -；一格健康项带耗时与检查时间，空串的说明与错误当作没有', () => {
    expect(healthData(health({ data: { pool: '8', lastError: null } }))).toEqual([
      { label: 'pool', value: '8' },
      { label: 'lastError', value: '-' },
    ])
    expect(healthCard(t, health({ state: 'Failed', errorCode: 'CACHE_UNAVAILABLE_001', errorMessage: '连不上', description: '' }))).toEqual({
      name: 'database',
      stateLabel: 'maintenance.server.health_states.failed',
      tone: 'danger',
      duration: 'maintenance.server.duration_value:{"value":"1.23 ms"}',
      checkedAt: 'maintenance.server.checked_at_value:{"value":"-"}',
      description: null,
      errorCode: 'CACHE_UNAVAILABLE_001',
      errorMessage: '连不上',
      data: [],
    })
  })
})

describe('服务监控：取数与自动刷新', () => {
  function stubServer(respond: () => Promise<Response> | Response): string[] {
    const urls: string[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        urls.push(`${init?.method ?? 'GET'} ${String(url)}`)
        return respond()
      }) as unknown as typeof fetch,
    })
    return urls
  }

  function ok(body: object): Response {
    return new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }

  it('按 GET /api/server 取整份；上一次在途时不再发', async () => {
    let release!: (response: Response) => void
    const pending = new Promise<Response>((done) => {
      release = done
    })
    const urls = stubServer(() => pending)
    const state = useServerInfo()
    expect(state.initialized.value).toBe(false)
    const first = state.refresh()
    expect(state.loading.value).toBe(true)
    expect(await state.refresh()).toBe(false)
    release(ok(info()))
    expect(await first).toBe(true)
    expect(urls).toEqual(['GET /api/server'])
    expect(state.info.value?.processor.name).toBe('AMD Ryzen 9 7950X')
    expect([state.loading.value, state.initialized.value]).toEqual([false, true])
  })

  it('失败时抛出并保留上一次的数据，仍算取过一次', async () => {
    stubServer(() => ok(info()))
    const state = useServerInfo()
    await state.refresh()
    stubServer(() => new Response(JSON.stringify({ code: 'MAINTENANCE_UNAVAILABLE_009', status: 503, detail: 'down', traceId: 'x' }), { status: 503, headers: { 'Content-Type': 'application/problem+json' } }))
    await expect(state.refresh()).rejects.toMatchObject({ code: 'MAINTENANCE_UNAVAILABLE_009' })
    expect(state.info.value?.runtime.machineName).toBe('XIHAN-01')
    expect([state.loading.value, state.initialized.value]).toEqual([false, true])
  })

  it('定时刷新按 15 秒（4.x 同）；重复 start 不叠加，stop 之后不再触发', () => {
    vi.useFakeTimers()
    const tick = vi.fn()
    const polling = createPolling(tick)
    polling.start()
    polling.start()
    vi.advanceTimersByTime(REFRESH_INTERVAL_MS * 2)
    expect(tick).toHaveBeenCalledTimes(2)
    polling.stop()
    polling.stop()
    vi.advanceTimersByTime(REFRESH_INTERVAL_MS * 3)
    expect(tick).toHaveBeenCalledTimes(2)
    expect(REFRESH_INTERVAL_MS).toBe(15_000)
  })
})
