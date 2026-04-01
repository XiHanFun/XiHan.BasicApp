import { useBaseApi } from '../base'

const api = useBaseApi('Server')

export interface SysRuntimeInfo {
  osName: string
  osDescription: string
  osVersion: string
  osArchitecture: string
  processArchitecture: string
  frameworkDescription: string
  runtimeVersion: string
  is64BitOperatingSystem: boolean
  is64BitProcess: boolean
  isInteractive: boolean
  interactiveMode: string
  processorCount: number
  systemDirectory: string
  currentDirectory: string
  machineName: string
  userName: string
  userDomainName: string
  workingSet: number
  systemStartTime: string
  systemUptime: string
  processStartTime: string
  processUptime: string
  processId: number
  processName: string
  clrVersion: string
  environmentVariableCount: number
  commandLineArgs: string[]
}

export interface SysCpuInfo {
  processorName: string
  processorArchitecture: string
  physicalCoreCount: number
  logicalCoreCount: number
  baseClockSpeed: number
  cacheBytes: number
  usagePercentage: number
}

export interface SysMemoryInfo {
  totalBytes: number
  usedBytes: number
  freeBytes: number
  availableBytes: number
  buffersCachedBytes: number
  usagePercentage: number
  availablePercentage: number
}

export interface SysDiskInfo {
  diskName: string
  typeName: string
  totalSpace: number
  freeSpace: number
  usedSpace: number
  availableRate: number
}

export interface SysNetworkIpAddress {
  address: string
  subnetMask: string
  prefixLength: number
}

export interface SysNetworkStatistics {
  bytesReceived: number
  bytesSent: number
  packetsReceived: number
  packetsSent: number
  incomingPacketsDiscarded: number
  outgoingPacketsDiscarded: number
  incomingPacketsWithErrors: number
  outgoingPacketsWithErrors: number
}

export interface SysNetworkInfo {
  name: string
  description: string
  type: string
  operationalStatus: string
  speed: string
  physicalAddress: string
  supportsMulticast: boolean
  isReceiveOnly: boolean
  dnsAddresses: string[]
  gatewayAddresses: string[]
  dhcpServerAddresses: string[]
  iPv4Addresses: SysNetworkIpAddress[]
  iPv6Addresses: SysNetworkIpAddress[]
  statistics?: SysNetworkStatistics
}

export interface SysBoardInfo {
  product: string
  manufacturer: string
  serialNumber: string
  version: string
}

export interface SysGpuInfo {
  name: string
  description: string
  vendor: string
  deviceId: string
  busInfo: string
  driverVersion: string
  memoryBytes: number
  temperature?: number
  videoModeDescription: string
  status: string
  utilizationPercentage?: number
  memoryUtilizationPercentage?: number
}

export interface SysServerInfo {
  runtimeInfo: SysRuntimeInfo
  cpuInfo: SysCpuInfo
  memoryInfo: SysMemoryInfo
  diskInfos: SysDiskInfo[]
  networkInfos: SysNetworkInfo[]
  boardInfo: SysBoardInfo
  gpuInfos: SysGpuInfo[]
  collectedAt: string
}

export interface SysNuGetPackage {
  packageName: string
  packageVersion: string
}

export const serverApi = {
  getServerInfo: (params?: { includeDisk?: boolean, includeNetwork?: boolean }) =>
    api.request.get<SysServerInfo>(`${api.baseUrl}ServerInfo`, { params }),

  getRuntimeInfo: () => api.request.get<SysRuntimeInfo>(`${api.baseUrl}RuntimeInfo`),

  getCpuInfo: () => api.request.get<SysCpuInfo>(`${api.baseUrl}CpuInfo`),

  getMemoryInfo: () => api.request.get<SysMemoryInfo>(`${api.baseUrl}MemoryInfo`),

  getDiskInfo: () => api.request.get<SysDiskInfo[]>(`${api.baseUrl}DiskInfo`),

  getNetworkInfo: () => api.request.get<SysNetworkInfo[]>(`${api.baseUrl}NetworkInfo`),

  getBoardInfo: () => api.request.get<SysBoardInfo>(`${api.baseUrl}BoardInfo`),

  getGpuInfo: () => api.request.get<SysGpuInfo[]>(`${api.baseUrl}GpuInfo`),

  getNuGetPackages: () => api.request.get<SysNuGetPackage[]>(`${api.baseUrl}NuGetPackages`),
}
