<template>
  <two-col-layout class="upstreamList" :left="0">
    <template v-slot:btn>
      <Button type="primary" @click="handleAdd">新增</Button>
      <!-- <Button type="primary" @click="handleOpenCopy">复制集群</Button> -->
      <Button type="text" @click="handleSearch(true)">刷新</Button>
    </template>
    <template v-slot:right>
      <collapse class="emp-content flex-nowrap padding-for-page" :hideExpend="true">
        <template v-slot:content>
          <Table :columns="columns" :data="list">
            <template slot-scope="{ row }" slot="action">
              <div class="row-action">
                <button @click="handleEdit(row)">编辑</button>
                <button @click="handleSureCopy(row)">复制集群</button>
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
    <clusterModal v-model="showClusterModal" :info="currentCluster" @success="handleSearch(true)" />
    <!-- <copyModal v-model="showCopyModal" :fun="clusterDropList" @success="handleSureCopy" /> -->
    <delete-modal v-model="showDeleteModal" :content="deleteInfo" @handleChangeDelete="confirmDelete"></delete-modal>
  </two-col-layout>
</template>
<script>
import { getClusterPage, deleteCluster, getClusterList} from '@/api/modules/common';
import clusterModal from "./components/clusterModal";
// import copyModal from "./components/copyModal";
export default {
  name: 'upstreamList',
  components: {
    clusterModal
    // copyModal
  },
  mixins: [],
  data() {
    return {
      showCopyModal: false,
      showClusterModal: false,
      clusterDropList: getClusterList,
      disOptions: {
        disabledDate(date) {
          return date && date.valueOf() > Date.now();
        }
      },
      showDeleteModal: false,
      deleteInfo: "",
      clusterId: "",
      currentCluster: {},
      formData: {},
      columns: [
        {
          title: '序号',
          type: 'index',
          width: 45
        },
        {
          title: 'id',
          key: 'id',
          minWidth: 100
        },
        {
          title: '类型',
          key: 'loadBalancingPolicy',
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
      this.getAjaxList(getClusterPage, this.organizeData(params, true)).then((res) => {
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
      this.showClusterModal = true;
      this.currentCluster = {};
    },
    handleEdit(row) {
      row["_IsEdit"] = true;
      this.showClusterModal = true;
      this.currentCluster = _.cloneDeep(row);
    },
    handleOpenCopy() {
      this.showCopyModal = true;
    },
    dealWidthData(row) {
      let selectInfo = _.cloneDeep(row) || {};
      // 修改源数据中的id，唯一名称
      if (selectInfo) {
        selectInfo.id = selectInfo.id + "-1";
        if (selectInfo.httpRequest) {
          selectInfo.httpRequest.id = 0;
        }
        if (selectInfo.sessionAffinity) {
          selectInfo.sessionAffinity.id = 0;
          selectInfo.sessionAffinity.clusterId = null;
          _.each(selectInfo.settings || [], chl => {
            chl.id = 0;
          });
        }
        if (selectInfo.healthCheck) {
          selectInfo.healthCheck.id = 0;
          selectInfo.healthCheck.clusterId = null;
          if (selectInfo.healthCheck.passive) {
            selectInfo.healthCheck.passive.id = 0;
            selectInfo.healthCheck.passive.healthCheckOptionsId = 0;
          }
          if (selectInfo.healthCheck.active) {
            selectInfo.healthCheck.active.id = 0;
            selectInfo.healthCheck.active.healthCheckOptionsId = 0;
          }
        }
        if (selectInfo.httpClient) {
          selectInfo.httpClient.id = 0;
          selectInfo.httpClient.clusterId = null;
          _.each(selectInfo.httpClient || [], chl => {
            chl.id = 0;
          });
          if (selectInfo.httpClient.clientCertificate) {
            selectInfo.httpClient.clientCertificate.id = 0;
            selectInfo.httpClient.clientCertificate.proxyHttpClientOptionsId = null;
          }
        }
        if (selectInfo.httpRequest) {
          selectInfo.httpRequest.id = 0;
          selectInfo.httpRequest.clusterId = null;
        }
        _.each(selectInfo.destinations || [], chl => {
          chl.id = 0;
          _.each(chl.metadata || [], mate => {
            mate.id = 0;
          });
        });
        _.each(selectInfo.metadata || [], mate => {
          mate.id = 0;
        });
        selectInfo.proxyRoutes = null;
      }
      return selectInfo;
    },
    handleSureCopy(row) {
      let info = this.dealWidthData(row);
      this.showCopyModal = false;
      this.showClusterModal = true;
      this.currentCluster = _.cloneDeep(info);
    },
    handleDel(row) {
      this.clusterId = row.id;
      this.deleteInfo = `确定删除集群 <span class="name">${row.id}</span> 吗？`;
      this.showDeleteModal = true;
    },
    confirmDelete() {
      if (!this.clusterId) {
        return;
      }
      deleteCluster({clusterId: this.clusterId}).then(res => {
        if (res.data) {
          this.clusterId = "";
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
.upstreamList{
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
