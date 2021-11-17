// import router from '@/router';
export default {
  data() {
    return {
      Permissions: {
        view: false,
        add: false,
        edit: false,
        delete: false,
        assign: false,
        point: false,
        refound: false,
        status: false,
        send: false,
        jurisdiction: false,
        export: false,
        import: false,
        grant: false,
        onSheft: false,
        offSheft: false,
        turning: false,
        daily: false,
        weekly: false,
        monthly: false,
        resend: false,
        unbind: false
      },
      rightList: {},
      isGetRightNow: false,
      menuLocalList: [
        {
          "PlatVersionId": "25dfbe4e-9fc6-4c30-9073-5e15d4e3f44b",
          "PlatId": "3e2083d0-da27-4636-bfd6-ef466fdeea25",
          "PlatName": "OA",
          "PlatIcon": "",
          "PlatSort": 2,
          "ClientList": [
            {
              "ClientId": 1,
              "ClientName": "PC",
              "MenuList": [
                {
                  "MenuId": "7d98feed-1d0e-481b-98c2-e284f0d0f604",
                  "MenuName": "网关设置",
                  "Url": "",
                  "RouteName": "",
                  "Display": true,
                  "MenuIcon": "",
                  "Sort": 10,
                  "Type": 1,
                  "ClientId": 1,
                  "ButtonList": null,
                  "ChildList": [
                    {
                      "MenuId": "144f46fd-a3a5-489d-976a-cffcc75a6aa2",
                      "MenuName": "集群列表",
                      "Url": "/upstreamList",
                      "RouteName": "upstreamList",
                      "Display": true,
                      "MenuIcon": "",
                      "Sort": 10,
                      "Type": 1,
                      "ClientId": 1,
                      "ButtonList": [
                        {
                          "ButtonId": "212f1c39-3a18-40a6-a1f8-79c418acece1",
                          "Name": "查看",
                          "Icon": "",
                          "Action": "view",
                          "Route": null,
                          "Controller": "",
                          "Sort": 1
                        },
                        {
                          "ButtonId": "a05f984f-199f-44ff-8b3e-0744f33b80de",
                          "Name": "新建",
                          "Icon": "",
                          "Action": "add",
                          "Route": null,
                          "Controller": "",
                          "Sort": 3
                        },
                        {
                          "ButtonId": "d465cfef-d208-4a1c-b74a-8b4b7b1ebe33",
                          "Name": "编辑",
                          "Icon": "",
                          "Action": "edit",
                          "Route": null,
                          "Controller": "",
                          "Sort": 4
                        },
                        {
                          "ButtonId": "da286e05-1952-4539-ba5d-7789d3869575",
                          "Name": "删除",
                          "Icon": "",
                          "Action": "delete",
                          "Route": null,
                          "Controller": "",
                          "Sort": 5
                        }
                      ],
                      "ChildList": null
                    },
                    {
                      "MenuId": "5db5750e-0792-425b-991b-d3eb2f69116a",
                      "MenuName": "路由列表",
                      "Url": "/routeList",
                      "RouteName": "routeList",
                      "Display": true,
                      "MenuIcon": "",
                      "Sort": 10,
                      "Type": 1,
                      "ClientId": 1,
                      "ButtonList": [
                        {
                          "ButtonId": "212f1c39-3a18-40a6-a1f8-79c418acece1",
                          "Name": "查看",
                          "Icon": "",
                          "Action": "view",
                          "Route": null,
                          "Controller": "",
                          "Sort": 1
                        },
                        {
                          "ButtonId": "a05f984f-199f-44ff-8b3e-0744f33b80de",
                          "Name": "新建",
                          "Icon": "",
                          "Action": "add",
                          "Route": null,
                          "Controller": "",
                          "Sort": 3
                        },
                        {
                          "ButtonId": "d465cfef-d208-4a1c-b74a-8b4b7b1ebe33",
                          "Name": "编辑",
                          "Icon": "",
                          "Action": "edit",
                          "Route": null,
                          "Controller": "",
                          "Sort": 4
                        },
                        {
                          "ButtonId": "da286e05-1952-4539-ba5d-7789d3869575",
                          "Name": "删除",
                          "Icon": "",
                          "Action": "delete",
                          "Route": null,
                          "Controller": "",
                          "Sort": 5
                        }
                      ],
                      "ChildList": null
                    }
                  ]
                }
              ]
            }
          ],
          "RoleDateConfigList": null
        }
      ]
    }
  },
  computed: {
    menuList() {
      return this.$stores.menuList
    },
    isHeadStore() {
      return this.$stores.isHeadStore
    },
    rights() {
      return this.$stores.rights
    },
    finalPermission() {
      return this.filterRight(this.Permissions)
    }
  },
  methods: {
    getRights() {
      return new Promise((resolve, reject) => {
        if (!this.rights || Object.keys(this.rights).length == 0) {
          if (this.$stores.isGetRightNow && this.$options.name.toLowerCase() == 'layout') {
            resolve()
            return false
          }
          const data = {
            platId: '3e2083d0-da27-4636-bfd6-ef466fdeea25'
          }
          this.rightList = {}
          this.$mutations.setIsGetRightNow(true);
          let result = this.menuLocalList;
          let menuList = []
          _.each(result, item => {
            menuList = (item.ClientList && item.ClientList[0] && item.ClientList[0].MenuList) || []
          })
          this.$mutations.setIsGetRightNow(false)
          if (!menuList.length) {
            // router.push({
            //   name: 'login',
            //   query: router.currentRoute.query
            // })
            return false
          }
          menuList = setMenuRight(menuList, this, 1)
          this.$mutations.setMenuList(menuList)
          this.$mutations.setRights(this.rightList)
          resolve();
        } else {
          resolve()
        }
      })
    },
    getChildRights(parentRoutName = this.$route.name) {
      return this.getRights().then(() => {
        return this.filterRight(this.Permissions, parentRoutName)
      })
    },
    filterRight(defaultRight = this.Permissions, name = this.$route.name) {
      defaultRight = _.clone(defaultRight)
      let right = this.rights[name]
      if (right) {
        _.each(right, (value, key) => {
          defaultRight[key] = value || false
        })
      }
      return defaultRight
    }
  }
}

function setMenuRight(menuList, vm, level) {
  return _.filter(menuList, menu => {
    menu.Level = level
    if (level == 2 && menu.RouteName) {
      let icon
      try {
        icon = require(`@/assets/images/menu/${menu.RouteName}.png`)
      } catch (e) {
        icon = require(`@/assets/images/menu/base.png`)
      }
      if (icon) {
        menu.MenuIcon = icon
      }
    }
    if (menu.RouteName && menu.Url && menu.ButtonList) {
      vm.$set(vm.rightList, menu.RouteName, {})
      _.each(menu.ButtonList, btn => {
        vm.$set(vm.rightList[menu.RouteName], btn.Action, true)
      })
    }
    if (menu.ChildList && menu.ChildList.length) {
      menu.isNotLast = true
      menu.ChildList = setMenuRight(menu.ChildList, vm, level + 1)
    } else {
      // if (Math.random() > .5) {
      //   menu.isNotLast = true
      //   menu.ChildList = [
      //     {
      //       MenuId: Math.random(),
      //       MenuName: 'account',
      //       RouteName: 'account'
      //     },
      //     {
      //       MenuId: Math.random(),
      //       MenuName: 'empList',
      //       RouteName: 'empList'
      //     },
      //     {
      //       MenuId: Math.random(),
      //       MenuName: 'storeList',
      //       RouteName: 'storeList'
      //     }
      //   ]
      // }
    }
    return (vm.isHeadStore && menu.Type != 2) || !vm.isHeadStore
  })
}
