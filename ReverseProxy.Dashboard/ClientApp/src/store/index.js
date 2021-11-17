import Vue from 'vue'
export let store = Vue.observable({
  IsAllowNewRole: false,
  IsHeader: false,
  isHeadStore: localStorage.getItem('IsHeadStore') == 'true',
  menuList: [],
  rights: {},
  Logo: '',
  EmpName: '',
  StoreName: '',
  MerchantName: '',
  Account: '',
  JobNumber: '',
  isGetRightNow: false,
  offsetWidth: 0
})
export let mutations = {
  setMenuList(data) {
    store.menuList = data || []
  },
  setIsHeadStore(data) {
    store.isHeadStore = data || false
  },
  setIsHeader(data) {
    store.IsHeader = data || false
  },
  setIsAllowNewRole(data) {
    store.IsAllowNewRole = data || false
  },
  setRights(data) {
    store.rights = data || {}
  },
  setLogo(data) {
    store.Logo = data
  },
  setEmpName(data) {
    store.EmpName = data
  },
  setStoreName(data) {
    store.StoreName = data
  },
  setMerchantName(data) {
    store.MerchantName = data
  },
  setAccount(data) {
    store.Account = data
  },
  setJobNumber(data) {
    store.JobNumber = data
  },
  setIsGetRightNow(data) {
    store.isGetRightNow = data
  },
  setOffsetWidth(data) {
    store.offsetWidth = data
  }
}
