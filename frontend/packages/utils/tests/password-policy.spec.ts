import type { PasswordPolicy } from '~/types'
import { describe, expect, it } from 'vitest'
import { passwordPolicyError } from '~/utils'

function t(key: string, params?: Record<string, unknown>) {
  return params
    ? `${key}:${JSON.stringify(params)}`
    : key
}

const DEFAULT: PasswordPolicy = {
  minLength: 8,
  requireLetter: true,
  requireDigit: true,
  requireUppercase: false,
  requireLowercase: false,
  requireSymbol: false,
}
const STRICT: PasswordPolicy = {
  minLength: 12,
  requireLetter: true,
  requireDigit: true,
  requireUppercase: true,
  requireLowercase: true,
  requireSymbol: true,
}

describe('passwordPolicyError', () => {
  it('按后端缺省规则：至少 8 位、须含字母与数字', () => {
    expect(passwordPolicyError('Ab1', DEFAULT, t)).toBe('page.auth.password_rule_length:{"min":8}')
    expect(passwordPolicyError('12345678', DEFAULT, t)).toBe('page.auth.password_rule_letter')
    expect(passwordPolicyError('abcdefgh', DEFAULT, t)).toBe('page.auth.password_rule_digit')
    expect(passwordPolicyError('abcdefg1', DEFAULT, t)).toBeNull()
  })

  it('收紧后按后端的顺序报第一条不满足的，大小写与其他字符按 Unicode', () => {
    expect(passwordPolicyError('abcdefg12', STRICT, t)).toBe('page.auth.password_rule_length:{"min":12}')
    expect(passwordPolicyError('abcdefgh1234', STRICT, t)).toBe('page.auth.password_rule_uppercase')
    expect(passwordPolicyError('ABCDEFGH1234', STRICT, t)).toBe('page.auth.password_rule_lowercase')
    expect(passwordPolicyError('Abcdefgh1234', STRICT, t)).toBe('page.auth.password_rule_symbol')
    // 空格不算其他字符，标点与中文符号算
    expect(passwordPolicyError('Abcdefgh 1234', STRICT, t)).toBe('page.auth.password_rule_symbol')
    expect(passwordPolicyError('Abcdefgh-1234', STRICT, t)).toBeNull()
    expect(passwordPolicyError('Abcdefgh，1234', STRICT, t)).toBeNull()
  })

  it('放宽后不要求的类别不报', () => {
    expect(passwordPolicyError('abcdef', { ...DEFAULT, minLength: 6, requireDigit: false }, t)).toBeNull()
  })
})
