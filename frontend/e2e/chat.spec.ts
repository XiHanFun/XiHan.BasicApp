import type { Browser, Page } from '@playwright/test'
import { expect, test } from '@playwright/test'
import { INITIAL_PASSWORD, SUPER_ADMIN } from './env.ts'
import { createUser, grantDirectly, login, RUN } from './support.ts'

// 聊天的主干：超管建一个平台账号并直授聊天权限 → 两个人单聊、互发消息实时收到 → 语音通话接通再挂断 → 聊天审计里查到消息与通话记录
// 平台账号之间聊天不过套餐白名单；后一步用前一步的账号与会话，串行跑，前一步失败后面跳过
test.describe.configure({ mode: 'serial' })

const peer = {
  username: `chat-${RUN}`,
  displayName: `E2E 聊天 ${RUN}`,
  email: `chat-${RUN}@e2e.test`,
  password: INITIAL_PASSWORD,
}
// 对方只拿聊天要用的五个权限：看会话与消息、发消息、看通话与打电话
const peerGrants = [
  'chat.chat-conversations.read',
  'chat.chat-messages.read',
  'chat.chat-messages.send',
  'chat.chat-calls.read',
  'chat.chat-calls.call',
]
const hello = `你好 ${RUN}`
const reply = `收到 ${RUN}`

/** 两个人各开一个浏览器上下文登录：实时推送与通话都要两端同时在线 */
async function signInBoth(browser: Browser): Promise<[Page, Page]> {
  const mine = await (await browser.newContext()).newPage()
  const theirs = await (await browser.newContext()).newPage()
  await Promise.all([
    login(mine, SUPER_ADMIN),
    login(theirs, peer),
  ])
  return [mine, theirs]
}

// 聊天页之外壳层里还挂着一份收起的聊天面板，页面上的聊天部件都按可见的取
const conversation = (page: Page, name: string) => page.locator('.chat-conv-item').filter({ visible: true }).filter({ hasText: name })
const composer = (page: Page) => page.getByPlaceholder('输入消息，Enter 发送 / Shift+Enter 换行').filter({ visible: true })
const message = (page: Page, text: string) => page.locator('[id^="chat-msg-"]').filter({ visible: true }).filter({ hasText: text })

/** 进聊天页并打开与某人的单聊 */
async function openChatWith(page: Page, name: string) {
  await page.goto('/#/chat/conversations')
  await conversation(page, name).click()
  await expect(composer(page)).toBeVisible()
}

async function say(page: Page, text: string) {
  await composer(page).fill(text)
  await composer(page).press('Enter')
  await expect(message(page, text)).toBeVisible()
}

test('超管建聊天对象并直授聊天权限', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await createUser(page, peer)
  await grantDirectly(page, peer, peerGrants)
})

test('两个人单聊：发起会话、互发消息，对方实时收到', async ({ browser }) => {
  const [mine, theirs] = await signInBoth(browser)
  // 对方先停在聊天页的会话列表：还没有这个会话
  await theirs.goto('/#/chat/conversations')
  await expect(theirs.getByRole('button', { name: '发起聊天' })).toBeVisible()

  await mine.goto('/#/chat/conversations')
  await mine.getByRole('button', { name: '发起聊天' }).click()
  await mine.getByRole('menuitem', { name: '发起单聊' }).click()
  const start = mine.getByRole('dialog', { name: '发起单聊' })
  await start.getByRole('textbox', { name: '用户' }).fill(peer.username)
  await start.getByRole('listbox', { name: '用户' }).getByRole('option', { name: peer.displayName }).click()
  await start.getByRole('button', { name: '确定' }).click()
  await expect(start).toBeHidden()
  await say(mine, hello)

  // 对方没刷新：会话出现在列表里、带着这条消息的预览，点开看到正文
  await expect(conversation(theirs, SUPER_ADMIN.username)).toContainText(hello)
  await conversation(theirs, SUPER_ADMIN.username).click()
  await expect(message(theirs, hello)).toBeVisible()
  await say(theirs, reply)
  await expect(message(mine, reply)).toBeVisible()
})

test('语音通话：呼叫、接听、挂断，会话里留下通话记录', async ({ browser }) => {
  const [mine, theirs] = await signInBoth(browser)
  await openChatWith(mine, peer.username)
  // 对方停在工作台：来电在任何页面都能接；等他的实时连接连上（对方在线才打得出去）
  await expect(mine.getByText('在线', { exact: true }).filter({ visible: true })).toBeVisible()

  await mine.getByRole('button', { name: '语音通话', exact: true }).click()
  const incoming = theirs.getByRole('alertdialog')
  await expect(incoming).toContainText('邀请你语音通话')
  await incoming.getByRole('button', { name: '接听' }).click()

  // 两端真的连上才开始计时
  for (const page of [mine, theirs])
    await expect(page.getByRole('timer')).toHaveText(/^\d{2}:\d{2}$/, { timeout: 30_000 })
  await expect(incoming).toBeHidden()

  await mine.getByRole('button', { name: '挂断' }).click()
  for (const page of [mine, theirs])
    await expect(page.getByRole('timer')).toHaveCount(0)
  await expect(message(mine, '通话时长')).toContainText(/通话时长 \d{2}:\d{2}/)
})

test('聊天审计查得到那条消息与那通电话', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await page.goto('/#/chat/audit')
  await expect(page.getByRole('tab', { name: '消息' })).toHaveAttribute('aria-selected', 'true')
  await page.getByRole('textbox', { name: '关键字', exact: true }).fill(hello)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  await expect(page.getByRole('row').filter({ hasText: hello })).toContainText(SUPER_ADMIN.username)

  await page.getByRole('tab', { name: '通话记录' }).click()
  const call = page.getByRole('row').filter({ hasText: peer.displayName })
  await expect(call).toContainText('语音通话')
  await expect(call).toContainText('已完成')
  await call.getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '详情' }).click()
  const detail = page.getByRole('dialog').filter({ hasText: '参与者（2）' })
  await expect(detail).toContainText(peer.username)
  await expect(detail).toContainText('已离开')
})
