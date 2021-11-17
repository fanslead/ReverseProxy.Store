module.exports = {
  root: true,
  env: {
    node: true,
    jquery: true
  },
  globals: {
    _: true
  },
  'extends': [
    'plugin:vue/essential',
    '@vue/standard'
  ],
  rules: {
    'no-console': process.env.NODE_ENV === 'production' ? 'error' : 'off',
    'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off'
  },
  parserOptions: {
    parser: 'babel-eslint'
  },
  // required to lint *.vue files
  plugins: [
    'vue'
  ],
  // add your custom rules here
  rules: {
    // allow async-await
    'generator-star-spacing': 'off',
    // allow debugger during development
		'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',
		// 不能用多余的空格
		"no-multi-spaces": 1,
		// 禁止重复的函数声明
		"no-func-assign": 2,
		// 必须使用全等
		"eqeqeq": 0,
		// 禁止重复声明变量
    "no-redeclare": [2, {
      "builtinGlobals": true
		}],
		// 禁止使用new Function
		"no-new-func": 2,
		// 对象字面量项尾不能有逗号
		"comma-dangle": [2, "never"],
		// 必须要分号结尾
		// 'semi': ["error", "always"],
		// 引号类型 `` "" ''
    "quotes": [0],
    "space-before-function-paren": 0,
    "new-cap": [0]
  }
}
