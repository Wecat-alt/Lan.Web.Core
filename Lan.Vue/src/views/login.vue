<template>
  <starBackground></starBackground>
  <div class="login-wrap">
    <div style="position: fixed; top: 20px; right: 20px">
      <LanguageSwitcher />
    </div>
    <div class="login-box">
      <h2 class="title">{{ $t('common.system_title') }}</h2>
      <div class="login-r-con">
        <img src="/images/login_logo.png" class="login-logo" />
        <el-form
          ref="loginRef"
          :model="loginForm"
          :rules="loginRules"
          class="login-form"
          v-show="loginType == 1"
        >
          <el-form-item prop="username">
            <el-input v-model="loginForm.username" type="text" auto-complete="off">
              <template #prefix>
                <!-- <svg-icon name="user" class="input-icon" /> -->
                <img src="../assets/icons/svg/user.svg" />
              </template>
            </el-input>
          </el-form-item>
          <el-form-item prop="password">
            <el-input
              v-model="loginForm.password"
              show-password
              type="password"
              auto-complete="off"
              @keyup.enter="handleLogin"
            >
              <template #prefix>
                <!-- <svg-icon name="password" class="input-icon" /> -->
                <img src="../assets/icons/svg/password.svg" />
              </template>
            </el-input>
          </el-form-item>

          <el-form-item
            style="width: 100%"
            :style="{ 'margin-top': captchaOnOff == 'off' ? '40px' : '' }"
          >
            <el-button
              :loading="loading"
              size="default"
              round
              type="primary"
              style="width: 100%"
              @click.prevent="handleLogin"
            >
              <span v-if="!loading">{{ $t('login.loginBtn') }}</span>
              <span v-else>登 录 中...</span>
            </el-button>
          </el-form-item>

          <div style="display: flex; justify-content: space-between; align-items: center">
            <el-checkbox v-model="loginForm.rememberMe">{{ $t('login.rememberPwd') }}</el-checkbox>
          </div>
        </el-form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { login } from '@/api/system/login'
import { initializeLocalPlayerSocket } from '@/utils/localPlayerSocket'
import starBackground from '@/views/components/starBackground.vue'
// import { ref } from 'vue'
import { useRouter } from 'vue-router'
// import { useAuthStore } from '../stores/auth'

//import useSocketStore from '@/stores/socket'

import LanguageSwitcher from '../components/LanguageSwitch.vue'

const { proxy } = getCurrentInstance()
const router = useRouter()
// const authStore = useAuthStore()

const loginForm = ref({
  username: 'admin',
  password: 'admin',
  code: '',
  uuid: '',
  loginIP: '',
  clientId: '',
})
const loginRules = {
  username: [
    {
      required: true,
      trigger: 'blur',
      message: proxy.$t('validation.username_null'),
    },
  ],
  password: [
    {
      required: true,
      trigger: 'blur',
      message: proxy.$t('validation.password_null'),
    },
  ],
}
const loginType = ref(1)
const loading = ref(false)

const handleLogin = async () => {
  loading.value = true
  try {
    const res = await login(loginForm.value.username, loginForm.value.password)
    const { code, data } = res.data
    if (code == 200) {
      // 保存 token（如果后端返回）并标记已登录
      const token = data && (data.token || data.accessToken || data.Authorization)
      if (token) {
        localStorage.setItem('token', token)
      }
      localStorage.setItem('isAuthenticated', 'true')
      initializeLocalPlayerSocket().catch(() => {})
      router.push('/main')
    } else {
      const msg = proxy.$t('message.checkLogin')
      proxy.$modal && proxy.$modal.msgError && proxy.$modal.msgError(msg)
    }
  } catch (err) {
    proxy.$modal &&
      proxy.$modal.msgError &&
      proxy.$modal.msgError(err && err.message ? err.message : '登录失败')
  } finally {
    loading.value = false
  }
}
</script>

<style lang="scss" scoped>
@use '../assets/styles/login.scss';
</style>
