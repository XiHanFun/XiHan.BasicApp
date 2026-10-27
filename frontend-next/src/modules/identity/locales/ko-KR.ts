// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: '사용자',
      description: '현재 테넌트의 사용자 (Identity 패키지의 N0 샘플).',
      fields: {
        user_name: '사용자 이름',
        display_name: '표시 이름',
        email: '이메일',
        status: '상태',
        created_at: '생성 시각',
      },
      actions: {
        create: '사용자 생성',
      },
      empty: '아직 사용자가 없습니다.',
      total: '사용자 {total}명',
    },
  },
}
