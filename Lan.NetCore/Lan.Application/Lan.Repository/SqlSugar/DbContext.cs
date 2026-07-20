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
        // 全局唯一 SqlSugarClient（延迟初始化，线程安全）
        // SqlSugarClient 官方推荐单例模式，共享实例可复用内部元数据缓存和连接池
        private static readonly Lazy<SqlSugarClient> _sharedClient = new(() =>
        {
            var connStr = AppSettings.Get<string>("ConnectionStrings:conn");
            return new SqlSugarClient(new ConnectionConfig()
            {
                DbType = DbType.MySql,
                ConnectionString = connStr,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
        });

        /// <summary>全局共享的 SqlSugar 客户端实例</summary>
        public SqlSugarClient Db => _sharedClient.Value;

        public SimpleClient<T> CurrentDb => new(Db);

        // 保留无参构造以兼容直接 new 的场景（如 AlarmAndRadarBackgroundService 中直接 new 的 Service）
        public DbContext() { }
    }
}
