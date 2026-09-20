<script lang="ts" setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { XSegmented } from '~/components'
import { useIsMobile } from '~/composables'
import { CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, LOGIN_PATH, QRCODE_LOGIN_PATH } from '~/constants'

/**
 * 登录方式切换。四种入口互斥、各自是一条路由，没有面板，
 * 所以用分段控制器而不是标签页——tablist 却没有 tabpanel 是错的语义。
 * 标签用 page.auth.entry.* 短说法，不借页面标题键：登录卡片右栏固定宽，长标题会把扫码入口挤出容器。
 * 小屏上表单栏只剩三百来像素，lg 档四段排不下会折成 3 + 1：那里降到 md 档并把段的衬距
 * 收到 sm 档，四段仍在一行（各语言里最宽的一组按 14px 字量过，合计不超过 300px）。
 */
defineOptions({ name: 'AuthEntrySwitcher' })

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { isMobile } = useIsMobile()

const entryList = computed(() => [
  { value: LOGIN_PATH, label: t('page.auth.entry.account') },
  { value: CODE_LOGIN_PATH, label: t('page.auth.entry.mobile') },
  { value: EMAIL_LOGIN_PATH, label: t('page.auth.entry.email') },
  { value: QRCODE_LOGIN_PATH, label: t('page.auth.entry.qrcode') },
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
  <!-- 小屏的断点只认 useIsMobile 一处：档位与衬距槽都跟它走，不另写媒体查询 -->
  <XSegmented
    v-model:value="activeEntry"
    block
    :size="isMobile ? 'md' : 'lg'"
    :style="isMobile ? { '--xh-segmented-item-px': 'var(--xh-control-px-sm)' } : undefined"
    :options="entryList"
    :aria-label="t('page.auth.login_method')"
  />
</template>
