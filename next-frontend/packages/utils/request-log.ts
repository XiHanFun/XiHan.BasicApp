import type { FrontendRequestLog } from '~/types'
import { readonly, ref } from 'vue'

const MAX_REQUEST_LOG_COUNT = 300
const requestLogs = ref<FrontendRequestLog[]>([])

export function appendRequestLog(log: FrontendRequestLog) {
  requestLogs.value = [log, ...requestLogs.value].slice(0, MAX_REQUEST_LOG_COUNT)
}

export function updateRequestLog(requestId: string, patch: Partial<FrontendRequestLog>) {
  requestLogs.value = requestLogs.value.map(log =>
    log.requestId === requestId
      ? {
          ...log,
          ...patch,
        }
      : log,
  )
}

export function clearRequestLogs() {
  requestLogs.value = []
}

export function useRequestLogs() {
  return readonly(requestLogs)
}
