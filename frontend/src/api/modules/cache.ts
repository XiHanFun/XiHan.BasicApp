import requestClient from '../request'

const CACHE_API = '/api/Cache'

export function getCacheStringApi(key: string) {
  return requestClient.get<string | null>(`${CACHE_API}/String`, {
    params: { key },
  })
}

export function setCacheStringApi(payload: { key: string, value: string, expireSeconds?: number }) {
  return requestClient.post<void>(`${CACHE_API}/SetString`, undefined, {
    params: {
      key: payload.key,
      value: payload.value,
      expireSeconds: payload.expireSeconds ?? 300,
    },
  })
}

export function removeCacheApi(key: string) {
  return requestClient.delete<void>(`${CACHE_API}/Remove`, {
    params: { key },
  })
}

export function removeManyCacheApi(keys: string[]) {
  return requestClient.delete<void>(`${CACHE_API}/Many`, {
    data: keys,
  })
}

export function existsCacheApi(key: string) {
  return requestClient.post<boolean>(`${CACHE_API}/Exists`, undefined, {
    params: { key },
  })
}

export function getCacheKeysApi(pattern = '*') {
  return requestClient.get<string[]>(`${CACHE_API}/Keys`, {
    params: { pattern },
  })
}

export function removeCacheByPatternApi(pattern = '*') {
  return requestClient.delete<number>(`${CACHE_API}/ByPattern`, {
    params: { pattern },
  })
}
