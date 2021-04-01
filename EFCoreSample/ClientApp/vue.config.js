const path = require('path')
const webpack = require('webpack')
function resolve(dir) {
  return path.join(__dirname, dir)
}
module.exports = {
  lintOnSave: false,
  chainWebpack(config) {
    config.resolve.alias
      .set('components', resolve('src/components'))
      .set('view', resolve('src/view'))
      .set('assets', resolve('src/assets'))
      .set('api', resolve('src/api'))
      .set('mixins', resolve('src/mixins'))
      .set('store', resolve('src/store'))
  },
  configureWebpack: {
    plugins: [
      new webpack.ProvidePlugin({
        $:"jquery",
        jQuery:"jquery",
        "windows.jQuery":"jquery"
      })
    ]
  }
}
