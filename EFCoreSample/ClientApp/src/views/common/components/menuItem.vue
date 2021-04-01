<template>
  <div class="menu-item" :style="{height: height + 'px'}">
    <Row type="flex" align="middle" class="menu-col" :class="[menu.isNotLast ? 'has-child': 'child', menu.Level == 2 ? 'level-2' : '', isActive ? 'active' : '']" @click.native="handleCollapse" ref="col">
      <Row type="flex" align="middle" justify="center" class="menu-icon">
        <template v-if="menu.Level ==2">
          <img :src="menu.MenuIcon" v-if="menu.MenuIcon">
          <img src="~@/assets/images/menu/base.png" v-else>
        </template>
      </Row>
      <span v-show="value">{{menu.MenuName}}</span>
      <Row type="flex" align="middle" justify="center" class="menu-arrow" v-show="value">
        <img src="~@/assets/images/common/up.png">
      </Row>
    </Row>
    <slot name="child" ref="child"></slot>
  </div>
</template>
<script>
export default {
  name: 'menuItem',
  props: {
    menu: {
      type: Object,
      default() {
        return {}
      }
    },
    value: {
      type: Boolean,
      default: true
    }
  },
  data() {
    return {
      isExpand: false,
      colHeight: 0,
      height: 0,
      jumpBySelf: false
    }
  },
  computed: {
    isActive() {
      return this.$route.name == this.menu.RouteName
    }
  },
  watch: {
    $route: 'changeByRoute',
    value(val) {
      if (!val) {
        if (this.menu.isNotLast) {
          this.isExpand = false
          if (!this.isExpand) {
            this.height = this.colHeight
            return false
          }
          this.expand()
        }
      }
    }
  },
  methods: {
    changeByRoute(to, from) {
      if (this.jumpBySelf) {
        this.jumpBySelf = false
        return false
      }
      if (!this.value) {
        return false
      }
      if (to.name == this.menu.RouteName) {
        if (!this.$refs.child) {
          this.$nextTick(() => {
            if (this.expand) {
              this.expand()
            }
            let parent = this.$parent
            while (parent) {
              if (parent.$options.name == 'menuItem') {
                break
              } else {
                parent = parent.$parent
              }
            }
            if (parent) {
              parent && parent.handleCollapse && parent.handleCollapse()
            }
            this.$emit('menu-change')
          })
        }
      }
    },
    expand() {
      let len = (this.menu.ChildList && this.menu.ChildList.length) || 0
      let height = this.colHeight || this.$refs.col.$el.offsetHeight
      this.height = (len + 1) * height
    },
    handleCollapse(status = true) {
      if (!this.menu.isNotLast) {
        if (this.menu.RouteName && this.menu.Url) {
          this.jumpBySelf = true
          this.$router.push({
            name: this.menu.RouteName
          })
        }
        return false
      }
      if (!this.value) {
        return false
      }
      this.isExpand = status === true ? true : !this.isExpand
      if (!this.isExpand) {
        this.height = this.colHeight
        return false
      }
      this.expand()
    }
  },
  mounted() {
    let len = (this.menu.ChildList && this.menu.ChildList.length) || 0
    this.colHeight = this.$refs.col.$el.offsetHeight || 40 * (length + 1)
    this.height = this.colHeight
    this.changeByRoute({name: this.routeName})
  }
}
</script>
<style lang="less">
.menu-item{
  height: 40px;
  // background: #222549;
  background: #121438;
  color: rgba(255, 255, 255, .8);
  font-size: 14px;
  transition: all .1s linear 0s;
  overflow: hidden;
  .menu-col{
    height: 40px;
    padding: 0 15px;
    cursor: pointer;
    &.child{
      &:not(.level-2){
        background: #121438;
      }
    }
    &.active{
      background: #202CBC !important;
      // background: #1A2065 !important;
      color: #fff;
    }
    span{
      margin-left: 12px;
    }
    &.level-2{
      .menu-icon{
        visibility: visible;
      }
    }
    &.has-child{
      .menu-arrow{
        display: flex;
      }
    }
    &:hover:not(.has-child) {
      // background: #202CBC;
      background: #1A2065;
      color: #fff;
    }
  }
  .menu-icon{
    width: 14px;
    height: 14px;
    visibility: hidden;
    img{
      max-width: 100%;
      max-height: 100%;
    }
  }
  .menu-arrow{
    width: 10px;
    height: 10px;
    position: absolute;
    right: 35px;
    display: none;
    transform: rotate(180deg);
    img{
      max-width: 100%;
      max-height: 100%;
    }
  }
}
@media screen and (max-width: 1441px) {
  .menu-item{
    font-size: 12px;
    .menu-col{
      padding: 0 6px;
      span{
        margin-left: 6px;
      }
    }
  }
}
</style>
