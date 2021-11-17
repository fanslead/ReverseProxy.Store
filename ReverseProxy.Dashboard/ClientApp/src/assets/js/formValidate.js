// 加权因子;
// 身份证验证位值，10代表X;
let Wi = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
let ValideCode = [1, 0, 10, 9, 8, 7, 6, 5, 4, 3, 2];
function isTrueValidateCodeBy18IdCard(aIdCard) {
  // 声明加权求和变量
  let sum = 0;
  if (aIdCard[17].toLowerCase() == 'x') {
    aIdCard[17] = 10; // 将最后位为x的验证码替换为10方便后续操作
  }
  for (let i = 0; i < 17; i++) {
    // 加权求和
    sum += Wi[i] * aIdCard[i];
  }
  // 得到验证码所位置
  let valCodePosition = sum % 11;
  if (aIdCard[17] == ValideCode[valCodePosition]) {
    return true;
  } else {
    return false;
  }
}

function isValidityBrithBy18IdCard(idCard18) {
  let year = idCard18.substring(6, 10);
  let month = idCard18.substring(10, 12);
  let day = idCard18.substring(12, 14);
  let tempDate = new Date(year, parseFloat(month) - 1, parseFloat(day));
  // 这里用getFullYear()获取年份，避免千年虫问题
  if (tempDate.getFullYear() != parseFloat(year) || tempDate.getMonth() != parseFloat(month) - 1 || tempDate.getDate() != parseFloat(day)) {
    return false;
  } else {
    return true;
  }
}

function isValidityBrithBy15IdCard(idCard15) {
  let year = idCard15.substring(6, 8);
  let month = idCard15.substring(8, 10);
  let day = idCard15.substring(10, 12);
  let tempDate = new Date(year, parseFloat(month) - 1, parseFloat(day));
  // 对于老身份证中的你年龄则不需考虑千年虫问题而使用getYear()方法
  if (isValidityProvice(idCard15)) {
    if (tempDate.getYear() != parseFloat(year) || tempDate.getMonth() != parseFloat(month) - 1 || tempDate.getDate() != parseFloat(day)) {
      return false;
    } else {
      return true;
    }
  }
}

function isValidityProvice(val) {
  let vcity = {
    11: "北京",
    12: "天津",
    13: "河北",
    14: "山西",
    15: "内蒙古",
    21: "辽宁",
    22: "吉林",
    23: "黑龙江",
    31: "上海",
    32: "江苏",
    33: "浙江",
    34: "安徽",
    35: "福建",
    36: "江西",
    37: "山东",
    41: "河南",
    42: "湖北",
    43: "湖南",
    44: "广东",
    45: "广西",
    46: "海南",
    50: "重庆",
    51: "四川",
    52: "贵州",
    53: "云南",
    54: "西藏",
    61: "陕西",
    62: "甘肃",
    63: "青海",
    64: "宁夏",
    65: "新疆",
    71: "台湾",
    81: "香港",
    82: "澳门",
    91: "国外"
  };
  let provinceCode = val.substring(0, 2);
  if (vcity[provinceCode] != undefined) {
    return true;
  } else {
    return false;
  }
}
const formValidate = {
  isMobile: (rule, value, callback) => {
    let name = '';
    if (rule.name) {
      name = rule.name;
    }
    if (value) {
      // 手机号码（考虑086）
      let test = /^(\+)?(0|086|86|17951)?(13[0-9]|15[0-9]|17[0-9]|18[0-9]|14[5678]|19[0-9]|16[56])[0-9]{8}$/;
      if (!test.test(value)) {
        callback(new Error((name && '请输入正确的' + name) || '请输入正确格式的手机号'));
      } else {
        callback();
      }
    } else if (rule.required) {
      callback(new Error((name && name + '不可为空') || '手机号不可为空'));
    } else {
      callback();
    }
  },
  isTelAndMobile: (rule, value, callback) => {
    let name = '';
    if (rule.name) {
      name = rule.name;
    }
    if (value) {
      // 固定电话和手机号码(手机号不考虑086，不含直播号)
      let test = /(^(13[0-9]|15[0-9]|17[0-9]|18[0-9]|14[5678]|19[0-9]|16[56])[0-9]{8}$)|(^0(\d{2}|\d{3})-(\d{7,8})$)|(^(\d{7,8})$)/;
      let delTel = /(^(13[0-9]|15[0-9]|17[0-9]|18[0-9]|14[5678]|19[0-9]|16[56])[0-9]{4,5}$)/; // 手机号前7/8位
      if (!test.test(value)) {
        callback(new Error((name && '请输入正确的' + name) || '请输入正确格式的电话'));
      } else {
        if (delTel.test(value)) { // 满足7/8位数，但value若是手机号的前部分，这种情况不允许通过
          callback(new Error((name && '请输入正确的' + name) || '请输入正确格式的电话'));
        } else {
          callback();
        }
      }
    } else if (rule.required) {
      callback(new Error((name && name + '不可为空') || '电话不可为空'));
    } else {
      callback();
    }
  },
  isPassport: (rule, value, callback) => {
    if (value) {
      let reg = /^([a-zA-z]|[0-9]){5,17}$/g;
      if (reg.test(value)) {
        callback();
      } else {
        callback(new Error('请输入正确的护照号码'));
      }
    } else if (!value && rule.required) {
      callback(new Error('护照号码不可为空'));
    } else {
      callback();
    }
  },
  isIdcard: (rule, value, callback) => {
    if (value) {
      let gets = value;
      let decide;
      if (gets.length == 15) {
        if (isValidityProvice(gets) && isValidityBrithBy15IdCard(gets)) {
          decide = true;
        } else {
          decide = false;
        }
      } else if (gets.length == 18) {
        // 得到身份证数组
        let aIdCard = gets.split('');
        if (isValidityProvice(gets) && isTrueValidateCodeBy18IdCard(aIdCard) && isValidityBrithBy18IdCard(gets)) {
          decide = true;
        } else {
          decide = false;
        }
      }

      if (!decide) {
        callback(new Error('请输入正确的身份证号'));
      } else {
        callback();
      }
    } else if (!value && rule.required) {
      callback(new Error('身份证号不可为空'));
    } else {
      callback();
    }
  },
  isWholeInteger: (rule, value, callback) => {
    let name = rule.name || "数值";
    if (value) {
      // 数值
      let num = /^\d+$/;
      if (!num.test(value)) {
        callback(new Error(name + '须为大于等于零的整数'));
      } else {
        callback();
      }
    } else if (rule.required) {
      callback(new Error(name + '不可为空'));
    } else {
      callback();
    }
  },
  isTel: (rule, value, callback) => {
    let name = rule.name || '座机'
    if (rule.required) {
      if (!value) {
        callback(name + '不能为空')
      }
    }
    if (value) {
      let reg = /^(\(\d{3,4}\)|\d{3,4}-|\s)?\d{7,8}$/
      if (reg.test(value)) {
        callback()
      } else {
        callback(`${name}格式错误`)
      }
    } else {
      callback()
    }
  },
  isFullAddress: (rule, value, callback) => {
    let name = rule.name || "网址";
    let test = /^(?=^.{3,255}$)(http(s)?:\/\/)?(www\.)?[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+(:\d+)*(\/\w+\.\w+)*$/;
    if (value && !test.test(value)) {
      callback(new Error('请输入完整的' + name));
    } else if (rule.required) {
      callback(new Error(name + '不可为空'));
    } else {
      callback();
    }
  },
  pwdValidator: (rule, value, callback) => {
    let reg = /^[a-zA-Z0-9]{6,18}$/g
    let name = rule.name || '密码'
    if (reg.test(value)) {
      callback()
    } else {
      callback(new Error(`${name}只能由6到18位的字母或数字组成`))
    }
  },
  accountValidator: (rule, value, callback) => {
    let reg = /^[a-zA-Z0-9]{1,20}$/g
    let name = rule.name || '用户名'
    if (reg.test(value)) {
      callback()
    } else {
      callback(new Error(`${name}只能由1到20位的字母或数字组成`))
    }
  },
  simpleWordValidator: (rule, value, callback) => {
    let len = rule.length || 20
    let reg = /^[a-zA-Z0-9]{0,}$/g
    let name = rule.name || ''
    if (reg.test(value)) {
      if (value.length > len) {
        callback(new Error(`${name}不能超过${len}个字符`))
      } else {
        callback()
      }
    } else {
      callback(new Error(`${name}只能由字母或数字组成`))
    }
  },
  isWeixinNo: (rule, value, callback) => {
    // let reg = /^[a-zA-Z]([-_a-zA-Z0-9]{5,19})+$/
    let reg = /^[-_a-zA-Z0-9]{5,20}$/
    let name = rule.name || '微信号'
    if (value) {
      if (reg.test(value)) {
        callback()
      } else {
        callback(new Error(`${name}格式不正确`))
      }
    } else if (rule.required) {
      callback(new Error(`${name}不能为空`))
    } else {
      callback()
    }
  },
  textLengthValidator: (rule, value, callback) => {
    let reg = rule.reg
    let name = rule.name || ''
    let message = rule.message || `${name}格式不正确`
    if (value) {
      if (reg) {
        if (reg.test(value)) {
          callback()
        } else {
          callback(new Error(`${name}格式不正确`))
        }
      } else if (rule.length) {
        if (value.length > rule.length) {
          callback(new Error(`${name}不能超过${rule.length}个字符`))
        } else {
          callback()
        }
      } else {
        callback()
      }
    } else if (rule.required) {
      callback(new Error(message || `${name}不能为空`))
    } else {
      callback()
    }
  }
};
export default formValidate;
