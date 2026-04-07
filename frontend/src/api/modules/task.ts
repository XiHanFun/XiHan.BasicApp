import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Task')

export interface SysTask {
  basicId: string
  taskCode: string
  taskName: string
  taskDescription?: string
  taskGroup?: string
  taskClass: string
  taskMethod?: string
  taskParams?: string
  triggerType: number
  cronExpression?: string
  runTaskStatus: number
  priority: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface TaskPageQuery extends PageQuery {
  triggerType?: number
  runTaskStatus?: number
  status?: number
}

function normalizeTask(raw: Record<string, any>): SysTask {
  return {
    basicId: toId(raw.basicId),
    taskCode: raw.taskCode ?? '',
    taskName: raw.taskName ?? '',
    taskDescription: raw.taskDescription ?? '',
    taskGroup: raw.taskGroup ?? '',
    taskClass: raw.taskClass ?? '',
    taskMethod: raw.taskMethod ?? '',
    taskParams: raw.taskParams ?? '',
    triggerType: toNumber(raw.triggerType, 0),
    cronExpression: raw.cronExpression ?? '',
    runTaskStatus: toNumber(raw.runTaskStatus, 0),
    priority: toNumber(raw.priority, 3),
    status: toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysTask>) {
  return {
    taskCode: data.taskCode ?? '',
    taskName: data.taskName ?? '',
    taskDescription: data.taskDescription ?? '',
    taskGroup: data.taskGroup ?? '',
    taskClass: data.taskClass ?? '',
    taskMethod: data.taskMethod ?? '',
    taskParams: data.taskParams ?? '',
    triggerType: toNumber(data.triggerType, 0),
    cronExpression: data.cronExpression ?? '',
    priority: toNumber(data.priority, 3),
    allowConcurrent: false,
    maxRetryCount: 0,
    repeatCount: 0,
    timeoutSeconds: 0,
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysTask>) {
  return {
    ...toCreatePayload(data),
    runTaskStatus: toNumber(data.runTaskStatus, 0),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

export const taskApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['TaskCode', 'TaskName', 'TaskClass'],
      filterFieldMap: {
        triggerType: 'TriggerType',
        runTaskStatus: 'RunTaskStatus',
        status: 'Status',
      },
    }),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeTask),

  create: (data: Partial<SysTask>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysTask>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getByCode: (taskCode: string, tenantId?: number) =>
    api.request
      .get<any>(`${api.baseUrl}ByTaskCode/${tenantId ?? 0}`, { params: { taskCode } })
      .then((raw: any) => (raw ? normalizeTask(raw) : null)),
}
