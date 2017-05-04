using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class LeftoversRepository : IRepository<leftover>
	{
		private DataContext db;

		public LeftoversRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<leftover> GetAll()
		{
			return db.leftovers.OrderBy(x => x.id);
		}

		public void Add(leftover item)
		{
			db.leftovers.Add(item);
		}

		public void Change(leftover item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(leftover item)
		{
			db.leftovers.Remove(item);
		}

		public leftover Get(int id)
		{
			return db.leftovers.SingleOrDefault(x => x.id == id);
		}

		public leftover GetWithGoods(int id)
		{
			return db.leftovers.Include("good").SingleOrDefault(x => x.id == id);
		}
	}
}