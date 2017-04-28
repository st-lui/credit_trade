using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class UsersRepository : IRepository<user>
	{
		private DataContext db;

		public UsersRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<user> GetAll()
		{
			return db.users;
		}

		public void Add(user item)
		{
			db.users.Add(item);
		}

		public void Change(user item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(user item)
		{
			db.users.Remove(item);
		}

		public user Get(int id)
		{
			return db.users.SingleOrDefault(x => x.id == id);
		}

		public user Get(string name)
		{
			return db.users.SingleOrDefault(x => x.username == name);
		}
	}
}