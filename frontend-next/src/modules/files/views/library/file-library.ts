import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, ResponseOf } from '~/contracts'
import { files as filesApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { parseCsvRows } from '~/utils'

export type FileRow = ResponseOf<'files.list'>['items'][number]
export type FileKind = FileRow['kind']
export type FileStatus = FileRow['status']
export type FileAccessLevel = FileRow['accessLevel']
export type FileStorageRow = ResponseOf<'files.storages'>['items'][number]
export type FileStorageStatus = FileStorageRow['status']

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 色调，与 XiHan.UI 标签一致 */
export type FileTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning'

export const FILE_KINDS: readonly FileKind[] = ['Image', 'Document', 'Video', 'Audio', 'Archive', 'Other']
export const FILE_STATUSES: readonly FileStatus[] = ['Normal', 'Archived', 'Corrupted']
export const FILE_ACCESS_LEVELS: readonly FileAccessLevel[] = ['Authenticated', 'Authorized']
/** 来源（后端写入的字面量） */
export const FILE_SOURCES = ['upload', 'fast-upload'] as const

/**
 * 列表的后端筛选（与后端白名单一致）：关键字匹配原始文件名、系统文件名、扩展名、内容类型与标签；
 * 大类、状态、访问级别多选（合成 in）；上传时间、到期时间按区间（拆成 ge / le）；临时、来源、扩展名、内容类型精确匹配。
 */
export const FILE_QUERY: PageQueryMapping = {
  multiples: ['kind', 'status', 'accessLevel'],
  ranges: ['createdAt', 'expiresAt'],
  filters: { isTemporary: 'eq', source: 'eq', extension: 'eq', contentType: 'eq' },
}

/** 后端存的扩展名是小写带点（`.png`）：搜索时写成 `png`、`PNG` 也能命中 */
export function normalizeExtension(value: unknown): unknown {
  if (typeof value !== 'string' || !value.trim())
    return value
  const extension = value.trim().toLowerCase()
  return extension.startsWith('.') ? extension : `.${extension}`
}

/** 批量操作只回失败项的错误码：认得的翻成文案，其余原样 */
export function batchFailureMessage(t: Translate, code: string): string {
  switch (code) {
    case 'FILES_NOTFOUND_001': return t('files.library.batch.not_found')
    case 'FILES_CONFLICT_005': return t('files.library.batch.unavailable')
    default: return code
  }
}

export function throwOnFailed(t: Translate, result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(failure => batchFailureMessage(t, failure.code)).join('; '))
}

/** SchemaPage 的资源：分页（缺省按上传时间倒序）与删除（内容不再被别的文件用到时随之从存储里删除） */
export function fileResource(t: Translate): SchemaResource<FileRow> {
  return {
    page: async (params: SchemaQueryParams) => {
      const normalized = { ...params, filters: { ...params.filters, extension: normalizeExtension(params.filters.extension) } }
      return toSchemaPageResult(await filesApi.list(toPageRequest(normalized, FILE_QUERY)), params)
    },
    remove: async id => throwOnFailed(t, await filesApi.delete({ ids: [String(id)] })),
  }
}

// ── 上传与元数据 ────────────────────────────────────────────────

/** 上传与编辑元数据共用的表单（到期时间是时间戳，只对临时文件生效） */
export interface FileDetailsForm {
  accessLevel: FileAccessLevel
  isTemporary: boolean
  expiresAt: null | number
  tags: string
  remark: string
}

/** 临时文件缺省保留 7 天（4.x 的保留天数缺省值） */
export const DEFAULT_RETENTION_DAYS = 7

export function defaultExpiry(now = Date.now()): number {
  return now + DEFAULT_RETENTION_DAYS * 24 * 60 * 60 * 1000
}

export function emptyDetailsForm(): FileDetailsForm {
  return { accessLevel: 'Authorized', isTemporary: false, expiresAt: null, tags: '', remark: '' }
}

export function toDetailsForm(file: FileRow): FileDetailsForm {
  return {
    accessLevel: file.accessLevel,
    isTemporary: file.isTemporary,
    expiresAt: file.expiresAt ? new Date(file.expiresAt).getTime() : null,
    tags: file.tags ?? '',
    remark: file.remark ?? '',
  }
}

/** 表单 → 上传与 files.update 的字段：非临时文件不带到期时间，空串写成 null */
export function toFileDetails(form: FileDetailsForm) {
  return {
    accessLevel: form.accessLevel,
    isTemporary: form.isTemporary,
    expiresAt: form.isTemporary && form.expiresAt != null ? new Date(form.expiresAt).toISOString() : null,
    tags: form.tags.trim() || null,
    remark: form.remark.trim() || null,
  }
}

// ── 预览 ────────────────────────────────────────────────────────

/** 浏览器直接渲染得了的几类（4.x 同） */
export type PreviewKind = 'audio' | 'csv' | 'image' | 'markdown' | 'text' | 'video'

/** 文本类预览上限 2MB，超出只提示下载，避免大文件拖垮浏览器 */
export const PREVIEW_TEXT_MAX_BYTES = 2 * 1024 * 1024

const IMAGE_EXTENSIONS = new Set(['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg', 'bmp', 'ico'])
const VIDEO_EXTENSIONS = new Set(['mp4', 'webm', 'ogv'])
const AUDIO_EXTENSIONS = new Set(['mp3', 'wav', 'ogg'])
const MARKDOWN_EXTENSIONS = new Set(['md', 'markdown'])

/** 文本、源码、配置与数据交换的扩展名 → 代码视图的语言名（空串表示不指定） */
const TEXT_LANGS: Record<string, string> = {
  txt: '',
  log: '',
  json: 'json',
  xml: 'xml',
  yaml: 'yaml',
  yml: 'yaml',
  toml: 'ini',
  ini: 'ini',
  conf: 'ini',
  env: 'ini',
  html: 'html',
  htm: 'html',
  css: 'css',
  js: 'javascript',
  mjs: 'javascript',
  cjs: 'javascript',
  jsx: 'javascript',
  ts: 'typescript',
  tsx: 'typescript',
  vue: 'html',
  cs: 'csharp',
  java: 'java',
  go: 'go',
  py: 'python',
  php: 'php',
  sql: 'sql',
}

/** 不带点的小写扩展名 */
export function extensionOf(file: Pick<FileRow, 'extension'>): string {
  return (file.extension ?? '').replace(/^\./, '').toLowerCase()
}

/** 文件能预览成哪一类；浏览器渲染不了的为空 */
export function previewKindOf(file: Pick<FileRow, 'contentType' | 'extension' | 'kind'>): PreviewKind | null {
  const contentType = file.contentType.toLowerCase()
  const extension = extensionOf(file)
  if (contentType.startsWith('image/') || IMAGE_EXTENSIONS.has(extension) || file.kind === 'Image')
    return 'image'
  if (contentType.startsWith('video/') || VIDEO_EXTENSIONS.has(extension) || file.kind === 'Video')
    return 'video'
  if (contentType.startsWith('audio/') || AUDIO_EXTENSIONS.has(extension) || file.kind === 'Audio')
    return 'audio'
  if (MARKDOWN_EXTENSIONS.has(extension))
    return 'markdown'
  if (extension === 'csv')
    return 'csv'
  if (contentType.startsWith('text/') || TEXT_LANGS[extension] !== undefined)
    return 'text'
  return null
}

/** 正常且能预览的文件才给预览入口 */
export function canPreview(file: FileRow): boolean {
  return file.status === 'Normal' && previewKindOf(file) !== null
}

/** 读成文本再渲染的几类（受大小上限约束） */
export function isTextPreview(kind: PreviewKind | null): boolean {
  return kind === 'markdown' || kind === 'text' || kind === 'csv'
}

export function textLangOf(file: Pick<FileRow, 'extension'>): string | undefined {
  return TEXT_LANGS[extensionOf(file)] || undefined
}

/** CSV 表格：首行作表头，各行按列号取值（缺的格补空串），行号记在 `_row` 作行主键 */
export function toCsvTable(text: string): { header: string[], rows: Record<string, string>[] } {
  const parsed = parseCsvRows(text)
  const header = parsed[0] ?? []
  const rows = parsed.slice(1).map((row, rowIndex) => ({
    ...Object.fromEntries(header.map((_, index) => [String(index), row[index] ?? ''])),
    _row: String(rowIndex + 1),
  }))
  return { header, rows }
}

// ── 副本 ────────────────────────────────────────────────────────

/** 能设为主副本：正常且校验过（后端同一条件），本来就是主副本的不用设 */
export function canSetPrimary(storage: FileStorageRow): boolean {
  return !storage.isPrimary && storage.status === 'Normal' && storage.verifiedAt != null
}

/**
 * 启停副本的目标状态：正常 ↔ 停用；主副本不能停用，校验失败的要重新校验，这两种为空。
 */
export function storageToggleTarget(storage: FileStorageRow): 'Normal' | 'Unavailable' | null {
  if (storage.status === 'Normal')
    return storage.isPrimary ? null : 'Unavailable'
  if (storage.status === 'Unavailable')
    return 'Normal'
  return null
}

// ── 文案与色调 ──────────────────────────────────────────────────

/** 大类 → 图标（离线预载的 lucide 集） */
export function kindIcon(kind: FileKind): string {
  switch (kind) {
    case 'Image': return 'lucide:image'
    case 'Document': return 'lucide:file-text'
    case 'Video': return 'lucide:file-video'
    case 'Audio': return 'lucide:file-audio'
    case 'Archive': return 'lucide:file-archive'
    default: return 'lucide:file'
  }
}

export function fileStatusTone(status: FileStatus): FileTone {
  switch (status) {
    case 'Normal': return 'success'
    case 'Corrupted': return 'danger'
    default: return 'neutral'
  }
}

export function storageStatusTone(status: FileStorageStatus): FileTone {
  return status === 'Normal' ? 'success' : 'danger'
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function kindLabel(t: Translate, kind: FileKind): string {
  switch (kind) {
    case 'Image': return t('files.library.kinds.image')
    case 'Document': return t('files.library.kinds.document')
    case 'Video': return t('files.library.kinds.video')
    case 'Audio': return t('files.library.kinds.audio')
    case 'Archive': return t('files.library.kinds.archive')
    default: return t('files.library.kinds.other')
  }
}

export function fileStatusLabel(t: Translate, status: FileStatus): string {
  switch (status) {
    case 'Archived': return t('files.library.statuses.archived')
    case 'Corrupted': return t('files.library.statuses.corrupted')
    default: return t('files.library.statuses.normal')
  }
}

export function accessLevelLabel(t: Translate, level: FileAccessLevel): string {
  return level === 'Authenticated' ? t('files.library.access_levels.authenticated') : t('files.library.access_levels.authorized')
}

/** 来源是字符串：认得的翻成文案，其余原样 */
export function sourceLabel(t: Translate, source: string): string {
  switch (source) {
    case 'upload': return t('files.library.sources.upload')
    case 'fast-upload': return t('files.library.sources.fast_upload')
    default: return source || '-'
  }
}

export function storageStatusLabel(t: Translate, status: FileStorageStatus): string {
  switch (status) {
    case 'VerificationFailed': return t('files.library.storage_statuses.verification_failed')
    case 'Unavailable': return t('files.library.storage_statuses.unavailable')
    default: return t('files.library.storage_statuses.normal')
  }
}

/** 校验失败时后端在备注里记的原因 */
export function storageReasonLabel(t: Translate, remark: null | string | undefined): string {
  switch (remark) {
    case 'missing': return t('files.library.storage_reasons.missing')
    case 'hash-mismatch': return t('files.library.storage_reasons.hash_mismatch')
    default: return remark || ''
  }
}
