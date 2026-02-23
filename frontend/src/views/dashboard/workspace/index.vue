<script lang="ts" setup>
import { ref, h } from 'vue'
import {
  NCard,
  NGrid,
  NGridItem,
  NStatistic,
  NNumberAnimation,
  NAvatar,
  NIcon,
  NTag,
  NSpace,
  NSkeleton,
} from 'naive-ui'
import { Icon } from '@iconify/vue'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'
import dayjs from 'dayjs'

defineOptions({ name: 'WorkspacePage' })

const userStore = useUserStore()

const stats = ref([
  {
    label: '今日访客',
    value: 1284,
    trend: '+12.5%',
    up: true,
    icon: 'lucide:users',
    color: '#18a058',
    bgColor: '#f0fdf4',
  },
  {
    label: '用户总数',
    value: 38462,
    trend: '+2.3%',
    up: true,
    icon: 'lucide:user-check',
    color: '#2080f0',
    bgColor: '#eff6ff',
  },
  {
    label: '在线人数',
    value: 856,
    trend: '-5.1%',
    up: false,
    icon: 'lucide:activity',
    color: '#f0a020',
    bgColor: '#fffbeb',
  },
  {
    label: '今日请求',
    value: 98234,
    trend: '+8.7%',
    up: true,
    icon: 'lucide:zap',
    color: '#d03050',
    bgColor: '#fff1f2',
  },
])

const recentActivities = ref([
  { user: '张三', action: '登录了系统', time: dayjs().subtract(5, 'minute').toDate(), avatar: '' },
  {
    user: '李四',
    action: '新增了一个用户',
    time: dayjs().subtract(18, 'minute').toDate(),
    avatar: '',
  },
  {
    user: '王五',
    action: '修改了角色权限',
    time: dayjs().subtract(35, 'minute').toDate(),
    avatar: '',
  },
  {
    user: '赵六',
    action: '导出了报表数据',
    time: dayjs().subtract(1, 'hour').toDate(),
    avatar: '',
  },
  {
    user: 'Admin',
    action: '更新了系统配置',
    time: dayjs().subtract(2, 'hour').toDate(),
    avatar: '',
  },
])

const quickLinks = [
  { label: '用户管理', icon: 'lucide:users', to: '/system/user', color: '#18a058' },
  { label: '角色管理', icon: 'lucide:shield', to: '/system/role', color: '#2080f0' },
  { label: '菜单管理', icon: 'lucide:list-tree', to: '/system/menu', color: '#f0a020' },
  { label: '个人中心', icon: 'lucide:user', to: '/profile', color: '#d03050' },
]
</script>

<template>
  <div class="space-y-4">
    <!-- 欢迎横幅 -->
    <NCard :bordered="false" class="!bg-gradient-to-r from-green-500 to-emerald-600">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="text-xl font-bold text-white">
            欢迎回来，{{ userStore.nickname || userStore.username }} 👋
          </h2>
          <p class="mt-1 text-sm text-green-100">
            {{ formatDate(new Date(), 'YYYY年MM月DD日 dddd') }}，祝您工作愉快！
          </p>
        </div>
        <div class="hidden opacity-80 sm:block">
          <NIcon size="64" class="text-white">
            <Icon icon="lucide:layout-dashboard" />
          </NIcon>
        </div>
      </div>
    </NCard>

    <!-- 统计数据 -->
    <NGrid :x-gap="16" :y-gap="16" :cols="2" responsive="screen" :item-responsive="true">
      <NGridItem v-for="stat in stats" :key="stat.label" span="2 s:1 m:1 l:1">
        <NCard :bordered="false" class="hover-card">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm text-gray-500">{{ stat.label }}</p>
              <div class="mt-1 flex items-end gap-2">
                <NStatistic>
                  <template #default>
                    <NNumberAnimation :from="0" :to="stat.value" :duration="1500" />
                  </template>
                </NStatistic>
              </div>
              <p class="mt-1 text-xs" :class="stat.up ? 'text-green-500' : 'text-red-500'">
                <NIcon size="12" class="mr-1 inline-block align-middle">
                  <Icon :icon="stat.up ? 'lucide:trending-up' : 'lucide:trending-down'" />
                </NIcon>
                {{ stat.trend }} 较昨日
              </p>
            </div>
            <div
              class="flex h-12 w-12 items-center justify-center rounded-xl"
              :style="{ backgroundColor: stat.bgColor }"
            >
              <NIcon size="24" :style="{ color: stat.color }">
                <Icon :icon="stat.icon" />
              </NIcon>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>

    <NGrid :x-gap="16" :y-gap="16" :cols="3" responsive="screen" :item-responsive="true">
      <!-- 快捷入口 -->
      <NGridItem span="3 m:1">
        <NCard title="快捷入口" :bordered="false">
          <NGrid :x-gap="12" :y-gap="12" :cols="2">
            <NGridItem v-for="link in quickLinks" :key="link.label">
              <router-link :to="link.to">
                <div
                  class="flex cursor-pointer flex-col items-center gap-2 rounded-lg p-4 transition-all hover:shadow-md"
                  :style="{ backgroundColor: link.color + '15' }"
                >
                  <div
                    class="flex h-10 w-10 items-center justify-center rounded-lg"
                    :style="{ backgroundColor: link.color }"
                  >
                    <NIcon size="20" class="text-white">
                      <Icon :icon="link.icon" />
                    </NIcon>
                  </div>
                  <span class="text-sm font-medium">{{ link.label }}</span>
                </div>
              </router-link>
            </NGridItem>
          </NGrid>
        </NCard>
      </NGridItem>

      <!-- 最近动态 -->
      <NGridItem span="3 m:2">
        <NCard title="最近动态" :bordered="false">
          <div class="space-y-4">
            <div
              v-for="(activity, index) in recentActivities"
              :key="index"
              class="flex items-start gap-3"
            >
              <NAvatar
                round
                :size="36"
                :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${activity.user}`"
              />
              <div class="flex-1">
                <p class="text-sm">
                  <span class="font-medium">{{ activity.user }}</span>
                  <span class="text-gray-500">{{ activity.action }}</span>
                </p>
                <p class="mt-0.5 text-xs text-gray-400">
                  {{ formatDate(activity.time, 'HH:mm') }}
                </p>
              </div>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>
