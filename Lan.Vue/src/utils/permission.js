const AUTH_PROFILE_KEY = 'authProfile'

const LOCAL_AUTH_PRESETS = {
  admin: {
    roles: ['admin'],
    permissions: ['*:*:*'],
    menus: ['*'],
  },
  operator: {
    roles: ['operator'],
    permissions: [
      'realtime_map:view',
      'alarm:view',
      'livePreview:view',
      'camera:add',
      'defencearea:edit',
    ],
    menus: ['realtime_map', 'alarm', 'livePreview', 'camera', 'radar'],
  },
  guest: {
    roles: ['guest'],
    permissions: ['realtime_map:view'],
    menus: ['realtime_map'],
  },
}

function normalizeArray(value) {
  if (Array.isArray(value)) {
    return value.filter(Boolean)
  }
  if (typeof value === 'string' && value.trim()) {
    return value
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean)
  }
  return []
}

function safeParse(raw) {
  if (!raw) return {}
  try {
    return JSON.parse(raw)
  } catch {
    return {}
  }
}

export function getAuthProfile() {
  return safeParse(localStorage.getItem(AUTH_PROFILE_KEY))
}

export function clearAuthProfile() {
  localStorage.removeItem(AUTH_PROFILE_KEY)
}

export function buildAuthProfile(loginData = {}, username = '') {
  const user = loginData.user || {}
  const currentUsername =
    loginData.username || loginData.userName || user.userName || user.username || username || ''

  const preset = LOCAL_AUTH_PRESETS[String(currentUsername).toLowerCase()] || {}

  const roles = normalizeArray(loginData.roles || loginData.roleCodes || user.roles || preset.roles)
  const permissions = normalizeArray(
    loginData.permissions || loginData.perms || user.permissions || preset.permissions,
  )
  const menus = normalizeArray(loginData.menus || loginData.menuKeys || user.menus || preset.menus)

  return {
    username: currentUsername,
    roles,
    permissions,
    menus,
  }
}

export function setAuthProfile(profile = {}) {
  localStorage.setItem(AUTH_PROFILE_KEY, JSON.stringify(profile))
}

function isSuperAdmin(profile) {
  const roles = normalizeArray(profile.roles)
  const permissions = normalizeArray(profile.permissions)
  const menus = normalizeArray(profile.menus)
  return (
    roles.includes('admin') ||
    permissions.includes('*:*:*') ||
    permissions.includes('*') ||
    menus.includes('*')
  )
}

export function hasPermi(permission) {
  if (!permission) return false
  const profile = getAuthProfile()
  if (isSuperAdmin(profile)) return true
  const permissions = normalizeArray(profile.permissions)
  return permissions.includes(permission)
}

export function hasPermiOr(permissions = []) {
  return permissions.some((item) => hasPermi(item))
}

export function hasPermiAnd(permissions = []) {
  return permissions.every((item) => hasPermi(item))
}

export function hasRole(role) {
  if (!role) return false
  const profile = getAuthProfile()
  if (isSuperAdmin(profile)) return true
  const roles = normalizeArray(profile.roles)
  return roles.includes(role)
}

export function hasRoleOr(roles = []) {
  return roles.some((item) => hasRole(item))
}

export function hasRoleAnd(roles = []) {
  return roles.every((item) => hasRole(item))
}

export function canAccessMenu(menuKey) {
  if (!menuKey) return false
  const profile = getAuthProfile()
  if (isSuperAdmin(profile)) return true

  const menus = normalizeArray(profile.menus)
  if (menus.length > 0) {
    return menus.includes(menuKey)
  }

  const permissions = normalizeArray(profile.permissions)
  if (permissions.length > 0) {
    return permissions.includes(menuKey) || permissions.includes(`${menuKey}:view`)
  }

  // 兼容未返回权限数据的旧后端，默认放行
  return true
}
