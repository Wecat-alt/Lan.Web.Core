using Infrastructure.Extensions;
using Lan.Model;
using Lan.Repository.SqlSugar;
using Mapster;
using Model;
using SqlSugar;
using SqlSugar.IOC;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Lan.Repository
{
    public class Repository<T> : DbContext<T> where T : class, new()
    {
        private static readonly object TrackInfoTableCreateLock = new();
        //private readonly ISqlSugarClient _db;
        public Repository(ISqlSugarClient db = null)
        {
            //base.Context = db;
        }
        public IInsertable<T> Insertable(T t)
        {
            return Db.Insertable(t);
        }
        public ISugarQueryable<T> Queryable()
        {
            //Db.DbMaintenance.CreateDatabase();

            //Db.CodeFirst.InitTables(typeof(CameraModel));

            return Db.Queryable<T>();
        }
        public int DeleteTable(object id)
        {
            return Db.Deleteable<T>(id).ExecuteCommand();
        }
        public int Insert(List<T> t)
        {
            return Db.Insertable(t).ExecuteCommand();
        }
        public int Update(T entity, bool ignoreNullColumns = false, object data = null)
        {
            return Db.Updateable(entity).IgnoreColumns(ignoreNullColumns)
                .EnableDiffLogEventIF(data.IsNotEmpty(), data).ExecuteCommand();
        }
        public int Update(Expression<Func<T, bool>> where, Expression<Func<T, T>> columns)
        {
            return Db.Updateable<T>().SetColumns(columns).Where(where).RemoveDataCache().ExecuteCommand();
        }


        public IDeleteable<T> Deleteable()
        {
            return Db.Deleteable<T>();
        }
        public int Delete(object id, string title = "")
        {
            return Db.Deleteable<T>(id).EnableDiffLogEventIF(title.IsNotEmpty(), title).ExecuteCommand();
        }
        public int Add(T t, bool ignoreNull = true)
        {
            return Db.Insertable(t).IgnoreColumns(ignoreNullColumn: ignoreNull).ExecuteCommand();
        }
        public async Task<int> BatchInsertAsync(List<TrackInfo> batch)
        {
            if (batch == null || batch.Count == 0)
                return 0;

            var total = 0;
            var groups = batch.GroupBy(x => x.UpdateTime.ToString("yyyyMM"));
            foreach (var group in groups)
            {
                var tableName = $"trackinfo{group.Key}";
                EnsureTrackInfoMonthlyTable(tableName);
                total += await Db.Insertable(group.ToList()).AS(tableName).ExecuteCommandAsync();
            }

            return total;
        }

        protected void EnsureTrackInfoMonthlyTable(string tableName)
        {
            if (Db.DbMaintenance.IsAnyTable(tableName, false))
            {
                return;
            }
 
            lock (TrackInfoTableCreateLock)
            {
                if (Db.DbMaintenance.IsAnyTable(tableName, false))
                {
                    return;
                }

                const string baseTableName = "trackinfo";
                if (Db.DbMaintenance.IsAnyTable(baseTableName, false))
                {
                    Db.Ado.ExecuteCommand($"CREATE TABLE `{tableName}` LIKE `{baseTableName}`");
                }
                else
                {
                    Db.CodeFirst.As<TrackInfo>(tableName).InitTables<TrackInfo>();
                }
            }
        }

        public int SqlCommand(string sql)
        {
            return Db.Ado.ExecuteCommand(sql);
        }
    }

    public static class QueryableExtension
    {
        /// <summary>
        /// 读取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">查询表单式</param>
        /// <param name="parm">分页参数</param>
        /// <returns></returns>
        public static PagedInfo<T> ToPage<T>(this ISugarQueryable<T> source, PagerInfo parm)
        {
            var page = new PagedInfo<T>();
            var total = 0;
            page.PageSize = parm.PageSize;
            page.PageIndex = parm.PageNum;
            if (parm.Sort.IsNotEmpty())
            {
                source.OrderByPropertyName(parm.Sort, parm.SortType.Contains("desc") ? OrderByType.Desc : OrderByType.Asc);
            }
            page.Result = source
                //.OrderByIF(parm.Sort.IsNotEmpty(), $"{parm.Sort.ToSqlFilter()} {(!string.IsNullOrWhiteSpace(parm.SortType) && parm.SortType.Contains("desc") ? "desc" : "asc")}")
                .ToPageList(parm.PageNum, parm.PageSize, ref total);
            page.TotalNum = total;
            return page;
        }

        /// <summary>
        /// 转指定实体类Dto
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="parm"></param>
        /// <returns></returns>
        public static PagedInfo<T2> ToPage<T, T2>(this ISugarQueryable<T> source, PagerInfo parm)
        {
            var page = new PagedInfo<T2>();
            var total = 0;
            page.PageSize = parm.PageSize;
            page.PageIndex = parm.PageNum;
            if (parm.Sort.IsNotEmpty())
            {
                source.OrderByPropertyName(parm.Sort, parm.SortType.Contains("desc") ? OrderByType.Desc : OrderByType.Asc);
            }
            var result = source
                //.OrderByIF(parm.Sort.IsNotEmpty(), $"{parm.Sort.ToSqlFilter()} {(!string.IsNullOrWhiteSpace(parm.SortType) && parm.SortType.Contains("desc") ? "desc" : "asc")}")
                .ToPageList(parm.PageNum, parm.PageSize, ref total);

            page.TotalNum = total;
            page.Result = result.Adapt<List<T2>>();
            return page;
        }
    }
}
