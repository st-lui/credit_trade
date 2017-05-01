using System;
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
			return db.users.Include("postoffice").SingleOrDefault(x => x.id == id);
		}

		public user Get(string name)
		{
			return db.users.Include("postoffice").SingleOrDefault(x => x.username == name);
		}

		public user CreateUser(string fio, int warehouse_id, string username)
		{
			return new user()
			{
				fio = fio,
				warehouse_id = warehouse_id,
				username = username
			};
		}

		public user CreateUser(string fio, string ops_idx, string username, IEnumerable<postoffice> postoffices)
		{
			warehouse wh = postoffices.Single(x => x.idx == ops_idx).warehouses.Single(x => !x.name.ToLower().Contains("магазин"));
			return new user()
			{
				fio = fio,
				warehouse_id = wh.id,
				username = username
			};
		}

		public user CreateUser(string fio, string ops_idx, string username)
		{
			warehouse wh = db.postoffices.Include("warehouses").Single(x => x.idx == ops_idx).warehouses.Single(x => !x.name.ToLower().Contains("магазин"));
			return new user()
			{
				fio = fio,
				warehouse_id = wh.id,
				username = username
			};
		}

		public IEnumerable<user> GetUsers(string ops_idx)
		{
			warehouse wh = db.postoffices.Include("warehouses").Single(x => x.idx == ops_idx).warehouses.Single(x => !x.name.ToLower().Contains("магазин"));
			return wh.users.ToList();
		}

		public IEnumerable<user> GetUsers(string ops_idx, IEnumerable<postoffice> postoffices)
		{
			warehouse wh = postoffices.Single(x => x.idx == ops_idx).warehouses.Single(x => !x.name.ToLower().Contains("магазин"));
			return wh.users.ToList();
		}

		public request CreateRequest(user currentUser,string comment, int buyer_id)
		{
			return new request()
			{
				date = DateTime.Now,
				username = currentUser.username,
				comment = comment,
				user_id = currentUser.id,
				buyer_id = buyer_id,
				paid = false,
				pay_date = null,
				cost = 0
			};
		}
		public request CreateRequest(user currentUser, string comment, int buyer_id, IEnumerable<request_rows> requestRows)
		{
			request request =new request()
			{
				date = DateTime.Now,
				username = currentUser.username,
				comment = comment,
				user_id = currentUser.id,
				buyer_id = buyer_id,
				paid = false,
				pay_date = null,
				cost = 0
			};
			foreach (var requestRow in requestRows)
			{
				request.request_rows.Add(requestRow);
				requestRow.request = request;
			}
			return request;
		}

		public void MakePay(request request)
		{
			request.paid = true;
			request.pay_date = DateTime.Now;
		}

		public void AddRequestRow(request request, request_rows requestRow)
		{
			request.request_rows.Add(requestRow);
			requestRow.request = request;
		}

		public buyer CreateBuyer(user currentUser, string fio)
		{
			return new buyer()
			{
				fio =fio,
				warehouse_id = currentUser.warehouse_id
			};
		}

		public IEnumerable<buyer> GetBuyers(user currentUser,List<buyer> buyersList)
		{
			return buyersList.Where(x => x.warehouse_id == currentUser.warehouse_id).ToList();
		}

		public IEnumerable<buyer> GetBuyers(user currentUser)
		{
			return db.buyers.Where(x => x.warehouse_id == currentUser.warehouse_id).ToList();
		}
	}
}