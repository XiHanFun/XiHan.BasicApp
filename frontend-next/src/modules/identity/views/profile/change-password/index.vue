<script lang="ts" setup>
import { useI18n } from 'vue-i18n'
import { useChangePassword } from './use-change-password'

defineOptions({ name: 'IdentityChangePasswordView' })

const { t } = useI18n()
const page = useChangePassword(() => t('identity.profile.change_password.mismatch'))
</script>

<template>
  <section class="flex max-w-md flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('identity.profile.change_password.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('identity.profile.change_password.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>
    <form class="grid gap-3 rounded-surface border border-border bg-surface p-4" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('identity.profile.change_password.fields.current_password')" :error-text="page.fieldError('currentPassword')" :invalid="!!page.fieldError('currentPassword')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.currentPassword" name="currentPassword" type="password" autocomplete="current-password" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.profile.change_password.fields.new_password')" :error-text="page.fieldError('newPassword')" :invalid="!!page.fieldError('newPassword')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.newPassword" name="newPassword" type="password" autocomplete="new-password" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.profile.change_password.fields.confirm_password')" :error-text="page.fieldError('confirmPassword')" :invalid="!!page.fieldError('confirmPassword')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.confirmPassword" name="confirmPassword" type="password" autocomplete="new-password" />
        </template>
      </XhFieldRoot>
      <p v-if="page.error.value" class="text-caption text-fg-danger" role="alert">
        {{ page.error.value }}
      </p>
      <p v-if="page.succeeded.value" class="text-caption text-fg-success" role="status">
        {{ t('identity.profile.change_password.success') }}
      </p>
      <div>
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('identity.profile.change_password.actions.submit') }}
        </XhButton>
      </div>
    </form>
  </section>
</template>
