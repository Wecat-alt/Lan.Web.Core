import ElementPlus from 'element-plus'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router' // 引入路由配置
//import { pinia } from 'pinia'
import 'element-plus/dist/index.css'
import downFile from './utils/request.js'

import { initializeLocalPlayerSocket } from '@/utils/localPlayerSocket'
import { i18n } from './i18n/index.js'
// 分页组件
import Pagination from '@/components/Pagination'

// svg图标
import {
  addDateRange,
  download,
  handleTree,
  parseTime,
  resetForm,
  selectDictLabel,
} from '@/utils/ruoyi'
import { getConfigKey } from './api/config/config'
import { getDicts } from './api/system/dict/data.js'
import './assets/iconfont/iconfont.js' //iconfont
import SvgIcon from './components/SvgIcon/index.vue'
import elementIcons from './components/SvgIcon/svgicon.js'

// 注册指令
import plugins from './plugins' // plugin
//import pinia from './stores/index'

// 字典标签组件
import './assets/styles/index.scss' // 全局自定义样式 css
import DictTag from './components/DictTag/index.vue'

const app = createApp(App)

// signalR.init(import.meta.env.VITE_SIGNALR_URL)
// app.config.globalProperties.signalr = signalR

app.config.globalProperties.getConfigKey = getConfigKey
app.config.globalProperties.download = download
app.config.globalProperties.downFile = downFile
app.config.globalProperties.parseTime = parseTime
app.config.globalProperties.resetForm = resetForm
app.config.globalProperties.handleTree = handleTree
app.config.globalProperties.addDateRange = addDateRange
app.config.globalProperties.selectDictLabel = selectDictLabel
app.config.globalProperties.getDicts = getDicts

app.config.globalProperties.$t = i18n.global.t
app.config.globalProperties.$locale = i18n.global.locale

// 全局组件挂载
app.component('DictTag', DictTag)
app.component('Pagination', Pagination)
app.component('svg-icon', SvgIcon)
//app.use(pinia);
//app.use(pinia)
app.use(ElementPlus)
app.use(router)
app.use(plugins)
app.use(elementIcons)

// 提供国际化功能到全局
app.use(i18n)

if (localStorage.getItem('isAuthenticated') === 'true') {
  initializeLocalPlayerSocket().catch(() => {})
}

app.mount('#app')
