<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { SigningKeyRow } from '../signing-keys'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle } from '@xihan-ui/vue'
import { computed, h, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderRowActionMenu, renderTag, XDataTable } from '~/components'
import { confirmDanger, dialog, toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { errorText } from '../oauth-app'
import { canRetireSigningKey, leadMinutes, signingKeyStatusLabel, signingKeyStatusTone, useSigningKeys } from '../signing-keys'

/**
 * id_token 的签名密钥（只在平台）：列出各把的状态，轮换出新密钥、停用不再签名的
 */
defineOptions({ name: 'SigningKeysDrawer' })

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const auth = useAuthStore()
const state = useSigningKeys()
const { keys, leadSeconds, loading, working } = state

const canRotate = computed(() => auth.can('openplatform.oauth-signing-keys.rotate'))
const canRetire = computed(() => auth.can('openplatform.oauth-signing-keys.retire'))
const minutes = computed(() => leadMinutes(leadSeconds.value))
/** 待接替的那把：它接替之前后端不让再轮换 */
const pending = computed(() => keys.value.find(key => key.status === 'Pending'))
const signing = computed(() => keys.value.some(key => key.status === 'Signing'))

async function load() {
  try {
    await state.load()
  }
  catch (error) {
    toast.danger(errorText(error) || t('openplatform.signing_keys.load_failed'))
  }
}

function confirmRotate() {
  void dialog.confirm({
    badge: 'warning',
    title: t('openplatform.signing_keys.rotate'),
    // 还没有在签名的密钥时，新密钥一建好就签
    content: signing.value
      ? t('openplatform.signing_keys.confirm_rotate', { minutes: minutes.value })
      : t('openplatform.signing_keys.confirm_first'),
    okText: t('openplatform.signing_keys.rotate'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const created = await state.rotate()
        toast.success(created?.status === 'Pending'
          ? t('openplatform.signing_keys.rotated', {
              time: formatDate(created.signsFrom),
            })
          : t('openplatform.signing_keys.rotated_now'))
      }
      catch (error) {
        toast.danger(errorText(error) || t('openplatform.signing_keys.rotate_failed'))
      }
    },
  })
}

function confirmRetire(key: SigningKeyRow) {
  void confirmDanger({
    title: t('openplatform.signing_keys.retire'),
    content: key.status === 'Pending'
      ? t('openplatform.signing_keys.confirm_cancel', { keyId: key.keyId })
      : t('openplatform.signing_keys.confirm_retire', { keyId: key.keyId }),
    okText: t('openplatform.signing_keys.retire'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await state.retire(key)
        toast.success(t('openplatform.signing_keys.retired', { keyId: key.keyId }))
      }
      catch (error) {
        toast.danger(errorText(error) || t('openplatform.signing_keys.retire_failed'))
      }
    },
  })
}

const columns = computed<XDataTableColumn<SigningKeyRow>[]>(() => [
  {
    key: 'keyId',
    title: t('openplatform.signing_keys.key_id'),
    minWidth: 180,
    render: key => h('code', key.keyId),
  },
  {
    key: 'status',
    title: t('openplatform.signing_keys.status'),
    width: 100,
    render: key => renderTag(signingKeyStatusLabel(t, key.status), signingKeyStatusTone(key.status)),
  },
  {
    key: 'signsFrom',
    title: t('openplatform.signing_keys.signs_from'),
    minWidth: 160,
    render: key => formatDate(key.signsFrom),
  },
  {
    key: 'createdAt',
    title: t('openplatform.signing_keys.created_at'),
    minWidth: 160,
    render: key => formatDate(key.createdAt),
  },
  {
    key: 'retiredAt',
    title: t('openplatform.signing_keys.retired_at'),
    minWidth: 160,
    render: key => key.retiredAt
      ? formatDate(key.retiredAt)
      : '-',
  },
  ...(canRetire.value
    ? [
        {
          key: 'actions',
          title: t('component.schema_page.actions_column'),
          width: 90,
          // 与列表页同一个「更多」菜单：正在签名的与已停用的没有操作
          render: (key: SigningKeyRow) => renderRowActionMenu(canRetireSigningKey(key)
            ? [
                {
                  key: 'retire',
                  title: t('openplatform.signing_keys.retire'),
                  icon: 'lucide:ban',
                  tone: 'danger' as const,
                  disabled: working.value,
                },
              ]
            : [], t('component.schema_page.more'), () => confirmRetire(key)),
        },
      ]
    : []),
])

watch(open, (value) => {
  if (value)
    void load()
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 960px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('openplatform.signing_keys.title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body signing-keys" data-testid="signing-keys-drawer">
        <p v-if="leadSeconds > 0" class="signing-keys__tip">
          {{ t('openplatform.signing_keys.tip', { minutes }) }}
        </p>
        <div class="signing-keys__bar">
          <XhButton v-if="canRotate" variant="solid" tone="brand" size="sm" :loading="working" :disabled="!!pending" @click="confirmRotate">
            <XhButtonIndicator>
              <Icon icon="lucide:refresh-ccw-dot" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('openplatform.signing_keys.rotate') }}</XhButtonLabel>
          </XhButton>
          <XhButton variant="ghost" size="sm" :loading="loading" @click="load">
            <XhButtonIndicator>
              <Icon icon="lucide:refresh-cw" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('openplatform.signing_keys.refresh') }}</XhButtonLabel>
          </XhButton>
          <span v-if="canRotate && pending" class="signing-keys__hint">
            {{ t('openplatform.signing_keys.pending_hint', { time: formatDate(pending.signsFrom) }) }}
          </span>
        </div>
        <XDataTable
          :columns="columns"
          :data="keys"
          row-key="id"
          :loading="loading"
          :empty-text="t('openplatform.signing_keys.empty')"
        />
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.signing-keys {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.signing-keys__tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.signing-keys__hint {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.signing-keys__bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}
</style>
