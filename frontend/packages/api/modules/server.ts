import type {
  SysBoardInfo,
  SysCpuInfo,
  SysDiskInfo,
  SysGpuInfo,
  SysMemoryInfo,
  SysNetworkInfo,
  SysNuGetPackage,
  SysRuntimeInfo,
  SysServerInfo,
} from '~/types'
import requestClient from '../request'

const SERVER_API = '/api/Server'

export function getServerInfoApi(params?: { includeDisk?: boolean, includeNetwork?: boolean }) {
  return requestClient.get<SysServerInfo>(`${SERVER_API}/ServerInfo`, { params })
}

export function getRuntimeInfoApi() {
  return requestClient.get<SysRuntimeInfo>(`${SERVER_API}/RuntimeInfo`)
}

export function getCpuInfoApi() {
  return requestClient.get<SysCpuInfo>(`${SERVER_API}/CpuInfo`)
}

export function getMemoryInfoApi() {
  return requestClient.get<SysMemoryInfo>(`${SERVER_API}/MemoryInfo`)
}

export function getDiskInfoApi() {
  return requestClient.get<SysDiskInfo[]>(`${SERVER_API}/DiskInfo`)
}

export function getNetworkInfoApi() {
  return requestClient.get<SysNetworkInfo[]>(`${SERVER_API}/NetworkInfo`)
}

export function getBoardInfoApi() {
  return requestClient.get<SysBoardInfo>(`${SERVER_API}/BoardInfo`)
}

export function getGpuInfoApi() {
  return requestClient.get<SysGpuInfo[]>(`${SERVER_API}/GpuInfo`)
}

export function getNuGetPackagesApi() {
  return requestClient.get<SysNuGetPackage[]>(`${SERVER_API}/NuGetPackages`)
}
