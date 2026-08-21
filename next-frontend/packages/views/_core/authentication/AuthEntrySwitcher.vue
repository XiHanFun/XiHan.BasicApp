<script lang="ts" setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { XSegmented } from '~/components'
import { CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, LOGIN_PATH, QRCODE_LOGIN_PATH } from '~/constants'

/**
 * 登录方式切换。四种入口互斥、各自是一条路由，没有面板，
 * 所以用分段控制器而不是标签页——tablist 却没有 tabpanel 是错的语义。
 */
defineOptions({ name: 'AuthEntrySwitcher' })

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const entryList = computed(() => [
  { value: LOGIN_PATH, label: t('page.login.title') },
  { value: CODE_LOGIN_PATH, label: t('page.auth.mobile_login') },
  { value: EMAIL_LOGIN_PATH, label: t('page.auth.email_login') },
  { value: QRCODE_LOGIN_PATH, label: t('page.auth.qrcode_login') },
])

/** 选中项取自当前路由；选中即跳转，不另存一份状态 */
const activeEntry = computed({
  get: () => {
    const path = route.path
    return entryList.value.some(item => item.value === path) ? path : LOGIN_PATH
  },
  set: (next: string) => {
    if (route.path !== next) {
      router.push(next)
    }
  },
})
</script>

<template>
  <XSegmented
    v-model:value="activeEntry"
    block
    size="md"
    :options="entryList"
    :aria-label="t('page.auth.login_method')"
  />
</template>
