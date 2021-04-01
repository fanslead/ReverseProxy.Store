# ReverseProxy.Store
yarp用EFCore存储配置

# 先配置数据库链接字符串，然后执行code first还原数据库，然后就可以用了。
appsettings.json加上下面东西，Password是前端登录验证密码（做做样子的233333）
```
  "ConnectionStrings": {
    "Default": ""
  },
    "Password": "password"
```
