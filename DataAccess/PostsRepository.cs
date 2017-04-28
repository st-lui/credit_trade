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
			return db.posts.SingleOrDefault(x => x.id == id);
		}

		public post Get(string name)
		{
			return db.posts.SingleOrDefault(x => x.name == name);
		}
	}
}