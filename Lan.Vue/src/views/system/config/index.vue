<template>
  <div class="app-container">
    <el-form :model="queryParams" ref="queryForm" :inline="true" v-show="showSearch" label-width="125px" style="text-align: left;">
      <el-form-item :label="$t('config.configName')" prop="configName">
        <el-input v-model="queryParams.configName" clearable @keyup.enter="handleQuery" />
      </el-form-item>
      <el-form-item :label="$t('config.configKey')" prop="configKey">
        <el-input v-model="queryParams.configKey" clearable @keyup.enter="handleQuery" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="search" @click="handleQuery">{{ $t('common.search') }}</el-button>
      </el-form-item>
    </el-form>

    <el-row :gutter="10" class="mb8">
      <el-col :span="1.5">
        <el-button type="primary"   @click="handleAdd" v-hasPermi="['system:config:add']">
          {{ $t('common.add') }}
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="success"  :disabled="single" @click="handleUpdate" v-hasPermi="['system:config:edit']">
          {{ $t('common.edit') }}
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="danger"   :disabled="multiple" @click="handleDelete" v-hasPermi="['system:config:remove']">
          {{ $t('common.delete') }}
        </el-button>
      </el-col>
      <right-toolbar :showSearch="showSearch" @queryTable="getList"></right-toolbar>
    </el-row>

    <el-table v-loading="loading" :data="configList" border stripe header-cell-class-name="el-table-header-cell" highlight-current-row 
    @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="55" align="center" />
      <el-table-column label="NO." align="center" prop="configId" />
      <el-table-column :label="$t('config.configName')" align="center" prop="configName" />
      <el-table-column :label="$t('config.configKey')" align="center" prop="configKey" />
      <el-table-column :label="$t('config.configValue')" align="center" prop="configValue" width="280" />
      <!-- <el-table-column label="操作" align="center" class-name="small-padding fixed-width">
        <template #default="scope">
          <el-button size="small" text icon="edit" @click="handleUpdate(scope.row)" v-hasPermi="['system:config:edit']">
            修改
          </el-button>
          <el-button size="small" text icon="delete" @click="handleDelete(scope.row)" v-hasPermi="['system:config:remove']">
            删除
          </el-button>
        </template>
      </el-table-column> -->
    </el-table>

    <!-- <pagination v-model:total="total" v-model:page="queryParams.pageNum" v-model:limit="queryParams.pageSize" @pagination="getList" /> -->
    <pagination :total="total" v-model:page="queryParams.pageNum" v-model:limit="queryParams.pageSize"
			@pagination="getList" />
    <!-- 添加或修改参数配置对话框 -->
    <el-dialog :title="title" v-model="open" width="500px" append-to-body :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="140px">
        <el-form-item :label="$t('config.configName')" prop="configName">
          <el-input v-model="form.configName"  />
        </el-form-item>
        <el-form-item :label="$t('config.configKey')" prop="configKey">
          <el-input v-model="form.configKey"  />
        </el-form-item>
        <el-form-item :label="$t('config.configValue')" prop="configValue">
          <el-input v-model="form.configValue" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button text @click="cancel">{{ $t('common.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ $t('common.save') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { listConfig, getConfig, delConfig, addConfig, updateConfig } from '@/api/system/config'

  // import { i18n } from '../../i18n/index.js'
  // const { t } = i18n.global
// 遮罩层
const loading = ref(true)
// 选中数组
const ids = ref([])
// 非单个禁用
const single = ref(true)
// 非多个禁用
const multiple = ref(true)
// 显示搜索条件
const showSearch = ref(true)
// 总条数
const total = ref(0)
// 参数表格数据
const configList = ref([])
// 弹出层标题
const title = ref('')
// 是否显示弹出层
const open = ref(false)
// 日期范围
const dateRange = ref([])
// 系统是否
const sysYesNoOptions = ref([])
const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  configName: undefined,
  configKey: undefined,
  configType: undefined
})

  import { i18n } from '../../../i18n/index.js'
  const { t } = i18n.global

const state = reactive({
  form: {},
  rules: {
    configName: [{ required: true, message: t('validation.configName'), trigger: 'blur' }],
    configKey: [{ required: true, message: t('validation.configKey'), trigger: 'blur' }],
    configValue: [{ required: true, message: t('validation.configValue'), trigger: 'blur' }]
  }
})
const formRef = ref()
const { form, rules } = toRefs(state)
const { proxy } = getCurrentInstance()

/** 查询参数列表 */
function getList() {
  loading.value = true
  listConfig(queryParams).then((res) => {
    const { code, data } = res.data
    if (code == 200) {
      configList.value = data.result;
      total.value = data.totalNum;
      loading.value = false;
    }

  })
}
// 取消按钮
function cancel() {
  open.value = false
  reset()
}
// 表单重置
function reset() {
  form.value = {
    configId: undefined,
    configName: undefined,
    configKey: undefined,
    configValue: undefined,
    configType: '1',
  }
  proxy.resetForm('formRef')
}
/** 搜索按钮操作 */
function handleQuery() {
  queryParams.pageNum = 1
  getList()
}

/** 新增按钮操作 */
function handleAdd() {
  reset()
  open.value = true
  title.value = proxy.$t('common.add')
}
// 多选框选中数据
function handleSelectionChange(selection) {
  ids.value = selection.map((item) => item.configId)
  single.value = selection.length != 1
  multiple.value = !selection.length
}
/** 修改按钮操作 */
function handleUpdate(row) {
  reset()
  const configId = row.configId || ids.value
  getConfig(configId).then((response) => {
    form.value = response.data.data
    open.value = true
    title.value = proxy.$t('common.edit')
  })
}
/** 提交按钮 */
function submitForm() {
  proxy.$refs['formRef'].validate((valid) => {

    form.value.configType='1';

    if (valid) {
      if (form.value.configId != undefined) {
        updateConfig(form.value).then((response) => {
          proxy.$modal.msgSuccess(proxy.$t('message.editSuccess'))
          open.value = false
          getList()
        })
      } else {
        addConfig(form.value).then((response) => {
          proxy.$modal.msgSuccess(proxy.$t('message.addSuccess'))
          open.value = false
          getList()
        })
      }
    }
  })
}

function handleDelete(row) {
  const configIds = row.configId || ids.value
  proxy
    .$confirm(proxy.$t('message.deleteConfirm', { id:configIds }))
    .then(function () {
      return delConfig(configIds)
    })
    .then(() => {
      getList()
      proxy.$modal.msgSuccess(proxy.$t('message.deleteSuccess'))
    })
    .catch(() => {})
}

proxy.getDicts('sys_yes_no').then((response) => {
  sysYesNoOptions.value = response.data
})
getList()

onMounted(() => {
  console.log('Component mounted')
})

</script>
