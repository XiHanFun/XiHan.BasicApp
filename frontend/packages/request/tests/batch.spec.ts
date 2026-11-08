import { describe, expect, it } from 'vitest'
import { throwOnFailed } from '~/request/batch'

describe('批量结果的逐项失败', () => {
  it('没有失败不抛错', () => {
    expect(() => throwOnFailed({ failed: [] })).not.toThrow()
  })

  it('显示已本地化的原因，不显示错误码；相同的原因只留一条', () => {
    const failed = [
      { code: 'ACCESS_CONFLICT_002', message: '内建角色不能删除。' },
      { code: 'ACCESS_CONFLICT_002', message: '内建角色不能删除。' },
      { code: 'ACCESS_CONFLICT_008', message: '角色仍被继承。' },
    ]
    expect(() => throwOnFailed({ failed })).toThrow(/^内建角色不能删除。; 角色仍被继承。$/)
  })
})
