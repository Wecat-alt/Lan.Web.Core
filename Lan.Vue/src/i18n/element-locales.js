import { ElMessage } from 'element-plus'

// Element Plus语言包
export const elementLocales = {
  zh: {
    name: 'zh-cn',
    el: {
      colorpicker: {
        confirm: '确定',
        clear: '清空'
      },
      datepicker: {
        now: '此刻',
        today: '今天',
        cancel: '取消',
        clear: '清空',
        confirm: '确定',
        selectDate: '选择日期',
        selectTime: '选择时间',
        startDate: '开始日期',
        startTime: '开始时间',
        endDate: '结束日期',
        endTime: '结束时间',
        prevYear: '前一年',
        nextYear: '后一年',
        prevMonth: '上个月',
        nextMonth: '下个月',
        year: '年',
        month1: '1 月',
        month2: '2 月',
        month3: '3 月',
        month4: '4 月',
        month5: '5 月',
        month6: '6 月',
        month7: '7 月',
        month8: '8 月',
        month9: '9 月',
        month10: '10 月',
        month11: '11 月',
        month12: '12 月',
        week: '周次',
        weeks: {
          sun: '日',
          mon: '一',
          tue: '二',
          wed: '三',
          thu: '四',
          fri: '五',
          sat: '六'
        },
        months: {
          jan: '一月',
          feb: '二月',
          mar: '三月',
          apr: '四月',
          may: '五月',
          jun: '六月',
          jul: '七月',
          aug: '八月',
          sep: '九月',
          oct: '十月',
          nov: '十一月',
          dec: '十二月'
        }
      },
      select: {
        loading: '加载中',
        noMatch: '无匹配数据',
        noData: '无数据',
        placeholder: '请选择'
      },
      table: {
        noData: '暂无数据',
        noFilteredData: '暂无筛选结果',
        confirmFilter: '筛选',
        resetFilter: '重置',
        clearFilter: '全部'
      },
      messagebox: {
        title: '提示',
        confirm: '确定',
        cancel: '取消',
        error: '输入的数据不合法!'
      },
      upload: {
        deleteTip: '按 delete 键可删除',
        delete: '删除',
        preview: '查看图片',
        continue: '继续上传'
      },
      pagination: {
        goto: '前往',
        pagesize: '条/页',
        total: '共 {total} 条',
        pageClassifier: '页'
      }
    }
  },
  en: {
    name: 'en',
    el: {
      colorpicker: {
        confirm: 'OK',
        clear: 'Clear'
      },
      datepicker: {
        now: 'Now',
        today: 'Today',
        cancel: 'Cancel',
        clear: 'Clear',
        confirm: 'OK',
        selectDate: 'Select date',
        selectTime: 'Select time',
        startDate: 'Start Date',
        startTime: 'Start Time',
        endDate: 'End Date',
        endTime: 'End Time',
        prevYear: 'Previous Year',
        nextYear: 'Next Year',
        prevMonth: 'Previous Month',
        nextMonth: 'Next Month',
        year: '',
        month1: 'January',
        month2: 'February',
        month3: 'March',
        month4: 'April',
        month5: 'May',
        month6: 'June',
        month7: 'July',
        month8: 'August',
        month9: 'September',
        month10: 'October',
        month11: 'November',
        month12: 'December',
        weeks: {
          sun: 'Sun',
          mon: 'Mon',
          tue: 'Tue',
          wed: 'Wed',
          thu: 'Thu',
          fri: 'Fri',
          sat: 'Sat'
        },
        months: {
          jan: 'Jan',
          feb: 'Feb',
          mar: 'Mar',
          apr: 'Apr',
          may: 'May',
          jun: 'Jun',
          jul: 'Jul',
          aug: 'Aug',
          sep: 'Sep',
          oct: 'Oct',
          nov: 'Nov',
          dec: 'Dec'
        }
      },
      select: {
        loading: 'Loading',
        noMatch: 'No matching data',
        noData: 'No data',
        placeholder: 'Select'
      },
      table: {
        noData: 'No Data',
        noFilteredData: 'No filter data',
        confirmFilter: 'Confirm',
        resetFilter: 'Reset',
        clearFilter: 'Clear'
      },
      messagebox: {
        title: 'Message',
        confirm: 'OK',
        cancel: 'Cancel',
        error: 'Illegal input'
      },
      upload: {
        deleteTip: 'press delete to remove',
        delete: 'Delete',
        preview: 'Preview',
        continue: 'Continue'
      },
      pagination: {
        goto: 'Go to',
        pagesize: '/page',
        total: 'Total {total}',
        pageClassifier: ''
      }
    }
  }
}

// 切换Element Plus语言
export function changeElementPlusLocale(locale) {
  // 这里可以扩展Element Plus的语言切换逻辑
  // 在实际项目中，你可能需要将elementLocales[locale]传递给Element Plus
  console.log(`Element Plus language changed to: ${locale}`)
}

// 显示切换语言成功的消息
export function showLanguageSwitchMessage(locale) {
  const messages = {
    zh: '语言已切换为中文',
    en: 'Language switched to English'
  }
  
  ElMessage.success(messages[locale] || 'Language switched')
}