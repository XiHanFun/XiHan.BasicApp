/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import type { Translate } from './oauth-app'
import type { ResponseOf } from '~/contracts'
import { ref } from 'vue'
import { oauthSigningKeys as signingKeysApi } from '~/contracts'

export type SigningKeys = ResponseOf<'oauth-signing-keys.list'>
export type SigningKeyRow = SigningKeys['items'][number]
export type SigningKeyStatus = SigningKeyRow['status']

// 状态值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function signingKeyStatusLabel(t: Translate, status: SigningKeyStatus): string {
  switch (status) {
    case 'Signing': return t('openplatform.signing_keys.statuses.signing')
    case 'Pending': return t('openplatform.signing_keys.statuses.pending')
    case 'Retained': return t('openplatform.signing_keys.statuses.retained')
    default: return t('openplatform.signing_keys.statuses.retired')
  }
}

export function signingKeyStatusTone(status: SigningKeyStatus): Tone {
  switch (status) {
    case 'Signing': return 'success'
    case 'Pending': return 'info'
    case 'Retained': return 'neutral'
    default: return 'danger'
  }
}

/** 能不能停用：正在签名的后端拒绝，已停用的不必再停 */
export function canRetireSigningKey(key: SigningKeyRow): boolean {
  return key.status === 'Pending' || key.status === 'Retained'
}

/** 轮换出的新密钥多少分钟后接替签名（向上取整） */
export function leadMinutes(seconds: number): number {
  return Math.max(1, Math.ceil(seconds / 60))
}

/**
 * 签名密钥抽屉的数据：列出、轮换与停用都回全部密钥，整表换掉
 * 出错照样抛给调用方提示
 */
export function useSigningKeys() {
  const keys = ref<SigningKeyRow[]>([])
  const leadSeconds = ref(0)
  const loading = ref(false)
  const working = ref(false)

  function apply(result: SigningKeys) {
    keys.value = result.items
    leadSeconds.value = result.rotationLeadSeconds
  }

  async function load() {
    loading.value = true
    try {
      apply(await signingKeysApi.list({}))
    }
    finally {
      loading.value = false
    }
  }

  /** 轮换，返回新生成的那把 */
  async function rotate(): Promise<SigningKeyRow | undefined> {
    working.value = true
    try {
      const before = new Set(keys.value.map(key => key.id))
      const result = await signingKeysApi.rotate({})
      apply(result)
      return result.items.find(key => !before.has(key.id))
    }
    finally {
      working.value = false
    }
  }

  async function retire(key: SigningKeyRow) {
    working.value = true
    try {
      apply(await signingKeysApi.retire({ id: key.id }))
    }
    finally {
      working.value = false
    }
  }

  return {
    keys,
    leadSeconds,
    loading,
    working,
    load,
    rotate,
    retire,
  }
}
