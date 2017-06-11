using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
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
			var l = db.leftovers.Include(x => x.good).Where(x=>x.warehouse_id==warehouse.id && x.amount>x.expenditure.Value);
			return l.ToList();
			//			var leftovers = db.leftovers.SqlQuery(@"select l.[id],l.[warehouse_id],l.[good_id],l.[amount],l.[expenditure]
			//,g.[id],g.[nom_id],g.[parent_id],g.[name],g.[edizm],g.[price],g.[barcode],g.[category]
			//FROM leftovers l,goods g
			//where l.warehouse_id=@warehouse_id and l.good_id=g.id", new SqlParameter("@warehouse_id", warehouse.id));
			//			foreach (var leftover in leftovers)
			//			{
			//				yield return leftover;
			//			}
		}

		public IEnumerable<request> GetNotPaidRequests(int warehouse_id)
		{
			return db.requests.Where(x => x.buyer.warehouse_id == warehouse_id && x.paid==false).ToList();
		}
	}
}