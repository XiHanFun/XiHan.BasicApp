<script lang="ts" setup>
import type { UserProfile } from '~/types'
import { NTabPane, NTabs, NSpin, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { getProfileApi } from '@/api'
import ProfileBanner from './ProfileBanner.vue'
import ProfileTabDeveloper from './ProfileTabDeveloper.vue'
import ProfileTabInfo from './ProfileTabInfo.vue'
import ProfileTabNotifications from './ProfileTabNotifications.vue'
import ProfileTabSecurity from './ProfileTabSecurity.vue'

defineOptions({ name: 'ProfilePage' })

const message = useMessage()
const activeTab = ref('profile')
const profileLoading = ref(false)
const profile = ref<UserProfile | null>(null)
const securityRef = ref<InstanceType<typeof ProfileTabSecurity> | null>(null)

async function loadProfile() {
  profileLoading.value = true
  try { profile.value = await getProfileApi() }
  catch (e: any) { message.error(e?.message || '加载个人资料失败') }
  finally { profileLoading.value = false }
}

onMounted(() => { loadProfile() })
</script>

<template>
  <div class="pf-page">
    <NSpin :show="profileLoading && !profile">
      <ProfileBanner
        :profile="profile"
        :sessions-count="securityRef?.sessions?.length ?? 0"
        :sessions-loaded="securityRef?.sessionsLoaded ?? false"
      />

      <NTabs v-model:value="activeTab" type="line" animated class="pf-tabs">
        <NTabPane name="profile" tab="个人资料">
          <ProfileTabInfo :profile="profile" @saved="loadProfile" />
        </NTabPane>

        <NTabPane name="security" tab="安全设置">
          <ProfileTabSecurity ref="securityRef" :profile="profile" @updated="loadProfile" />
        </NTabPane>

        <NTabPane name="notifications" tab="通知偏好">
          <ProfileTabNotifications />
        </NTabPane>

        <NTabPane name="developer" tab="开发者设置">
          <ProfileTabDeveloper />
        </NTabPane>
      </NTabs>
    </NSpin>
  </div>
</template>

<style scoped>
.pf-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.pf-tabs {
  padding: 0 24px;
}

.pf-tabs :deep(.n-tabs-nav) {
  padding-top: 0;
}

.pf-tabs :deep(.n-card__action) {
  box-shadow: none !important;
  border-top: none !important;
  background: transparent;
}

@media (max-width: 640px) {
  .pf-tabs {
    padding: 0 12px;
  }
}
</style>
