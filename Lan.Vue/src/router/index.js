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
    component: () => import('../views/Home.vue'),
  },
  {
    path: '/screen',
    name: 'screen',
    component: () => import('../views/dataScreen/index.vue'),
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/login.vue'),
  },
  {
    path: '/main',
    name: 'main',
    component: () => import('@/views/index.vue'),
  },
  {
    path: '/user',
    name: 'user',
    component: () => import('@/views/system/user/index'),
  },
  {
    path: '/camera',
    name: 'camera',
    component: () => import('@/views/device/camera/index'),
  },
  {
    path: '/radar',
    name: 'radar',
    component: () => import('@/views/device/radar/index'),
  },
  {
    path: '/defencearea',
    name: 'defencearea',
    component: () => import('@/views/device/defencearea/index'),
  },
  {
    path: '/ptz',
    name: 'ptz',
    component: () => import('@/views/components/ptz'),
  },
  {
    path: '/calibration',
    name: 'calibration',
    component: () => import('@/views/device/calibration/index'),
  },
  {
    path: '/alarm',
    name: 'alarm',
    component: () => import('@/views/query/alarm_index'),
  },
  {
    path: '/realtime_map',
    name: 'realtime_map',
    component: () => import('@/views/map/realtime_map'),
  },
  {
    path: '/autoMap',
    name: 'autoMap',
    component: () => import('@/views/map/AutoMap.vue'),
  },
  {
    path: '/historical_map',
    name: 'historical_map',
    component: () => import('@/views/map/historical map'),
  },
  {
    path: '/play',
    name: 'play',
    component: () => import('@/views/map/play'),
  },
  {
    path: '/config',
    name: 'config',
    component: () => import('@/views/system/config/index'),
  },
]

// 创建路由实例
const router = createRouter({
  history: createWebHistory(), // 使用 HTML5 history 模式
  routes: routes,
})

export default router
