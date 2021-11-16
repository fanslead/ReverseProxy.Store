<template>
  <div id="left-menu" :class="{'shrink': !value}">
    <div class="sidebar-wrap" :class="{shrink: !value}">
      <div class="sidebar-box">
        <Row id="logo" type="flex" align="middle" justify="center">
          <!-- <img :src="$stores.Logo | getImgUrl" alt="logo" v-if="$stores.Logo">
          <img :src="defaultLogo" v-else> -->
        </Row>
        <div class="sidebar-menu">
          <Row v-for="item in showMenu" :key="item.MenuId">
            <menu-item :menu="item" :key="item.MenuId" @menu-change="menuChange(item)" v-model="value" @mouseover.native="item.showMenuWrap=true" @mouseout.native="item.showMenuWrap=false">
              <template v-slot:child>
                <template v-if="item.ChildList && item.ChildList.length">
                  <template v-if="value">
                    <menu-item :menu="chl" :key="chl.MenuId" v-for="chl in item.ChildList" @menu-change="menuChange(item, chl)"></menu-item>
                  </template>
                  <template v-else>
                    <div class="menu-wrap" v-show="item.showMenuWrap">
                      <div class="menu-wrap-box">
                        <div v-for="chl in item.ChildList" :key="chl.MenuId" :class="{active: chl.RouteName == $route.name}" @click="gotoMenu(chl)">{{chl.MenuName}}</div>
                      </div>
                    </div>
                  </template>
                </template>
                <template v-else>
                  <div class="menu-wrap" v-show="item.showMenuWrap && !value">
                    <div class="menu-wrap-box">
                      <div @click="gotoMenu(item)" :class="{active: item.RouteName == $route.name}">{{item.MenuName}}</div>
                    </div>
                  </div>
                </template>
              </template>
            </menu-item>
          </Row>
        </div>
      </div>
    </div>
  </div>
</template>
<script>
import menuItem from './menuItem'
export default {
  name: 'myMenu',
  props: {
    activeMenuId: '',
    value: {
      type: Boolean,
      default: true
    }
  },
  components: {
    menuItem
  },
  computed: {
    showMenu() {
      if (!this.activeMenuId) {
        return []
      }
      let activeMenuItem = _.filter(this.menuList, menu => {
        return menu.MenuId == this.activeMenuId
      })
      return ((activeMenuItem[0] && activeMenuItem[0].ChildList) || []).map(menu => {
        this.$set(menu, 'showMenuWrap', false)
        return menu
      })
    }
  },
  data() {
    return {
      defaultLogo: require('@/assets/images/common/default-logo.png')
    }
  },
  methods: {
    gotoMenu(menu) {
      this.$router.push({
        name: menu.RouteName
      })
    },
    menuChange(menu) {
      this.$emit('get-active-id', menu.MenuId)
    }
  }
}
</script>
<style lang="less">
#left-menu{
  width: 180px;
  height: 100%;
  overflow: hidden;
  // transition: width 0.1s linear 0s;
  // background: #222549;
  background: #0A0C24;
  position: relative;
  z-index: 1000;
  .sidebar-wrap{
    width: 200px;
    height: 100%;
    overflow-x: hidden;
    overflow-y: auto;
  }
  .sidebar-box{
    width: 200px;
    overflow: auto;
  }
  .sidebar-menu{
    width: 100%;
  }
  &.shrink{
    width: 40px;
    padding-top: 64px;
    overflow: unset;
    .sidebar-wrap, .sidebar-box{
      width: 100%;
      overflow: unset;
    }
    .menu-item{
      overflow: unset;
      .menu-item{
        display: none;
      }
    }
    #logo{
      display: none;
    }
  }
  .menu-wrap{
    width: 122px;
    position: absolute;
    left: 40px;
    top: 0;
    z-index: 10;
    padding-left: 6px;
  }
  .menu-wrap-box{
    background: #222549;
    border-radius: 2px;
    cursor: pointer;
    & > div{
      height: 40px;
      line-height: 40px;
      text-align: center;
      font-size: 14px;
      &.active{
        // background: #202CBC !important;
        color: #fff;
      }
    }
  }
}
#logo{
  width: 180px;
  height: 64px;
  padding: 10px 0;
  cursor: pointer;
  img{
    max-width: 100%;
    max-height: 100%;
  }
}
@media screen and (max-width: 1441px) {
  #left-menu{
    width: 140px;
    .sidebar-wrap, .sidebar-box{
      width: 160px;
    }
    &.shrink{
      width: 26px;
      padding-top: 40px;
    }
    .menu-wrap{
      left: 26px;
      padding-left: 4px;
    }
    .menu-wrap-box{
      & > div{
        font-size: 12px;
      }
    }
  }
  #logo{
    width: 140px;
    height: 40px;
    padding: 5px 0;
  }
}
</style>
