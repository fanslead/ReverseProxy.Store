import axios from 'axios';
import router from '@/router';
// import apiConfig from './config';
import { Message } from 'view-design';

let ajaxCount = 0;
let loader = null;

const Axios = axios.create({
  baseURL: apiConfig.baseURL,
  responseType: 'json',
  withCredentials: false,
  // timeout: 20000,
  timeout: 100000,
  headers: {
    'Content-Type': 'application/json;charset=utf-8',
    'Accept': 'application/json'
  }
});

Axios.interceptors.request.use(config => {
  let token = localStorage.getItem('token');
  let password = localStorage.getItem('AccountP');
  if (config.withoutToken !== true) {
    if (!token) {
      // router.push({
      //   name: 'login',
      //   query: router.currentRoute.query
      // });
      return new Promise((resolve, reject) => {});
    } else {
      config.headers = {
        'Content-Type': 'application/json;charset=utf-8',
        'Accept': 'application/json',
        'tokenStr': token
      };
    }
  }
  if (config.withoutPassword !== true) {
    if (!password) {
      router.push({
        name: 'login'
      });
      return new Promise((resolve, reject) => {});
    }
  }
  ajaxCount++;
  if (ajaxCount == 1) {
    loader = Message.loading({
      content: config.loadingText || '正在加载中',
      duration: 0
    });
  }
  return config;
}, error => {
  ajaxCount--;
  return Promise.reject(error);
});

Axios.interceptors.response.use(res => {
  try {
    if (res.data === null) {
      res.data = {code: -1001, message: '接口返回null', data: null};
      return Promise.reject(res.data);
    }
    if (Object.prototype.toString.call(res.data) == '[object Blob]') {
      return new Promise((resolve, reject) => {
        let reader = new FileReader();
        reader.onload = (e) => {
          let data = e.target.result;
          res.data = {code: 1001, message: '', data: data};
          resolve(res.data);
        };
        reader.readAsDataURL(res.data);
      });
    }
    
    if (res.data.code === 1301 || res.data.code === 1303) {
      // 登录失败或需要登录跳转回登录页面
      router.push({
        name: 'login',
        query: router.currentRoute.query
      });
      /* eslint-disable */
      return new Promise((resolve, reject) => {});
      /* eslint-disable */
    }
    if (res.data.code === 1001 || res.data.code === 1005 || res.data.code === 0 || (res.data.Result && res.data.Result.code === 1005)) {
      // 成功返回
      return Promise.resolve(res.data);
    }
    return Promise.resolve(res.data);
    // 失败返回
    return Promise.reject(res.data);
  } catch(e) {
    return Promise.reject(e);
  } finally{
    setTimeout(() => {
      ajaxCount--;
      if (ajaxCount == 0) {
        loader();
      }
    }, 300);
  }
}, error => {
  // 浏览器抛出错误
  ajaxCount--;
  if (ajaxCount == 0) {
    loader();
  }
  return Promise.reject(error);
});

export default Axios;
