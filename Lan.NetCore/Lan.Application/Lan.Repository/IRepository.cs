using Lan.Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Repository
{
    public interface IRepository<T> : ISimpleClient<T> where T : class, new()
    {
        IInsertable<T> Insertable(T t);
        ISugarQueryable<T> Queryable();
        int DeleteTable();

        int Update(Expression<Func<T, bool>> where, Expression<Func<T, T>> columns);
        IDeleteable<T> Deleteable();
    }
}
