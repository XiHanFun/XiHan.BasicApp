import type { ApiId, PageResult } from '../../types'
import type {
  MigrationHistoryDetailDto,
  MigrationHistoryListItemDto,
  MigrationHistoryPageQueryDto,
  VersionCreateDto,
  VersionDetailDto,
  VersionListItemDto,
  VersionPageQueryDto,
  VersionUpdateDto,
  VersionUpgradeFinishDto,
  VersionUpgradeStartDto,
} from './version.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const versionQueryApi = createDynamicApiClient('VersionQuery')
const versionCommandApi = createDynamicApiClient('Version')

export const versionApi = {
  // Query
  detail(id: ApiId) {
    return versionQueryApi.get<VersionDetailDto | null>(
      `VersionDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  migrationHistoryDetail(id: ApiId) {
    return versionQueryApi.get<MigrationHistoryDetailDto | null>(
      `MigrationHistoryDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  migrationHistoryPage(input: MigrationHistoryPageQueryDto) {
    return versionQueryApi.post<PageResult<MigrationHistoryListItemDto>>(
      'MigrationHistoryPage',
      input,
    )
  },
  page(input: VersionPageQueryDto) {
    return versionQueryApi.post<PageResult<VersionListItemDto>>('VersionPage', input)
  },
  // Commands（动态 API 会剥离方法名动词前缀 Create/Update/Delete：实际路由不含动词）
  create(input: VersionCreateDto) {
    return versionCommandApi.post<VersionDetailDto, VersionCreateDto>('Version', input)
  },
  delete(id: ApiId) {
    return versionCommandApi.delete(`Version/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: VersionUpdateDto) {
    return versionCommandApi.put<VersionDetailDto, VersionUpdateDto>('Version', input)
  },
  // Start/Finish 不在动词剥离表内：路由保留完整方法名（默认 POST）
  finishUpgrade(input: VersionUpgradeFinishDto) {
    return versionCommandApi.post<VersionDetailDto, VersionUpgradeFinishDto>('FinishVersionUpgrade', input)
  },
  startUpgrade(input: VersionUpgradeStartDto) {
    return versionCommandApi.post<VersionDetailDto, VersionUpgradeStartDto>('StartVersionUpgrade', input)
  },
}
