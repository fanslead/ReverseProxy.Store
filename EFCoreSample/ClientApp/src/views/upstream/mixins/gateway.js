export default {
  data() {
    return {
      transformType: {
        1: "PathPrefix",
        2: "PathRemovePrefix",
        3: "PathSet",
        4: "PathPattern",
        5: "QueryValueParameter",
        6: "QueryRouteParameter",
        7: "QueryRemoveParameter",
        8: "HttpMethod",
        9: "RequestHeadersCopy",
        10: "RequestHeaderOriginalHost",
        11: "RequestHeader",
        12: "X-Forwarded",
        13: "Forwarded",
        14: "ClientCert",
        15: "ResponseHeadersCopy",
        16: "ResponseHeader",
        17: "ResponseTrailersCopy",
        18: "ResponseTrailer"
      },
      transformTypeList: [
        {
          Id: 1,
          Name: "PathPrefix",
          Params: [
            {
              Key: "PathPrefix",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 2,
          Name: "PathRemovePrefix",
          Params: [
            {
              Key: "PathRemovePrefix",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 3,
          Name: "PathSet",
          Params: [
            {
              Key: "PathSet",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 4,
          Name: "PathPattern",
          Params: [
            {
              Key: "PathPattern",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 5,
          Name: "QueryValueParameter",
          Params: [
            {
              Key: "QueryValueParameter",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Set",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Append",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 6,
          Name: "QueryRouteParameter",
          Params: [
            {
              Key: "QueryRouteParameter",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Set",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Append",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 7,
          Name: "QueryRemoveParameter",
          Params: [
            {
              Key: "QueryRemoveParameter",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 8,
          Name: "HttpMethod",
          Params: [
            {
              Key: "HttpMethod",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Set",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 9,
          Name: "RequestHeadersCopy",
          Params: [
            {
              Key: "RequestHeadersCopy",
              IsRequiredValue: true,
              Default: "true"
            }
          ]
        },
        {
          Id: 10,
          Name: "RequestHeaderOriginalHost",
          Params: [
            {
              Key: "RequestHeaderOriginalHost",
              IsRequiredValue: true,
              Default: "true"
            }
          ]
        },
        {
          Id: 11,
          Name: "RequestHeader",
          Params: [
            {
              Key: "RequestHeader",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Set",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Append",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 12,
          Name: "X-Forwarded",
          Params: [
            {
              Key: "X-Forwarded",
              IsRequiredValue: true,
              Default: "for,proto,host,PathBase"
            },
            {
              Key: "Prefix",
              IsRequiredValue: false,
              Default: "X-Forwarded-"
            },
            {
              Key: "Append",
              IsRequiredValue: false,
              Default: "true"
            }
          ]
        },
        {
          Id: 13,
          Name: "Forwarded",
          Params: [
            {
              Key: "Forwarded",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "ForFormat",
              IsRequiredValue: false,
              Default: "Random" // Random/RandomAndPort/RandomAndRandomPort/Unknown/UnknownAndPort/UnknownAndRandomPort/Ip/IpAndPort/IpAndRandomPort
            },
            {
              Key: "ByFormat",
              IsRequiredValue: false,
              Default: "Random"
            },
            {
              Key: "Append",
              IsRequiredValue: false,
              Default: "true"
            }
          ]
        },
        {
          Id: 14,
          Name: "ClientCert",
          Params: [
            {
              Key: "ClientCert",
              IsRequiredValue: true,
              Default: ""
            }
          ]
        },
        {
          Id: 15,
          Name: "ResponseHeadersCopy",
          Params: [
            {
              Key: "ResponseHeadersCopy",
              IsRequiredValue: true,
              Default: "true"
            }
          ]
        },
        {
          Id: 16,
          Name: "ResponseHeader",
          Params: [
            {
              Key: "ResponseHeader",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Set",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Append",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "When",
              IsRequiredValue: false,
              Default: "Success" // Success/Always
            }
          ]
        },
        {
          Id: 17,
          Name: "ResponseTrailersCopy",
          Params: [
            {
              Key: "ResponseTrailersCopy",
              IsRequiredValue: true,
              Default: "true"
            }
          ]
        },
        {
          Id: 18,
          Name: "ResponseTrailer",
          Params: [
            {
              Key: "ResponseTrailer",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Set",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "Append",
              IsRequiredValue: true,
              Default: ""
            },
            {
              Key: "When",
              IsRequiredValue: false,
              Default: "Success"
            }
          ]
        },
      ],
      headerMatchMode: [
        {
          Id: 0,
          Name: "ExactHeader"
        },
        {
          Id: 1,
          Name: "HeaderPrefix"
        },
        {
          Id: 2,
          Name: "Exists"
        }
      ],
      HTTPMethods: ["GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"], // http请求方法
      loadBPList: ["RoundRobin", "Random", "LeastRequests", "PowerOfTwoChoices"], // 目的地算法类型
      failurePolicyList: ["Redistribute", "Return503"], // 失败策略
      versionPolicyType: ["RequestVersionOrLower", "RequestVersionOrHigher", "RequestVersionExact"], // 外发请求选择最终版本,枚举值是0,1,2
      sslProtocols: ["Default", "None", "Ssl2", "Ssl3", "Tls", "Tls11", "Tls12", "Tls13"],
      sslProtocolList: [
        {Id: 240, Name: "Default"},
        {Id: 0, Name: "None"},
        {Id: 12, Name: "Ssl2"},
        {Id: 48, Name: "Ssl3"},
        {Id: 192, Name: "Tls"},
        {Id: 768, Name: "Tls11"},
        {Id: 3072, Name: "Tls12"},
        {Id: 12288, Name: "Tls13"}
      ],
      activityHeaderList: [
        {Id: 0, Name: "None"},
        {Id: 1, Name: "Baggage"},
        {Id: 2, Name: "CorrelationContext"},
        {Id: 3, Name: "BaggageAndCorrelationContext"}
      ]
    }
  },
  methods: {}
}
