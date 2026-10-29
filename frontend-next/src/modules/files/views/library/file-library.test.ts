import type { FileRow, FileStorageRow } from './file-library'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { QueryOperator } from '~/types/contracts'
import {
  accessLevelLabel,
  batchFailureMessage,
  canPreview,
  canSetPrimary,
  defaultExpiry,
  emptyDetailsForm,
  FILE_ACCESS_LEVELS,
  FILE_QUERY,
  isTextPreview,
  normalizeExtension,
  previewKindOf,
  sourceLabel,
  storageReasonLabel,
  storageToggleTarget,
  textLangOf,
  toCsvTable,
  toDetailsForm,
  toFileDetails,
} from './file-library'

const t = (key: string, values?: Record<string, unknown>) => `${key}${values ? JSON.stringify(values) : ''}`

function file(patch: Partial<FileRow> = {}): FileRow {
  return {
    id: 'f1',
    name: '0b6c.png',
    originalName: 'logo.png',
    extension: '.png',
    kind: 'Image',
    contentType: 'image/png',
    size: 1024,
    hash: 'abc',
    status: 'Normal',
    accessLevel: 'Authorized',
    isTemporary: false,
    expiresAt: null,
    source: 'upload',
    tags: null,
    remark: null,
    downloadCount: 0,
    lastDownloadedAt: null,
    createdAt: '2026-10-01T00:00:00Z',
    createdBy: null,
    modifiedAt: null,
    ...patch,
  }
}

function storage(patch: Partial<FileStorageRow> = {}): FileStorageRow {
  return { id: 's1', storageConfigId: 'c1', storageConfigCode: 'local', storageConfigName: '本地', storageType: 'Local', bucket: 'files', key: 'k', isPrimary: false, status: 'Normal', verifiedAt: '2026-10-01T00:00:00Z', remark: null, createdAt: '2026-10-01T00:00:00Z', ...patch }
}

describe('文件列表的后端筛选', () => {
  it('大类、状态、访问级别多选合成 in，上传时间区间拆成 ge / le，临时与来源精确匹配', () => {
    const request = toPageRequest({
      page: 2,
      pageSize: 20,
      filters: { keyword: ' logo ', kind: ['Image', 'Video'], status: ['Normal'], accessLevel: ['Authorized'], createdAt: [1, 2], isTemporary: 'true', source: 'fast-upload' },
      conditionFilters: [
        { field: 'kind', operator: QueryOperator.In, values: ['Image', 'Video'] },
        { field: 'status', operator: QueryOperator.In, values: ['Normal'] },
        { field: 'accessLevel', operator: QueryOperator.In, values: ['Authorized'] },
        { field: 'createdAt', operator: QueryOperator.Between, values: ['2026-10-01T00:00:00.000Z', '2026-10-02T00:00:00.000Z'] },
      ],
      sorts: [{ field: 'size', order: 'desc' }],
    }, FILE_QUERY)
    expect(request.keyword).toBe('logo')
    expect(request.filters).toEqual([
      { field: 'kind', operator: 'in', value: 'Image,Video' },
      { field: 'status', operator: 'in', value: 'Normal' },
      { field: 'accessLevel', operator: 'in', value: 'Authorized' },
      { field: 'createdAt', operator: 'ge', value: '2026-10-01T00:00:00.000Z' },
      { field: 'createdAt', operator: 'le', value: '2026-10-02T00:00:00.000Z' },
      { field: 'isTemporary', operator: 'eq', value: 'true' },
      { field: 'source', operator: 'eq', value: 'fast-upload' },
    ])
    expect(request.sorts).toEqual([{ field: 'size', descending: true }])
  })

  it('扩展名补上点并转小写，空值原样', () => {
    expect(normalizeExtension('PNG')).toBe('.png')
    expect(normalizeExtension(' .Md ')).toBe('.md')
    expect(normalizeExtension('')).toBe('')
    expect(normalizeExtension(undefined)).toBeUndefined()
  })

  it('批量操作的失败码认得的翻成文案，其余原样', () => {
    expect(batchFailureMessage(t, 'FILES_CONFLICT_005')).toBe('files.library.batch.unavailable')
    expect(batchFailureMessage(t, 'FILES_NOTFOUND_001')).toBe('files.library.batch.not_found')
    expect(batchFailureMessage(t, 'X_Y_001')).toBe('X_Y_001')
  })
})

describe('上传与元数据', () => {
  it('非临时文件不带到期时间，空串写成 null', () => {
    const form = { ...emptyDetailsForm(), expiresAt: Date.UTC(2026, 10, 1), tags: '  ', remark: ' 合同 ' }
    expect(toFileDetails(form)).toEqual({ accessLevel: 'Authorized', isTemporary: false, expiresAt: null, tags: null, remark: '合同' })
    expect(toFileDetails({ ...form, isTemporary: true })).toMatchObject({ isTemporary: true, expiresAt: '2026-11-01T00:00:00.000Z' })
  })

  it('已有文件回填表单，临时文件缺省保留 7 天', () => {
    expect(toDetailsForm(file({ isTemporary: true, expiresAt: '2026-11-01T00:00:00Z', tags: 'a,b', accessLevel: 'Authenticated' }))).toEqual({
      accessLevel: 'Authenticated',
      isTemporary: true,
      expiresAt: Date.UTC(2026, 10, 1),
      tags: 'a,b',
      remark: '',
    })
    expect(defaultExpiry(0)).toBe(7 * 24 * 60 * 60 * 1000)
  })

  it('访问级别三档（含头像用的公开）各有文案，上传与修改的下拉照此列出', () => {
    expect(FILE_ACCESS_LEVELS.map(level => accessLevelLabel(t, level))).toEqual([
      'files.library.access_levels.authenticated',
      'files.library.access_levels.authorized',
      'files.library.access_levels.public',
    ])
  })
})

describe('预览', () => {
  it('按内容类型、扩展名与大类判定预览种类，渲染不了的为空', () => {
    expect(previewKindOf(file())).toBe('image')
    expect(previewKindOf(file({ kind: 'Video', contentType: 'video/mp4', extension: '.mp4' }))).toBe('video')
    expect(previewKindOf(file({ kind: 'Audio', contentType: 'application/octet-stream', extension: '.mp3' }))).toBe('audio')
    expect(previewKindOf(file({ kind: 'Document', contentType: 'text/markdown', extension: '.md' }))).toBe('markdown')
    expect(previewKindOf(file({ kind: 'Document', contentType: 'text/csv', extension: '.csv' }))).toBe('csv')
    expect(previewKindOf(file({ kind: 'Document', contentType: 'application/json', extension: '.json' }))).toBe('text')
    expect(previewKindOf(file({ kind: 'Archive', contentType: 'application/zip', extension: '.zip' }))).toBeNull()
    expect(textLangOf(file({ extension: '.ts' }))).toBe('typescript')
    expect(textLangOf(file({ extension: '.txt' }))).toBeUndefined()
    expect([isTextPreview('csv'), isTextPreview('markdown'), isTextPreview('image'), isTextPreview(null)]).toEqual([true, true, false, false])
  })

  it('只有正常的文件给预览入口', () => {
    expect(canPreview(file())).toBe(true)
    expect(canPreview(file({ status: 'Archived' }))).toBe(false)
    expect(canPreview(file({ kind: 'Other', contentType: 'application/octet-stream', extension: null }))).toBe(false)
  })

  it('逗号分隔表格首行作表头，缺的格补空串，行号作行主键', () => {
    expect(toCsvTable('名称,数量\n苹果,3\n梨\n')).toEqual({
      header: ['名称', '数量'],
      rows: [{ 0: '苹果', 1: '3', _row: '1' }, { 0: '梨', 1: '', _row: '2' }],
    })
    expect(toCsvTable('')).toEqual({ header: [], rows: [] })
  })
})

describe('存储副本', () => {
  it('正常且校验过的非主副本才能设为主副本', () => {
    expect(canSetPrimary(storage())).toBe(true)
    expect(canSetPrimary(storage({ isPrimary: true }))).toBe(false)
    expect(canSetPrimary(storage({ verifiedAt: null }))).toBe(false)
    expect(canSetPrimary(storage({ status: 'Unavailable' }))).toBe(false)
  })

  it('启停的目标：正常 ↔ 停用；主副本不能停用，校验失败的要重新校验', () => {
    expect(storageToggleTarget(storage())).toBe('Unavailable')
    expect(storageToggleTarget(storage({ status: 'Unavailable' }))).toBe('Normal')
    expect(storageToggleTarget(storage({ isPrimary: true }))).toBeNull()
    expect(storageToggleTarget(storage({ status: 'VerificationFailed' }))).toBeNull()
  })

  it('校验失败的原因与来源翻成文案，认不得的原样', () => {
    expect(storageReasonLabel(t, 'hash-mismatch')).toBe('files.library.storage_reasons.hash_mismatch')
    expect(storageReasonLabel(t, 'missing')).toBe('files.library.storage_reasons.missing')
    expect(storageReasonLabel(t, null)).toBe('')
    expect(sourceLabel(t, 'fast-upload')).toBe('files.library.sources.fast_upload')
    expect(sourceLabel(t, 'import')).toBe('import')
  })
})
