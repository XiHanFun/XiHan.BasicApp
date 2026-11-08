/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { codegenHistories, toPageRequest, toSchemaPageResult } from '~/contracts'
import { downloadEndpoint } from '~/request'
import { downloadBlob } from '~/utils'

export type HistoryRow = ResponseOf<'codegen-histories.list'>['items'][number]
export type HistoryDetail = ResponseOf<'codegen-histories.get'>

/** 生成历史的资源：只读分页，关键字匹配表名与批次号 */
export function historiesResource(): SchemaResource<HistoryRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await codegenHistories.list(toPageRequest(params, {
        ranges: ['generatedAt'],
        multiples: ['generationStatus'],
      })),
      params,
    ),
  }
}

/** 耗时：满一秒按秒、两位小数，否则按毫秒 */
export function formatDuration(ms: number): string {
  return ms >= 1000
    ? `${(ms / 1000).toFixed(2)}s`
    : `${ms}ms`
}

/** 大小：按 B / KB / MB */
export function formatSize(bytes: number): string {
  if (bytes >= 1024 * 1024)
    return `${(bytes / 1024 / 1024).toFixed(2)} MB`
  return bytes >= 1024
    ? `${(bytes / 1024).toFixed(2)} KB`
    : `${bytes} B`
}

/** 产物包还能不能下载：有产物包且没到期 */
export function hasPackage(row: Pick<HistoryRow, 'packageExpiresAt'>, now: number = Date.now()): boolean {
  return row.packageExpiresAt != null && new Date(row.packageExpiresAt).getTime() > now
}

/** 下载一次生成的产物包（普通端点，带令牌）：文件名取响应头，没有就按表名与批次号 */
export async function downloadPackage(row: Pick<HistoryRow, 'id' | 'tableName' | 'batchNumber'>): Promise<void> {
  const file = await downloadEndpoint(`/api/codegen-histories/${row.id}/package`)
  downloadBlob(file.blob, file.fileName ?? `${row.tableName}-${row.batchNumber ?? row.id}.zip`)
}
