<template>
  <Row type="flex" id="layout" v-show="show">
    <my-menu :activeMenuId="activeMenuId" v-show="!isHome" @get-active-id="getActiveId" v-model="isExpand"></my-menu>
    <div id="right-content" v-show="show">
      <Row id="header" type="flex" justify="space-between">
        <Row type="flex" align="middle" class="menu-name" :class="{'visable-hidden': isHome}">
          <span v-for="item in topMenu" :key="item.MenuId" :class="{active: activeMenuId == item.MenuId}" @click="changeMenu(item)">{{item.MenuName}}</span>
        </Row>
        <div class="user-info-wrap" @mouseover="showTop" @mouseout="hideTop">
          <Row class="user-info" type="flex" align="middle" v-if="false">
            <img :src="userImg">
            <span class="name">{{showName}}</span>
            <div class="user-name-arrow"></div>
          </Row>
          <div class="topbar-wrap" v-show="false && showTopBar">
            <span @click="showPwdModal=true">修改密码</span>
            <span @click="afterLogout">切换账号</span>
          </div>
        </div>
      </Row>
      <tab-nav @set_include="setInclude" v-show="!isHome" v-model="isExpand"></tab-nav>
      <div id="content" :class="{'is-home': isHome}">
        <!-- <scrollView height="100%" :baseWrap="true"> -->
          <keep-alive :include="includeList">
            <router-view id="main" @set-select="selectMenuFormHome"></router-view>
          </keep-alive>
        <!-- </scrollView> -->
      </div>
      <div id="copy-right" v-show="!isHome">©2019&nbsp;深圳市宸熠珠宝科技有限公司 粤ICP备17142766号-3</div>
    </div>
  </Row>
</template>
<script>
import myMenu from './components/menu'
import tabNav from './components/tabbar'
export default {
  name: 'layout',
  components: {
    myMenu,
    tabNav
  },
  data() {
    return {
      includeList: [],
      activeMenuId: '',
      userImg: require('@/assets/images/common/userImg.png'),
      show: false,
      showPwdModal: false,
      showTopBar: false,
      isExpand: true,
      defaultLogo: require('@/assets/images/common/default-logo.png')
    }
  },
  computed: {
    topMenu() {
      return _.map(this.menuList, menu => {
        return {
          MenuId: menu.MenuId,
          MenuName: menu.MenuName
        }
      })
    },
    showName() {
      return this.$stores.EmpName || this.$stores.StoreName || this.$stores.MerchantName || this.$stores.Account
    },
    isHome() {
      return this.routeName == 'home'
    }
  },
  watch: {
    $route: "changeMenuByRoute"
  },
  methods: {
    showTop() {
      this.showTopBar = true;
    },
    hideTop() {
      this.showTopBar = false;
    },
    logout() {},
    afterLogout() {
      localStorage.clear()
      sessionStorage.clear()
      this.$mutations.setMenuList([])
      this.$mutations.setRights({})
      this.$router.push({name: 'login'})
      sessionStorage.setItem("includeList", [])
    },
    getActiveId(menuId) {
      _.some(this.menuList, menu => {
        return _.some(menu.ChildList, chl => {
          if (chl.MenuId == menuId) {
            this.activeMenuId = menu.MenuId
            return true
          }
        })
      })
    },
    collapsedChange() {},
    changeMenu(menu) {
      if (this.activeMenuId != menu.MenuId) {
        this.activeMenuId = menu.MenuId
      }
    },
    setInclude(params) {
      this.includeList = _.map(params.list, item => item.name)
      sessionStorage.setItem("includeList", JSON.stringify(params.list))
      this.$nextTick(() => {
        params.cb && params.cb()
      })
    },
    changeMenuByRoute(to, from) {
      this.getMenuListAndRight()
    },
    selectMenuFormHome() {
      // home页面回调函数
      if (!this.menuList || !this.menuList.length) {
         this.menuList = this.$stores.menuList
      }
      let currentRouteName = this.$route.name || "";
      if (this.menuList && this.menuList.length) {
        let firstMenu = "";
        _.some(this.menuList, menu => {
          return _.some(menu.ChildList || [], chl => {
            this.activeMenuId = menu.MenuId
            if (chl.isNotLast) {
              return _.some(chl.ChildList || [], third => {
                if (third.RouteName) {
                  firstMenu = third.RouteName
                  return true
                }
              })
            } else if (chl.RouteName) {
              firstMenu = chl.RouteName
              return true
            }
          })
        })
        if (firstMenu) {
          setTimeout(() => {
            this.show = true
          }, 300);
          this.$router.push({
            name: currentRouteName || firstMenu
          })
        }
      } else {
        this.show = true
        // this.$router.push({
        //   name: 'login'
        // })
      }
    },
    getMenuListAndRight() {
      setTimeout(() => {
        this.getRights().then(() => {
          if (!this.menuList || !this.menuList.length) {
            if (this.$stores.menuList && this.$stores.menuList.length) {
              this.menuList = this.$stores.menuList
            } else {
              this.$nextTick(() => {
                this.getMenuListAndRight()
              })
            }
          } else {
            _.some(this.menuList, menu => {
              return _.some(menu.ChildList || [], chl => {
                if (chl.isNotLast && chl.ChildList) {
                  return _.some(chl.ChildList, third => {
                    if (this.routeName == third.RouteName) {
                      this.activeMenuId = menu.MenuId
                      return true
                    }
                  })
                } else {
                  if (this.routeName == chl.RouteName) {
                    this.activeMenuId = menu.MenuId
                    return true
                  }
                }
              })
            })
          }
        })
      }, 50)
    }
  },
  created() {
    this.getRights().then(res => {
      this.selectMenuFormHome()
    });
    /* 不使用 */
    if (this.routeName == 'home') {
      let { TokenStr, IsHeadStore, Account } = this.$route.query
      if (IsHeadStore) {
        localStorage.setItem('IsHeadStore', IsHeadStore)
        this.$mutations.setIsHeader(IsHeadStore)
      }
      if (Account) {
        localStorage.setItem('Account', Account)
        this.$mutations.setAccount(Account)
      }
      if (TokenStr) {
        localStorage.setItem('token', TokenStr)
        sessionStorage.clear()
        this.$nextTick(() => {
          this.getRights().then(res => {
            this.selectMenuFormHome()
          }).catch(() => {
            this.show = true
          })
        })
        // this.$router.replace({
        //   name: 'home'
        // })
      } else {
        this.show = true
      }
    } else {
      this.show = true
    }
    /* end */
    if (sessionStorage.includeList && sessionStorage.getItem('includeList') != 'undefined') {
      let params = {
        list: JSON.parse(sessionStorage.getItem("includeList"))
      };
      this.setInclude(params)
    }
  },
  mounted() {
    this.getMenuListAndRight()
    $(document).on('focus', '.ivu-table-wrapper input[type="text"]', function(event) {
      event.target.select()
    })
  }
}
</script>
<style lang="less">
#layout{
  flex-wrap: nowrap;
}
#right-content{
  flex: 1;
  background: #F5F5F5;
  overflow: hidden;
}
#header{
  height: 64px;
  // background: #222549;
  background: #0A0C24;
  .menu-name{
    min-width: 1px;
    height: 100%;
    font-size: 16px;
    span{
      position: relative;
      // margin-left: 42px;
      // color: rgba(255, 255, 255, .5);
      color: rgba(255, 255, 255, 1);
      cursor: pointer;
      padding: 0 16px;
      height: 100%;
      display: flex;
      align-items: center;
      &.active{
      // color: #fff;
      font-weight: 700;
      color: #0A0C24;
      cursor: default;
      background:linear-gradient(180deg,rgba(255,255,255,1) 0%,rgba(255,255,255,0.66) 100%);
        // &::after{
        //   position: absolute;
        //   content: '';
        //   width: 24px;
        //   height: 8px;
        //   border-radius: 4px;
        //   background: #fff;
        //   left: 50%;
        //   bottom: -12px;
        //   transform: translateX(-50%);
        // }
      }
    }
  }
  .user-info-wrap{
    position: relative;
    cursor: pointer;
    .topbar-wrap{
      width: 150px;
      position: absolute;
      right: 0;
      transition: opacity .15s, visibility 0s .15s;
      z-index: 9999;
      background: #fff;
      border: 1px solid #D2D9DF;
      box-shadow: 0 2px 4px 0 rgba(0,0,0,0.12), 0 0 6px 0 rgba(0,0,0,0.04);
      border-radius: 3px;
      text-align: center;
      font-size: 12px;
      color: #333;
      span{
        width: 100%;
        height: 40px;
        line-height: 40px;
        display: block;
        &:not(:last-child) {
          border-bottom: 1px solid #e7e7e7;
        }
        &:hover{
          background: #202CBC;
          color: #fff;
          border-color: #202CBC;
        }
      }
    }
  }
  .user-info{
    height: 100%;
    font-size: 14px;
    color: #fff;
    img{
      width: 32px;
      height: 32px;
    }
    span{
      margin-left: 20px;
    }
    .user-name-arrow{
      width: 8px;
      height: 5px;
      margin: 0 10px;
      background: url('~@/assets/images/common/user-arrow.png') no-repeat center;
      background-size: cover;
      transition: all .1s linear 0s;
    }
    &:hover{
      .user-name-arrow{
        transform: rotate(180deg);
      }
    }
  }
  .logout{
    color: #DD3B3D;
    cursor: pointer;
  }
}
#content{
  width: 100%;
  height: calc(100% - 136px);
  overflow: hidden;
  &.is-home{
    left: 0;
    top: 64px;
  }
}
#main{
  height: 100%;
}
#copy-right{
  background: #fff;
  font-size: 14px;
  height: 32px;
  line-height: 32px;
  border-top: 1px solid #E8E8E8;
  color: #666;
  text-align: center;
}
@media screen and (max-width: 1441px) {
  #header{
    .menu-name{
      font-size: 12px;
      span{
        margin-left: 20px;
      }
    }
  }
  #header{
    height: 40px;
    .menu-name{
      span{
        &.active{
          &::after{
            height: 4px;
            border-radius: 2px;
            bottom: -6px;
          }
        }
      }
    }
    .user-info-wrap{
      .topbar-wrap{
        width: 100px;
        span{
          height: 24px;
          line-height: 24px;
        }
      }
    }
    .user-info{
      font-size: 12px;
      img{
        width: 24px;
        height: 24px;
      }
      span{
        margin-left: 10px;
      }
      .user-name-arrow{
        margin: 0 8px;
      }
    }
  }
  #content{
    width: 100%;
    height: calc(100% - 92px);
    overflow: hidden;
    &.is-home{
      left: 0;
      top: 64px;
    }
  }
  #copy-right{
    font-size: 12px;
    height: 24px;
    line-height: 24px;
  }
}
</style>
