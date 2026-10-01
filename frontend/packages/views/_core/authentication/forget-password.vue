<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import { XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger } from '@xihan-ui/vue'

import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppContext } from '~/stores'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'ForgetPasswordPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const { apis } = useAppContext()

const formData = ref({
  email: '',
})

const rules = computed<FormRules>(() => ({
  email: [
    { required: true, message: t('page.auth.email_placeholder') },
    { type: 'email', message: t('page.auth.email_invalid') },
  ],
}))

// 返回的 Promise 交给表单：落定前提交钮报在途、再按不重复提交；
// 表单只把拒绝转成 submit-error，失败要在这里自己接住提示
async function onSubmit() {
  try {
    const result = await apis.requestPasswordResetApi(formData.value.email)
    if (result.debugResetUrl) {
      // 开发环境（未配 SMTP）回显重置链接，便于本地联调
      toast.success(`${t('page.auth.reset_link_sent')}（重置链接：${result.debugResetUrl}）`)
    }
    else {
      toast.success(t('page.auth.reset_link_sent'))
    }
  }
  catch (e: unknown) {
    const msg = (e as Error)?.message
    if (msg)
      toast.danger(msg)
  }
}

const onAuthInvalid = useAuthFormInvalid()
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.forget_password_title') }}
      </h1>
      <p
        class="mt-3 auth-body"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.forget_password_subtitle') }}
      </p>
    </div>

    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @invalid="onAuthInvalid"
      @submit="onSubmit"
    >
      <XhFormFieldGroup name="email" class="!mb-6">
        <XhFieldRoot>
          <!-- 占位只是示例地址，名字给读屏 -->
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.email_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.email"
              size="lg"
              placeholder="example@example.com"
              autocomplete="email"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormSubmitTrigger class="auth-submit">
        {{ t('page.auth.send_reset_link') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <p
      class="mt-6 auth-helper text-center"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      <span class="cursor-pointer link-primary" @click="router.push(LOGIN_PATH)">
        {{ t('page.auth.back_to_login') }}
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
