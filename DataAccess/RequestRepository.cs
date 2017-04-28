using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class RequestRepository : IRepository<request>
	{
		private DataContext db;

		public RequestRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<request> GetAll()
		{
			return db.requests;
		}

		public void Add(request item)
		{
			db.requests.Add(item);
		}

		public void Delete(request item)
		{
			db.requests.Remove(item);
		}

		public request Get(int id)
		{
			return db.requests.SingleOrDefault(x => x.id == id);
		}

		public void Change(request item)
		{
			db.Entry(item).State=EntityState.Modified;
		}
	}
}