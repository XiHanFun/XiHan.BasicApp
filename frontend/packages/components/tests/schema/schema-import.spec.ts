/**
 * SchemaPage 的导入：按批交给 importMany 时一批接一批地提交，失败按批内序号挂回各行
 */
import type { ImportManyFailure, ListFieldSchema } from '~/components/schema/types'
import { describe, expect, it } from 'vitest'
import { buildImportTemplate, useSchemaImport } from '~/components/schema/useSchemaImport'

const fields: ListFieldSchema[] = [
  {
    key: 'userName',
    title: '用户名',
    dataType: 'string',
    required: true,
  },
  {
    key: 'roleCodes',
    title: '角色码',
    dataType: 'string',
    importHint: '多个用逗号分隔',
  },
]

function csv(...lines: string[]): File {
  return new File([`${lines.join('\r\n')}\r\n`], 'users.csv', { type: 'text/csv' })
}

describe('按批导入', () => {
  it('按 batchSize 分批、上一批返回后才交下一批；批内失败按序号挂回行并换成字段标题，整批抛错时这批每行都记为失败', async () => {
    const events: string[] = []
    const batches: Record<string, unknown>[][] = []
    const importer = useSchemaImport({
      fields: () => fields,
      fileName: () => 'identity.users',
      batchSize: 2,
      importMany: async (records): Promise<ImportManyFailure[]> => {
        const batch = batches.push(records)
        events.push(`start ${batch}`)
        await Promise.resolve()
        events.push(`end ${batch}`)
        if (batch === 2) {
          throw new Error('整批被拒')
        }
        return [
          { index: 1, field: 'roleCodes', message: '角色不存在' },
        ]
      },
    })
    await importer.loadFile(csv('用户名,角色码', 'amy,auditor', 'ben,ghost', 'cat,'))
    expect(importer.validRows.value).toHaveLength(3)

    const summary = await importer.run()
    expect(events).toEqual(['start 1', 'end 1', 'start 2', 'end 2'])
    expect(batches).toEqual([
      [
        { userName: 'amy', roleCodes: 'auditor' },
        { userName: 'ben', roleCodes: 'ghost' },
      ],
      [
        { userName: 'cat' },
      ],
    ])
    expect(summary).toEqual({ total: 3, success: 1, failed: 2 })
    expect(importer.progress.value).toBe(3)
    expect(importer.errorRows.value.map(row => row.errors)).toEqual([
      [
        { row: 2, field: '角色码', message: '角色不存在' },
      ],
      [
        { row: 3, message: '整批被拒' },
      ],
    ])
  })

  it('模板说明行用字段自己的取值提示', () => {
    expect(buildImportTemplate(fields).split('\r\n')[1]).toContain('多个用逗号分隔')
  })
})
