export const API_CONTRACT = {
  auth: {
    loginConfig: '/Auth/LoginConfig',
    login: '/Auth/Login',
    refreshToken: '/Auth/RefreshToken',
    currentUser: '/Auth/CurrentUser',
    permissions: '/Auth/Permissions',
    logout: '/Auth/Logout',
    codes: '/Auth/PermissionCodes',
    userMenus: '/Menu/UserMenus',
  },
  system: {
    users: '/System/Users',
    roles: '/System/Roles',
    permissions: '/System/Permissions',
    menus: '/System/Menus',
  },
  accessLog: '/api/AccessLog',
  operationLog: '/api/OperationLog',
  exceptionLog: '/api/ExceptionLog',
  auditLog: '/api/AuditLog',
} as const

export type ApiContract = typeof API_CONTRACT
