import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('TaskLog')

// -------- 类型 --------
export interface SysTaskLog {
  basicId?: string
  taskId?: string
  taskCode?: string
  taskName?: string
  batchNumber?: string
  serverName?: string
  processId?: number
  threadId?: number
  taskStatus?: number
  startTime?: string
  endTime?: string
  executionTime?: number
  executionResult?: string
  exceptionMessage?: string
  exceptionStackTrace?: string
  outputLog?: string
  memoryUsage?: number
  cpuUsage?: number
  retryCount?: number
  triggerMode?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

function normalize(raw: Record<string, any>): SysTaskLog {
  return {
    basicId: toId(raw.basicId),
    taskId: toId(raw.taskId),
    taskCode: raw.taskCode ?? '',
    taskName: raw.taskName ?? '',
    batchNumber: raw.batchNumber ?? '',
    serverName: raw.serverName ?? '',
    processId: toNumber(raw.processId),
    threadId: toNumber(raw.threadId),
    taskStatus: toNumber(raw.taskStatus),
    startTime: raw.startTime ?? '',
    endTime: raw.endTime ?? '',
    executionTime: toNumber(raw.executionTime),
    executionResult: raw.executionResult ?? '',
    exceptionMessage: raw.exceptionMessage ?? '',
    exceptionStackTrace: raw.exceptionStackTrace ?? '',
    outputLog: raw.outputLog ?? '',
    memoryUsage: toNumber(raw.memoryUsage),
    cpuUsage: toNumber(raw.cpuUsage),
    retryCount: toNumber(raw.retryCount),
    triggerMode: raw.triggerMode ?? '',
    extendData: raw.extendData ?? '',
    remark: raw.remark ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

// -------- API --------
export const taskLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalize(item)),
    })),
  clear: () => api.clear(),
}
