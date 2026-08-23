import type { ApiId, PageResult } from '../../types'
import type {
  MigrationHistoryDetailDto,
  MigrationHistoryListItemDto,
  MigrationHistoryPageQueryDto,
  VersionDetailDto,
  VersionListItemDto,
  VersionPageQueryDto,
} from './version.types'
import { createDynamicApiClient } from '../../base'

const versionQueryApi = createDynamicApiClient('VersionQuery')

export const versionApi = {
  // Query
  detail(id: ApiId) {
    return versionQueryApi.get<VersionDetailDto | null>(
      'VersionDetail',
      { id },
    )
  },
  migrationHistoryDetail(id: ApiId) {
    return versionQueryApi.get<MigrationHistoryDetailDto | null>(
      'MigrationHistoryDetail',
      { id },
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
}
