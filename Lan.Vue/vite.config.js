import vue from '@vitejs/plugin-vue'
import path from 'path'
import { fileURLToPath } from 'url'
import { defineConfig } from 'vite'

import AutoImport from 'unplugin-auto-import/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'
import Components from 'unplugin-vue-components/vite'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

// https://vite.dev/config/
export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
    extensions: ['.mjs', '.js', '.ts', '.jsx', '.tsx', '.json', '.vue'],
  },
  plugins: [
    vue(),
    Components({
      resolvers: [ElementPlusResolver()],
    }),
    AutoImport({
      imports: ['vue', 'vue-router', 'pinia'], // 根据需要添加其他库的导入配置，例如Element Plus组件的自动导入。注意：直接从Element Plus导入组件通常不需要这样做，除非你想自动导入所有组件的API。通常推荐手动按需引入。
      dts: 'src/auto-imports.d.ts', // 自动生成类型文件路径，可选。leader1
      eslintrc: { enabled: true }, // 在.eslintrc.cjs中启用自动导入的解析，可选。
    }),
  ],
})
