<template>
  <div class="language-switcher">
    <el-select
      v-model="currentLanguage"
      @change="handleLanguageChange"
      size="small"
      style="width: 120px"
    >
      <el-option
        v-for="lang in languageOptions"
        :key="lang.value"
        :label="lang.label"
        :value="lang.value"
      />
    </el-select>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { changeLanguage, getCurrentLanguage } from '../i18n/index.js'
import { showLanguageSwitchMessage } from '../i18n/element-locales.js'
import { ElMessage } from 'element-plus'

export default {
  name: 'LanguageSwitcher',
  
  setup() {
    const { t } = useI18n()
    
    // 当前语言
    const currentLanguage = ref('zh')
    
    // 语言选项
    const languageOptions = ref([
      { label: '中文', value: 'zh' },
      { label: 'English', value: 'en' }
    ])
    
    // 处理语言切换
    const handleLanguageChange = (lang) => {
      changeLanguage(lang)
      showLanguageSwitchMessage(lang)
    }
    
    // 初始化
    onMounted(() => {
      currentLanguage.value = getCurrentLanguage()
    })
    
    return {
      currentLanguage,
      languageOptions,
      handleLanguageChange
    }
  }
}
</script>

<style scoped>
.language-switcher {
  display: inline-block;
}
</style>