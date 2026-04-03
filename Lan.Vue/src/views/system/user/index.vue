<template>
  <div class="app-container">
    <el-form :model="queryParams" ref="queryForm" :inline="true" v-show="showSearch" label-width="68px" style="text-align: left;">
      <el-form-item label="登录名" prop="userName">
        <el-input v-model="queryParams.userName" placeholder="请输入用户名" clearable @keyup.enter="handleQuery" />
      </el-form-item>
      <el-form-item label="手机号码" prop="phonenumber">
        <el-input v-model="queryParams.phonenumber" placeholder="请输入手机号码" clearable @keyup.enter="handleQuery" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="search" @click="handleQuery">查询</el-button>
      </el-form-item>
    </el-form>

    <!-- <el-row :gutter="10" class="mb8">
      <el-col :span="1.5">
        <el-button type="primary"   @click="handleAdd" >
          添加
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="success"  :disabled="single" @click="handleUpdate" >
          修改
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="danger"   :disabled="multiple" @click="handleDelete" >
          删除
        </el-button>
      </el-col>
      <right-toolbar :showSearch="showSearch" @queryTable="getList"></right-toolbar>
    </el-row> -->

    <el-table v-loading="loading" :data="userList" border stripe header-cell-class-name="el-table-header-cell" highlight-current-row 
    @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="55" align="center" />
      <el-table-column label="用户编号" align="center" prop="userId" key="userId" />
      <el-table-column label="登录名" align="center" prop="userName" />
      <el-table-column label="用户昵称" align="center" prop="nickName" />
      <el-table-column label="手机号码" align="center" prop="phonenumber" />
      <el-table-column label="创建时间" align="center" prop="createTime" width="180">
        <template #default="scope">
          <span>{{ scope.row.createTime }}</span>
        </template>
      </el-table-column>
      <el-table-column label="操作" align="center" >
          <template #default="scope">
            <el-button type="success" size="small" icon="edit" title="编辑" v-hasPermi="['defencearea:edit']"
              @click="handleUpdate(scope.row)"></el-button>
            <!-- <el-button type="danger" size="small" icon="delete" title="删除" v-hasPermi="['defencearea:delete']"
              @click="handleDelete(scope.row)"></el-button> -->
          </template>
        </el-table-column>
    </el-table>
    <pagination :total="total" v-model:page="queryParams.pageNum" v-model:limit="queryParams.pageSize"
			@pagination="getList" />
    <!-- 添加或修改参数配置对话框 -->
    <el-dialog :title="title" v-model="open" width="500px" append-to-body :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <!-- <el-form-item label="用户编号" prop="userId">
          <el-input v-model="form.userId" placeholder="请输入参数名称" />
        </el-form-item> -->
        <el-form-item label="登录名" prop="userName">
          <el-input v-model="form.userName" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item label="用户密码" prop="password">
          <el-input v-model="form.password" placeholder="请输入用户密码" />
        </el-form-item>
        <el-form-item label="昵称" prop="nickName">
          <el-input v-model="form.nickName" placeholder="请输入昵称" />
        </el-form-item>
        <el-form-item label="电话" prop="phonenumber">
          <el-input v-model="form.phonenumber" placeholder="请输入电话" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button text @click="cancel">取消</el-button>
        <el-button type="primary" @click="submitForm">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { listUser, getUser, addUser, updateUser, delUser } from '@/api/system/user'
const { proxy } = getCurrentInstance()

const userList = ref([])
const open = ref(false)
const loading = ref(true)
const ids = ref([])
const single = ref(true)
const multiple = ref(true)
const showSearch = ref(true)
const total = ref(0)
const title = ref('')


const data = reactive({
  form: {},
  queryParams: {
    pageNum: 1,
    pageSize: 10,
    userName: undefined,
    phonenumber: undefined,
  },
  rules: {
    
  }
})

const { queryParams, form, rules } = toRefs(data)

const formRef = ref()



/** 查询参数列表 */
function getList() {
  loading.value = true
  listUser(queryParams.value).then((res) => {
    const { code, data } = res.data
    if (code == 200) {
      userList.value = data.result;
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
    userId: undefined,
    userName: undefined,
    password: undefined,
    nickName: undefined,
    phonenumber: undefined
  }
  proxy.resetForm('formRef')
}
/** 搜索按钮操作 */
function handleQuery() {
  queryParams.value.pageNum = 1
  getList()
}

/** 新增按钮操作 */
function handleAdd() {
  reset()
  open.value = true
  title.value = '添加参数'
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
  const userId = row.userId || ids.value
  getUser(userId).then((response) => {
    form.value = response.data.data
    open.value = true
    title.value = '修改参数'
  })
}
/** 提交按钮 */
function submitForm() {
  proxy.$refs['formRef'].validate((valid) => {

    if (valid) {
      if (form.value.userId != undefined) {
        updateUser(form.value).then((response) => {
          proxy.$modal.msgSuccess(this.$t('message.editSuccess'))
          open.value = false
          getList()
        })
      } else {
        addUser(form.value).then((response) => {
          proxy.$modal.msgSuccess('新增成功')
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
    .$confirm('是否确认删除参数编号为"' + configIds + '"的数据项？')
    .then(function () {
      return delUser(configIds)
    })
    .then(() => {
      getList()
      proxy.$modal.msgSuccess(this.$t('message.deleteSuccess'))
    })
    .catch(() => {})
}


getList()
</script>
