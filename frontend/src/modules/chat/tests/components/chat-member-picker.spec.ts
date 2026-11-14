/**
 * components/chat-member-picker.ts：选人的已选合并
 * 锁定：候选换了一批（搜索、翻页、换范围）之后，不在眼前这批里的已选不丢；这批里的按勾选增减；选不了的人不收
 */
import type { ChatUserOption } from '@/modules/chat/types'
import { describe, expect, it } from 'vitest'
import { mergePickedUsers } from '@/modules/chat/components/chat-member-picker'

function user(userId: string): ChatUserOption {
  return {
    userId,
    userName: userId,
    displayName: userId.toUpperCase(),
    avatarFileId: null,
    departmentName: null,
    isOnline: false,
  }
}

const ids = (users: ChatUserOption[]) => users.map(item => item.userId)

describe('mergePickedUsers', () => {
  it('新勾的排在已选后面，按候选里的先后', () => {
    const picked = [
      user('a'),
    ]
    const candidates = [
      user('a'),
      user('b'),
      user('c'),
    ]

    expect(ids(mergePickedUsers(picked, candidates, ['c', 'a', 'b'], new Set()))).toEqual(['a', 'b', 'c'])
  })

  it('这批候选里取消勾选的从已选里拿掉', () => {
    const picked = [
      user('a'),
      user('b'),
    ]
    const candidates = [
      user('a'),
      user('b'),
    ]

    expect(ids(mergePickedUsers(picked, candidates, ['b'], new Set()))).toEqual(['b'])
  })

  it('不在这批候选里的已选原样留着：列表交回来的勾选里有没有它都一样', () => {
    const picked = [
      user('a'),
      user('z'),
    ]
    const candidates = [
      user('a'),
      user('b'),
    ]

    expect(ids(mergePickedUsers(picked, candidates, ['a', 'b'], new Set()))).toEqual(['a', 'z', 'b'])
    expect(ids(mergePickedUsers(picked, candidates, [], new Set()))).toEqual(['z'])
  })

  it('选不了的人（已在群里）即使被交回来也不收', () => {
    const candidates = [
      user('a'),
      user('b'),
    ]

    expect(ids(mergePickedUsers([], candidates, ['a', 'b'], new Set(['a'])))).toEqual(['b'])
  })

  it('已选沿用当初勾选时的那条数据，不被后来的候选替换', () => {
    const original = {
      ...user('a'),
      departmentName: 'R&D',
    }
    const merged = mergePickedUsers([original], [
      user('a'),
    ], ['a'], new Set())

    expect(merged[0]).toBe(original)
  })
})
