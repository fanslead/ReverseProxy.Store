<template>
  <div class="login">
    <div class="top-wrap">
      <div class="top login-wrap">
        <Row type="flex" align="middle" justify="center" class="logo">
          <!-- <img src="~@/assets/images/common/logo.png" alt="logo"> -->
          <img src="~@/assets/images/common/brand-logo.png" alt="logo">
        </Row>
      </div>
    </div>
    <div class="center-wrap">
      <div class="bg"></div>
      <Row type="flex" class="center login-wrap" justify="space-between">
        <Row class="left-info">
          <Row type="flex">
            <div class="deep-blue"></div>
            <div class="deep-blue" style="width: 8px;margin-left: 8px;"></div>
          </Row>
          <p class="color-333">欢迎回来。</p>
          <!-- <p class="color-666">如果这里有展示文案的内容，就会在这里展示，没有就忽略。</p> -->
        </Row>
        <Row class="login-box">
          <p class="color-333">登录</p>
          <Form class="login-center" ref="formData" :model="formData" :rules="formDataValidate" :label-width="0" onsubmit="return false">
            <!-- <Form-item prop="Account">
              <Input v-model.trim="formData.Account" placeholder="登录账号" />
            </Form-item> -->
            <Form-item prop="Password">
              <Input type="password" v-model.trim="formData.Password" placeholder="登录密码" />
            </Form-item>
            <Row class="to-forget-pwd" type="flex" justify="space-between">
              <!-- <span class="register" style="visibility: hidden;">账号注册</span>
              <span @click="toFindPwd" class="color-primary-light">忘记密码 ></span> -->
            </Row>
            <button type="submit" @click="handleSubmit('formData')" class="login-btn">登录</button>
          </Form>
        </Row>
      </Row>
      <div class="orange"></div>
      <div class="blue"></div>
    </div>
    <div class="bottom-wrap">
      <div class="bottom login-wrap color-999">©2019&nbsp;深圳市宸熠珠宝科技有限公司 粤ICP备17142766号-3</div>
    </div>
  </div>
</template>
<script>
import { loginGateWay } from "@/api/modules/common"
// import md5 from 'js-md5'
export default {
  name: 'login',
  data() {
    return {
      formData: {
        Account: 'admin',
        Password: ''
      },
      formDataValidate: {
        // Account: {required: true, message: '账号不能为空'},
        Password: {required: true, message: '密码不能为空'}
      }
    }
  },
  methods: {
    handleSubmit(name) {
      this.$refs[name].validate((valid) => {
        if (valid) {
          let params = {
            // Account: this.formData.Account,
            password: this.formData.Password
          }
          loginGateWay(params).then(res => {
            localStorage.clear()
            sessionStorage.clear()
            let data = res && res.data;
            if (data) {
              // localStorage.setItem('IsHeadStore', data.IsHeadStore)
              // localStorage.setItem('token', data.TokenStr)
              // localStorage.setItem('Account', data.Account)
              // sessionStorage.setItem("includeList", [])
              // this.$mutations.setIsHeadStore(data.IsHeadStore)
              // this.$mutations.setAccount(data.Account)
              if (params.password) {
                localStorage.setItem('AccountP', params.password);
              }
              this.$router.push({name: 'upstreamList'});
            } else {
              this.$Message.error('登录失败!');
            }
          }).catch(err => {
            this.$Message.error(err.message || '登录失败!')
          })
        }
      })
    }
  },
  created() {
    localStorage.clear()
    sessionStorage.clear()
    this.$mutations.setMenuList([])
    this.$mutations.setRights({})
    sessionStorage.setItem("includeList", [])
  }
}
</script>
<style lang="less">
.login{
  font-size: 14px;
  padding-top: 40px;
  .login-wrap{
    width: 1200px;
    margin: 0 auto;
  }
  .logo{
    width: 256px;
    height: 80px;
    // background: rgba(216, 216, 216, .2);
    display: flex;
    align-items: center;
    justify-content: center;
    img {
      max-width: 100%;
      max-height: 100%;
    }
  }
  .center-wrap{
    height: 480px;
    position: relative;
    margin-top: 80px;
  }
  .blue{
    width: 120px;
    height: 120px;
    position: absolute;
    left: 0;
    top: 0;
    background: linear-gradient(320deg, rgba(18, 55, 135, .7) 0%,rgba(50, 148, 219, .7) 100%);
  }
  .orange{
    width: 87px;
    height: 134px;
    position: absolute;
    left: 72px;
    top: 40px;
    background: linear-gradient(141deg,rgba(250, 217, 97, .7) 0%,rgba(247, 107, 28, .7) 100%);
  }
  .bg{
    width: 100%;
    height: 400px;
    position: absolute;
    left: 0;
    top: 40px;
    background: url('~@/assets/images/common/login-bg.png');
    background-size: cover;
  }
  .bottom-wrap{
    margin-top: 150px;
    margin-bottom: 20px;
    .bottom{
      text-align: center;
      font-size: 12px;
    }
  }
  .left-info{
    margin: 130px 0 0 140px;
    P{
      width: 400px;
      &.color-333{
        font-size: 24px;
        line-height: 24px;
        margin: 24px 0 10px 0;
      }
    }
  }
  .deep-blue{
    width: 48px;
    height: 8px;
    background: #202CBC;
  }
  .login-box{
    width: 432px;
    height: 480px;
    background: #fff;
    box-shadow: 0px 0px 30px 0px rgba(0, 0, 0, 0.05);
    border-radius: 10px;
    p{
      text-align: center;
      margin: 32px 0 80px;
      font-size: 24px;
      line-height: 32px;
    }
  }
  .login-center{
    padding: 0 30px
  }
  .ivu-form-item{
    width: 100%;
    margin-bottom: 24px;
  }
  .ivu-input{
    border-radius: 0;
    border: none;
    border: 1px solid #E5E5E5;
    line-height: 20px;
    padding: 8px 0;
    color: #999;
    font-size: 16px;
    border-color: transparent transparent #e5e5e5 transparent !important;
  }
  .ivu-input:focus{
    border-color: transparent;
    outline: none;
    box-shadow: none;
    border-bottom: 1px solid #E5E5E5;
  }
  .ivu-form-item-error .ivu-input {
    border-bottom: 1px solid #ed3f14;
  }
  .ivu-form-item-error-tip{
    font-size: 14px;
  }
  .login-btn{
    width: 372px;
    height: 40px;
    background:rgb(32, 44, 188);
    border-radius: 4px;
    margin-top: 80px;
    cursor: pointer;
    border: none;
    outline: none;
    font-size: 16px;
    color: #fff;
  }
  .color-primary-light{
    cursor: pointer;
  }
}
</style>
