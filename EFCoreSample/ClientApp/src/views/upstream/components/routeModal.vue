<template>
  <Modal v-model="visible" :title="`${info._IsEdit ? '编辑' : '新建'}路由`" width="1250" :fullscreen="false" class="routerModal">
    <div class="routerModal-content">
      <!-- 基本信息 -->
      <template>
        <div class="module-title flex col-center">
          <div class="label"></div>
          <span>基本信息<span class="tips">[BasicInformation]</span></span>
        </div>
        <div class="module-content">
          <Form ref="form" :model="formData" class="i-form self" :label-width="160" :rules="validateFormData" onsubmit="return false">
            <Row type="flex">
              <FormItem label="名称[RouteId]" prop="routeId">
                <Input v-model.trim="formData.routeId" placeholder="请输入名称"/>
              </FormItem>
              <FormItem label="集群[ClusterId]" prop="clusterId" :label-width="220">
                <Select v-model="formData.clusterId" placeholder="请选择集群" transfer filterable clearable>
                  <Option v-for="(item, index) in allClusterList" :key="index" :value="item.id">{{item.id}}</Option>
                </Select>
              </FormItem>
              <FormItem label="顺序[Order]" prop="order" :label-width="100">
                <Input v-model.trim="formData.order" placeholder="请输入顺序"/>
              </FormItem>
            </Row>
            <Row type="flex">
              <FormItem label="CORS策略[CorsPolicy]" prop="corsPolicy" :label-width="160">
                <Input v-model.trim="formData.corsPolicy" placeholder="请输入CORS策略"/>
              </FormItem>
              <FormItem label="授权策略[AuthorizationPolicy]" prop="authorizationPolicy" :label-width="220">
                <Input v-model.trim="formData.authorizationPolicy" placeholder="请输入授权策略"/>
              </FormItem>
            </Row>
          </Form>
        </div>
        <div class="module-title flex col-center">
          <div class="label"></div>
          <span>匹配规则<span class="tips">[MatchConfig]</span></span>
        </div>
        <div class="module-content">
          <Form ref="matchForm" :model="formData.match" class="i-form self" :label-width="160" :rules="matchFormData" onsubmit="return false">
            <Row type="flex">
              <FormItem label="主机[Hosts]" prop="hosts" style="width:100%;">
                <Input v-model.trim="formData.match.hosts" placeholder="请输入主机" style="width:100%;" />
              </FormItem>
            </Row>
            <Row type="flex">
              <FormItem label="方法[Methods]" prop="methods">
                <CheckboxGroup v-model="formData.match.methods">
                  <Checkbox :label="type" v-for="type in HTTPMethods" :key="type"></Checkbox>
                </CheckboxGroup>
              </FormItem>
            </Row>
            <Row type="flex">
              <FormItem label="路径[Path]" prop="path" :label-width="180" style="width:100%;">
                <Input v-model.trim="formData.match.path" placeholder="请输入路径" style="width:100%;" />
              </FormItem>
            </Row>
            <Row type="flex">
              <FormItem label="头部[Headers]" prop="headers">
                <div class="flex flex-col">
                  <div v-for="(item, index) in formData.match.headers" :key="index" class="flex col-center margin-bottom-16 IP-row">
                    <Select v-model="item.mode" placeholder="请选择模式" transfer clearable>
                      <Option v-for="modetype in headerMatchMode" :key="modetype.Id" :value="modetype.Id">{{modetype.Name}}</Option>
                    </Select>
                    <Input v-model.trim="item.name" placeholder="请输入name" />
                    <Input v-model.trim="item.values" placeholder="请输入values" style="width:350px;" />
                    <Checkbox v-model="item.isCaseSensitive">是否区分大小写</Checkbox>
                    <div class="flex col-center row-center icon" @click="handleDeleteKeyValue('match', 'headers', index)" v-if="formData.match.headers && formData.match.headers.length > 1">
                      <img src="~@/assets/images/gateWay/delete.png" alt="">
                    </div>
                    <div class="flex col-center row-center icon" @click="handleAddKeyValue(formData.match.headers, 'headers')" v-if="formData.match.headers.length == index + 1">
                      <img src="~@/assets/images/gateWay/add.png" alt="">
                    </div>
                  </div>
                </div>
              </FormItem>
            </Row>
          </Form>
        </div>
        <div class="module-title flex col-center">
          <div class="label"></div>
          <span>转换配置<span class="tips">[TransformConfig]</span></span>
        </div>
        <div class="module-content">
          <Form ref="transForm" :model="formData" class="i-form self" :label-width="160" :rules="transFormData" onsubmit="return false">
            <Row type="flex">
              <FormItem label="转换[Transforms]" prop="transforms">
                <div class="flex flex-col">
                  <div v-for="(item, index) in formData.transforms" :key="index" class="flex col-center margin-bottom-16 IP-row">
                    <Select v-model="item.type" placeholder="请选择类型" transfer filterable clearable>
                      <Option v-for="typeItem in transformTypeList" :key="typeItem.Id" :value="typeItem.Id">{{typeItem.Name}}</Option>
                    </Select>
                    <Input v-model.trim="item.key" placeholder="请输入key" />
                    <Input v-model.trim="item.value" placeholder="请输入value" style="width:350px;" />
                    <div class="flex col-center row-center icon" @click="handleDeleteKeyValue('transforms', '', index)" v-if="formData.transforms && formData.transforms.length > 1">
                      <img src="~@/assets/images/gateWay/delete.png" alt="">
                    </div>
                    <div class="flex col-center row-center icon" @click="handleAddKeyValue(formData.transforms, 'transforms')" v-if="formData.transforms.length == index + 1">
                      <img src="~@/assets/images/gateWay/add.png" alt="">
                    </div>
                  </div>
                </div>
              </FormItem>
            </Row>
          </Form>
        </div>
      </template>
    </div>
    <div slot="footer">
      <Button type="text" @click="visible=false">取消</Button>
      <Button type="primary" @click="handlerSubmit('form')">提交</Button>
    </div>
  </Modal>
</template>

<script>
import gateway from "../mixins/gateway";
import { createProxyRoute, modifyProxyRoute, getClusterList } from '@/api/modules/common';
export default {
  name: 'routerModal',
  props: {
    value: {
      type: Boolean,
      default: false
    },
    info: {
      type: Object,
      default: () => {
        return {};
      }
    }
  },
  mixins: [gateway],
  data () {
    return {
      allClusterList: [],
      visible: false,
      formData: {
        id: 0,
        routeId: "",
        match: {
          id: 0,
          methods: [], // 方法,提交时转换为英文逗号隔开的字符串
          hosts: "", // 主机名
          path: "",
          headers: [
            {
              // id: 0,
              name: "",
              values: "",
              mode: null, // 来源于headerMatchMode
              isCaseSensitive: false // 是否区分大小写
              // proxyMatchId: 0 // 代理匹配
            }
          ],
          proxyRouteId: 0
        },
        order: 0, // 排序
        clusterId: "", // 集群Id
        authorizationPolicy: "",
        corsPolicy: "",
        metadata: [ // 元数据
          // {
          //   key: "",
          //   value: "",
          //   id: 0
          // }
        ],
        transforms: [ // 转换
          {
            key: "",
            value: "",
            id: 0,
            type: 0 // 来源于transformType的枚举值
          }
        ]
      },
      clearFormData: {
        id: 0,
        routeId: "",
        match: {
          id: 0,
          methods: [],
          hosts: "",
          path: "",
          headers: [
            {
              id: 0,
              name: "",
              values: "",
              mode: null,
              isCaseSensitive: false,
              proxyMatchId: 0
            }
          ],
          proxyRouteId: 0
        },
        order: 0,
        clusterId: "",
        authorizationPolicy: "",
        corsPolicy: "",
        metadata: [
          // {
          //   key: "",
          //   value: "",
          //   id: 0
          // }
        ],
        transforms: [
          {
            key: "",
            value: "",
            id: 0,
            type: 0
          }
        ]
      },
      validateFormData: {
        routeId: [{required: true, message: "名称不可为空"}],
        clusterId: [{required: true, message: "集群不可为空"}],
        destinations: [{required: true, message: "目的地不可为空"}]
      },
      matchFormData: {
        methods: [
          {required: true, type: 'array', min: 1, message: "方法不可为空", trigger: 'change'}
        ],
        // headers: [{required: true, message: "头部设置不可为空"}]
      },
      transFormData: {
        transforms: [{required: true, message: "转换不可为空"}]
      }, 
      tipContent: {
        Header: "指定传出请求的活动关联标头",
        HTTP: "获取或设置一个值，该值指示当所有现有连接的并发流达到最大数量时，是否可以向同一服务器建立额外的HTTP/2连接。"
      }
    };
  },
  computed: {},
  watch: {
    value(val) {
      this.visible = val;
      if (val) {
        this.clearFormValidte();
        this.formData = _.cloneDeep(this.clearFormData);
        this.getDropList();
        if (this.info && this.info.routeId) {
          _.each(this.info || {}, (value, key) => {
            if (value) {
              if (key == "match") {
                if (value && value.methods) {
                  value.methods = value.methods.split(",");
                }
                if (!(value && value.headers && value.headers.length)) {
                  value.headers = [];
                  value.headers.push({
                    // id: 0,
                    name: "",
                    values: "",
                    mode: null,
                    isCaseSensitive: false
                    // proxyMatchId: 0
                  });

                }
              }
              this.formData[key] = value;
            }
          });
        }
        console.log(this.formData);
      }
    },
    visible(val) {
      this.$emit("input", val);
    }
  },
  methods: {
    getDropList() {
      getClusterList().then(data => {
        this.allClusterList = data || [];
      }).catch(() => {
        this.allClusterList = [];
      });
    },
    showPlaceHoleder(num) {
      return `等${num + 1}项`;
    },
    clearFormValidte() {
      this.handleResetForm("form");
      this.handleResetForm("matchForm");
      this.handleResetForm("transForm");
    },
    handleResetForm(name) {
      if (this.$refs[name]) {
        this.$refs[name].resetFields();
      }
    },
    handlerSubmit(name) {
      let formData = _.cloneDeep(this.formData);
      let flag = true;
      // 基本信息验证
      if (this.$refs[name]) {
        this.$refs[name].validate((valid) => {
          if (!valid) {
            flag = false;
          }
        });
      }
      if (this.$refs["matchForm"]) {
        this.$refs["matchForm"].validate((valid) => {
          if (valid) {
            if (formData.match) {
              // 将methods转换为字符串
              if (formData.match.methods) {
                formData.match.methods = formData.match.methods.join(",");
              }
              _.remove(formData.match.headers || [], item => {
                item.mode = Number(item.mode);
                return !(item.name && item.values && (item.mode || item.mode == 0));
              });
              if (!formData.match.headers.length) {
                formData.match.headers = null;
              }
            }
          } else {
            flag = false;
          }
        });
      }
      if (this.$refs["transForm"]) {
        this.$refs["transForm"].validate((valid) => {
          if (valid) {
            // 验证转换必须有一条
            _.remove(formData.transforms || [], item => {
              item.type = Number(item.type);
              return !(item.key && item.value && item.type);
            });
            if (!formData.transforms.length) {
              this.$Message.error("请完善转换配置");
              flag = false;
            }
          } else {
            flag = false;
          }
        });
      }
      formData.order = +formData.order;
      _.remove(formData.metadata || [], item => {
        return !(item.key && item.value);
      });
      if (!(formData.metadata && formData.metadata.length)) {
        formData.metadata = null;
      }
      delete formData._rowKey;
      if (flag) {
        console.log("最终提交的验证通过的数据：", formData);
        let fun = modifyProxyRoute;
        if (!formData._IsEdit) {
          fun = createProxyRoute;
        }
        fun(formData).then(() => {
          this.visible = false;
          this.$emit("success");
        }).catch(err => {
          this.$Message.error(err.message || "保存失败");
        });
      }
    },
    handleAddKeyValue(list, key) {
      if (key == "headers") {
        list.push({
          // id: 0,
          name: "",
          values: "",
          mode: null,
          isCaseSensitive: false
          // proxyMatchId: 0
        });
      } else if (key == "transforms") {
        list.push({
          key: "",
          value: "",
          // id: 0,
          type: 0 // 来源于transformType的枚举值
        });
      }
    },
    handleDeleteKeyValue(firstName, secondName, index) {
      let list = [];
      if (firstName && secondName) {
        list = _.cloneDeep(this.formData[firstName][secondName]) || [];
      } else {
        list = _.cloneDeep(this.formData[firstName]) || [];
      }
      _.remove(list || [], (item, listIndex) => {
        return listIndex === index;
      });
      if (firstName && secondName) {
        this.formData[firstName][secondName] = list;
      } else {
        this.formData[firstName] = list;
      }
    }
  }
}

</script>
<style lang='less'>
.routerModal {
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
    max-height: 600px;
    margin-left: 20px;
    overflow-y: auto;
    .i-form.self {
      .ivu-form-item {
        margin-bottom: 16px;
        margin-right: 0;
        &-content {
          .ivu-input-wrapper, .ivu-select, .ivu-input-number {
            width: 200px;
          }
          .ivu-date-picker {
            width: 200px;
            &-rel {
              width: 100%;
              .ivu-input-wrapper {
                width: 100%;
              }
            }
          }
        }
        &-error-tip {
          font-size: 14px;
          padding-top: 1px;
        }
      }
      .IP-row {
        .ivu-input-wrapper, .ivu-select {
          margin-right: 10px;
        }
      }
      .icon {
        width: 15px;
        height: 14px;
        cursor: pointer;
        transform: scale(1.5);
        &:not(:last-child) {
          margin-right: 20px;
        }
        img {
          max-width: 100%;
          max-height: 100%;
        }
      }
    }
    .module {
      &-title {
        margin-bottom: 10px;
      }
      &-content {
        margin-bottom: 10px;
      }
    }
    .label {
      width: 4px;
      height: 14px;
      background: rgba(82,141,255,1);
      border-radius: 2px;
      margin-right: 8px;
      & + span {
        font-weight: bold;
      }
    }
  }
}

</style>