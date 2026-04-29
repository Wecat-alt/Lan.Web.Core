import {
  hasPermi,
  hasPermiAnd,
  hasPermiOr,
  hasRole,
  hasRoleAnd,
  hasRoleOr,
} from '@/utils/permission'

export default {
  // 验证用户是否具备某权限
  hasPermi(permission) {
    return hasPermi(permission)
  },
  // 验证用户是否含有指定权限，只需包含其中一个
  hasPermiOr(permissions) {
    return hasPermiOr(permissions)
  },
  // 验证用户是否含有指定权限，必须全部拥有
  hasPermiAnd(permissions) {
    return hasPermiAnd(permissions)
  },
  // 验证用户是否具备某角色
  hasRole(role) {
    return hasRole(role)
  },
  // 验证用户是否含有指定角色，只需包含其中一个
  hasRoleOr(roles) {
    return hasRoleOr(roles)
  },
  // 验证用户是否含有指定角色，必须全部拥有
  hasRoleAnd(roles) {
    return hasRoleAnd(roles)
  },
}
