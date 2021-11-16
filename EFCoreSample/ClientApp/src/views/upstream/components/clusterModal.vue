<template>
  <Modal v-model="visible" :title="`${info._IsEdit ? '编辑' : '新建'}集群`" width="1000" :fullscreen="false" class="clusterModal">
    <div class="clusterModal-content">
      <!-- 基本信息 -->
      <template>
        <div class="module-title flex col-center">
          <div class="label"></div>
          <span>基本信息<span class="tips">[BasicInformation]</span></span>
        </div>
        <div class="module-content">
          <Form ref="form" :model="formData" class="i-form self" :label-width="180" :rules="validateFormData" onsubmit="return false">
            <Row type="flex">
              <FormItem label="名称[Id]" prop="id">
                <Input v-model.trim="formData.id" :disabled="info._IsEdit ? true: false" placeholder="名称须全局唯一"/>
              </FormItem>
              <FormItem label="负载均衡策略[LoadBalancingPolicy]" prop="loadBalancingPolicy" :label-width="240">
                <Select v-model="formData.loadBalancingPolicy" placeholder="请选择负载均衡策略" @click="handleSelectloadBP" transfer clearable>
                  <Option v-for="(type, index) in loadBPList" :key="index" :value="type">{{type}}</Option>
                </Select>
              </FormItem>
            </Row>
            <!-- 目的地 -->
            <template>
              <FormItem label="目的地[Destinations]" prop="destinations" :label-width="180">
                <div class="flex flex-col">
                  <div v-for="(item, index) in formData.destinations" :key="index" class="flex col-center margin-bottom-16 IP-row">
                    <Input v-model.trim="item.name" placeholder="请输入名称"/>
                    <Input v-model.trim="item.address" placeholder="请输入域名/IP"/>
                    <Input v-model.trim="item.health" placeholder="请输入Health地址"/>
                    <div class="flex col-center row-center icon" @click="handleDeleteList('destinations', index)" v-if="formData.destinations && formData.destinations.length > 1">
                      <img src="~@/assets/images/gateWay/delete.png" alt="">
                    </div>
                    <div class="flex col-center row-center icon" @click="handleAddList('destinations')" v-if="formData.destinations.length == index + 1">
                      <img src="~@/assets/images/gateWay/add.png" alt="">
                    </div>
                  </div>
                </div>
              </FormItem>
            </template>
          </Form>
        </div>
      </template>
      <!-- HTTP请求配置 -->
      <div class="module-title flex col-center">
        <div class="label"></div>
        <span>HTTP请求配置<span class="tips">[HttpRequest]</span></span>
        <i-switch v-model="formData.httpRequest.enabled" style="margin-left: 10px;" />
      </div>
      <div class="module-content flex" v-if="formData.httpRequest.enabled">
        <Form ref="httpRequestForm" :model="formData.httpRequest" class="i-form self" :label-width="180" :rules="httpValidate" onsubmit="return false">
          <Row type="flex">
            <FormItem label="超时[Timeout]" :label-width="180" prop="timeout">
              <TimePicker type="time" placement="top" v-model.trim="formData.httpRequest.activityTimeout" placeholder="请选择" :editable="false"></TimePicker>
            </FormItem>
            <FormItem label="版本[Version]" :label-width="170" prop="version">
              <Input v-model.trim="formData.httpRequest.version" placeholder="请输入" />
            </FormItem>
            <FormItem label="外发请求[VersionPolicy]" :label-width="180">
              <Select v-model="formData.httpRequest.versionPolicy" placeholder="请选择" transfer style="width:200px;" clearable>
                <Option v-for="(type, index) in versionPolicyType" :key="index" :value="type">{{type}}</Option>
              </Select>
            </FormItem>
            <FormItem label="" :label-width="55">
              <Checkbox v-model="formData.httpRequest.allowResponseBuffering">客户端是否AllowResponseBuffering</Checkbox>
            </FormItem>
          </Row>
        </Form>
      </div>
      <!-- HTTP客户端 -->
      <div class="module-title flex col-center">
        <div class="label"></div>
        <span>HTTP客户端配置<span class="tips">[HttpClient]</span></span>
        <i-switch v-model="formData.httpClient.enabled" style="margin-left: 10px;" />
      </div>
      <div class="module-content" v-if="formData.httpClient.enabled">
        <Form ref="httpClientForm" :model="formData.httpClient" class="i-form self" :label-width="180" :rules="httpValidate" onsubmit="return false">
          <Row type="flex">
            <FormItem label="协议[SslProtocols]" :label-width="150" prop="sslProtocols">
              <Select v-model="formData.httpClient.sslProtocols" placeholder="请选择协议" transfer :max-tag-count="1" :max-tag-placeholder="showPlaceHoleder" multiple clearable>
                <Option v-for="(type, index) in sslProtocols" :key="index" :value="type">{{type}}</Option>
              </Select>
            </FormItem>
          </Row>
          <Row type="flex">
            <FormItem label="连接最大数量" :label-width="150" prop="maxConnectionsPerServer">
              <InputNumber v-model.trim="formData.httpClient.maxConnectionsPerServer" placeholder="请输入" :max="maxNumber" :min="1"></InputNumber>
            </FormItem>
            <FormItem label="" :label-width="55">
              <Checkbox v-model="formData.httpClient.dangerousAcceptAnyServerCertificate">客户端是否检查服务器端SSL证书的有效性</Checkbox>
            </FormItem>
          </Row>
          <Row type="flex">
            <FormItem label="RequestHeaderEncoding" prop="requestHeaderEncoding" key="requestHeaderEncoding">
              <Input v-model.trim="formData.httpClient.requestHeaderEncoding" placeholder="请输入RequestHeaderEncoding" />
            </FormItem>
            <FormItem label="" :label-width="55">
              <Checkbox v-model="formData.httpClient.enableMultipleHttp2Connections">是否建立HTTP/2连接</Checkbox>
              <Poptip width="200" :word-wrap="true" :content="tipContent.HTTP">
                <Icon type="ios-information-circle" color="#202cbc" size="18" />
              </Poptip>
            </FormItem>
          </Row>
          <Row type="flex" v-if="formData.httpClient.webProxy">
            <div class="module-title flex col-center" style="margin-left:40px;">
              <div class="label"></div>
              <span>WebProxy<span class="tips">[WebProxy]</span></span>
              <i-switch v-model="formData.httpClient.webProxy.enabled" style="margin-left: 10px;" />
            </div>
            <div class="module-content" v-if="formData.httpClient.webProxy.enabled">
              <Row type="flex" class="flex-col">
                <Row type="flex">
                  <FormItem label="Address[Address]" :label-width="200">
                    <Input v-model.trim="formData.httpClient.webProxy.address" placeholder="请输入" type="text" clearable />
                  </FormItem>
                  <FormItem prop="bypassOnLocal" key="bypassOnLocal" >
                    <Checkbox v-model="formData.httpClient.webProxy.bypassOnLocal">是否BypassOnLocal</Checkbox>
                  </FormItem>
                  <FormItem prop="useDefaultCredentials" key="useDefaultCredentials" >
                    <Checkbox v-model="formData.httpClient.webProxy.UseDefaultCredentials">是否UseDefaultCredentials</Checkbox>
                  </FormItem>
                </Row>
              </Row>
            </div>
          </Row>
        </Form>
      </div>
      <!-- 会话亲和性 -->
      <template>
        <div class="module-title flex col-center">
          <div class="label"></div>
          <span>会话配置<span class="tips">[SessionAffinity]</span></span>
          <i-switch v-model="formData.sessionAffinity.enabled" style="margin-left: 10px;" />
        </div>
        <div class="module-content" v-if="formData.sessionAffinity.enabled">
          <Form ref="sessionForm" :model="formData.sessionAffinity" class="i-form self" :label-width="180" :rules="sessionValidate" onsubmit="return false">
            <Row type="flex">
              <FormItem label="Policy" prop="policy" key="policy">
                <Input v-model.trim="formData.sessionAffinity.policy" placeholder="请输入policy" />
              </FormItem>
              <FormItem label="AffinityKeyName" prop="affinityKeyName" key="affinityKeyName">
                <Input v-model.trim="formData.sessionAffinity.affinityKeyName" placeholder="请输入AffinityKeyName" />
              </FormItem>
              <FormItem label="失败策略[FailurePolicy]" :label-width="180">
                <!-- <Input v-model.trim="formData.sessionAffinity.failurePolicy" placeholder="请输入失败策略" /> -->
                <Select v-model="formData.sessionAffinity.failurePolicy" placeholder="请选择失败策略" transfer clearable>
                  <Option v-for="(type, index) in failurePolicyList" :key="index" :value="type">{{type}}</Option>
                </Select>
              </FormItem>
            </Row>
            <span>Cookie配置<span class="module-content">[Cookie]</span></span>
            <i-switch v-model="formData.sessionAffinity.cookie.enabled" style="margin-left: 10px;" />
            <div class="module-content" v-if="formData.sessionAffinity.cookie.enabled">
              <Row type="flex">
                <FormItem label="Path[Path]" prop="path" key="path">
                  <Input v-model="formData.sessionAffinity.cookie.path" placeholder="请输入Path" />
                </FormItem>
                <FormItem label="Domain[Domain]" prop="domain" key="domain">
                  <Input v-model="formData.sessionAffinity.cookie.domain" placeholder="请输入Domain" />
                </FormItem>
                <FormItem label="Expiration[Expiration]" prop="expiration" key="expiration">
                  <Input v-model="formData.sessionAffinity.cookie.expiration" placeholder="请输入Expiration" />
                </FormItem>
                <FormItem prop="httpOnly" key="httpOnly">
                  <Checkbox v-model="formData.sessionAffinity.cookie.httpOnly">是否httpOnly</Checkbox>
                </FormItem>
                <FormItem label="SecurePolicy[SecurePolicy]" :label-width="180">
                  <Select v-model="formData.sessionAffinity.cookie.securePolicy" placeholder="请选择SecurePolicy" transfer clearable>
                    <Option v-for="(type, index) in securePolicyList" :key="index" :value="type.value">{{type.name}}</Option>
                  </Select>
                </FormItem>
                <FormItem label="SameSiteMode[SameSiteMode]" :label-width="180">
                  <Select v-model="formData.sessionAffinity.cookie.sameSite" placeholder="请选择SameSiteMode" transfer clearable>
                    <Option v-for="(type, index) in sameSiteModeList" :key="index" :value="type.value">{{type.name}}</Option>
                  </Select>
                </FormItem>
                <FormItem label="MaxAge[MaxAge]" prop="maxAge" key="maxAge">
                  <Input v-model="formData.sessionAffinity.cookie.maxAge" placeholder="请输入MaxAge" />
                </FormItem>
                <FormItem prop="isEssential" key="isEssential">
                  <Checkbox v-model="formData.sessionAffinity.cookie.isEssential">是否Essential</Checkbox>
                </FormItem>
              </Row>
            </div>
          </Form>
        </div>
      </template>
      <!-- 健康检查 -->
      <template>
        <div class="module-title flex col-center">
          <div class="label"></div>
          <span>健康检查<span class="tips">[HealthCheck]</span></span>
        </div>
        <div class="module-content">
          <div class="module-title flex col-center">
            <div class="file-label">主动健康检查</div>
            <i-switch v-model="formData.healthCheck.active.enabled" />
          </div>
          <div class="module-content flex col-center" v-if="formData.healthCheck.active.enabled">
            <Form ref="healthActiveForm" :model="formData.healthCheck.active" class="i-form self" :label-width="180" :rules="healthValidate" inline onsubmit="return false">
              <FormItem label="间隔[Interval]" prop="interval">
                <TimePicker type="time" placement="top" v-model.trim="formData.healthCheck.active.interval" placeholder="请选择时间" :editable="false"></TimePicker>
              </FormItem>
              <FormItem label="超时[Timeout]" :label-width="180" prop="timeout">
                <TimePicker type="time" placement="top" v-model.trim="formData.healthCheck.active.timeout" placeholder="请选择时间" :editable="false"></TimePicker>
              </FormItem>
              <FormItem label="策略[Policy]" :label-width="180">
                <Input v-model.trim="formData.healthCheck.active.policy" placeholder="请输入策略" />
              </FormItem>
              <FormItem label="路径[Path]" prop="path">
                <Input v-model.trim="formData.healthCheck.active.path" placeholder="请输入路径" />
              </FormItem>
            </Form>
          </div>
          <div class="module-title flex col-center">
            <div class="file-label">被动健康检查</div>
            <i-switch v-model="formData.healthCheck.passive.enabled" />
          </div>
          <div class="module-content flex col-center" v-if="formData.healthCheck.passive.enabled">
            <Form ref="healthPassiveForm" :model="formData.healthCheck.passive" class="i-form self" :label-width="100" :rules="healthPassiveValidate" inline onsubmit="return false">
              <FormItem label="重启激活周期[ReactivationPeriod]" :label-width="250" prop="reactivationPeriod">
                <TimePicker type="time" placement="top" v-model.trim="formData.healthCheck.passive.reactivationPeriod" placeholder="请选择时间" :editable="false"></TimePicker>
              </FormItem>
              <FormItem label="策略[Policy]" prop="policy" :label-width="110">
                <Input v-model.trim="formData.healthCheck.passive.policy" placeholder="请输入策略" />
              </FormItem>
            </Form>
          </div>
        </div>
      </template>
    </div>
    <div slot="footer">
      <Button type="text" @click="visible=false">取消</Button>
      <Button type="primary" @click="handlerSubmit('form')">保存</Button>
    </div>
  </Modal>
</template>

<script>
import gateway from "../mixins/gateway";
import { createCluster, modifyCluster } from '@/api/modules/common';
export default {
  name: 'clusterModal',
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
      visible: false,
      maxNumber: 2147483647,
      formData: {
        id: "",
        loadBalancingPolicy: "RoundRobin", // 默认RoundRobin，可chash
        sessionAffinity: { // 会话亲和性
          id: 0,
          enabled: false,
          policy: "Cookie",
          failurePolicy: "Redistribute", // Redistribute、Return503
          affinityKeyName: "",
          cookie: {
            id: 0,
            enabled: false,
            path: "",
            domain: "",
            httpOnly: true,
            securePolicy: 0,
            sameSite: 0,
            expiration: "",
            maxAge: "",
            isEssential: true
          },
          clusterId: null
        },
        healthCheck: { // 健康检查
          id: 0,
          passive: { // 被动
            id: 0,
            enabled: false,
            policy: "TransportFailureRate",
            reactivationPeriod: "00:02:00", // 重启激活周期00:02:00
            healthCheckOptionsId: 0
          },
          active: { // 主动
            id: 0,
            enabled: false,
            interval: "00:00:15", // 发送运行状况探测请求的周期。默认“00:00:15”
            timeout: "00:00:10", // 调查请求超时。默认“00:00:10”
            policy: "ConsecutiveFailures", // 评估目的地活动运行状况状态的策略的名称。必填
            path: "", // 所有集群目标上的运行状况检查路径。默认"null",/api/health
            healthCheckOptionsId: 0
          },
          clusterId: null
        },
        httpClient: {
          id: 0,
          enabled: false,
          sslProtocols: [], // 协议名称(多选)，最终以英文逗号,隔开的字符串
          dangerousAcceptAnyServerCertificate: false, // 客户端是否检查服务器端SSL证书的有效性。将其设置为' true '完全禁用验证。默认值为“false”。
          maxConnectionsPerServer: 0,
          webProxy: {
            id: 0,
            enabled: false,
            address: "",
            bypassOnLocal: true,
            useDefaultCredentials: true,
            httpClientConfigId: 0,
            httpClientConfig: ""
          }, 
          requestHeaderEncoding: "", // 指定传出请求的活动关联标头
          enableMultipleHttp2Connections: false,
          clusterId: null
        },
        httpRequest: { // HTTP请求配置
          id: 0,
          enabled: false,
          activityTimeout: "00:00:30", // 格式(00:00:30)如此（页面上输入毫秒，转为时分秒格式）
          version: "", // 外发请求[Version]。目前支持的值是' 1.0 '、' 1.1 '和' 2 '。默认值为2。
          versionPolicy: "RequestVersionOrLower", // 定义如何为外发请求选择最终版本。["RequestVersionOrLower", "RequestVersionOrHigher", "RequestVersionExact"]
          allowResponseBuffering: false,
          clusterId: null
        },
        destinations: [ // 目的地
          {
            id: 0,
            name: "",
            address: "",
            health: "",
            metadata: [ // 元数据
              // {
              //   key: "",
              //   value: "",
              //   id: 0
              // }
            ]
          }
        ],
        metadata: [
          // {
          //   key: "",
          //   value: "",
          //   id: 0
          // }
        ],
        proxyRoutes: null
      },
      clearFormData: {
        id: "",
        loadBalancingPolicy: "RoundRobin", // 默认RoundRobin，可chash
        sessionAffinity: { // 会话亲和性
          id: 0,
          enabled: false,
          policy: "Cookie",
          failurePolicy: "Redistribute", // Redistribute、Return503
          affinityKeyName: "",
          cookie: {
            id: 0,
            enabled: false,
            path: "",
            domain: "",
            httpOnly: true,
            securePolicy: 0,
            sameSite: 0,
            expiration: "",
            maxAge: "",
            isEssential: true
          },
          clusterId: null
        },
        healthCheck: { // 健康检查
          id: 0,
          passive: { // 被动
            id: 0,
            enabled: false,
            policy: "TransportFailureRate",
            reactivationPeriod: "00:02:00", // 重启激活周期00:02:00
            healthCheckOptionsId: 0
          },
          active: { // 主动
            id: 0,
            enabled: false,
            interval: "00:00:15", // 发送运行状况探测请求的周期。默认“00:00:15”
            timeout: "00:00:10", // 调查请求超时。默认“00:00:10”
            policy: "ConsecutiveFailures", // 评估目的地活动运行状况状态的策略的名称。必填
            path: "", // 所有集群目标上的运行状况检查路径。默认"null",/api/health
            healthCheckOptionsId: 0
          },
          clusterId: null
        },
        httpClient: {
          id: 0,
          enabled: false,
          sslProtocols: [], // 协议名称(多选)，最终以英文逗号,隔开的字符串
          dangerousAcceptAnyServerCertificate: false, // 客户端是否检查服务器端SSL证书的有效性。将其设置为' true '完全禁用验证。默认值为“false”。
          maxConnectionsPerServer: 0,
          webProxy: {
            id: 0,
            enabled: false,
            address: "",
            bypassOnLocal: true,
            useDefaultCredentials: true,
            httpClientConfigId: 0,
            httpClientConfig: ""
          }, 
          requestHeaderEncoding: "", // 指定传出请求的活动关联标头
          enableMultipleHttp2Connections: false,
          clusterId: null
        },
        httpRequest: { // HTTP请求配置
          id: 0,
          enabled: false,
          activityTimeout: "00:00:30", // 格式(00:00:30)如此（页面上输入毫秒，转为时分秒格式）
          version: "", // 外发请求[Version]。目前支持的值是' 1.0 '、' 1.1 '和' 2 '。默认值为2。
          versionPolicy: "RequestVersionOrLower", // 定义如何为外发请求选择最终版本。["RequestVersionOrLower", "RequestVersionOrHigher", "RequestVersionExact"]
          allowResponseBuffering: false,
          clusterId: null
        },
        destinations: [ // 目的地
          {
            id: 0,
            name: "",
            address: "",
            health: "",
            metadata: [ // 元数据
              // {
              //   key: "",
              //   value: "",
              //   id: 0
              // }
            ]
          }
        ],
        metadata: [
          // {
          //   key: "",
          //   value: "",
          //   id: 0
          // }
        ],
        proxyRoutes: []
      },
      validateFormData: {
        id: [{required: true, message: "名称不可为空"}],
        // loadBalancingPolicy: [{required: true, message: "负载均衡策略不可为空"}],
        destinations: [{required: true, message: "目的地不可为空"}]
      },
      sessionValidate: {
        mode: [{required: true, message: "模式不可为空"}]
      },
      httpValidate: {
        // timeout: [{required: true, message: "超时不可为空"}],
        // version: [{required: true, message: "版本不可为空"}],
        sslProtocols: [{required: true, message: "协议不可为空"}],
        maxConnectionsPerServer: [{required: true, message: "连接最大数量不可为空"}]
      },
      healthValidate: {
        // interval: [{required: true, message: "间隔不可为空"}],
        // timeout: [{required: true, message: "超时不可为空"}],
        policy: [{required: true, message: "策略不可为空"}],
        // reactivationPeriod: [{required: true, message: "重启激活周期不可为空"}]
      },
      healthPassiveValidate: {
        policy: [{required: true, message: "策略不可为空"}]
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
        if (this.info && this.info.id) {
          _.each(this.info || {}, (value, key) => {
            if (value) {
              // 如果是对象值
              if (key == "httpClient") {
                if (value && value.sslProtocols) {
                  value.sslProtocols = value.sslProtocols.split(",");
                }
              }
              this.formData[key] = value;
            }
          });
        }
      }
    },
    visible(val) {
      this.$emit("input", val);
    }
  },
  methods: {
    showPlaceHoleder(num) {
      return `等${num + 1}项`;
    },
    clearFormValidte() {
      this.handleResetForm("form");
      this.handleResetForm("httpRequestForm");
      this.handleResetForm("httpClientForm");
      this.handleResetForm("sessionForm");
      this.handleResetForm("healthActiveForm");
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
          if (valid) {
            // 验证目的地必须有一条
            _.remove(formData.destinations || [], item => {
              if (!(item.metadata && item.metadata.length)) {
                item.metadata = null;
              }
              item.health = item.health || null;
              return !(item.name && item.address);
            });
            if (!formData.destinations.length) {
              this.$Message.error("请配置目的地");
              flag = false;
            }
          } else {
            flag = false;
          }
        });
      }
      // http请求验证
      if (formData.httpRequest.enabled) {
        if (this.$refs["httpRequestForm"]) {
          this.$refs["httpRequestForm"].validate((valid) => {
            if (!valid) {
              flag = false;
            }
          });
        }
      } else {
        formData.httpRequest = null;
      }
      if (formData.httpClient.enabled) {
        if (this.$refs["httpClientForm"]) {
          this.$refs["httpClientForm"].validate((valid) => {
            if (valid) {
              // 强制转换协议数组的格式为字符串
              if (formData.httpClient.sslProtocols) {
                formData.httpClient.sslProtocols = formData.httpClient.sslProtocols.join(",") || "";
              } else {
                formData.httpClient.sslProtocols = "";
              }
              if (formData.httpClient.maxConnectionsPerServer <= 0) {
                this.$Message.error("HTTP客户端配置的连接最大数量须大于0");
                flag = false;
              }
            } else {
              flag = false;
            }
          });
        }
      } else {
        formData.httpClient = null;
      }
      // 会话配置
      if (formData.sessionAffinity && formData.sessionAffinity.enabled) {
        if (this.$refs["sessionForm"]) {
          this.$refs["sessionForm"].validate((valid) => {
            if (valid) {
              _.remove(formData.sessionAffinity.settings || [], item => {
                item.id = Number(item.id);
                return !(item.key && item.value);
              });
              if (!formData.sessionAffinity.settings.length) {
                this.$Message.error("请配置会话设置");
                flag = false;
              }
            } else {
              flag = false;
            }
          });
        }
      } else {
        formData.sessionAffinity = null;
      }
      // 主动健康检查
      if (formData.healthCheck && formData.healthCheck.active && formData.healthCheck.active.enabled) {
        if (this.$refs["healthActiveForm"]) {
          this.$refs["healthActiveForm"].validate((valid) => {
            if (!valid) {
              flag = false;
            }
          });
        }
      } else {
        formData.healthCheck.active = null;
      }
      // 被动健康检查
      if (formData.healthCheck && formData.healthCheck.passive && formData.healthCheck.passive.enabled) {
        if (this.$refs["healthPassiveForm"]) {
          this.$refs["healthPassiveForm"].validate((valid) => {
            if (!valid) {
              flag = false;
            }
          });
        }
      } else {
        formData.healthCheck.passive = null;
      }
      if (!formData.healthCheck.passive && !formData.healthCheck.active) {
        formData.healthCheck = null;
      }
      if (!(formData.destinations && formData.destinations.length)) {
        formData.destinations = null;
      }
      if (!(formData.metadata && formData.metadata.length)) {
        formData.metadata = null;
      }
      if (!(formData.proxyRoutes && formData.proxyRoutes.length)) {
        formData.proxyRoutes = null;
      }
      delete formData._rowKey;
      delete formData._index;
      if (flag) {
        console.log("最终提交的验证通过的数据：", formData);
        let fun = modifyCluster;
        if (!formData._IsEdit) {
          fun = createCluster;
        }
        fun(formData).then(() => {
          this.visible = false;
          this.$Message.success("保存成功");
          this.$emit("success");
        }).catch(err => {
          this.$Message.error(err.message || "保存失败");
        });
      }
    },
    handleSelectloadBP() {
      if (this.formData.loadBalancingPolicy === 'RoundRobin') {
        this.formData.destinations.push({
          id: 0,
          name: "",
          address: "",
          health: "",
          metadata: []
        });
      } else if (formData.loadBalancingPolicy === 'chash') {
        
      }
    },
    handleAddKeyValue(list) {
      list.push({
        key: "",
        value: "",
        id: 0
      });
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
    },
    handleAddList(key) {
      if (key === "destinations") {
        this.formData.destinations.push({
          id: 0,
          name: "",
          address: "",
          health: "",
          metadata: []
        });
      }
    },
    handleDeleteList(key, index) {
      let list = _.cloneDeep(this.formData[key]);
      _.remove(list || [], (item, listIndex) => {
        return listIndex === index;
      });
      this.formData[key] = list;
    }
  }
}

</script>
<style lang='less'>
.clusterModal {
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
    .child-title {
      color: #6e6e6e;
      font-style: italic;
      margin: 0 0 10px 50px;
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