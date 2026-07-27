using Infrastructure;
using Lan.Infrastructure;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Repository.SqlSugar
{
    public class DbContext<T> where T : class, new()
    {
        // 全局唯一 SqlSugarScope（延迟初始化，线程安全）
        // SqlSugarScope 是 SqlSugarClient 的线程安全版本，专为单例+多线程场景设计
        private static readonly Lazy<ISqlSugarClient> _sharedClient = new(() =>
        {
            var connStr = AppSettings.Get<string>("ConnectionStrings:conn");
            return new SqlSugarScope(new ConnectionConfig()
            {
                DbType = DbType.MySql,
                ConnectionString = connStr,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
        });

        /// <summary>全局共享的 SqlSugar 客户端实例（线程安全）</summary>
        public ISqlSugarClient Db => _sharedClient.Value;

        public SimpleClient<T> CurrentDb => new(Db);

        // 保留无参构造以兼容直接 new 的场景（如 AlarmAndRadarBackgroundService 中直接 new 的 Service）
        public DbContext() { }
    }
}
