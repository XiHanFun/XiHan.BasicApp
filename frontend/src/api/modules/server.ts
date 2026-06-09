import { createDynamicApiClient } from '../base'

const serverApiClient = createDynamicApiClient('Server')

export interface SysRuntimeInfo {
  clrVersion: string
  commandLineArgs: string[]
  currentDirectory: string
  environmentVariableCount: number
  frameworkDescription: string
  interactiveMode: string
  is64BitOperatingSystem: boolean
  is64BitProcess: boolean
  isInteractive: boolean
  machineName: string
  osArchitecture: string
  osDescription: string
  osName: string
  osVersion: string
  processArchitecture: string
  processId: number
  processName: string
  processStartTime: string
  processUptime: string
  processorCount: number
  runtimeVersion: string
  systemDirectory: string
  systemStartTime: string
  systemUptime: string
  userDomainName: string
  userName: string
  workingSet: number
}

export interface SysCpuInfo {
  baseClockSpeed: number
  cacheBytes: number
  logicalCoreCount: number
  physicalCoreCount: number
  processorArchitecture: string
  processorName: string
  usagePercentage: number
}

export interface SysMemoryInfo {
  availableBytes: number
  availablePercentage: number
  buffersCachedBytes: number
  freeBytes: number
  totalBytes: number
  usagePercentage: number
  usedBytes: number
}

export interface SysDiskInfo {
  availableRate: number
  diskName: string
  freeSpace: number
  totalSpace: number
  typeName: string
  usedSpace: number
}

export interface SysNetworkIpAddress {
  address: string
  prefixLength: number
  subnetMask: string
}

export interface SysNetworkStatistics {
  bytesReceived: number
  bytesSent: number
  incomingPacketsDiscarded: number
  incomingPacketsWithErrors: number
  outgoingPacketsDiscarded: number
  outgoingPacketsWithErrors: number
  packetsReceived: number
  packetsSent: number
}

export interface SysNetworkInfo {
  description: string
  dhcpServerAddresses: string[]
  dnsAddresses: string[]
  gatewayAddresses: string[]
  iPv4Addresses: SysNetworkIpAddress[]
  iPv6Addresses: SysNetworkIpAddress[]
  isReceiveOnly: boolean
  name: string
  operationalStatus: string
  physicalAddress: string
  speed: string
  statistics?: SysNetworkStatistics
  supportsMulticast: boolean
  type: string
}

export interface SysBoardInfo {
  manufacturer: string
  product: string
  serialNumber: string
  version: string
}

export interface SysGpuInfo {
  busInfo: string
  description: string
  deviceId: string
  driverVersion: string
  memoryBytes: number
  memoryUtilizationPercentage?: number
  name: string
  status: string
  temperature?: number
  utilizationPercentage?: number
  vendor: string
  videoModeDescription: string
}

export interface SysServerInfo {
  boardInfo: SysBoardInfo
  collectedAt: string
  cpuInfo: SysCpuInfo
  diskInfos: SysDiskInfo[]
  gpuInfos: SysGpuInfo[]
  memoryInfo: SysMemoryInfo
  networkInfos: SysNetworkInfo[]
  runtimeInfo: SysRuntimeInfo
}

export interface SysNuGetPackage {
  packageName: string
  packageVersion: string
}

export const serverApi = {
  getBoardInfo() {
    return serverApiClient.get<SysBoardInfo>('BoardInfo')
  },
  getCpuInfo() {
    return serverApiClient.get<SysCpuInfo>('CpuInfo')
  },
  getDiskInfo() {
    return serverApiClient.get<SysDiskInfo[]>('DiskInfo')
  },
  getGpuInfo() {
    return serverApiClient.get<SysGpuInfo[]>('GpuInfo')
  },
  getMemoryInfo() {
    return serverApiClient.get<SysMemoryInfo>('MemoryInfo')
  },
  getNetworkInfo() {
    return serverApiClient.get<SysNetworkInfo[]>('NetworkInfo')
  },
  getNuGetPackages() {
    return serverApiClient.get<SysNuGetPackage[]>('NuGetPackages')
  },
  getRuntimeInfo() {
    return serverApiClient.get<SysRuntimeInfo>('RuntimeInfo')
  },
  getServerInfo(params?: { includeDisk?: boolean, includeNetwork?: boolean }) {
    return serverApiClient.get<SysServerInfo>('ServerInfo', {
      IncludeDisk: params?.includeDisk,
      IncludeNetwork: params?.includeNetwork,
    })
  },
}
