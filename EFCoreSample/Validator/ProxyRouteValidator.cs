using FluentValidation;
using ReverseProxy.Store.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFCoreSample.Validator
{
    public class ProxyRouteValidator : AbstractValidator<ProxyRoute>
    {
        public ProxyRouteValidator()
        {
            RuleFor(x=>x.Match.Methods)
                .Must(methods => 
                {
                    var ms = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS", "HEAD", "CONNECT", "OPTIONS", "TRACE" };
                    var arr = methods.Split(",");
                    if (arr.Length > 0)
                    {
                        return arr.All(a => ms.Contains(a));
                    }
                    return true;
                })
                .When(x => x.Match != null)
                .WithMessage("ProxyRoute.Match.Methods must in (GET, POST, PUT, DELETE, OPTIONS, HEAD, CONNECT, OPTIONS, TRACE)")
                ;
        }
    }
}
