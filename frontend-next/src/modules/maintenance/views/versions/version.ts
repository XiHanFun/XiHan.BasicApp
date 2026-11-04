import type { Tone } from '@xihan-ui/core'
import type { Translate } from '../jobs/job'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, ResponseOf } from '~/contracts'
import { toPageRequest, toSchemaPageResult, versions as versionsApi } from '~/contracts'

/** 版本台账的一行：一个库一行（平台库为 platform，已就绪的独立库按租户 ID 各一行；读不到台账的带 error） */
export type VersionRow = ResponseOf<'versions.list'>['items'][number]

/** 平台库在台账里的库名 */
export const PLATFORM_DATABASE = 'platform'

/**
 * 列表的后端筛选：关键字匹配库名、应用版本、数据库版本与升级节点；应用版本、数据库版本与是否升级中精确匹配
 * 台账由升级执行器写入（升级中即持有升级锁），页面只读
 */
export const VERSION_QUERY: PageQueryMapping = {
  filters: { appVersion: 'eq', dbVersion: 'eq', isUpgrading: 'eq' },
}

/** 库名：平台库显示「平台库」，租户库原样显示库名 */
export function databaseLabel(t: Translate, database: string): string {
  return database === PLATFORM_DATABASE ? t('maintenance.versions.platform_database') : database
}

export function upgradingLabel(t: Translate, upgrading: boolean): string {
  return upgrading ? t('maintenance.versions.upgrading') : t('maintenance.versions.normal')
}

/** 升级状态的语气（4.x 同）：升级中黄、正常绿 */
export function upgradingTone(upgrading: boolean): Tone {
  return upgrading ? 'warning' : 'success'
}

/** 升级状态的搜索选项：后端按 `isUpgrading eq true / false` 匹配 */
export function upgradingOptions(t: Translate): { label: string, value: string }[] {
  return [
    { label: upgradingLabel(t, true), value: 'true' },
    { label: upgradingLabel(t, false), value: 'false' },
  ]
}

/** 列表行：库名的显示文本是列也是导出字段（导出按行里的键取值），库名本身留作行键与详情的 ID */
export type VersionListRow = VersionRow & { databaseName: string }

/** SchemaPage 的资源：只有分页（没有写操作，导出是 SchemaPage 的本地 CSV） */
export function versionResource(t: Translate): SchemaResource<VersionListRow> {
  return {
    page: async (params: SchemaQueryParams) => {
      const result = await versionsApi.list(toPageRequest(params, VERSION_QUERY))
      return toSchemaPageResult({ ...result, items: result.items.map(row => ({ ...row, databaseName: databaseLabel(t, row.database) })) }, params)
    },
  }
}

/** 详情：按库名（platform 或租户 ID）取最新一份；每个库的台账行 ID 相同，列表的行键也用库名 */
export function getVersion(database: string): Promise<VersionRow> {
  return versionsApi.get({ id: database })
}
