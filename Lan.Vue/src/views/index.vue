<template>
  <el-container class="app-shell">
    <el-header class="app-header">
      <div class="header-content">
        <div class="logo-con">
          <img src="/images/login_logo.png" class="top-logo" />
          <label>{{ $t('common.system_title') }}</label>
        </div>
        <app-sidebar @menu-change="handleMenuChange" />
        <div class="user-info">
          <!-- <div class="user-avatar">A</div> -->

          <a href="/play.rar" download="play.rar">Download</a>
          <el-icon><Avatar /></el-icon>
          <span>admin</span>
          <LanguageSwitcher />
        </div>
      </div>
    </el-header>
    <el-main class="app-main">
      <component :is="currentView" />
    </el-main>
    <!-- <el-footer class="app-footer">
          <div class="footer-content">
            <p>@@@.</p>
          </div>
        </el-footer> -->
  </el-container>
</template>

<script>
import { defineComponent, onBeforeUnmount, onMounted, shallowRef } from 'vue'

import AppSidebar from '../components/AppSidebar.vue'
import Home from '../views/Home.vue'

import calibration from '../views/device/calibration/index'
import camera from '../views/device/camera/index'
import defencearea from '../views/device/defencearea/index'
import radar from '../views/device/radar/index'
import alarm from '../views/query/alarm_index'

import autoMap from '../views/map/AutoMap.vue'
import realtime_map from '../views/map/realtime_map'
import config from '../views/system/config/index'
import user from '../views/system/user/index'

import LanguageSwitcher from '../components/LanguageSwitch.vue'

//import { useI18n } from 'vue-i18n'

export default defineComponent({
  name: 'App',
  components: {
    AppSidebar,
    LanguageSwitcher,
  },
  setup() {
    //const { t } = useI18n()
    const currentView = shallowRef(realtime_map)
    const views = {
      home: Home,
      defencearea: defencearea,
      camera: camera,
      radar: radar,
      wizard: radar,
      realtime_map: realtime_map,
      autoMap: autoMap,
      alarm: alarm,
      config: config,
      user: user,
      calibration: calibration,

      LanguageSwitcher: LanguageSwitcher,
    }

    const handleMenuChange = (view) => {
      // wizard flow: use localStorage wizard key to control next step
      const wizardStep = localStorage.getItem('wizard')
      if (view === 'radar' && wizardStep) {
        currentView.value = views['radar'] || realtime_map
        return
      }
      currentView.value = views[view] || realtime_map
    }

    const wizardHandler = (e) => {
      const next = e && e.detail ? e.detail : null
      if (next && views[next]) {
        currentView.value = views[next]
      }
    }

    onMounted(() => {
      window.addEventListener('wizard-next', wizardHandler)
    })

    onBeforeUnmount(() => {
      window.removeEventListener('wizard-next', wizardHandler)
    })

    return {
      currentView,
      handleMenuChange,
    }
  },
})
</script>

<style lang="scss">
.app-shell {
  height: 100vh;
  overflow: hidden;
}

.app-main {
  min-height: 0;
  overflow: auto;
  box-sizing: border-box;
}
</style>
