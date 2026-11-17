/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import type { Translate } from '../jobs/job'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, ResponseOf, SchemaVersionStatus } from '~/contracts'
import { exportCenter, toPageRequest, toSchemaPageResult, versions as versionsApi } from '~/contracts'

/** 版本台账的一行：一个库一行（平台库为 platform，已就绪的独立库按租户 ID 各一行；读不到台账的状态为不可达并带 error） */
export type VersionRow = ResponseOf<'versions.list'>['items'][number]

/** 一个库升到当前应用版本还要做的结构变更 */
export type VersionPlan = ResponseOf<'versions.plan'>

/** 重试升级一个租户独立库的结果 */
export type VersionUpgrade = ResponseOf<'versions.upgrade'>

/** 平台库在台账里的库名 */
export const PLATFORM_DATABASE = 'platform'

/**
 * 列表的后端筛选：关键字匹配库名、应用版本、数据库版本与升级节点；应用版本、数据库版本、库类型与结构状态精确匹配
 * 台账由升级执行器写入；页面能预览、重试租户独立库的升级与释放留下的升级锁
 */
export const VERSION_QUERY: PageQueryMapping = {
  filters: {
    appVersion: 'eq',
    dbVersion: 'eq',
    provider: 'eq',
    status: 'eq',
  },
}

/** 库名：平台库显示「平台库」，租户库原样显示库名 */
export function databaseLabel(t: Translate, database: string): string {
  return database === PLATFORM_DATABASE
    ? t('maintenance.versions.platform_database')
    : database
}

const STATUS_KEYS: Record<SchemaVersionStatus, string> = {
  Current: 'maintenance.versions.status_current',
  Behind: 'maintenance.versions.status_behind',
  Upgrading: 'maintenance.versions.status_upgrading',
  Failed: 'maintenance.versions.status_failed',
  Unreachable: 'maintenance.versions.status_unreachable',
}

const STATUS_TONES: Record<SchemaVersionStatus, Tone> = {
  Current: 'success',
  Behind: 'warning',
  Upgrading: 'info',
  Failed: 'danger',
  Unreachable: 'danger',
}

export function statusLabel(t: Translate, status: SchemaVersionStatus): string {
  return t(STATUS_KEYS[status])
}

/** 结构状态的语气：最新绿、待升级黄、升级中蓝、失败与不可达红 */
export function statusTone(status: SchemaVersionStatus): Tone {
  return STATUS_TONES[status]
}

/** 结构状态的搜索选项：后端按 `status eq` 匹配 */
export function statusOptions(t: Translate): { label: string, value: string }[] {
  return (Object.keys(STATUS_KEYS) as SchemaVersionStatus[]).map(status => ({
    label: statusLabel(t, status),
    value: status,
  }))
}

/** 能在页面上重试升级的库：读得到台账的租户独立库，且失败了或还没升到应用的版本（平台库在节点启动时升级） */
export function canUpgrade(row: VersionRow): boolean {
  return row.database !== PLATFORM_DATABASE && (row.status === 'Failed' || row.status === 'Behind')
}

/** 能释放的锁：台账上还标着升级中（过期的锁也算） */
export function canReleaseLock(row: VersionRow): boolean {
  return row.isUpgrading && !!row.upgradeNode
}

/** 列表行：库名的显示文本是列也是导出字段（导出按行里的键取值），库名本身留作行键与详情的 ID */
export type VersionListRow = VersionRow & { databaseName: string }

/** SchemaPage 的资源：分页与导出；导出中心的库名取原值（platform 或租户 ID） */
export function versionResource(t: Translate): SchemaResource<VersionListRow> {
  return {
    page: async (params: SchemaQueryParams) => {
      const result = await versionsApi.list(toPageRequest(params, VERSION_QUERY))
      return toSchemaPageResult({
        ...result,
        items: result.items.map(row => ({
          ...row,
          databaseName: databaseLabel(t, row.database),
        })),
      }, params)
    },
    export: exportCenter('versions.list', VERSION_QUERY, {
      columnPaths: { databaseName: 'database' },
    }),
  }
}

/**
 * 按库名（platform 或租户 ID）取最新的版本详情
 * 每个库的台账行 ID 相同，列表的行键也用库名
 */
export function getVersion(database: string): Promise<VersionRow> {
  return versionsApi.get({ id: database })
}

/** 预览一个库的结构升级（只读，不执行） */
export function planVersion(database: string): Promise<VersionPlan> {
  return versionsApi.plan({ id: database })
}

/** 重试升级一个租户独立库 */
export function upgradeVersion(database: string): Promise<VersionUpgrade> {
  return versionsApi.upgrade({ id: database })
}

/** 释放节点留下的升级锁：锁还在这个节点手里才释放 */
export function releaseVersionLock(database: string, node: string): Promise<VersionRow> {
  return versionsApi.releaseLock({ id: database, node })
}
