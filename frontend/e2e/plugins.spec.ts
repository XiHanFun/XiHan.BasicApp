import type { Page } from '@playwright/test'
import { expect, test } from '@playwright/test'
import { INITIAL_PASSWORD, SUPER_ADMIN } from './env.ts'
import { crmPluginPackage } from './plugin-package.ts'
import { expectToast, login, openPage, RUN, search } from './support.ts'

// 一条主干：超管装示例插件并启用 → 套餐放行插件的权限码、开通租户 → 租户管理员从菜单进插件页面增删改查 → 超管看过卸载预览后卸载
// 后一步用前一步的结果，串行跑；库在本机保留，上一次没走完留下的示例插件先卸掉
test.describe.configure({ mode: 'serial' })

const MANIFEST_ID = 'xihan.samples.crm'
const tenant = { id: `e2e-crm-${RUN}`, name: `E2E 插件租户 ${RUN}` }
const admin = { tenant: tenant.id, username: 'admin', password: INITIAL_PASSWORD }
const codes = [
  'crm.crm-customers.read',
  'crm.crm-customers.create',
  'crm.crm-customers.update',
  'crm.crm-customers.delete',
]

/** 插件列表里示例插件的那一行 */
async function pluginRow(page: Page) {
  await openPage(page, '/plugins/installed')
  return search(page, MANIFEST_ID, '关键词')
}

/** 卸载示例插件：先看预览，再确认 */
async function uninstall(page: Page) {
  const row = await pluginRow(page)
  await row.getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '卸载' }).click()
  const dialog = page.getByRole('dialog', { name: '卸载 crm' })
  await expect(dialog.getByText(/卸载会删掉插件及其全部版本与授予/)).toBeVisible()
  await dialog.getByRole('button', { name: '卸载', exact: true }).click()
  await expect(dialog).toBeHidden()
  await expectToast(page, '删除成功')
  await expect(row).toHaveCount(0)
}

test('超管上传示例插件，看过检查报告后安装并启用', async ({ page }) => {
  const plugin = crmPluginPackage()
  await login(page, SUPER_ADMIN)
  if (await (await pluginRow(page)).count() > 0)
    await uninstall(page)

  await page.getByRole('button', { name: '安装插件' }).click()
  const wizard = page.getByRole('dialog', { name: '安装插件' })
  await wizard.locator('input[type="file"]').setInputFiles({ name: 'crm.xhplugin', mimeType: 'application/zip', buffer: plugin })
  // 检查报告：插件的身份、四个操作与要批准的授予
  await expect(wizard.getByText(MANIFEST_ID)).toBeVisible()
  await expect(wizard.getByText('crm-customers.update', { exact: true })).toBeVisible()
  await expect(wizard.getByText('插件不需要任何授予')).toBeVisible()
  await wizard.getByRole('button', { name: '安装', exact: true }).click()
  await expect(wizard.getByText('crm 1.0.0 已安装，当前是停用的；准备好后在列表里启用')).toBeVisible()
  await wizard.getByRole('button', { name: '关闭', exact: true }).last().click()
  await expect(wizard).toBeHidden()

  const row = await pluginRow(page)
  await expect(row).toContainText('已停用')
  await row.getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '启用 / 停用' }).click()
  await page.getByRole('button', { name: '确定', exact: true }).click()
  await expectToast(page, '状态已更新')
  await expect(row).toContainText('已启用')
  await expect(row).not.toContainText('PLUGINS_')
})

test('超管在企业版放行插件的权限码，开通租户', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await openPage(page, '/tenancy/editions')
  await page.getByRole('row').filter({ hasText: '企业版' }).getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '权限白名单' }).click()
  const whitelist = page.getByRole('dialog', { name: '权限白名单 — 企业版' })
  for (const code of codes) {
    await whitelist.getByRole('textbox', { name: '可放行权限' }).fill(code)
    await whitelist.getByRole('listbox', { name: '可放行权限' }).getByRole('option', { name: code }).click()
    await whitelist.getByRole('button', { name: '将选中项移到「已放行」' }).click()
  }
  await whitelist.getByRole('button', { name: '保存', exact: true }).click()
  await expectToast(page, '白名单已保存，绑定它的租户随即生效。')

  await openPage(page, '/tenancy/tenants')
  await page.getByRole('button', { name: '开通租户' }).click()
  const create = page.getByRole('dialog', { name: '开通租户' })
  await create.getByRole('textbox', { name: '租户 ID' }).fill(tenant.id)
  await create.getByRole('textbox', { name: '名称' }).fill(tenant.name)
  await create.getByRole('combobox', { name: '套餐' }).click()
  await page.getByRole('option', { name: '企业版' }).click()
  await create.getByRole('button', { name: '保存' }).click()
  const init = page.getByRole('dialog', { name: `初始化管理员 — ${tenant.name}` })
  await init.getByRole('textbox', { name: '登录名' }).fill(admin.username)
  await init.getByRole('textbox', { name: '显示名' }).fill('E2E 插件管理员')
  await init.getByRole('textbox', { name: '邮箱' }).fill(`admin@${tenant.id}.e2e.test`)
  await init.getByRole('textbox', { name: '初始密码' }).fill(INITIAL_PASSWORD)
  await init.getByRole('button', { name: '保存' }).click()
  await expect(init).toBeHidden()
})

test('租户管理员从「插件应用」菜单进客户页面，增删改查走一遍', async ({ page }) => {
  await login(page, admin)
  const sidebar = page.getByRole('navigation', { name: '侧边栏' })
  await sidebar.getByText('插件应用', { exact: true }).click()
  await sidebar.getByText('客户', { exact: true }).click()
  await expect(page).toHaveURL(/#\/plugins\/crm\/customers/)
  await expect(page.getByRole('grid').first()).toBeVisible()

  // 新建：字段名来自插件文案，邮箱的说明收在问号提示里
  await page.getByRole('button', { name: '新建', exact: true }).click()
  const create = page.getByRole('dialog', { name: '新建' })
  await create.getByRole('textbox', { name: '名称' }).fill('Ada')
  await create.getByRole('textbox', { name: '邮箱' }).fill('ada@crm.e2e.test')
  await create.getByRole('button', { name: '保存' }).click()
  await expect(create).toBeHidden()
  const ada = page.getByRole('row').filter({ hasText: 'ada@crm.e2e.test' })
  await expect(ada).toContainText('Ada')

  // 名称为空：插件的失败按它带来的文案翻成中文
  await page.getByRole('button', { name: '新建', exact: true }).click()
  await create.getByRole('button', { name: '保存' }).click()
  await expect(create.getByRole('alert')).toContainText('客户名称不能为空。')
  await create.getByRole('button', { name: '取消' }).click()

  // 修改
  await ada.getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '编辑' }).click()
  const edit = page.getByRole('dialog', { name: '编辑' })
  await expect(edit.getByRole('textbox', { name: '名称' })).toHaveValue('Ada')
  await edit.getByRole('textbox', { name: '名称' }).fill('Ada Lovelace')
  await edit.getByRole('button', { name: '保存' }).click()
  await expect(edit).toBeHidden()
  await expect(ada).toContainText('Ada Lovelace')

  // 查：关键字在各列里找
  await expect(await search(page, 'Lovelace', '关键词')).toHaveCount(1)

  // 删除
  await ada.getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '删除' }).click()
  await page.getByRole('button', { name: '确定', exact: true }).click()
  await expectToast(page, '删除成功')
  await expect(ada).toHaveCount(0)
})

test('超管看过卸载预览后卸载示例插件，套餐里的插件码一并清掉', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await uninstall(page)
})
