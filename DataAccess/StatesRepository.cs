using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class StatesRepository : IRepository<state>
	{
		private DataContext db;

		public StatesRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<state> GetAll()
		{
			return db.states;
		}

		public void Add(state item)
		{
			db.states.Add(item);
		}

		public void Change(state item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(state item)
		{
			db.states.Remove(item);
		}

		public state Get(int id)
		{
			return db.states.SingleOrDefault(x => x.id == id);
		}

		public state Get(string name)
		{
			return db.states.SingleOrDefault(x => x.name == name);
		}
	}
}