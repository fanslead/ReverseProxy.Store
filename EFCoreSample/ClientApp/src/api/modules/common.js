import Axios from '../index'
let apis = {
  getClusterPage: 'ReverseProxy/ClusterPage', // 集群分页列表
  getProxyRoutePage: 'ReverseProxy/ProxyRoutePage', // 路由分页列表
  commonCluster: 'ReverseProxy/Cluster', // 集群api通用
  commonProxyRoute: 'ReverseProxy/ProxyRoute', // 路由api通用
  login: "ReverseProxy/Login"
}
export function getClusterPage(option) {
  return Axios.get(apis.getClusterPage, { params: option, withoutToken: true })
}
export function getProxyRoutePage(option) {
  return Axios.get(apis.getProxyRoutePage, { params: option, withoutToken: true })
}
export function createCluster(option) {
  return Axios.post(apis.commonCluster, option, { withoutToken: true })
}
export function modifyCluster(option) {
  return Axios.put(apis.commonCluster, option, { withoutToken: true })
}
export function deleteCluster(option) {
  return Axios.delete(apis.commonCluster, { params: option, withoutToken: true })
}
export function getClusterList(option) {
  return Axios.get(apis.commonCluster, { params: option, withoutToken: true })
}
export function createProxyRoute(option) {
  return Axios.post(apis.commonProxyRoute, option, { withoutToken: true })
}
export function modifyProxyRoute(option) {
  return Axios.put(apis.commonProxyRoute, option, { withoutToken: true })
}
export function deleteProxyRoute(option) {
  return Axios.delete(apis.commonProxyRoute, { params: option, withoutToken: true })
}
export function getProxyRouteList(option) {
  return Axios.get(apis.commonProxyRoute, { params: option, withoutToken: true })
}
export function loginGateWay(option) {
  return Axios.post(apis.login + "?password=" + option.password, {}, { withoutToken: true, withoutPassword: true })
}
