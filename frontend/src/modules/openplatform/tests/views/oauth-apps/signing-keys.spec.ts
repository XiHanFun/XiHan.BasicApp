import type { SigningKeyRow } from '@/modules/openplatform/views/oauth-apps/signing-keys'
import { describe, expect, it } from 'vitest'
import { canRetireSigningKey, leadMinutes, signingKeyStatusLabel, useSigningKeys } from '@/modules/openplatform/views/oauth-apps/signing-keys'
import { configureRequest } from '~/request'

function json(status: number, body: object): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
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

function key(id: string, status: SigningKeyRow['status']): SigningKeyRow {
  return {
    id,
    keyId: `kid-${id}`,
    status,
    signsFrom: '2026-11-20T08:00:00+00:00',
    createdAt: '2026-11-20T08:00:00+00:00',
    retiredAt: null,
  }
}

describe('signing keys', () => {
  it('offers retiring only keys that no longer sign and are still published', () => {
    expect([
      'Signing',
      'Pending',
      'Retained',
      'Retired',
    ].map(status => canRetireSigningKey(key('a', status as SigningKeyRow['status'])))).toEqual([false, true, true, false])
  })

  it('labels every status and rounds the lead up to whole minutes', () => {
    const t = (k: string) => k
    expect([
      'Signing',
      'Pending',
      'Retained',
      'Retired',
    ].map(status => signingKeyStatusLabel(t, status as SigningKeyRow['status']))).toEqual([
      'openplatform.signing_keys.statuses.signing',
      'openplatform.signing_keys.statuses.pending',
      'openplatform.signing_keys.statuses.retained',
      'openplatform.signing_keys.statuses.retired',
    ])
    expect([900, 901, 30].map(leadMinutes)).toEqual([15, 16, 1])
  })

  it('replaces the whole list after each call and returns the rotated key', async () => {
    const lists = [
      {
        items: [
          key('a', 'Signing'),
        ],
        rotationLeadSeconds: 900,
      },
      {
        items: [
          key('b', 'Pending'),
          key('a', 'Signing'),
        ],
        rotationLeadSeconds: 900,
      },
      {
        items: [
          key('b', 'Retired'),
          key('a', 'Signing'),
        ],
        rotationLeadSeconds: 900,
      },
    ]
    const calls = record(() => json(200, lists.shift()!))
    const state = useSigningKeys()

    await state.load()
    expect((state.keys.value.map(k => k.id))).toEqual(['a'])
    expect(state.leadSeconds.value).toBe(900)

    const created = await state.rotate()
    expect(created?.id).toBe('b')
    expect(state.keys.value.map(k => k.status)).toEqual(['Pending', 'Signing'])

    await state.retire(created!)
    expect(state.keys.value.map(k => k.status)).toEqual(['Retired', 'Signing'])
    expect(state.working.value).toBe(false)
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual([
      'GET /api/oauth-signing-keys',
      'POST /api/oauth-signing-keys:rotate',
      'POST /api/oauth-signing-keys/b:retire',
    ])
  })

  it('keeps the list and lets the caller report a refused retirement', async () => {
    record(call => call.method === 'GET'
      ? json(200, {
          items: [
            key('a', 'Signing'),
          ],
          rotationLeadSeconds: 900,
        })
      : json(409, {
          type: 'about:blank',
          title: 'Conflict',
          status: 409,
          code: 'OPENPLATFORM_CONFLICT_005',
          detail: 'in use',
        }))
    const state = useSigningKeys()
    await state.load()

    await expect(state.retire(state.keys.value[0]!)).rejects.toThrow()
    expect(state.keys.value.map(k => k.status)).toEqual(['Signing'])
    expect(state.working.value).toBe(false)
  })
})
