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
        phone: '전화',
        last_login_at: '최근 로그인',
      },
      actions: {
        create: '사용자 생성',
        lock: '잠금',
        unlock: '잠금 해제',
        reset_password: '비밀번호 재설정',
        delete: '사용자 삭제',
        save_roles: '역할 저장',
        add_department: '부서에 추가',
        remove_department: '제거',
      },
      empty: '아직 사용자가 없습니다.',
      total: '사용자 {total}명',
      sections: {
        account: '계정',
        roles: '역할',
        departments: '부서',
      },
      reset_form: {
        new_password: '새 비밀번호',
        submit: '재설정',
      },
      member_form: {
        department: '부서',
        position: '직위',
      },
      status: {
        active: '활성',
        disabled: '비활성',
        locked: '잠김',
      },
      roles_saved: '역할을 저장했습니다.',
      password_reset: '비밀번호를 재설정했습니다. 다음 로그인 시 변경해야 합니다.',
      department_added: '부서에 추가했습니다.',
      locked: '사용자를 잠갔습니다.',
      no_roles: '사용 가능한 역할이 없습니다.',
      no_departments: '소속 부서가 없습니다.',
      primary_badge: '주',
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
      subtitle: '사용자 이름과 비밀번호를 입력해 로그인하세요',
      user_name_placeholder: '사용자 이름 또는 이메일',
      password_placeholder: '비밀번호',
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
