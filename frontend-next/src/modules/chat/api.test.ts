/**
 * api.ts：聊天 API 契约到生成的操作客户端的映射
 *
 * 职责边界：锁定 6.0 与 4.x 不同的那几处——批量操作的逐项失败要抛出（不当成功吞掉）、置顶与免打扰传新值、
 * 转发走服务端、选人与部门树的字段换算、附件先秒传再普通上传、附件地址按会话签出且原样使用
 * 网络层用 configureRequest 换掉 fetch，普通上传（XMLHttpRequest）替身掉 uploadForm
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureRequest } from '~/request'
import { CHAT_ATTACHMENT_UPLOAD_PATH, createChatApi } from './api'

const upload = vi.hoisted(() => ({
  calls: [] as { path: string, fileName: string | null }[],
  result: {
    attachment: {
      fileId: 'f-up',
      fileName: 'a.png',
      fileSize: 3,
      contentType: 'image/png',
    },
  } as unknown,
}))

vi.mock('~/request', async importOriginal => ({
  ...await importOriginal<typeof import('~/request')>(),
  uploadForm: (path: string, form: FormData, options?: { onProgress?: (percent: number) => void }) => {
    const file = form.get('file')
    upload.calls.push({
      path,
      fileName: file instanceof File
        ? file.name
        : null,
    })
    options?.onProgress?.(100)
    return Promise.resolve(upload.result)
  },
}))

interface Call { method: string, url: string, body: unknown }

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

function record(respond: (call: Call) => Response, baseUrl = ''): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl,
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

beforeEach(() => {
  upload.calls = []
  upload.result = {
    attachment: {
      fileId: 'f-up',
      fileName: 'a.png',
      fileSize: 3,
      contentType: 'image/png',
    },
  }
})

afterEach(() => {
  configureRequest({ baseUrl: '' })
})

describe('批量操作的逐项失败', () => {
  it('移出成员走批量接口，被逐项拒绝时带着原因抛出', async () => {
    const calls = record(() => json(200, {
      succeeded: [],
      failed: [
        { id: 'u-2', code: 'CHAT_DENIED_004', message: 'CHAT_DENIED_004 的原因' },
      ],
    }))

    await expect(createChatApi().removeMember('c-1', 'u-2')).rejects.toThrow('CHAT_DENIED_004 的原因')
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/chat-conversations:remove-members',
      body: {
        conversationId: 'c-1',
        userIds: ['u-2'],
      },
    })
  })

  it('禁言单个成员也是批量接口，成功时不抛', async () => {
    const calls = record(() => json(200, {
      succeeded: ['u-2'],
      failed: [],
    }))

    await createChatApi().setMemberSilence('c-1', 'u-2', true)

    expect(calls[0]).toMatchObject({
      url: '/api/chat-conversations:silence-members',
      body: {
        conversationId: 'c-1',
        userIds: ['u-2'],
        isSilenced: true,
      },
    })
  })

  it('转发是服务端操作：目标会话逐项失败时抛出', async () => {
    const calls = record(() => json(200, {
      succeeded: [],
      failed: [
        { id: 'c-9', code: 'CHAT_NOTFOUND_001', message: 'CHAT_NOTFOUND_001 的原因' },
      ],
    }))

    await expect(createChatApi().forwardMessage('m-1', ['c-9'])).rejects.toThrow('CHAT_NOTFOUND_001 的原因')
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/chat-messages:forward',
      body: {
        messageId: 'm-1',
        conversationIds: ['c-9'],
      },
    })
  })
})

describe('个人开关与查询', () => {
  it('置顶与免打扰传新值（6.0 不是切换），返回生效后的状态', async () => {
    const calls = record(call => json(200, { isOn: (call.body as { isPinned?: boolean, isMuted?: boolean }).isPinned ?? (call.body as { isMuted: boolean }).isMuted }))
    const api = createChatApi()

    await expect(api.setConversationPinned('c-1', true)).resolves.toEqual({ isOn: true })
    await expect(api.setConversationMuted('c-1', false)).resolves.toEqual({ isOn: false })
    expect(calls.map(call => [call.url, call.body])).toEqual([
      [
        '/api/chat-conversations:pin',
        { conversationId: 'c-1', isPinned: true },
      ],
      [
        '/api/chat-conversations:mute',
        { conversationId: 'c-1', isMuted: false },
      ],
    ])
  })

  it('历史走只读 GET，游标与条数进查询串；列表类响应取 items', async () => {
    const calls = record(call => (call.url.includes('history')
      ? json(200, { items: [], hasMore: true })
      : json(200, {
          items: [
            { conversationId: 'c-1' },
          ],
        })))
    const api = createChatApi()

    await expect(api.messageHistory({ conversationId: 'c-1', beforeMessageId: 'm-5', take: 30 })).resolves.toEqual({ items: [], hasMore: true })
    await expect(api.myConversations()).resolves.toEqual([
      { conversationId: 'c-1' },
    ])
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/chat-messages:history?conversationId=c-1&beforeMessageId=m-5&take=30' })
    expect(calls[1]).toMatchObject({ method: 'GET', url: '/api/chat-conversations:mine' })
  })

  it('已读上报不给消息即读到最新', async () => {
    const calls = record(() => json(200, { userId: 'me', lastReadMessageId: null, lastReadSeq: 0 }))

    await createChatApi().markRead('c-1')

    expect(calls[0]).toMatchObject({
      url: '/api/chat-conversations:mark-read',
      body: { conversationId: 'c-1', upToMessageId: null },
    })
  })

  it('选人：显示名放进 nickName 槽位、头像文件主键放进 avatar；空关键字不进查询串', async () => {
    const calls = record(() => json(200, {
      items: [
        {
          userId: 'u-1',
          userName: 'alice',
          displayName: 'Alice',
          avatarFileId: 'f-a',
        },
      ],
    }))

    await expect(createChatApi().selectUsers('  ', 20)).resolves.toEqual([
      {
        userId: 'u-1',
        userName: 'alice',
        nickName: 'Alice',
        avatar: 'f-a',
      },
    ])
    expect(calls[0]?.url).toBe('/api/chat-conversations:user-options?limit=20')
  })

  it('部门树：没有下级的节点 children 为空，不出空数组的展开箭头', async () => {
    record(() => json(200, {
      items: [
        {
          departmentId: 'd-1',
          departmentName: '研发',
          children: [
            { departmentId: 'd-2', departmentName: '前端', children: [] },
          ],
        },
      ],
    }))

    await expect(createChatApi().departmentTree()).resolves.toEqual([
      {
        departmentId: 'd-1',
        departmentName: '研发',
        children: [
          { departmentId: 'd-2', departmentName: '前端', children: null },
        ],
      },
    ])
  })
})

describe('附件', () => {
  it('秒传命中直接得到附件，不再普通上传', async () => {
    const calls = record(() => json(200, {
      attachment: {
        fileId: 'f-fast',
        fileName: 'a.png',
        fileSize: 3,
        contentType: 'image/png',
      },
    }))
    const progress: number[] = []

    const result = await createChatApi().uploadAttachment(new File(['abc'], 'a.png', { type: 'image/png' }), percent => progress.push(percent))

    expect(result).toEqual({ fileId: 'f-fast', fileName: 'a.png', fileSize: 3 })
    expect(progress).toEqual([100])
    expect(upload.calls).toEqual([])
    // SHA-256('abc')
    expect(calls[0]).toMatchObject({
      url: '/api/chat-messages:fast-upload',
      body: {
        hash: 'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad',
        size: 3,
        fileName: 'a.png',
        contentType: 'image/png',
      },
    })
  })

  it('秒传没命中或探测失败都回退普通上传（multipart 字段 file，聊天自己的端点）', async () => {
    record(() => json(200, { attachment: null }))
    await expect(createChatApi().uploadAttachment(new File(['abc'], 'a.png'))).resolves.toEqual({ fileId: 'f-up', fileName: 'a.png', fileSize: 3 })

    record(() => json(400, { code: 'CHAT_VALIDATION_001', status: 400, detail: 'bad hash' }, 'application/problem+json'))
    await expect(createChatApi().uploadAttachment(new File(['abc'], 'a.png'))).resolves.toMatchObject({ fileId: 'f-up' })

    expect(upload.calls).toEqual([
      { path: CHAT_ATTACHMENT_UPLOAD_PATH, fileName: 'a.png' },
      { path: CHAT_ATTACHMENT_UPLOAD_PATH, fileName: 'a.png' },
    ])
  })

  it('普通上传没给出附件时报错，而不是返回空附件', async () => {
    record(() => json(200, { attachment: null }))
    upload.result = { attachment: null }

    await expect(createChatApi().uploadAttachment(new File(['abc'], 'a.png'))).rejects.toThrow('no attachment')
  })

  it('附件地址按会话签出：相对地址补接口前缀，绝对地址原样使用、不再拼参数', async () => {
    const calls = record(call => json(200, {
      url: call.url.includes('download=true')
        ? 'https://oss.example/a.png?sig=1'
        : '/blobs/a.png?sig=1',
      expiresAt: '2026-11-01T01:00:00Z',
    }), '/base')
    const api = createChatApi()

    await expect(api.getFileUrl('c-1', 'f-1')).resolves.toEqual({ url: '/base/blobs/a.png?sig=1', expiresAt: '2026-11-01T01:00:00Z' })
    await expect(api.getFileUrl('c-1', 'f-1', true)).resolves.toMatchObject({ url: 'https://oss.example/a.png?sig=1' })
    expect(calls.map(call => call.url)).toEqual([
      '/base/api/chat-messages:presign?conversationId=c-1&fileId=f-1&download=false',
      '/base/api/chat-messages:presign?conversationId=c-1&fileId=f-1&download=true',
    ])
  })
})
