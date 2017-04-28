using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class PostofficeRepository : IRepository<postoffice>
	{
		private DataContext db;

		public PostofficeRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<postoffice> GetAll()
		{
			return db.postoffices.OrderBy(x => x.id);
		}

		public void Add(postoffice item)
		{
			db.postoffices.Add(item);
		}

		public void Change(postoffice item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(postoffice item)
		{
			db.postoffices.Remove(item);
		}

		public postoffice Get(int id)
		{
			return db.postoffices.SingleOrDefault(x => x.id == id);
		}

	}
}