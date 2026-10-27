// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: '사용자',
      description: '현재 주체가 볼 수 있는 사용자.',
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
    login: {
      title: '로그인',
      description: '사용자 이름과 비밀번호로 로그인합니다.',
      fields: {
        tenant: '테넌트',
        user_name: '사용자 이름',
        password: '비밀번호',
      },
      tenant_placeholder: '플랫폼 계정은 비워 두세요',
      actions: {
        sign_in: '로그인',
      },
    },
    profile: {
      change_password: {
        title: '비밀번호 변경',
        description: '현재 비밀번호와 새 비밀번호(8자 이상, 영문자와 숫자 포함)를 입력하세요. 다른 세션은 로그아웃됩니다.',
        fields: {
          current_password: '현재 비밀번호',
          new_password: '새 비밀번호',
          confirm_password: '새 비밀번호 확인',
        },
        mismatch: '새 비밀번호가 서로 일치하지 않습니다.',
        success: '비밀번호가 변경되었고 다른 세션은 로그아웃되었습니다.',
        actions: {
          submit: '비밀번호 변경',
        },
      },
    },
  },
}
