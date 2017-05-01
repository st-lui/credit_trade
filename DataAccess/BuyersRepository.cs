using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class BuyersRepository : IRepository<buyer>
	{
		private DataContext db;

		public BuyersRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<buyer> GetAll()
		{
			return db.buyers.OrderBy(x => x.fio);
		}

		public void Add(buyer item)
		{
			db.buyers.Add(item);
		}

		public void Change(buyer item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(buyer item)
		{
			db.buyers.Remove(item);
		}

		public buyer Get(int id)
		{
			return db.buyers.SingleOrDefault(x => x.id == id);
		}

		public buyer Get(string name)
		{
			return db.buyers.SingleOrDefault(x => x.fio == name);
		}

		public buyer CreateBuyer(int warehouse_id, string fio)
		{
			return new buyer()
			{
				fio = fio,
				warehouse_id = warehouse_id
			};
		}

		public IEnumerable<buyer> GetBuyers(int warehouse_id)
		{
			return db.buyers.Where(x => x.warehouse_id == warehouse_id).ToList();
		}


	}
}