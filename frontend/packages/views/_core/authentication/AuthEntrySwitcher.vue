<script lang="ts" setup>
import type { TabsValueChangeDetails } from '@xihan-ui/headless'
import { useElementSize } from '@vueuse/core'
import { XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, LOGIN_PATH, QRCODE_LOGIN_PATH } from '~/constants'

/**
 * 登录方式切换。四种入口互斥、各自是一条路由，走线形标签页：
 * 表单由路由渲染，这里把它接进当前标签的面板里——只摆一条 tablist 而没有 tabpanel 是错的语义，
 * 触发器上的 aria-controls 会指向一个不存在的区域。
 * 标签用 page.auth.entry.* 短说法，不借页面标题键：登录卡片右栏固定宽，长标题会把扫码入口挤出容器。
 */
defineOptions({ name: 'AuthEntrySwitcher' })

const props = defineProps<{
  /** 关掉时只渲染插槽内容：忘记密码、注册这类页面不出切换器 */
  enabled?: boolean
}>()

/**
 * 档位按标签带实际拿到的宽度选，不按视口是否小屏：表单栏在宽屏与平板竖屏上都是 460，
 * 手机上随视口在 250–360 之间，按「是否小屏」一刀切会让 375–767 这一大段都挤在 sm。
 * 放得下就取 lg，与下面 lg 档的表单控件同档，作为卡片的一级导航不被表单压过。
 * 阈值按各语言里最宽的一组（日文）四条合计量出：lg 359、md 294，各留一两像素余量
 */
const LG_MIN_WIDTH = 360
const MD_MIN_WIDTH = 296

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const rootRef = useTemplateRef('root')
const { width } = useElementSize(computed(() => rootRef.value?.$el as HTMLElement | undefined))
const entrySize = computed(() => {
  if (width.value >= LG_MIN_WIDTH)
    return 'lg'
  return width.value >= MD_MIN_WIDTH ? 'md' : 'sm'
})

/** 标签值用短键而不是路由路径：它要进 id / aria-controls，短键读起来也干净 */
const entryList = computed(() => [
  { value: 'account', path: LOGIN_PATH, label: t('page.auth.entry.account') },
  { value: 'mobile', path: CODE_LOGIN_PATH, label: t('page.auth.entry.mobile') },
  { value: 'email', path: EMAIL_LOGIN_PATH, label: t('page.auth.entry.email') },
  { value: 'qrcode', path: QRCODE_LOGIN_PATH, label: t('page.auth.entry.qrcode') },
])

/** 选中项取自当前路由，不另存一份状态 */
const activeEntry = computed(
  () => entryList.value.find(entry => entry.path === route.path)?.value ?? 'account',
)

function onEntryChange(details: TabsValueChangeDetails) {
  const target = entryList.value.find(entry => entry.value === details.value)
  if (target && route.path !== target.path) {
    router.push(target.path)
  }
}
</script>

<template>
  <XhTabsRoot
    v-if="props.enabled"
    ref="root"
    class="auth-entry-switcher"
    :value="activeEntry"
    variant="line"
    :size="entrySize"
    @value-change="onEntryChange"
  >
    <XhTabsList :aria-label="t('page.auth.login_method')">
      <XhTabsTrigger v-for="entry in entryList" :key="entry.value" :value="entry.value">
        {{ entry.label }}
      </XhTabsTrigger>
      <XhTabsIndicator />
    </XhTabsList>
    <!-- 面板只摆当前这一份：内容归路由渲染，值跟着选中走，aria-controls 才落得到实处 -->
    <XhTabsContent :value="activeEntry">
      <slot />
    </XhTabsContent>
  </XhTabsRoot>
  <slot v-else />
</template>

<style scoped>
/* 标签带与表单之间留一档间距；面板本身不加内衬，表单自己带 */
.auth-entry-switcher :deep([data-scope='tabs'][data-part='list']) {
  margin-block-end: var(--xh-space-6);
}

.auth-entry-switcher :deep([data-scope='tabs'][data-part='content']) {
  padding: 0;
}
</style>
