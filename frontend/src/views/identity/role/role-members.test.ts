import type { RoleMemberDto, UserSelectItemDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { diffRoleMembers, mergeMemberCandidates } from './role-members'

function candidate(basicId: string, userName: string, realName?: string): UserSelectItemDto {
  return { basicId, userName, realName } as UserSelectItemDto
}

function member(userId: string, userRoleId: string, isExternalMember = false): RoleMemberDto {
  return { userId, userRoleId, userName: `u${userId}`, isExternalMember } as RoleMemberDto
}

describe('mergeMemberCandidates', () => {
  it('候选之外的现有成员也并进来，外部成员的标记取自成员列表', () => {
    const items = mergeMemberCandidates(
      [candidate('1', 'alice', 'Alice'), candidate('2', 'bob')],
      [member('2', '20', true), member('3', '30')],
    )

    expect(items).toEqual([
      { basicId: '1', displayName: 'Alice', userName: 'alice', isExternalMember: false },
      { basicId: '2', displayName: 'bob', userName: 'bob', isExternalMember: true },
      { basicId: '3', displayName: 'u3', userName: 'u3', isExternalMember: false },
    ])
  })
})

describe('diffRoleMembers', () => {
  it('新加入按用户主键授予，移出按绑定主键撤销，留下的不动', () => {
    const members = [member('2', '20'), member('3', '30')]

    expect(diffRoleMembers(['1', '3'], members)).toEqual({
      grantUserIds: ['1'],
      revokeUserRoleIds: ['20'],
    })
  })

  it('没有改动时两边都为空', () => {
    expect(diffRoleMembers(['2'], [member('2', '20')])).toEqual({ grantUserIds: [], revokeUserRoleIds: [] })
  })
})
