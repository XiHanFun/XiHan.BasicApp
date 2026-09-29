import type { ApiId, EnableStatus } from '@/api'

/** 表单里各自走独立接口、各有按钮码门控的几项：状态、锁定与登录策略 */
export interface UserFormSecurity {
  status: EnableStatus
  isLocked: boolean
  multiLogin: boolean
  maxDev: number
}

/**
 * 角色/部门勾选块能否切换这一项：撤掉已生效的要撤销权限，新勾一个要授予权限；
 * 本次勾了又取消、或取消了又勾回，都不产生请求，随时可切
 */
export function canTogglePick(
  id: ApiId,
  selected: readonly ApiId[],
  effective: ReadonlySet<ApiId>,
  access: { grant: boolean, revoke: boolean },
) {
  const isSelected = selected.includes(id)
  if (effective.has(id)) {
    return isSelected ? access.revoke : true
  }
  return isSelected ? true : access.grant
}

/**
 * 与打开表单时相比改了哪几项。只提交改过的：每项各走一个受权限门控的接口，
 * 没改的也去调，没有那个按钮的人就会在资料已保存之后被拒，留下存了一半的用户
 */
export function diffUserFormSecurity(current: UserFormSecurity, original: UserFormSecurity) {
  return {
    status: current.status !== original.status,
    lock: current.isLocked !== original.isLocked,
    loginPolicy: current.multiLogin !== original.multiLogin || (current.maxDev || 0) !== (original.maxDev || 0),
  }
}
