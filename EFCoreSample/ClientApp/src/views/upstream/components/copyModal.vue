<template>
  <Modal v-model="visible" :title="`复制${isCluster ? '集群' : '路由'}`" width="350" :fullscreen="false" class="copyModal">
    <div class="copyModal-content">
      <div style="margin-bottom:10px;">* 请选择要复制的{{isCluster ? '集群' : '路由'}}</div>
      <Select v-model="selectId" :placeholder="`请选择${isCluster ? '集群' : '路由'}`" style="width:200px;" transfer filterable clearable>
        <Option v-for="(item, index) in allDropList" :key="index" :value="item._SelectId">{{item._SelectName}}</Option>
      </Select>
    </div>
    <div slot="footer">
      <Button type="text" @click="visible=false">取消</Button>
      <Button type="primary" @click="handlerSubmit">提交</Button>
    </div>
  </Modal>
</template>

<script>
export default {
  name: 'copyModal',
  props: {
    value: {
      type: Boolean,
      default: false
    },
    fun: {
      type: Function,
      default: null
    }
  },
  data () {
    return {
      visible: false,
      selectId: "",
      allDropList: []
    };
  },
  computed: {
    isCluster() { // 是否是集群列表，反之为路由列表
      return this.$route.name == "upstreamList" ?  true : false; 
    }
  },
  watch: {
    value(val) {
      this.visible = val;
      if (val) {
        this.selectId = "";
        this.allDropList = [];
        this.getDropList();
      }
    },
    visible(val) {
      this.$emit("input", val);
    }
  },
  methods: {
    getDropList() {
      let fun = this.fun;
      if (!fun) {
        return;
      }
      fun().then(data => {
        _.each(data || [], item => {
          item["_SelectName"] = "";
          item["_SelectId"] = String(item.id);
          if (this.isCluster) {
            item["_SelectName"] = item.id;
          } else {
            item["_SelectName"] = item.routeId;
          }
        });
        this.allDropList = data || [];
      }).catch(() => {
        this.allDropList = [];
      });
    },
    handlerSubmit() {
      if (!this.selectId) {
        this.$Message.error(`请选择${this.isCluster ? '集群' : '路由'}`);
        return;
      }
      let selectInfo = {};
      _.each(this.allDropList || [], item => {
        if (this.selectId == item._SelectId) {
          selectInfo = _.cloneDeep(item);
        }
      });
      // 修改源数据中的id，唯一名称
      if (selectInfo) {
        if (this.isCluster) {
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
        } else {
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
      }
      delete selectInfo._SelectId;
      delete selectInfo._SelectName;
      console.log(selectInfo);
      this.$emit("success", selectInfo);
    }
  }
}

</script>
<style lang='less'>
.copyModal {
  .flex {
    display: flex;
  }
  .flex-col {
    flex-direction: column;
  }
  .col-center {
    align-items: center;
  }
  .row-center {
    justify-content: center;
  }
  .tips {
    color: #cdcdcd;
    font-weight: normal;
    padding-left: 4px;
  }
  .margin-bottom {
    &-16 {
      margin-bottom: 16px;
    }
  }
  .file-label {
    text-align: right;
    min-width: 80px;
    padding: 0 10px;
  }
  .http-config {
    border: 1px solid red;
  }
  &-content {
    height: 100%;
    margin-left: 20px;
    overflow-y: auto;
  }
}

</style>