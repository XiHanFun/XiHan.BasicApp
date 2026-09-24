import type { ApiId, RoleMemberDto, UserSelectItemDto } from '@/api'

/** 角色成员穿梭框的一项：主键即用户主键 */
export interface RoleMemberCandidate {
  basicId: ApiId
  displayName: string
  userName: string
  isExternalMember: boolean
}

function displayNameOf(user: { userName: string, realName?: string | null, nickName?: string | null }) {
  return user.realName || user.nickName || user.userName
}

/**
 * 候选人（本租户的成员目录）并上现有成员：候选有上限，已在角色里的人可能不在候选里，
 * 不并进来就会在穿梭框里「消失」，保存时被当成移出
 */
export function mergeMemberCandidates(
  candidates: readonly UserSelectItemDto[],
  members: readonly RoleMemberDto[],
): RoleMemberCandidate[] {
  const external = new Map(members.map(member => [member.userId, member.isExternalMember]))
  const items: RoleMemberCandidate[] = candidates.map(user => ({
    basicId: user.basicId,
    displayName: displayNameOf(user),
    userName: user.userName,
    isExternalMember: external.get(user.basicId) ?? false,
  }))
  const known = new Set(items.map(item => item.basicId))
  for (const member of members) {
    if (!known.has(member.userId)) {
      items.push({
        basicId: member.userId,
        displayName: displayNameOf(member),
        userName: member.userName,
        isExternalMember: member.isExternalMember,
      })
    }
  }
  return items
}

/** 草稿与现有成员比出差量：新加入的按用户主键授予，移出的按绑定主键撤销 */
export function diffRoleMembers(draft: readonly ApiId[], members: readonly RoleMemberDto[]) {
  const current = new Set(members.map(member => member.userId))
  const selected = new Set(draft)
  return {
    grantUserIds: draft.filter(userId => !current.has(userId)),
    revokeUserRoleIds: members.filter(member => !selected.has(member.userId)).map(member => member.userRoleId),
  }
}
