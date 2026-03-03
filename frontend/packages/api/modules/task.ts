import type { PageResult, SysTask, TaskPageQuery } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getTaskPageApi(params: TaskPageQuery): Promise<PageResult<SysTask>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.tasks, { params })
  return normalizePageResult<SysTask>(data)
}

export function getTaskDetailApi(id: string) {
  return requestClient.get<SysTask>(`${API_CONTRACT.system.tasks}/${id}`)
}

export function createTaskApi(data: Partial<SysTask>) {
  return requestClient.post<void>(API_CONTRACT.system.tasks, data)
}

export function updateTaskApi(id: string, data: Partial<SysTask>) {
  return requestClient.put<void>(`${API_CONTRACT.system.tasks}/${id}`, data)
}

export function deleteTaskApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.tasks}/${id}`)
}

export function getTaskByCodeApi(taskCode: string, tenantId?: number) {
  return requestClient.get<SysTask | null>(`${API_CONTRACT.system.tasks}/by-code`, {
    params: { taskCode, tenantId },
  })
}
