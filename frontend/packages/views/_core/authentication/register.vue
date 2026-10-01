<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import type { LegalDocumentKind } from './legal'

import { XhCheckbox, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppContext } from '~/stores'
import LegalDocumentDialog from './LegalDocumentDialog.vue'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'RegisterPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const { apis } = useAppContext()
const agreePolicy = ref(false)
/** 同意条款那段文案里夹着两颗文书按钮，放不进复选框的标签插槽，复选框经 aria-labelledby 取它作名字 */
const agreeLabelId = useId()

/** 正在查看的法律文书；弹窗关掉后保留上一份，关闭动画里标题不会跳成另一份 */
const legalKind = ref<LegalDocumentKind>('privacy-policy')
const legalOpen = ref(false)

function openLegal(kind: LegalDocumentKind) {
  legalKind.value = kind
  legalOpen.value = true
}

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
      // 第二参是整表值，跨字段规则从它读，不必回头取 formData；先填它、再改密码时由 deps 带着重验
      deps: ['password'],
      validator: (value, values) =>
        value === values.password ? null : t('page.auth.password_mismatch'),
    },
  ],
}))

// 返回的 Promise 交给表单：落定前提交钮报在途、再按不重复提交，失败在这里自己接住
async function onSubmit() {
  try {
    if (!agreePolicy.value) {
      toast.warning(t('page.auth.agree_required'))
      return
    }
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
      toast.danger(error.message)
    }
  }
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
        class="mt-3 auth-body"
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
      @submit="onSubmit"
    >
      <!-- 各字段靠占位文案表意，标签只留给读屏 -->
      <XhFormFieldGroup name="username" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.username_placeholder') }}
          </XhFieldLabel>
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
      <XhFormFieldGroup name="email" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.email_placeholder') }}
          </XhFieldLabel>
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
      <!-- 密码档自带显隐钮，不再另摆眼睛图标 -->
      <XhFormFieldGroup name="password" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.login.password_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.password"
              type="password"
              size="lg"
              :placeholder="t('page.login.password_placeholder')"
              autocomplete="new-password"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="confirmPassword" :class="formData.password ? '!mb-3' : '!mb-6'">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.confirm_password_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.confirmPassword"
              type="password"
              size="lg"
              :placeholder="t('page.auth.confirm_password_placeholder')"
              autocomplete="new-password"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <!-- 密码强度放在两个密码框之后：输密码与确认密码紧挨着，中间不插一条强度条 -->
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
        <span class="auth-caption" :style="{ color: strengthColor }">{{ strengthLabel }}</span>
      </div>

      <!-- 文案里夹着按钮，不能塞进复选框的标签插槽（label 里不能套可聚焦元素）：并排放一段，复选框经 aria-labelledby 指向它 -->
      <div class="mb-6">
        <span class="xh-checkbox-row">
          <XhCheckbox v-model:checked="agreePolicy" :aria-labelledby="agreeLabelId" />
          <span :id="agreeLabelId" class="xh-checkbox-row__label auth-body">
            {{ t('page.auth.agree_text') }}
            <!-- 打开弹窗而不是跳页：原先的 href="#" 在哈希路由下会把人带回首页 -->
            <button type="button" class="link-primary legal-link" @click="openLegal('privacy-policy')">
              {{ t('page.auth.privacy_policy') }}
            </button>
            {{ t('page.auth.and') }}
            <button type="button" class="link-primary legal-link" @click="openLegal('terms-of-service')">
              {{ t('page.auth.terms_of_service') }}
            </button>
          </span>
        </span>
      </div>
      <LegalDocumentDialog v-model:open="legalOpen" :kind="legalKind" />

      <XhFormSubmitTrigger class="auth-submit">
        {{ t('page.auth.register_btn') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <p
      class="mt-6 auth-helper text-center"
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

/* 文书入口是按钮（打开弹窗），外观仍是行内链接 */
.legal-link {
  padding: 0;
  border: 0;
  background: none;
  font: inherit;
  cursor: pointer;
}

.legal-link:focus-visible {
  border-radius: var(--xh-radius-sm);
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-width);
}
</style>
