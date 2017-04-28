using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class GoodsRepository : IRepository<good>
	{
		private DataContext db;

		public GoodsRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<good> GetAll()
		{
			return db.goods.OrderBy(x => x.id);
		}

		public void Add(good item)
		{
			db.goods.Add(item);
		}

		public void Change(good item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(good item)
		{
			db.goods.Remove(item);
		}

		public good Get(int id)
		{
			return db.goods.SingleOrDefault(x => x.id == id);
		}

		public good Get(string name)
		{
			return db.goods.SingleOrDefault(x => x.name == name);
		}
	}
}

