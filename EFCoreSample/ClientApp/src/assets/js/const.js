// 硬编码 用于判断crm 消息任务的月报期限控制
// const PLAT_ID = 'f445aa10-4516-451d-925c-ecff307e9a6b';
const CRM_PLATID = "f445aa10-4516-451d-925c-ecff307e9a6b"; // 为了角色权限处，能取到月报的设置天数
const PLAT_ID = '3e2083d0-da27-4636-bfd6-ef466fdeea25'; // 当前平台的平台Id
const MENU_ID = 'a4c2a6d7-eef4-4e80-8c87-cc9155158ad0';
const BUTTON_ID = '770aa594-b853-439a-8088-c3d57cf30e59';
const MENU_IDList = ['a4c2a6d7-eef4-4e80-8c87-cc9155158ad0', 'addc7953-d9b1-4281-8de3-9a744c3ace31'];
// 客户端类型
const clientType = [
  {
    Id: 1,
    Name: 'PC'
  },
  {
    Id: 2,
    Name: 'H5'
  },
  {
    Id: 3,
    Name: '小程序'
  }
];
// 客户端类型
const clientTypeObj = {
  1: 'PC',
  2: 'H5',
  3: '小程序'
};
// 客户端类型默认值
const DEFAULT_CLIENT_TYPE = clientType[0].Id;
const DEFAULT_CLIENT_TYPE_NAME = clientType[0].Name;
export {
  CRM_PLATID,
  PLAT_ID,
  MENU_ID,
  BUTTON_ID,
  clientTypeObj,
  DEFAULT_CLIENT_TYPE,
  DEFAULT_CLIENT_TYPE_NAME,
  MENU_IDList
};
