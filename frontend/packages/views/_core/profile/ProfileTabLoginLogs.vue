<script lang="ts" setup>
import type { LoginLogItem } from '~/types'
import { NButton, NPagination, NSpin, NTag } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabLoginLogs' })

const { apis } = useAppContext()

const logs = ref<LoginLogItem[]>([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)

const loginResultLabel: Record<number, string> = {
  0: '成功',
  1: '密码错误',
  2: '账号锁定',
  3: '账号禁用',
  4: '需要两步验证',
}

async function loadLogs(nextPage = 1) {
  loading.value = true
  try {
    const res = await apis.getLoginLogsApi(nextPage, 12)
    logs.value = res.items
    total.value = res.total
    page.value = nextPage
  }
  catch {
    logs.value = []
    total.value = 0
  }
  finally {
    loading.value = false
  }
}

onMounted(() => loadLogs())
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:file-clock" width="16" />
            <span>登录日志</span>
          </div>
          <div class="pf-section__desc">
            最近的登录记录，发现异常请及时修改密码并登出可疑设备。
          </div>
        </div>
        <div class="pf-section__extra">
          <NButton size="tiny" quaternary @click="loadLogs(page)">
            <template #icon>
              <Icon icon="lucide:refresh-cw" />
            </template>
          </NButton>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="loading">
          <div v-if="logs.length === 0 && !loading" class="pf-empty">
            <span class="pf-empty__icon"><Icon icon="lucide:inbox" width="16" /></span>
            <span>暂无登录记录</span>
          </div>
          <div v-else class="pf-log-grid">
            <div v-for="(log, idx) in logs" :key="idx" class="pf-list-item pf-log-card">
              <div class="pf-list-icon" :class="{ 'pf-list-icon--danger': log.loginResult !== 0 }">
                <Icon :icon="log.loginResult === 0 ? 'lucide:log-in' : 'lucide:shield-alert'" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  <NTag :type="log.loginResult === 0 ? 'success' : 'error'" size="tiny" :bordered="false">
                    {{ loginResultLabel[log.loginResult] || `状态${log.loginResult}` }}
                  </NTag>
                  <span v-if="log.message" class="pf-log-message">{{ log.message }}</span>
                </div>
                <div class="pf-list-desc">
                  {{ log.loginIp || '未知 IP' }}
                  <template v-if="log.loginLocation">
                    · {{ log.loginLocation }}
                  </template>
                  <template v-if="log.browser">
                    · {{ log.browser }}
                  </template>
                  <template v-if="log.os">
                    · {{ log.os }}
                  </template>
                </div>
                <div class="pf-log-time">
                  {{ formatDate(log.loginTime) }}
                </div>
              </div>
            </div>
          </div>
          <div v-if="total > 12" class="pf-log-pagination">
            <NPagination
              :page="page"
              :page-size="12"
              :item-count="total"
              simple
              @update:page="loadLogs"
            />
          </div>
        </NSpin>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-log-grid {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.pf-log-card {
  align-items: flex-start;
}

.pf-log-message {
  color: var(--text-secondary);
  font-size: 12px;
}

.pf-log-time {
  margin-top: 6px;
  color: var(--text-secondary);
  font-size: 12.5px;
}

.pf-log-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
</style>
