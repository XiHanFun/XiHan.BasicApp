import { createDynamicApiClient } from '../base'

const cacheApiClient = createDynamicApiClient('Cache')

export const cacheApi = {
  /** 判断键是否存在 */
  exists(key: string) {
    return cacheApiClient.post<boolean>('Exists', undefined, {
      params: { Key: key },
    })
  },
  /** 按模式获取键列表 */
  getKeys(pattern = '*') {
    return cacheApiClient.get<string[]>('Keys', { Pattern: pattern })
  },
  /** 获取字符串值 */
  getString(key: string) {
    return cacheApiClient.get<null | string>('String', { Key: key })
  },
  /** 更新字符串值（鉴权关键命名空间禁止改写） */
  updateString(key: string, value: null | string) {
    return cacheApiClient.put<void>('String', { key, value })
  },
  /** 删除单个键 */
  remove(key: string) {
    return cacheApiClient.delete('Remove', {
      params: { Key: key },
    })
  },
  /** 按模式批量删除键 */
  removeByPattern(pattern = '*') {
    return cacheApiClient.delete<number>('ByPattern', {
      params: { Pattern: pattern },
    })
  },
}
