const moment = require('moment')
export function debounce(fn, wait) {
  var time;
  return function () {
    var self = this;
    var args = [].slice.apply(arguments);
    if (time) {
      clearInterval(time);
    }
    time = setTimeout(function () {
      fn.apply(self, args);
    }, wait);
  };
}
export function isType(type, obj) {
  return Object.prototype.toString.call(obj) == `[object ${type}]`;
}
export function dateFormat(date) {
  if (!date) {
    return '';
  }
  if (typeof date == 'string') {
    date = date.replace(/T/g, ' ').replace(/-/g, '/');
  }
  date = new Date(date);
  if (date == 'Invalid Date') {
    return '';
  }
  return date;
}
export function formatDate(date, symbol) {
  date = dateFormat(date);
  if (!date) {
    return '';
  }
  let month = date.getMonth() + 1;
  if (month < 10) {
    month = `0${month}`;
  }
  let day = date.getDate();
  if (day < 10) {
    day = `0${day}`;
  }
  return date.getFullYear() + (symbol || '-') + month + (symbol || '-') + day;
}
export function formatDateWithSeconds(date, symbol) {
  let preDate = formatDate(date, symbol);
  if (preDate) {
    return preDate + ' 00:00:00';
  }
  return '';
}
export function formatTime(date) {
  date = dateFormat(date);
  if (date) {
    let hour = date.getHours();
    hour = fillZero(hour);
    let min = date.getMinutes();
    min = fillZero(min);
    let sec = date.getSeconds();
    sec = fillZero(sec);
    return `${hour}:${min}:${sec}`;
  }
  return '';
}

export function getDateInfo(date) {
  date = dateFormat(date);
  let year, month, dates, hour, min, sec;
  if (date) {
    year = date.getFullYear();
    month = fillZero(date.getMonth() + 1);
    dates = fillZero(date.getDate());
    hour = fillZero(date.getHours());
    min = fillZero(date.getMinutes());
    sec = fillZero(date.getSeconds());
  }
  return {
    year,
    month,
    dates,
    hour,
    min,
    sec
  };
}

function fillZero(num) {
  if (isNaN(num)) {
    return '';
  }
  if (num < 10) {
    return `0${num}`;
  }
  return num;
}

export function isNull(obj) {
  return obj === null || obj === undefined || obj === '';
}

export function getRandomRGBColor() {
  let r = Math.floor(Math.random() * 256);
  let g = Math.floor(Math.random() * 256);
  let b = Math.floor(Math.random() * 256);
  return `rgb(${r},${g},${b})`;
}

export function smalltoBIG(n) {
  let fraction = ['角', '分'];
  let digit = ['零', '壹', '贰', '叁', '肆', '伍', '陆', '柒', '捌', '玖'];
  let unit = [['元', '万', '亿'], ['', '拾', '佰', '仟']];
  let head = n < 0 ? '欠' : '';
  n = Math.abs(n);
  let s = '';
  for (let i = 0; i < fraction.length; i++) {
    s += (digit[Math.floor(n * 10 * Math.pow(10, i)) % 10] + fraction[i]).replace(/零./, '');
  }
  s = s || '整';
  n = Math.floor(n);

  for (let i = 0; i < unit[0].length && n > 0; i++) {
    let p = '';
    for (let j = 0; j < unit[1].length && n > 0; j++) {
      p = digit[n % 10] + unit[1][j] + p;
      n = Math.floor(n / 10);
    }
    s = p.replace(/(零.)*零$/, '').replace(/^$/, '零') + unit[0][i] + s;
  }
  return head + s.replace(/(零.)*零元/, '元').replace(/(零.)+/g, '零').replace(/^整$/, '零元整');
}

export function oneOf(target, origin) {
  for (let i = 0, len = origin.length; i < len; i++) {
    if (origin[i] == target) {
      return true;
    }
  }
  return false;
}

export function formatPrice(price, unit = 2) {
  price = Number(price);
  if (isNaN(price)) {
    price = 0;
  }
  price = price.toFixed(unit).split('.');
  let reg = /(?=(\B)(\d{3}))+$/g;
  price[0] = price[0].replace(reg, ',');
  return '¥' + price.join('.');
}
/**
 * @param event 输入框事件源
 * @param form 所在表单 null则不处理
 * @param vm this指向
 * @param key 处理的字段名
 * @param all 是否将null '' undefined转化成0
 * @param min 最小值
 * @param max 最大值
 * @param unit 格式化小数位数
 */
export function sortChange(event, form, vm, key = 'Sort', all = true, min = -10000, max = 10000, unit = 2){
  let value = event.target.value
  if (typeof min != 'number') {
    min = -10000
  }
  if (typeof max != 'number') {
    max = 10000
  }
  if (typeof unit != 'number') {
    unit = 2
  }
  if (isNull(value)) {
    if (all) {
      value = min > 0 ? min : 0
    } else {
      return false
    }
  } else {
    value = +value
  }
  if(isNaN(value)){
    value = min > 0 ? min : 0
  } else if (value < min) {
    value = min
  } else if (value > max) {
    value = max
  }
  if (!Number.isInteger(value)) {
    value = value.toFixed(unit)
  }
  event.target.value = value
  if(form && vm[form]){
    if (!key) {
      key = 'Sort'
    }
    vm[form][key] = value
  }
  return value
}

export function handleSearchDateSelected(dateList, key, vm, form) {
  if (dateList[0] && dateList[1]) {
    vm[form][key + 'Start'] = dateList[0]
    vm[form][key + 'End'] = dateList[1]
  } else {
    delete vm[form][key + 'Start']
    delete vm[form][key + 'End']
  }
}
export function validDate(val, needTime) {
  if (val) {
    if (val == '0001-01-01T00:00:00' || val == '1900-01-01T00:00:00') {
      return '';
    } else {
      let format = 'YYYY-MM-DD'
      if (needTime) {
        format += ' HH:mm:ss'
      }
      return moment(val).format(format)
    }
  }
  return '';
}
export function contains(parent, node) {
  var contains = document.documentElement.contains ? _nativeContains : _compatibleContains
  return contains(parent, node)
}
function _nativeContains(parent, node) {
  return parent != node && parent.contains(node)
}
function _compatibleContains(parent, node) {
  while(node && (node = node.parentNode)) {
    if (node === parent) {
      return true
    }
  }
  return false
}
