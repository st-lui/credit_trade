using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class WarehousesRepository : IRepository<warehouse>
	{
		private DataContext db;

		public WarehousesRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<warehouse> GetAll()
		{
			return db.warehouses.OrderBy(x => x.id);
		}

		public void Add(warehouse item)
		{
			db.warehouses.Add(item);
		}

		public void Change(warehouse item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(warehouse item)
		{
			db.warehouses.Remove(item);
		}

		public warehouse Get(int id)
		{
			return db.warehouses.SingleOrDefault(x => x.id == id);
		}

		public warehouse Get(string name)
		{
			return db.warehouses.SingleOrDefault(x => x.name == name);
		}

		public IEnumerable<leftover> GetLeftovers(warehouse warehouse)
		{
			if (!db.Entry(warehouse).Collection(x => x.leftovers).IsLoaded)
				db.Entry(warehouse).Collection(x => x.leftovers).Load();
			foreach (var warehouseLeftover in warehouse.leftovers)
			{
				db.Entry(warehouseLeftover).Reference<good>("good").Load();
			}
			return warehouse.leftovers.Where(x=>x.amount!=0 || x.expenditure!=0).OrderBy(x=>x.good.parent_id).ToList();
		}
	}
}