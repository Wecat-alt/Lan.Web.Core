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
        public SqlSugarClient Db;
        public SimpleClient<T> CurrentDb { get { return new SimpleClient<T>(Db); } }
        //public 
        public DbContext()
        {
            var showDbLog = AppSettings.Get<string>("ConnectionStrings:conn");

            Db = new SqlSugarClient(new ConnectionConfig()
            {
                DbType = DbType.MySql,
                ConnectionString = showDbLog,
                //ConnectionString = $"",
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            //Db.Aop.OnLogExecuting = (sql, parameters) =>
            //{
            //    Console.WriteLine(sql); // 输出SQL
            //                            // 如果需要，还可以输出参数
            //                            // Console.WriteLine(string.Join(",", parameters?.Select(it => it.ParameterName + ":" + it.Value)));
            //};

        }
    }
}
