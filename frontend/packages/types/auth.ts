// ==================== 认证 & 用户类型 ====================

export interface UserInfo {
  basicId: number
  userName: string
  nickName?: string
  appTitle?: string
  appLogo?: string
  avatar?: string
  email?: string
  phone?: string
  roles: string[]
  permissions: string[]
}

export interface LoginParams {
  username: string
  password: string
  captchaCode?: string
  captchaKey?: string
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  issuedAt: string
  expiresAt: string
  user: UserInfo
  roles: string[]
  permissions: string[]
}
