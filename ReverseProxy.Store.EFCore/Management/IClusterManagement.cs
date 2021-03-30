using ReverseProxy.Store.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Store.EFCore.Management
{
    public interface IClusterManagement
    {
        IQueryable<Cluster> GetAll();
        Task<Cluster> Find(string id);
        Task<bool> Create(Cluster proxyRoute);
        Task<bool> Update(Cluster proxyRoute);
        Task<bool> Delete(string id);
    }
}
