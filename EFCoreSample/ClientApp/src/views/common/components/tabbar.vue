<template>
  <Row id="tabbar" type="flex" class="flex-nowrap">
    <Row type="flex" align="middle" justify="center" class="expand-icon" @click.native="handleChangeExpand">
      <img src="~@/assets/images/common/shrink.png" v-if="value">
      <img src="~@/assets/images/common/expand.png" v-else>
    </Row>
    <Row type="flex" id="tab-bar">
      <div class="left-arrow" @click="scroller('right')" v-if="iscroll"></div>
      <ul :style="{'marginLeft':-scrollWidth+'px'}" id="muneId" class="color-light">
        <li v-for="(tab,index) in list" :key="index" @contextmenu="menuEvent($event,tab)" :class="{active: tab.active, marginLeft: iscroll, firstAndEnd: !index && index == list.length - 1}" @click="activeTab(tab,$event)">
          <!-- <template v-if="tab.name == 'home'">
            <Row type="flex" align="middle" justify="center" class="back-home">
              <img src="~@/assets/images/common/home.png" v-if="!iscroll">
            </Row>
            <span class="color-primary-light">{{tab.name | getMenuName}}</span>
          </template> -->
          <template>
            <span :class="tab.active ? 'color-primary' : ''">{{tab.name | getMenuName}}</span>
            <span class="removeClass" @click.stop="delTab($event, tab)"></span>
          </template>
        </li>
      </ul>
      <div class="right-arrow" @click="scroller('left')" v-if="iscroll"></div>
      <div class="event-menu" :style="{top:toppx+'px',left:leftpx+'px'}" @contextmenu="menuEventDefault">
        <a href="javascript:;" @click="closeTag($event,'cur')" :class="{'disable': isHaveMoreTag.cur}">关闭</a>
        <a href="javascript:;" @click="closeTag($event,'left')" :class="{'disable': isHaveMoreTag.left}">关闭左侧</a>
        <a href="javascript:;" @click="closeTag($event,'right')" :class="{'disable': isHaveMoreTag.right}">关闭右侧</a>
        <a href="javascript:;" @click="closeTag($event,'all')" :class="{'disable': isHaveMoreTag.all}">关闭所有</a>
      </div>
    </Row>
  </Row>
</template>
<script>
import menu from '@/assets/js/menu';
export default {
  name: 'tabbar',
  props: {
    value: {
      type: Boolean,
      default: true
    }
  },
  data() {
    return {
      ismenu: false,
      list: [
        // {
        //   name: "home",
        //   path: "/",
        //   active: true
        // }
      ],
      toppx: 0,
      leftpx: 0,
      curTab: {},
      targetTab: {},
      tabNavWidth: 0,
      tabUlWidth: 0,
      iscroll: false,
      scrollWidth: 0,
      isHaveMoreTag: {
        cur: false,
        left: false,
        right: false,
        all: false
      },
      posiNum: 0,
      subWidth: 0,
      widthFun: 0
    };
  },
  mounted() {
    this.tabNavWidth = $("#tab-bar").width()
    this.tabUlWidth = $("#tab-bar ul").width()
    this.activeInnerView()
    window.addEventListener('resize', () => {
      this.tabNavWidth = $("#tab-bar").width()
      this.tabUlWidth = $("#tab-bar ul").width()
      this.$mutations.setOffsetWidth(document.documentElement.offsetWidth)
    })
    window.addEventListener('click', function() {
      if (document.querySelector(".event-menu")) {
        document.querySelector(".event-menu").style.display = "none"
      }
    })
  },
  watch: {
    $route: "changeTabByRoute",
    tabNavWidth: function(val, oval) {
      if (this.tabUlWidth > val) {
        this.iscroll = true
      } else {
        this.iscroll = false
        this.scrollWidth = 0
      }
    },
    tabUlWidth: function(val, oval) {
      if (val > this.tabNavWidth) {
        this.iscroll = true
      } else {
        this.iscroll = false
        this.scrollWidth = 0
      }
    }
  },
  filters: {
    getMenuName(name) {
      return menu[name] || name
    }
  },
  methods: {
    handleChangeExpand() {
      this.$emit('input', !this.value)
      setTimeout(() => {
        this.tabNavWidth = $("#tab-bar").width()
        this.tabUlWidth = $("#tab-bar ul").width()
        this.activeInnerView()
      }, 100)
    },
    activeTab(currentTab, event) {
      this.targetTab = currentTab
      _.each(this.list, tab => {
        tab.active = currentTab == tab
      })
      this.$router.push(currentTab)
      this.$nextTick(() => {
        this.activeInnerView()
      })
    },
    activeInnerView() {
      let target = document.querySelector("#tabbar").querySelector(".active")
      if (target) {
        let offLeft = target.offsetLeft
        let offwidth = target.offsetWidth
        let moveX = offwidth + offLeft
        let scroll = this.scrollWidth + (moveX - this.tabNavWidth)

        if (scroll > 0) {
          this.scrollWidth = scroll + 60
        } else {
          this.scrollWidth = 0
        }
      }
    },
    createTab(route) {
      if (!route || (route && !route.name)) {
        return
      }
      let currentTab = {
        name: route.name,
        path: route.path,
        params: route.params,
        query: route.query,
        active: true
      }
      let isExist = false
      _.each(this.list, tab => {
        tab.active = false
        if (currentTab.name == tab.name) {
          tab.active = isExist = true
          tab.path = currentTab.path
          tab.params = currentTab.params
          tab.query = currentTab.query
        }
      })
      this.targetTab = currentTab
      let params = {
        list: this.list
      }
      if (!isExist) {
        params = {
          list: this.list,
          cb: () => {
            this.tabNavWidth = $("#tab-bar").width()
            this.tabUlWidth = $("#tab-bar ul").width()
            this.activeInnerView()
          }
        }
        this.list.push(currentTab)
      }
      this.$emit("set_include", params)
    },
    delTab(e, currentTab) {
      e.stopPropagation()
      let index = _.indexOf(this.list, currentTab)
      if (currentTab.name == "home") {
        return
      }
      this.list.splice(index, 1)
      if (currentTab.active) {
        let activeTab = this.list[index] || this.list[index - 1]
        this.$router.push(activeTab)
      }
      let params = {
        list: this.list,
        cb: () => {
          this.tabNavWidth = $("#tab-bar").width()
          this.tabUlWidth = $("#tab-bar ul").width()
          this.scroller("right")
        }
      }
      this.$emit("set_include", params)
    },
    deleteNowTab(currentTab, newRoute) {
      _.remove(this.list, item => {
        return item.name == currentTab.name
      })
      let params = {
        list: this.list,
        cb: () => {
          this.tabNavWidth = $("#tab-bar").width()
          this.tabUlWidth = $("#tab-bar ul").width()
          this.scroller("right")
        }
      }
      this.$emit("set_include", params)
      this.$nextTick(() => {
        this.$router.push(newRoute)
      })
    },
    changeTabByRoute(to, from) {
      this.createTab(to)
    },
    menuEventDefault(evt) {
      evt.preventDefault()
    },
    menuEvent(evt, tab) {
      evt.preventDefault()
      var index = this.list.indexOf(tab)
      if (tab.name == "home") {
        if (this.list.length == 1) {
          return
        }
        this.isHaveMoreTag.cur = true
        this.isHaveMoreTag.left = true
        this.isHaveMoreTag.right = false
      } else {
        this.isHaveMoreTag.cur = false
        if (this.list.length == 2) {
          // 只有两个标签
          if (index == 1) {
            this.isHaveMoreTag.left = true
            this.isHaveMoreTag.right = true
          } else {
            this.isHaveMoreTag.left = false
            this.isHaveMoreTag.right = false
          }
        } else {
          if (index == 1) {
            this.isHaveMoreTag.left = true
          } else {
            this.isHaveMoreTag.left = false
          }
          if (index == this.list.length - 1) {
            this.isHaveMoreTag.right = true
          } else {
            this.isHaveMoreTag.right = false
          }
        }
      }
      this.toppx = evt.clientY
      this.leftpx = evt.clientX
      this.curTab = tab
      $("#tab-bar").focus()
      document.querySelector(".event-menu").style.display = "block"
    },
    closeTag(e, tag) {
      var index = this.list.indexOf(this.curTab)
      var tindex = this.list.indexOf(this.targetTab)
      if (this.list.length == 1) {
        return
      }
      switch (tag) {
        case "left":
          if (index == 1) {
            return
          }
          this.list.splice(1, index - 1)
          if (tindex < index) {
            this.$router.push(this.curTab)
          }
          this.scroller("right")
          break
        case "all":
          this.list.splice(1)
          this.$router.push({
            name: "home"
          })
          this.scroller("right")
          this.iscroll = false
          break
        case "right":
          if (index == this.list.length - 1) {
            return
          }
          this.list.splice(index + 1, this.list.length - index)
          this.$router.push(this.curTab)
          this.scroller("right")
          break
        default:
          this.delTab(e, this.curTab)
      }
      this.$emit("set_include", this.list)
      document.querySelector(".event-menu").style.display = "none"
    },
    scroller(sign) {
      this.tabNavWidth = $("#tab-bar").width()
      this.tabUlWidth = $("#tab-bar ul").width()
      let distance = 0
      if (sign == "left") {
        distance = this.tabUlWidth - (this.tabNavWidth + this.scrollWidth)
        if (distance > this.tabNavWidth) {
          this.scrollWidth += this.tabNavWidth
        } else {
          this.scrollWidth += distance
        }
      } else {
        distance = this.scrollWidth
        if (distance > this.tabNavWidth) {
          this.scrollWidth -= this.tabNavWidth
        } else {
          this.scrollWidth -= distance
        }
      }
    },
    closeTagMenu() {
      setTimeout(() => {
        document.querySelector(".event-menu").style.display = "none"
      }, 300)
    },
    init() {
      var flag = false
      _.each(this.list, (item, index, array) => {
        if (item.name == "home") {
          flag = true
        }
      })
      if (!flag) {
        let dashBoard = {
          name: "home",
          path: "/",
          params: {},
          query: {},
          active: false
        }
        if (this.list.length >= 0) {
          dashBoard.active = true
        }
        // this.list.unshift(dashBoard)
      }
      this.createTab(this.$route)
    }
  },
  created() {
    if (sessionStorage.includeList && sessionStorage.getItem('includeList') != 'undefined') {
      this.list = JSON.parse(sessionStorage.getItem("includeList"))
    } else {
      this.list = []
    }
    this.init()
  }
}
</script>
<style lang="less">
#tabbar{
  background: #fff;
  height: 40px;
  border-bottom: 1px solid #D9D9D9;
  outline: none;
  #tab-bar{
    height: 39px;
    line-height: 39px;
    position: relative;
    flex: 1;
    overflow: hidden;
  }
  .expand-icon{
    width: 39px;
    height: 39px;
    padding: 0 2px 0 4px;
    background: #fff;
    cursor: pointer;
    img{
      width: 32px;
      height: 32px;
    }
  }
  .left-arrow {
    width: 9px;
    height: 39px;
    padding: 13px;
    background: url(~@/assets/images/common/left-arrow.png) no-repeat;
    background-size: 9px 11px;
    background-position: 10px;
    position: absolute;
    left: 0px;
    top: 0px;
    cursor: pointer;
    z-index: 1;
    background-color: #fff;
  }
  .left-arrow i {
    font-size: 30px;
  }
  .right-arrow {
    width: 9px;
    height: 39px;
    padding: 13px;
    background: url(~@/assets/images/common/right-arrow.png) no-repeat;
    background-size: 9px 11px;
    background-position: 10px;
    position: absolute;
    right: 0px;
    top: 0px;
    cursor: pointer;
    z-index: 10;
    background-color: #fff;
  }
  .right-arrow i {
    font-size: 30px;
  }
  ul {
    display: flex;
    flex-wrap: nowrap;
    height: 40px;
    box-sizing: content-box;
    white-space: nowrap;
    transition: margin-left 0.5s linear;
    -webkit-transition: margin-left 0.5s linear;
    -moz-transition: margin-left 0.5s linear;
  }
  li {
    display: flex;
    align-items: center;
    line-height: 40px;
    padding: 0 20px;
    border-bottom: 0;
    background: #fff;
    box-sizing: border-box;
    cursor: pointer;
    text-decoration: none;
    position: relative;
    user-select: none;
    -webkit-user-select: none;
    -moz-user-select: none;
    font-size: 14px;
    border-bottom: 1px solid #D9D9D9;
    // &:not(:first-child) {
    //   border-radius: 4px 4px 0 0;
    //   background: rgba(0, 0, 0, 0.02);
    //   margin-right: 2px;
    //   border-left: 1px solid #D9D9D9;
    //   border-right: 1px solid #D9D9D9;
    //   &.active, &:hover{
    //     background: #fff;
    //     border-bottom: 1px solid #fff;
    //   }
    // }
    border-radius: 4px 4px 0 0;
    background: rgba(0, 0, 0, 0.02);
    margin-right: 2px;
    border-left: 1px solid #D9D9D9;
    border-right: 1px solid #D9D9D9;
    &.active, &:hover{
      background: #fff;
      border-bottom: 1px solid #fff;
    }
    &:first-child {
      border-left: none
    }
    .removeClass {
      width: 10px;
      height: 10px;
      cursor: pointer;
      display: block;
      background: url(~@/assets/images/common/remove.png) no-repeat center;
      margin-left: 5px;
      background-size: 100%;
    }
    &:first-child {
      padding-left: 0;
      &.marginLeft {
        margin-left: 6px;
      }
    }
    &:last-child {
      &.marginLeft {
        margin-right: 6px;
      }
    }
    &:after {
      content: "";
      display: block;
      position: absolute;
      width: 100%;
      height: 1px;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: transparent;
    }
  }
  .event-menu {
    position: fixed;
    z-index: 10000000;
    display: none;
    background-color: #fff;
    white-space: nowrap;
    box-shadow: 0 0 5px rgba(0, 0, 0, 0.2);
    a {
      display: block;
      height: 30px;
      line-height: 30px;
      border: none;
      padding: 0 10px;
      color: #333;
      &:hover {
        background-color: #f7f7f7;
      }
    }
    .disable {
      color: #bbbec4;
      border-color: #dddee1;
      background-color: #f7f7f7;
      &:hover {
        cursor: not-allowed;
      }
    }
  }
  .back-home{
    width: 17px;
    height: 16px;
    margin-right: 5px;
    img{
      width: 100%;
      height: 100%;
    }
  }
}
@media screen and (max-width: 1441px) {
  #tabbar{
    height: 28px;
    #tab-bar{
      height: 27px;
      line-height: 27px;
      position: relative;
    }
    .expand-icon{
      width: 27px;
      height: 27px;
      img{
        width: 100%;
        height: 100%;
      }
    }
    .left-arrow, .right-arrow {
      height: 27px;
    }
    ul{
      height: 28px;
    }
    li {
      line-height: 28px;
      padding: 0 10px;
      font-size: 12px;
      &:first-child {
        padding-left: 0;
        &.marginLeft {
          margin-left: 11px;
        }
      }
      &:last-child {
        &.marginLeft {
          margin-right: 11px;
        }
      }
    }
    .event-menu {
      position: fixed;
      z-index: 10000000;
      display: none;
      width: 84px;
      background-color: #fff;
      white-space: nowrap;
      box-shadow: 0 0 5px rgba(0, 0, 0, 0.2);
      a {
        display: block;
        border: none;
        padding: 5px 15px;
        font-size: 12px;
        color: #333;
        &:hover {
          background-color: #f7f7f7;
        }
      }
      .disable {
        color: #bbbec4;
        border-color: #dddee1;
        background-color: #f7f7f7;
        &:hover {
          cursor: not-allowed;
        }
      }
    }
  }
}
</style>
