<template>
  <two-col-layout class="routeList" :left="0">
    <template v-slot:btn>
      <Button type="primary" @click="handleAdd">新增</Button>
      <!-- <Button type="primary" @click="handleOpenCopy">复制路由</Button> -->
      <Button type="text" @click="handleSearch(true)">刷新</Button>
    </template>
    <template v-slot:right>
      <collapse class="emp-content flex-nowrap padding-for-page" :hideExpend="true">
        <template v-slot:content>
          <Table :columns="columns" :data="list">
            <template slot-scope="{ row }" slot="action">
              <div class="row-action">
                <button @click="handleEdit(row)">编辑</button>
                <button @click="handleSureCopy(row)">复制路由</button>
                <button class="color-delete" @click="handleDel(row)">删除</button>
              </div>
            </template>
          </Table>
        </template>
      </collapse>
    </template>
    <template v-slot:page>
      <Page
        show-sizer show-elevator placement="top" show-total
        :current="pager.pageIndex"
        :total="pager.total"
        :page-size="pager.pageSize"
        :page-size-opts="pageSizeOpt"
        @on-page-size-change="pageSizeChange"
        @on-change="pageChange"
        class="i-pages"
        />
    </template>
    <routeModal v-model="showRouteModal" :info="currentProxyRoute" @success="handleSearch(true)" />
    <!-- <copyModal v-model="showCopyModal" :fun="proxyRouteDropList" @success="handleSureCopy" /> -->
    <delete-modal v-model="showDeleteModal" :content="deleteInfo" @handleChangeDelete="confirmDelete"></delete-modal>
  </two-col-layout>
</template>
<script>
import { getProxyRoutePage, deleteProxyRoute, getProxyRouteList } from '@/api/modules/common';
import routeModal from "./components/routeModal";
// import copyModal from "./components/copyModal";
export default {
  name: 'routeList',
  components: {
    routeModal
    // copyModal
  },
  mixins: [],
  data() {
    return {
      showCopyModal: false,
      showRouteModal: false,
      proxyRouteDropList: getProxyRouteList,
      disOptions: {
        disabledDate(date) {
          return date && date.valueOf() > Date.now();
        }
      },
      showDeleteModal: false,
      deleteInfo: "",
      routerId: "",
      currentProxyRoute: {},
      formData: {},
      columns: [
        {
          title: '序号',
          type: 'index',
          width: 45
        },
        {
          title: '路由名称',
          key: 'routeId',
          minWidth: 100
        },
        {
          title: '集群名称',
          key: 'clusterId',
          minWidth: 100
        },
        {
          title: '授权策略',
          key: 'authorizationPolicy',
          minWidth: 100
        },
        {
          title: 'cors策略',
          key: 'corsPolicy',
          minWidth: 100
        },
        {
          title: '操作',
          slot: 'action',
          minWidth: 100
        }
      ]
    }
  },
  methods: {
    handleSearch(fromOne = true) {
      let pageIndex = fromOne ? 1 : this.pager.pageIndex;
      this.pageChange(pageIndex);
    },
    getEndParams(formData, params) {
      params = _.extend({}, formData, params);
      delete params.Time;
      _.each(params, (value, key) => {
        if (_.isArray(value) && !value[0]) {
          delete params[key];
        } else if (!value) {
          delete params[key];
        }
      });
      return params;
    },
    handleReset(name) {
      if (this.$refs[name]) {
        this.$refs[name].resetFields();
      }
      this.formData = {};
      this.handleSearch(true);
    },
    handleSearchDateSelected(dateList, key, start, end) {
      if (dateList[0] && dateList[1]) {
        this.formData[start] = dateList[0];
        this.formData[end] = dateList[1];
        this.formData[key] = dateList || [];
      } else {
        delete this.formData[start];
        delete this.formData[end];
        this.formData[key] = [];
      }
    },
    getList() {
      let params = {};
      params = this.getEndParams(this.formData, params);
      this.resetBaseScrollView();
      this.getAjaxList(getProxyRoutePage, this.organizeData(params, true)).then((res) => {
        let data = res.data;
        let list = [];
        if (data) {
          if (_.isArray(data)) {
            list = data || [];
          } else if (_.isArray(data.DataList)) {
            list = data.DataList || [];
          } else if (_.isArray(data.data_list)) {
            list = data.data_list || [];
          }
        }
        this.list = list;
      });
    },
    handleAdd() {
      this.showRouteModal = true;
      this.currentProxyRoute = {};
    },
    handleEdit(row) {
      row["_IsEdit"] = true;
      this.showRouteModal = true;
      this.currentProxyRoute = _.cloneDeep(row);
    },
    handleOpenCopy() {
      this.showCopyModal = true;
    },
    dealWidthData(row) {
      let selectInfo = _.cloneDeep(row) || {};
      // 修改源数据中的id，唯一名称
      if (selectInfo) {
        selectInfo.id = 0;
        selectInfo.routeId = selectInfo.routeId + "-1";
        if (selectInfo.match) {
          selectInfo.match.id = 0;
          selectInfo.match.proxyRouteId = 0;
          _.each(selectInfo.match.headers || [], chl => {
            chl.id = 0;
            chl.proxyMatchId = 0;
          });
        }
        _.each(selectInfo.metadata || [], mate => {
          mate.id = 0;
        });
        _.each(selectInfo.transforms || [], chl => {
          chl.id = 0;
        });
      }
      return selectInfo;
    },
    handleSureCopy(row) {
      let info = this.dealWidthData(row);
      this.showCopyModal = false;
      this.showRouteModal = true;
      this.currentProxyRoute = _.cloneDeep(info);
    },
    handleDel(row) {
      this.routerId = row.id;
      this.deleteInfo = `确定删除路由 <span class="name">${row.routeId}</span> 吗？`;
      this.showDeleteModal = true;
    },
    confirmDelete() {
      if (!this.routerId) {
        return;
      }
      deleteProxyRoute({routeId: this.routerId}).then(res => {
        if (res.data) {
          this.routerId = "";
          this.deleteInfo = "";
          this.showDeleteModal = false;
          this.$Message.success(res.message || '删除成功');
          this.handleSearch(true);
        } else {
          this.$Message.error(res.message || '删除失败');
        }
      }).catch(res => {
        this.$Message.error(res.message || '删除失败');
      });
    }
  },
  created() {
    this.handleSearch();
  }
}
</script>
<style lang="less">
.routeList{
  .long-form-time {
    width: 320px !important;
    .ivu-form-item-content {
      width: 100%;
      .ivu-date-picker {
        width: 100%;
        .ivu-picker-confirm {
          .ivu-btn {
            margin-top: 0;
          }
        }
      }
    }
  }
}
</style>
