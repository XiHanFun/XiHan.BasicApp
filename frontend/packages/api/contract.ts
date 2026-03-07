export const API_CONTRACT = {
  auth: {
    loginConfig: '/auth/login-config',
    login: '/auth/login',
    refreshToken: '/auth/refresh-token',
    currentUser: '/auth/me',
    permissions: '/auth/permissions',
    logout: '/auth/logout',
    codes: '/auth/permission-codes',
    userMenus: '/menu/userMenus',
  },
  system: {
    users: '/system/users',
    roles: '/system/roles',
    permissions: '/system/permissions',
    menus: '/system/menus',
    departments: '/system/departments',
    tenants: '/system/tenants',
    configs: '/system/configs',
    dicts: '/system/dicts',
    notifications: '/system/notifications',
    files: '/system/files',
    emails: '/system/emails',
    sms: '/system/sms',
    tasks: '/system/tasks',
    oauthApps: '/system/oauth-apps',
    reviews: '/system/reviews',
    userSessions: '/system/user-sessions',
  },
  logs: {
    access: '/access-logs',
    operation: '/operation-logs',
    exception: '/exception-logs',
    audit: '/audit-logs',
  },
} as const

export type ApiContract = typeof API_CONTRACT
