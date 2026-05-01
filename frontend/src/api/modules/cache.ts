import { createDynamicApiClient } from '../base'

const cacheApiClient = createDynamicApiClient('Cache')

export interface CacheStringSetInput {
  expireSeconds?: number
  key: string
  value: string
}

export interface CacheExistsResult {
  exists: boolean
  key: string
}

export interface CacheRemoveByPatternResult {
  pattern: string
  removedCount: number
}

export const cacheApi = {
  exists(key: string) {
    return cacheApiClient.post<boolean>('Exists', undefined, {
      params: { Key: key },
    })
  },
  getKeys(pattern = '*') {
    return cacheApiClient.get<string[]>('Keys', { Pattern: pattern })
  },
  getString(key: string) {
    return cacheApiClient.get<string | null>('String', { Key: key })
  },
  remove(key: string) {
    return cacheApiClient.delete('Remove', {
      params: { Key: key },
    })
  },
  removeByPattern(pattern = '*') {
    return cacheApiClient.delete<number>('ByPattern', {
      params: { Pattern: pattern },
    })
  },
  setString(input: CacheStringSetInput) {
    return cacheApiClient.post<void>('SetString', undefined, {
      params: {
        ExpireSeconds: input.expireSeconds,
        Key: input.key,
        Value: input.value,
      },
    })
  },
}
