import { isNull } from '@/assets/js/utils'
export default {
  data() {
    return {
      pageSizeOpt: [10, 15, 20, 30, 40],
      pager: {
        pageIndex: 1,
        pageSize: 10,
        total: 0
      },
      list: [],
      statusList: [
        {
          Id: 1,
          Name: '启用'
        },
        {
          Id: 0,
          Name: '禁用'
        }
      ]
    }
  },
  methods: {
    pageChange(pageIndex) {
      this.pager.pageIndex = pageIndex
      this.getList()
    },
    pageSizeChange(pageSize) {
      this.pager.pageSize = pageSize
      this.getList()
    },
    handleReset(form = 'form') {
      this.$refs[form] && this.$refs[form].resetFields()
    },
    hanldeClearForm(form = 'formData') {
      _.each(this[form], (value, key) => {
        this[form][key] = ''
      })
    },
    handleFormReset(callback) {
      this.handleReset()
      if (typeof callback == 'function') {
        callback()
      } else if (typeof callback == 'string') {
        this.hanldeClearForm(callback)
      }
    },
    handleFormResetAndGetList(callback) {
      this.handleFormReset(callback)
      this.pageChange(1)
    },
    organizeData(formData, isPost) {
      let pager = {
        ...this.pager
      }
      let data = _.extend({}, formData, pager)
      // post请求 不清理空参数
      if (!isPost) {
        _.each(data, (value, key) => {
          if (isNull(value)) {
            delete data[key]
          }
        })
      }
      delete data.total
      return data
    },
    transferKey(formData, uppercase = true) {
      _.each(formData, (value, key) => {
        key = String(key)
        if (key) {
          delete formData[key]
          formData[(uppercase ? key[0].toUpperCase() : key[0].toLowerCase()) + key.slice(1)] = value
        }
      })
      return formData
    },
    getAjaxList(fun, params, list) {
      if (list === undefined) {
        list = this.list
      }
      return new Promise((resolve, reject) => {
        fun(params).then(res => {
          let data = res.data
          let result = []
          if (!data) {
            result = []
          } else if (_.isArray(data)) {
            result = data
          } else if (data.data_list) {
            result = data.data_list
            this.pager.pageIndex = data.page
            this.pager.total = data.total_count
          }
          if (!data.page && params.pageSize) {
            this.pager.total = res.total || 0
          }
          if (!result.length) {
            this.pager.pageIndex = 1
            this.pager.total = 0
          }
          list.splice(0, list.length, ...result)
          resolve(res)
        }).catch(res => {
          list = []
          this.pager.pageIndex = 1
          this.pager.total = 0
          reject(res)
        })
      })
    }
  }
}
