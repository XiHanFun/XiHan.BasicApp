<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import { XhCheckbox, XhFieldControl, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger } from '@xihan-ui/vue'

import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'RegisterPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const { apis } = useAppContext()
const loading = ref(false)
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const agreePolicy = ref(false)

const formData = ref({
  username: '',
  email: '',
  password: '',
  confirmPassword: '',
})

const passwordStrength = computed(() => {
  const pwd = formData.value.password
  if (!pwd)
    return 0
  let score = 0
  if (pwd.length >= 8)
    score++
  if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd))
    score++
  if (/\d/.test(pwd))
    score++
  if (/[^a-z0-9]/i.test(pwd))
    score++
  return score
})

const strengthLabel = computed(() => {
  const labels = [
    t('page.auth.strength_weak'),
    t('page.auth.strength_weak'),
    t('page.auth.strength_medium'),
    t('page.auth.strength_strong'),
    t('page.auth.strength_very_strong'),
  ]
  return labels[passwordStrength.value] || ''
})

const strengthColor = computed(() => {
  const colors = ['#e53e3e', '#e53e3e', '#dd6b20', '#38a169', '#2b6cb0']
  return colors[passwordStrength.value] || '#e53e3e'
})

const rules = computed<FormRules>(() => ({
  username: [
    { required: true, message: t('page.auth.username_placeholder') },
    { min: 3, message: t('page.auth.username_min_length') },
  ],
  email: [
    { required: true, message: t('page.auth.email_placeholder') },
    { type: 'email', message: t('page.auth.email_invalid') },
  ],
  password: [
    { required: true, message: t('page.login.password_placeholder') },
    {
      // 返回文案即失败，返回空即通过；空值交给上面那条 required 管
      validator: (value) => {
        const password = String(value ?? '')
        if (!password)
          return null
        if (password.length < 8)
          return t('page.auth.password_rule_length')
        if (!/[a-z]/.test(password))
          return t('page.auth.password_rule_lower')
        if (!/[A-Z]/.test(password))
          return t('page.auth.password_rule_upper')
        if (!/\d/.test(password))
          return t('page.auth.password_rule_digit')
        if (!/[^a-z0-9]/i.test(password))
          return t('page.auth.password_rule_special')
        return null
      },
    },
  ],
  confirmPassword: [
    { required: true, message: t('page.auth.confirm_password_placeholder') },
    {
      // 第二参是整表值，跨字段规则从它读，不必回头取 formData
      validator: (value, values) =>
        value === values.password ? null : t('page.auth.password_mismatch'),
    },
  ],
}))

async function onSubmit() {
  try {
    if (!agreePolicy.value) {
      toast.warning(t('page.auth.agree_required'))
      return
    }
    loading.value = true
    await apis.registerApi({
      username: formData.value.username,
      email: formData.value.email,
      password: formData.value.password,
      nickName: formData.value.username,
    })
    toast.success(t('page.auth.register_success'))
    router.push(LOGIN_PATH)
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message) {
      toast.error(error.message)
    }
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter')
    onSubmit()
}
const onAuthInvalid = useAuthFormInvalid()
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.create_account_title') }}
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.register_subtitle') }}
      </p>
    </div>

    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @invalid="onAuthInvalid"
      @keydown="handleKeydown"
      @submit="onSubmit"
    >
      <XhFormFieldGroup value="username" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              v-model:value="formData.username"
              size="lg"
              :placeholder="t('page.auth.username_placeholder')"
              autocomplete="username"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>
      <XhFormFieldGroup value="email" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              v-model:value="formData.email"
              size="lg"
              :placeholder="`${t('page.auth.email_placeholder')}（${t('page.auth.register_email_tip')}）`"
              autocomplete="email"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>
      <XhFormFieldGroup value="password" class="!mb-3">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              v-model:value="formData.password"
              :type="showPassword ? 'text' : 'password'"
              size="lg"
              :placeholder="t('page.login.password_placeholder')"
              autocomplete="new-password"
            >
              <template #suffix>
                <span
                  class="cursor-pointer"
                  :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
                  @click="showPassword = !showPassword"
                ><Icon :icon="showPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" /></span>
              </template>
            </XInput>
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <!-- Password strength -->
      <div v-if="formData.password" class="flex gap-2 items-center mb-6">
        <div class="flex flex-1 gap-1">
          <div
            v-for="i in 4"
            :key="i"
            class="flex-1 h-1 rounded-full transition-colors"
            :style="{
              backgroundColor:
                i <= passwordStrength ? strengthColor : isDark ? '#374151' : '#e5e7eb',
            }"
          />
        </div>
        <span class="text-xs" :style="{ color: strengthColor }">{{ strengthLabel }}</span>
      </div>
      <div v-else class="mb-3" />

      <XhFormFieldGroup value="confirmPassword" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              v-model:value="formData.confirmPassword"
              :type="showConfirmPassword ? 'text' : 'password'"
              size="lg"
              :placeholder="t('page.auth.confirm_password_placeholder')"
              autocomplete="new-password"
            >
              <template #suffix>
                <span
                  class="cursor-pointer"
                  :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
                  @click="showConfirmPassword = !showConfirmPassword"
                ><Icon :icon="showConfirmPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" /></span>
              </template>
            </XInput>
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <!-- 复选框只是那个方框，没有标签插槽：文案是并排的一段，不能塞进它里面 -->
      <div class="mb-6">
        <span class="xh-checkbox-row">
          <XhCheckbox v-model:checked="agreePolicy" size="sm" />
          <span class="xh-checkbox-row__label text-sm">
            {{ t('page.auth.agree_text') }}
            <a class="link-primary" href="#">{{ t('page.auth.privacy_policy') }}</a>
            {{ t('page.auth.and') }}
            <a class="link-primary" href="#">{{ t('page.auth.terms_of_service') }}</a>
          </span>
        </span>
      </div>

      <XhFormSubmitTrigger class="auth-submit" :disabled="loading">
        {{ t('page.auth.register_btn') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <p
      class="mt-6 text-sm text-center"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.already_have_account') }}
      <span class="cursor-pointer link-primary" @click="router.push(LOGIN_PATH)">
        {{ t('page.auth.go_to_login') }}
      </span>
    </p>
  </div>
</template>

<style scoped>
.link-primary {
  color: hsl(var(--primary));
}

.link-primary:hover {
  text-decoration: underline;
}
</style>
