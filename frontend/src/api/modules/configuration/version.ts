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
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

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
    return versionQueryApi.get<PageResult<MigrationHistoryListItemDto>>(
      'MigrationHistoryPage',
      toMigrationHistoryPageParams(input),
    )
  },
  page(input: VersionPageQueryDto) {
    return versionQueryApi.get<PageResult<VersionListItemDto>>(
      'VersionPage',
      toVersionPageParams(input),
    )
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

function toVersionPageParams(input: VersionPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'AppVersion', input.appVersion)
  appendDynamicApiParam(params, 'DbVersion', input.dbVersion)
  appendDynamicApiParam(params, 'IsUpgrading', input.isUpgrading)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MinSupportVersion', input.minSupportVersion)
  appendDynamicApiParam(params, 'UpgradeNode', input.upgradeNode)
  appendDynamicApiParam(params, 'UpgradeStartTimeEnd', input.upgradeStartTimeEnd)
  appendDynamicApiParam(params, 'UpgradeStartTimeStart', input.upgradeStartTimeStart)
  return params
}

function toMigrationHistoryPageParams(input: MigrationHistoryPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ExecutedTimeEnd', input.executedTimeEnd)
  appendDynamicApiParam(params, 'ExecutedTimeStart', input.executedTimeStart)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'NodeName', input.nodeName)
  appendDynamicApiParam(params, 'ScriptName', input.scriptName)
  appendDynamicApiParam(params, 'Success', input.success)
  appendDynamicApiParam(params, 'Version', input.version)
  return params
}
