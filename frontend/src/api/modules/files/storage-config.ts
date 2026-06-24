import type { ApiId, PageResult } from '../../types'
import type {
  StorageConfigCreateDto,
  StorageConfigDefaultUpdateDto,
  StorageConfigDetailDto,
  StorageConfigListItemDto,
  StorageConfigPageQueryDto,
  StorageConfigStatusUpdateDto,
  StorageConfigUpdateDto,
} from './storage-config.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const storageConfigQueryApi = createDynamicApiClient('StorageConfigQuery')
const storageConfigCommandApi = createDynamicApiClient('StorageConfig')

export const storageConfigApi = {
  create(input: StorageConfigCreateDto) {
    return storageConfigCommandApi.post<StorageConfigDetailDto, StorageConfigCreateDto>('StorageConfig', input)
  },
  delete(id: ApiId) {
    return storageConfigCommandApi.delete(`StorageConfig/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return storageConfigQueryApi.get<StorageConfigDetailDto | null>(
      `StorageConfigDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: StorageConfigPageQueryDto) {
    return storageConfigQueryApi.post<PageResult<StorageConfigListItemDto>>('StorageConfigPage', input)
  },
  setDefault(input: StorageConfigDefaultUpdateDto) {
    // 后端 SetDefaultStorageConfigAsync：Set 前缀不在动词约定表，保留完整方法名，默认 POST
    return storageConfigCommandApi.post<StorageConfigDetailDto, StorageConfigDefaultUpdateDto>(
      'SetDefaultStorageConfig',
      input,
    )
  },
  update(input: StorageConfigUpdateDto) {
    return storageConfigCommandApi.put<StorageConfigDetailDto, StorageConfigUpdateDto>('StorageConfig', input)
  },
  updateStatus(input: StorageConfigStatusUpdateDto) {
    return storageConfigCommandApi.put<StorageConfigDetailDto, StorageConfigStatusUpdateDto>('StorageConfigStatus', input)
  },
}
