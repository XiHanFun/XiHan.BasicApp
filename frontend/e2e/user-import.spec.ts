import { Buffer } from 'node:buffer'
import { expect, test } from '@playwright/test'
import { INITIAL_PASSWORD, SUPER_ADMIN } from './env.ts'
import { login, openPage, RUN, search } from './support.ts'

// 平台超管从 CSV 批量导入平台账号：合格的行入库，不合格的行列出原因；导入的账号凭共用的初始密码登录
test('超管批量导入用户', async ({ page }) => {
  const imported = { username: `imp-${RUN}`, password: INITIAL_PASSWORD }
  await login(page, SUPER_ADMIN)
  await openPage(page, '/identity/users')

  await page.getByRole('button', { name: '导入（CSV）' }).click()
  const dialog = page.getByRole('dialog', { name: '导入数据' })
  await dialog.getByRole('textbox', { name: '初始密码' }).fill(imported.password)
  const csv = [
    '用户名,显示名,邮箱',
    `${imported.username},导入 ${RUN},${imported.username}@e2e.test`,
    `BAD-${RUN},不合格`,
  ].join('\r\n')
  await dialog.locator('input[type="file"]').setInputFiles({
    name: 'users.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from(csv),
  })
  await expect(dialog).toContainText('可导入 2 行')
  await dialog.getByRole('button', { name: '开始导入' }).click()

  // 用户名格式由后端按新建用户的规则判定：本地预检放过，导入后逐行报回
  await expect(dialog).toContainText('导入完成：成功 1 条，失败 1 条。')
  await expect(dialog.getByRole('row').filter({ hasText: '用户名' })).toHaveCount(1)
  await dialog.getByRole('button', { name: '完成' }).click()
  await expect(dialog).toBeHidden()
  await expect(await search(page, imported.username, '关键字')).toContainText(`@${imported.username}`)

  await page.getByRole('banner').getByRole('button', { name: '超级管理员' }).click()
  await page.getByRole('menuitem', { name: '退出登录' }).click()
  await expect(page).toHaveURL(/#\/auth\/login/)
  await login(page, imported)
})
