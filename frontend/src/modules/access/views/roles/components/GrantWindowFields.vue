<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PermissionWindowDraft } from '../grant-window'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDatePicker, XFieldLabel, XInput } from '~/components'
import { isGrantWindowValid } from '../grant-window'

defineOptions({ name: 'GrantWindowFields' })

defineProps<{
  /** 带原因：授予与拒绝有，成员指派没有 */
  withReason?: boolean
  /** 生效时间的问号提示：说清这组字段作用于哪些条目 */
  effectiveHelp?: string
  disabled?: boolean
}>()

const model = defineModel<PermissionWindowDraft>({ required: true })
const { t } = useI18n()
const valid = computed(() => isGrantWindowValid(model.value))

function patch(next: Partial<PermissionWindowDraft>) {
  model.value = {
    ...model.value,
    ...next,
  }
}
</script>

<template>
  <div class="xh-edit-form-grid">
    <XhFieldRoot>
      <XFieldLabel :help="effectiveHelp">
        {{ t('access.roles.window.effective_at') }}
      </XFieldLabel>
      <XhFieldControl>
        <XDatePicker :value="model.effectiveAt" name="effectiveAt" clearable show-time :disabled="disabled" @update:value="value => patch({ effectiveAt: value })" />
      </XhFieldControl>
    </XhFieldRoot>
    <XhFieldRoot :invalid="!valid">
      <XFieldLabel :help="t('access.roles.window.expires_help')">
        {{ t('access.roles.window.expires_at') }}
      </XFieldLabel>
      <XhFieldControl>
        <XDatePicker :value="model.expiresAt" name="expiresAt" clearable show-time :disabled="disabled" @update:value="value => patch({ expiresAt: value })" />
      </XhFieldControl>
      <XhFieldErrorText v-if="!valid">
        {{ t('access.roles.window.invalid') }}
      </XhFieldErrorText>
    </XhFieldRoot>
    <XhFieldRoot v-if="withReason" class="xh-span-2">
      <XhFieldLabel>{{ t('access.roles.window.reason') }}</XhFieldLabel>
      <XhFieldControl>
        <XInput :value="model.reason" name="reason" clearable :max-length="512" :placeholder="t('access.roles.window.reason_placeholder')" :disabled="disabled" @update:value="value => patch({ reason: value })" />
      </XhFieldControl>
    </XhFieldRoot>
  </div>
</template>
