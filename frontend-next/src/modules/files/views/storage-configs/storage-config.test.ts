import type { StorageConfigForm, StorageConfigRow } from './storage-config'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { QueryOperator } from '~/types/contracts'
import { batchFailureMessage, configProblemMessage, emptyStorageConfigForm, findConfigProblem, STORAGE_CONFIG_QUERY, toCreateBody, toStorageConfigForm, toUpdateBody } from './storage-config'

const t = (key: string, values?: Record<string, unknown>) => `${key}${values ? JSON.stringify(values) : ''}`

function remoteForm(patch: Partial<StorageConfigForm> = {}): StorageConfigForm {
  return { ...emptyStorageConfigForm(), code: 'oss-main', name: '阿里云', type: 'Oss', bucket: 'my-bucket', endpoint: 'https://oss-cn-hangzhou.aliyuncs.com', accessKeyId: 'AK', secretAccessKey: 'SK', ...patch }
}

describe('存储配置的列表筛选', () => {
  it('类型与状态多选合成 in，是否缺省按 true / false 精确匹配，关键字单独下发', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' oss ', type: ['S3', 'MinIO'], status: ['Active'], isDefault: 'false' },
      conditionFilters: [
        { field: 'type', operator: QueryOperator.In, values: ['S3', 'MinIO'] },
        { field: 'status', operator: QueryOperator.In, values: ['Active'] },
      ],
      sorts: [{ field: 'order', order: 'asc' }],
    }, STORAGE_CONFIG_QUERY)
    expect(request.keyword).toBe('oss')
    expect(request.filters).toEqual([
      { field: 'type', operator: 'in', value: 'S3,MinIO' },
      { field: 'status', operator: 'in', value: 'Active' },
      { field: 'isDefault', operator: 'eq', value: 'false' },
    ])
    expect(request.sorts).toEqual([{ field: 'order', descending: false }])
  })
})

describe('存储配置的表单', () => {
  it('提交前找出第一处问题：编码含空白、桶名不合规则、远程存储缺地址或密钥', () => {
    expect(findConfigProblem(remoteForm(), false)).toBeNull()
    expect(findConfigProblem(remoteForm({ code: 'oss main' }), false)).toBe('code')
    expect(findConfigProblem(remoteForm({ name: '  ' }), false)).toBe('name')
    expect(findConfigProblem(remoteForm({ bucket: 'My_Bucket' }), false)).toBe('bucket')
    expect(findConfigProblem(remoteForm({ bucket: '-ab' }), false)).toBe('bucket')
    expect(findConfigProblem(remoteForm({ endpoint: 'oss-cn-hangzhou.aliyuncs.com' }), false)).toBe('endpoint')
    expect(findConfigProblem(remoteForm({ endpoint: 'ftp://host' }), false)).toBe('endpoint')
    expect(findConfigProblem(remoteForm({ accessKeyId: '' }), false)).toBe('access_key_id')
    expect(findConfigProblem(remoteForm({ secretAccessKey: '' }), false)).toBe('secret')
    expect(configProblemMessage(t, 'bucket')).toBe('files.storage_configs.problems.bucket')
  })

  it('本地存储只看桶；编辑时编码不校验，已存了密钥的留空即保留', () => {
    expect(findConfigProblem({ ...emptyStorageConfigForm(), code: 'local', name: '本地', bucket: 'files' }, false)).toBeNull()
    expect(findConfigProblem({ ...emptyStorageConfigForm(), code: 'local', name: '本地', bucket: 'ab' }, false)).toBe('bucket')
    expect(findConfigProblem(remoteForm({ id: 'c1', code: '', secretAccessKey: '' }), true)).toBeNull()
    // 本地改成远程：原来没有密钥，仍要填
    expect(findConfigProblem(remoteForm({ id: 'c1', secretAccessKey: '' }), false)).toBe('secret')
  })

  it('本地存储的请求不带远程设置，停用的配置不会设为缺省，空串写成 null', () => {
    const local = { ...emptyStorageConfigForm(), code: ' local ', name: '本地', bucket: ' files ', endpoint: 'https://x', accessKeyId: 'AK', isDefault: true, status: 'Disabled' as const, order: null, remark: ' ' }
    expect(toCreateBody(local)).toEqual({ code: 'local', name: '本地', type: 'Local', bucket: 'files', endpoint: null, region: null, accessKeyId: null, secretAccessKey: null, isDefault: false, status: 'Disabled', order: 0, remark: null })
    expect(toUpdateBody({ ...remoteForm({ secretAccessKey: '  ', region: 'cn-hangzhou' }), id: 'c1' })).toEqual({
      id: 'c1',
      name: '阿里云',
      type: 'Oss',
      bucket: 'my-bucket',
      endpoint: 'https://oss-cn-hangzhou.aliyuncs.com',
      region: 'cn-hangzhou',
      accessKeyId: 'AK',
      secretAccessKey: null,
      order: 100,
      remark: null,
    })
  })

  it('已保存的配置回填表单，访问密钥不回显', () => {
    const row: StorageConfigRow = { id: 'c1', code: 'oss', name: 'OSS', type: 'Oss', endpoint: 'https://e', region: null, bucket: 'b-1', accessKeyId: 'AK', hasSecretAccessKey: true, isDefault: true, status: 'Active', order: 5, remark: null, createdAt: '2026-10-01T00:00:00Z', modifiedAt: null }
    expect(toStorageConfigForm(row)).toEqual({ id: 'c1', code: 'oss', name: 'OSS', type: 'Oss', bucket: 'b-1', endpoint: 'https://e', region: '', accessKeyId: 'AK', secretAccessKey: '', isDefault: true, status: 'Active', order: 5, remark: '' })
  })

  it('批量删除的失败码认得的翻成文案，其余原样', () => {
    expect(batchFailureMessage(t, 'FILES_CONFLICT_002')).toBe('files.storage_configs.batch.default_locked')
    expect(batchFailureMessage(t, 'FILES_CONFLICT_003')).toBe('files.storage_configs.batch.in_use')
    expect(batchFailureMessage(t, 'KERNEL_UNEXPECTED_000')).toBe('KERNEL_UNEXPECTED_000')
  })
})
