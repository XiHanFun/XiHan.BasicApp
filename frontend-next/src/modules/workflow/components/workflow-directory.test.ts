/**
 * 审批人选择数据源（workflow-directory.ts）的单元测试。
 *
 * 锁定：后端给的部门是平铺清单，按上级 ID 排成树序、label 带上级路径，停用的部门只念得出名字、不能再选；
 * 用户回显只拿 GUID 形式的 ID 去查，见过的用户不再请求。
 */
import type { AssigneeDepartment } from './workflow-directory'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { flattenDepartments, resolveUsers } from './workflow-directory'

function department(id: string, code: string, name: string, parentId: string | null = null, isActive = true): AssigneeDepartment {
  return { id, code, name, parentId, isActive }
}

describe('flattenDepartments', () => {
  it('按上级排成树序，label 是路径，description 是编码；上级不在清单里的当顶层', () => {
    const options = flattenDepartments([
      department('d-rd', 'rd', '研发部', 'd-hq'),
      department('d-hq', 'hq', '总部'),
      department('d-fe', 'fe', '前端组', 'd-rd'),
      department('d-x', 'x', '外部', 'd-missing'),
    ])

    expect(options.map(option => [option.value, option.label])).toEqual([
      ['hq', '总部'],
      ['rd', '总部 / 研发部'],
      ['fe', '总部 / 研发部 / 前端组'],
      ['x', '外部'],
    ])
    expect(options[0]?.description).toBe('hq')
  })

  it('停用的部门标为不可选', () => {
    const options = flattenDepartments([department('d-hq', 'hq', '总部'), department('d-old', 'old', '旧部门', 'd-hq', false)])

    expect(options.map(option => option.disabled === true)).toEqual([false, true])
  })
})

describe('resolveUsers', () => {
  it('只拿 GUID 去回显，命中缓存的不再请求', async () => {
    const urls: string[] = []
    const alice = { id: '0193f0aa-0000-7000-8000-00000000a001', userName: 'alice', displayName: 'Alice' }
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL) => {
        urls.push(String(url))
        return new Response(JSON.stringify({ items: [alice] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }) as unknown as typeof fetch,
    })

    expect(await resolveUsers([alice.id, '1001', ''])).toEqual([alice])
    expect(urls).toHaveLength(1)
    expect(urls[0]).toContain(alice.id)
    expect(urls[0]).not.toContain('1001')

    expect(await resolveUsers([alice.id])).toEqual([alice])
    expect(urls).toHaveLength(1)
  })
})
