import { expect, test } from '@playwright/test'
import { INITIAL_PASSWORD, SUPER_ADMIN } from './env.ts'
import { expectToast, login, openPage, RUN, search } from './support.ts'

// 一条主干：开通租户 → 租户管理员登录 → 建角色与成员 → 成员提交授权申请 → 管理员在审批中心同意 → 成员看到已批准
// 后一步用前一步开通的租户与账号，串行跑，前一步失败后面跳过
test.describe.configure({ mode: 'serial' })

const tenant = { id: `e2e-${RUN}`, name: `E2E 租户 ${RUN}` }
const admin = { tenant: tenant.id, username: 'admin', password: INITIAL_PASSWORD }
const member = {
  tenant: tenant.id,
  username: 'member',
  displayName: 'E2E 成员',
  password: INITIAL_PASSWORD,
}
const requestedRole = { code: 'auditor', name: 'E2E 审计员' }
// 成员只拿申请自己的两个权限：申请弹窗的候选不要求能读角色与权限目录
const memberGrants = [
  'access.permission-requests.read',
  'access.permission-requests.create',
]

test('超管开通企业版租户并初始化管理员', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await openPage(page, '/tenancy/tenants')

  await page.getByRole('button', { name: '开通租户' }).click()
  const create = page.getByRole('dialog', { name: '开通租户' })
  await create.getByRole('textbox', { name: '租户 ID' }).fill(tenant.id)
  await create.getByRole('textbox', { name: '名称' }).fill(tenant.name)
  await create.getByRole('combobox', { name: '套餐' }).click()
  await page.getByRole('option', { name: '企业版' }).click()
  await create.getByRole('button', { name: '保存' }).click()

  // 共库租户保存后接着弹初始化管理员
  const init = page.getByRole('dialog', { name: `初始化管理员 — ${tenant.name}` })
  await init.getByRole('textbox', { name: '登录名' }).fill(admin.username)
  await init.getByRole('textbox', { name: '显示名' }).fill('E2E 管理员')
  await init.getByRole('textbox', { name: '邮箱' }).fill(`admin@${tenant.id}.e2e.test`)
  await init.getByRole('textbox', { name: '初始密码' }).fill(INITIAL_PASSWORD)
  await init.getByRole('button', { name: '保存' }).click()
  await expect(init).toBeHidden()

  await expect(await search(page, tenant.id)).toContainText('企业版')
})

test('租户管理员以租户 ID 登录，进入本租户', async ({ page }) => {
  await login(page, admin)
  await expect(page.getByRole('banner').getByRole('button', { name: `当前：${tenant.name}，点击切换` })).toBeVisible()
})

test('租户管理员建角色与成员，并给成员直授申请权限', async ({ page }) => {
  await login(page, admin)

  await openPage(page, '/access/roles')
  await page.getByRole('button', { name: '新增角色' }).click()
  const role = page.getByRole('dialog', { name: '新增角色' })
  await role.getByRole('textbox', { name: '角色名称' }).fill(requestedRole.name)
  await role.getByRole('textbox', { name: '角色码' }).fill(requestedRole.code)
  await role.getByRole('button', { name: '保存' }).click()
  await expect(role).toBeHidden()

  await openPage(page, '/identity/users')
  await page.getByRole('button', { name: '新建用户' }).click()
  const user = page.getByRole('dialog', { name: '新建用户' })
  await user.getByRole('textbox', { name: '用户名' }).fill(member.username)
  await user.getByRole('textbox', { name: '显示名' }).fill(member.displayName)
  await user.getByRole('textbox', { name: '邮箱' }).fill(`${member.username}@${tenant.id}.e2e.test`)
  await user.getByRole('textbox', { name: '初始密码' }).fill(member.password)
  await user.getByRole('button', { name: '保存' }).click()
  await expect(user).toBeHidden()

  await page.getByRole('row').filter({ hasText: `@${member.username}` }).getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '权限直授' }).click()
  const grant = page.getByRole('dialog', { name: `权限直授 · ${member.displayName}` })
  for (const code of memberGrants) {
    await grant.getByRole('textbox', { name: '可授予' }).fill(code)
    await grant.getByRole('listbox', { name: '可授予' }).getByRole('option', { name: code }).click()
    await grant.getByRole('button', { name: '将选中项移到「已授予」' }).click()
  }
  await expect(grant.getByRole('listbox', { name: '已授予' }).getByRole('option')).toHaveCount(memberGrants.length)
  await grant.getByRole('button', { name: '保存直授' }).click()
  // 抽屉保存后不关，留着接着改
  await expectToast(page, `已保存（授予 ${memberGrants.length} 项，拒绝 0 项，撤销 0 项）`)
})

test('成员提交角色申请，进入待审批', async ({ page }) => {
  await login(page, member)
  await page.goto('/#/access/delegations')
  await page.getByRole('button', { name: '发起申请' }).click()
  const request = page.getByRole('dialog', { name: '发起权限申请' })
  await request.getByRole('combobox', { name: '申请角色' }).click()
  await page.getByRole('option', { name: requestedRole.name }).click()
  await request.getByRole('textbox', { name: '申请原因' }).fill(`E2E 冒烟 ${RUN}`)
  await request.getByRole('button', { name: '提交审批' }).click()
  await expect(request).toBeHidden()
  await expect(page.getByRole('row').filter({ hasText: requestedRole.name })).toContainText('待审批')
})

test('租户管理员在审批中心同意申请', async ({ page }) => {
  await login(page, admin)
  await page.goto('/#/workflow/approvals')
  await expect(page.getByRole('tab', { name: '待我审批' })).toHaveAttribute('aria-selected', 'true')
  const title = `授权申请：角色「${requestedRole.name}」`
  const task = page.getByRole('row').filter({ hasText: title }).filter({ hasText: member.displayName })
  // 缺省流程按需建：节点名与界面同一语言
  await expect(task).toContainText('管理员审批')
  await task.getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '办理' }).click()

  const detail = page.getByRole('dialog', { name: title })
  await expect(detail.getByRole('region', { name: '业务单据' })).toContainText(`E2E 冒烟 ${RUN}`)
  await detail.getByRole('textbox', { name: '意见' }).fill('同意')
  await detail.getByRole('button', { name: '同意', exact: true }).click()
  await expectToast(page, /^已办理/)
  await expect(task).toHaveCount(0)
})

test('成员看到申请已批准', async ({ page }) => {
  await login(page, member)
  await page.goto('/#/access/delegations')
  await expect(page.getByRole('row').filter({ hasText: requestedRole.name })).toContainText('已批准')
})
