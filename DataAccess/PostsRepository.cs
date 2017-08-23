using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class PostsRepository : IRepository<post>
	{
		private DataContext db;

		public PostsRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<post> GetAll()
		{
			return db.posts.OrderBy(x=>x.name);
		}

		public void Add(post item)
		{
			db.posts.Add(item);
		}

		public void Change(post item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(post item)
		{
			db.posts.Remove(item);
		}

		public post Get(int id)
		{
			return db.posts.Include("children").SingleOrDefault(x => x.id == id);
		}

		public post Get(string name)
		{
			return db.posts.SingleOrDefault(x => x.name == name);
		}

		public IEnumerable<buyer> GetBuyers(int postId)
		{
			var postofficeIds = db.postoffices.Where(po => po.post_id == postId).Select(x => x.id);
			var warehouseIds = db.warehouses.Where(wh => postofficeIds.Contains(wh.postoffice_id)).Select(x=>x.id);
			return db.buyers.Include("warehouse.postoffice").Where(buyer => warehouseIds.Contains(buyer.warehouse_id.Value)).ToList();
		}

		public IEnumerable<warehouse> GetWarehouses(int postId)
		{
			var postofficeIds = db.postoffices.Where(po => po.post_id == postId).Select(x => x.id);
			return db.warehouses.Include("postoffice").Where(wh => postofficeIds.Contains(wh.postoffice_id)).ToList();
		}
	}
}