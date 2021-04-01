/**
 @param level 层级 1：品牌，2：大区，3：小区，4：门店，5：部门
 @param selectedList 已选中的门店
*/
export function loop(target, source, parent, level, selectedList, key = 'checked') {
  _.each(source, s => {
    let obj = {
      title: s.DepartmentName || s.Name,
      value: s.DepartmentId || s.Id,
      expand: true,
      selected: false,
      children: [],
      parent: [...parent, {title: s.DepartmentName || s.Name, value: s.DepartmentId || s.Id}],
      level: level,
      checked: false
    }
    if (level == 5) {
      obj.storeId = parent[parent.length - 1].value
    }
    if (level == 4) {
      obj.type = 'final'
      if (typeof selectedList == 'string') {
        if (obj.value == selectedList) {
          obj[key] = true
        }
      } else if (selectedList && selectedList.length) {
        if (selectedList.includes(obj.value)) {
          obj[key] = true
        }
      }
    }
    s.Subordinate = s.Subordinate || s.list || []
    if (s.Subordinate && s.Subordinate.length) {
      loop(obj.children, s.Subordinate, obj.parent, level + 1, selectedList, key)
    }
    target.push(obj)
  })
}
/**
 @param level 层级 1：品牌，2：大区，3：小区，4：门店，5：部门
*/
export function loopForBranchDept(target, source, level) {
  _.each(source, s => {
    let obj = {
      title: s.DepartmentName || s.Name,
      value: s.DepartmentId || s.Id,
      expand: true,
      selected: false,
      children: [],
      level: level,
      checked: false
    }
    if (level == 4) {
      obj.type = 'final'
      obj.depts = s.Subordinate || []
    }
    if (level < 4) {
      s.Subordinate = s.Subordinate || s.list || []
      if (s.Subordinate && s.Subordinate.length) {
        loopForBranchDept(obj.children, s.Subordinate, level + 1)
      }
    }
    target.push(obj)
  })
}
/**
 @param level 层级 1：品牌，2：大区，3：小区，4：门店，5：部门
*/
export function loopForMainDept(target, source) {
  _.each(source, s => {
    let obj = {
      title: s.DepartmentName || s.Name,
      value: s.DepartmentId || s.Id,
      expand: true,
      selected: false,
      children: [],
      checked: false,
      type: 'final'
    }
    s.Subordinate = s.Subordinate || s.list || []
    if (s.Subordinate && s.Subordinate.length) {
      loopForMainDept(obj.children, s.Subordinate)
    }
    target.push(obj)
  })
}
