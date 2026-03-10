import type { PageResult, SysTask, TaskPageQuery } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const TASK_API = '/api/Task'

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

function toTaskCreatePayload(data: Partial<SysTask>) {
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

function toTaskUpdatePayload(id: string, data: Partial<SysTask>) {
  return {
    ...toTaskCreatePayload(data),
    runTaskStatus: toNumber(data.runTaskStatus, 0),
    status: toNumber(data.status, 1),
    basicId: toNumber(id, 0),
  }
}

export async function getTaskPageApi(params: TaskPageQuery): Promise<PageResult<SysTask>> {
  const data = await requestClient.post<any>(
    `${TASK_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['TaskCode', 'TaskName', 'TaskClass'],
      filterFieldMap: {
        triggerType: 'TriggerType',
        runTaskStatus: 'RunTaskStatus',
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeTask)
}

export function getTaskDetailApi(id: string) {
  return requestClient
    .get<any>(`${TASK_API}/ById`, { params: { id } })
    .then(raw => normalizeTask(raw))
}

export function createTaskApi(data: Partial<SysTask>) {
  return requestClient.post<void>(`${TASK_API}/Create`, toTaskCreatePayload(data))
}

export function updateTaskApi(id: string, data: Partial<SysTask>) {
  return requestClient.put<void>(`${TASK_API}/Update`, toTaskUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteTaskApi(id: string) {
  return requestClient.delete<void>(`${TASK_API}/Delete`, {
    params: { id },
  })
}

export function getTaskByCodeApi(taskCode: string, tenantId?: number) {
  return requestClient
    .get<any>(`${TASK_API}/ByTaskCode/${tenantId ?? 0}`, {
      params: { taskCode },
    })
    .then(raw => (raw ? normalizeTask(raw) : null))
}
