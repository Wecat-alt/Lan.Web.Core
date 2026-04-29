import { canAccessMenu } from '@/utils/permission'
import { createRouter, createWebHistory } from 'vue-router'

// 定义路由对象
const routes = [
  {
    path: '/',
    redirect: '/login', // 根路径重定向到登录页
  },
  {
    path: '/home',
    name: 'home',
    meta: { requiresAuth: true, menuKey: 'home' },
    component: () => import('../views/Home.vue'),
  },
  {
    path: '/screen',
    name: 'screen',
    meta: { requiresAuth: true, menuKey: 'screen' },
    component: () => import('../views/dataScreen/index.vue'),
  },
  {
    path: '/login',
    name: 'login',
    meta: { public: true },
    component: () => import('@/views/login.vue'),
  },
  {
    path: '/main',
    name: 'main',
    meta: { requiresAuth: true },
    component: () => import('@/views/index.vue'),
  },
  {
    path: '/user',
    name: 'user',
    meta: { requiresAuth: true, menuKey: 'user' },
    component: () => import('@/views/system/user/index'),
  },
  {
    path: '/camera',
    name: 'camera',
    meta: { requiresAuth: true, menuKey: 'camera' },
    component: () => import('@/views/device/camera/index'),
  },
  {
    path: '/radar',
    name: 'radar',
    meta: { requiresAuth: true, menuKey: 'radar' },
    component: () => import('@/views/device/radar/index'),
  },
  {
    path: '/defencearea',
    name: 'defencearea',
    meta: { requiresAuth: true, menuKey: 'defencearea' },
    component: () => import('@/views/device/defencearea/index'),
  },
  {
    path: '/ptz',
    name: 'ptz',
    meta: { requiresAuth: true, menuKey: 'camera' },
    component: () => import('@/views/components/ptz'),
  },
  {
    path: '/calibration',
    name: 'calibration',
    meta: { requiresAuth: true, menuKey: 'calibration' },
    component: () => import('@/views/device/calibration/index'),
  },
  {
    path: '/alarm',
    name: 'alarm',
    meta: { requiresAuth: true, menuKey: 'alarm' },
    component: () => import('@/views/query/alarm_index'),
  },
  {
    path: '/realtime_map',
    name: 'realtime_map',
    //meta: { requiresAuth: true, menuKey: 'realtime_map' },
    component: () => import('@/views/map/realtime_map'),
  },
  {
    path: '/autoMap',
    name: 'autoMap',
    meta: { requiresAuth: true, menuKey: 'autoMap' },
    component: () => import('@/views/map/AutoMap.vue'),
  },
  {
    path: '/livePreview',
    name: 'livePreview',
    meta: { requiresAuth: true, menuKey: 'livePreview' },
    component: () => import('@/views/livePreview/index.vue'),
  },
  {
    path: '/historical_map',
    name: 'historical_map',
    meta: { requiresAuth: true, menuKey: 'historical_map' },
    component: () => import('@/views/map/historical map'),
  },
  {
    path: '/play',
    name: 'play',
    meta: { requiresAuth: true, menuKey: 'play' },
    component: () => import('@/views/map/play'),
  },
  {
    path: '/config',
    name: 'config',
    meta: { requiresAuth: true, menuKey: 'config' },
    component: () => import('@/views/system/config/index'),
  },
]

// 创建路由实例
const router = createRouter({
  history: createWebHistory(), // 使用 HTML5 history 模式
  routes: routes,
})

router.beforeEach((to, from, next) => {
  const isAuthenticated = localStorage.getItem('isAuthenticated') === 'true'
  const isPublic = Boolean(to.meta && to.meta.public)

  if (!isPublic && !isAuthenticated) {
    next('/login')
    return
  }

  const menuKey = to.meta && to.meta.menuKey
  if (menuKey && !canAccessMenu(menuKey)) {
    next('/main')
    return
  }

  next()
})

export default router
