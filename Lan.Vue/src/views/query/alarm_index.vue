<template>
  <div>
    <el-row :gutter="20">
      <el-col :span="4" :xs="24">
        <!-- <div class="head-container">
          <el-input v-model="deptName" placeholder="请输入防区名称" clearable prefix-icon="el-icon-search" style="margin-bottom: 20px" />
        </div> -->
        <div class="head-container">
          <el-tree
            :data="deptOptions"
            :props="{ label: 'label', children: 'children' }"
            node-key="id"
            highlight-current
            default-expand-all
            @node-click="handleNodeClick"
          />
        </div>
      </el-col>

      <el-col :lg="20" :xm="24">
        <el-form
          :model="queryParams"
          label-position="right"
          inline
          ref="queryRef"
          v-show="showSearch"
          @submit.prevent
          style="text-align: left"
        >
          <el-form-item :label="$t('alarm.date')" prop="cameraIp">
            <el-date-picker
              @change="gettime"
              v-model="rangeDate"
              type="daterange"
              range-separator="-"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
            >
            </el-date-picker>
          </el-form-item>

          <el-form-item :label="$t('camera.ip')" prop="ip">
            <el-input
              v-model="queryParams.ip"
              clearable
              style="width: 240px"
              @keyup.enter="handleQuery"
            />
          </el-form-item>
          <el-form-item>
            <el-button icon="search" type="primary" @click="handleQuery">{{
              $t("common.search")
            }}</el-button>
          </el-form-item>

          <el-form-item>
            <el-button type="primary" @click="submitWith">{{
              $t("alarm.One")
            }}</el-button>
          </el-form-item>

          <el-form-item>
            <el-button type="primary" @click="submitAll">{{
              $t("alarm.processAll")
            }}</el-button>
          </el-form-item>
        </el-form>

        <el-table
          v-loading="loading"
          :data="dataList"
          border
          stripe
          header-cell-class-name="el-table-header-cell"
          highlight-current-row
          @selection-change="handleSelectionChange"
        >
          <el-table-column
            type="selection"
            :selectable="selectable"
            width="55"
          />
          <el-table-column
            prop="id"
            label="NO."
            width="120"
            align="center"
            fixed
          />
          <el-table-column
            :label="$t('zone.name')"
            :show-overflow-tooltip="true"
            width="180"
            align="center"
          >
            <template #default="scope">
              <el-link type="primary" @click="showDictData(scope.row)">{{
                scope.row.areaName
              }}</el-link>
            </template>
          </el-table-column>

          <el-table-column
            prop="latitude"
            :label="$t('zone.latitude')"
            width="140"
            align="center"
          />
          <el-table-column
            prop="longitude"
            :label="$t('zone.longitude')"
            width="140"
            align="center"
          />

          <el-table-column
            prop="dateTime"
            :label="$t('alarm.alarmTime')"
            width="180"
            align="center"
          />
          <el-table-column
            prop="cameraIp"
            :label="$t('alarm.ip')"
            width="160"
            align="center"
          />
          <el-table-column
            prop="level"
            :label="$t('alarm.alarmLevel')"
            width="120"
            align="center"
          />
          <el-table-column
            :label="$t('alarm.isProcessed')"
            align="center"
            prop="dealWith"
          >
            <template #default="scope">
              <el-tag
                :type="scope.row.dealWith == '已处理' ? 'success' : 'danger'"
              >
                {{
                  scope.row.dealWith == "已处理"
                    ? $t("alarm.Processed")
                    : $t("alarm.n_Processed")
                }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column
            prop="videoName"
            :label="$t('alarm.videoAddress')"
            align="center"
            :show-overflow-tooltip="true"
          />
        </el-table>

        <pagination
          :total="total"
          v-model:page="queryParams.pageNum"
          v-model:limit="queryParams.pageSize"
          @pagination="getList"
        />
      </el-col>
    </el-row>

    <!-- 添加或修改对话框 -->
    <el-dialog :title="title" v-model="open" append-to-body>
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="20">
          <el-col :lg="12" v-if="opertype != 1">
            <el-form-item label="编号" prop="id">
              <el-input-number
                v-model.number="form.id"
                controls-position="right"
                placeholder="请输入编号"
                :disabled="true"
              />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item label="IP" prop="ip">
              <el-input v-model="form.ip" placeholder="请输入IP" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item label="端口" prop="port">
              <el-input v-model.number="form.port" placeholder="请输入端口" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item label="北偏角" prop="northDeviationAngle">
              <el-input
                v-model="form.northDeviationAngle"
                placeholder="请输入北偏角"
              />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item label="纬度" prop="latitude">
              <el-input v-model="form.latitude" placeholder="请输入纬度" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item label="经度" prop="longitude">
              <el-input v-model="form.longitude" placeholder="请输入经度" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item label="状态">
              <el-radio-group v-model="form.status">
                <el-radio
                  v-for="dict in statusOptions"
                  :key="dict.dictValue"
                  :label="parseInt(dict.dictValue)"
                  >{{ dict.dictLabel }}</el-radio
                >
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer v-if="opertype != 3">
        <el-button text @click="cancel">取消</el-button>
        <el-button type="primary" @click="submitForm">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="dictDataVisible"
      draggable
      style="width: 1000px; height: 650px"
      :lock-scroll="false"
    >
      <dict-data v-model:dictId="dictId"></dict-data>
    </el-dialog>
  </div>
</template>

<script setup>
import {
  listAlarm,
  updateAlarm,
  updateAllAlarm,
} from "@/api/alarm/alarm_index";
import { treeselect } from "@/api/device/defencearea.js";
import dictData from "@/views/map/historical map.vue";

// import { i18n } from '../../i18n/index.js'
// const { t } = i18n.global

const deptOptions = ref(undefined);

var now = new Date();
var year = now.getFullYear(); // 得到年份
var month = now.getMonth(); // 得到月份
var date = now.getDate(); // 得到日期
month = month + 1;
month = month.toString().padStart(2, "0");
date = date.toString().padStart(2, "0");
var defaultDate = `${year}-${month}-${date}`;

let rangeDate = ref([defaultDate, defaultDate]);

const ids = ref([]);
const single = ref(true);
const multiple = ref(true);
const { proxy } = getCurrentInstance();

// 遮罩层
const loading = ref(true);
// 显示搜索条件
const showSearch = ref(true);
// 总条数
const total = ref(0);
// 表格数据
const dataList = ref([]);
// 弹出层标题
const title = ref("");
// 是否显示弹出层
const open = ref(false);
// 查询参数
const queryParams = ref({
  pageNum: 1,
  pageSize: 15,
  sort: "id",
  sortType: "asc",
  ip: undefined,
  areaId: "",
  startTime: "",
  endTime: "",
});
// 表单参数
const form = ref({});
// 字典弹出层
const dictDataVisible = ref(false);
// 字典Id传值给子组件
const dictId = ref(0);
getTreeselect();
getList();

function getTreeselect() {
  treeselect().then((response) => {
    deptOptions.value = response.data.data;
  });
}

function getList() {
  loading.value = true;
  queryParams.value.startTime = rangeDate.value[0];
  queryParams.value.endTime = rangeDate.value[1];
  listAlarm(queryParams.value).then((res) => {
    const { code, data } = res.data;

    if (code == 200) {
      dataList.value = data.result;
      total.value = data.totalNum;
      loading.value = false;
    }
  });
}
function cancel() {
  open.value = false;
  reset();
}
// 表单重置
function reset() {
  this.form = {
    id: undefined,
    bindingAreaId: -1,
    ip: null,
    port: "",
    status: "1",
    latitude: "0",
    longitude: "0",
    northDeviationAngle: "0",
  };
  this.resetForm("formRef");
}
const handleQuery = () => {
  queryParams.value.pageNum = 1;
  getList();
};
function resetQuery() {
  resetForm("queryForm");
  handleQuery();
}
function handleSelectionChange(selection) {
  ids.value = selection.map((item) => item.id);
  single.value = selection.length != 1;
  multiple.value = !selection.length;
}
function handleNodeClick(data) {
  queryParams.value.areaId = data.id;
  handleQuery();
}

const submitWith = (row) => {
  const Ids = row.id || ids.value;
  updateAlarm(Ids).then(() => {
    getList();
    proxy.$modal.msgSuccess(proxy.$t("message.one_msg"));
  });
};

const submitAll = () => {
  queryParams.value.startTime = rangeDate.value[0];
  queryParams.value.endTime = rangeDate.value[1];
  updateAllAlarm(queryParams.value).then(() => {
    getList();
    proxy.$modal.msgSuccess(proxy.$t("message.all_msg"));
  });
};

function gettime(rangeDate) {
  //定义开始结束时间
  if (rangeDate != null) {
    queryParams.value.startTime = rangeDate[0];
    queryParams.value.endTime = rangeDate[1];
  } else {
    queryParams.value.startTime = "";
    queryParams.value.endTime = "";
  }
}

function showDictData(row) {
  var url = window.__APP_CONFIG__.VITE_OPEN_URL;
  window.open(
    url +
      "/historical_map?alarmid=" +
      row.id +
      "&vn=" +
      row.videoName +
      "&lat=" +
      row.latitude +
      "&lon=" +
      row.longitude +
      "&areaid=" +
      row.areaId +
      "&time=" +
      encodeURIComponent(row.dateTime),
  );
  //window.open(`http://192.168.43.122:5122/historical_map?alarmid=`+row.id+'&vn='+row.videoName)
}
</script>
