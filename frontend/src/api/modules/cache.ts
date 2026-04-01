import { useBaseApi } from '../base'

const api = useBaseApi('Cache')

export const cacheApi = {
  getString: (key: string) =>
    api.request.get<string | null>(`${api.baseUrl}String`, { params: { key } }),

  setString: (payload: { key: string, value: string, expireSeconds?: number }) =>
    api.request.post<void>(`${api.baseUrl}SetString`, undefined, {
      params: { key: payload.key, value: payload.value, expireSeconds: payload.expireSeconds ?? 300 },
    }),

  remove: (key: string) => api.request.delete<void>(`${api.baseUrl}Remove`, { params: { key } }),

  removeMany: (keys: string[]) => api.request.delete<void>(`${api.baseUrl}Many`, { data: keys }),

  exists: (key: string) =>
    api.request.post<boolean>(`${api.baseUrl}Exists`, undefined, { params: { key } }),

  getKeys: (pattern = '*') =>
    api.request.get<string[]>(`${api.baseUrl}Keys`, { params: { pattern } }),

  removeByPattern: (pattern = '*') =>
    api.request.delete<number>(`${api.baseUrl}ByPattern`, { params: { pattern } }),
}
