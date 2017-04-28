using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class RequestRowRepository : IRepository<request_rows>
	{
		private DataContext db;

		public RequestRowRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<request_rows> GetAll()
		{
			return db.request_rows;
		}

		public void Add(request_rows item)
		{
			db.request_rows.Add(item);
		}

		public void Change(request_rows item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(request_rows item)
		{
			db.request_rows.Remove(item);
		}

		public request_rows Get(int id)
		{
			return db.request_rows.SingleOrDefault(x => x.id == id);
		}
	}
}