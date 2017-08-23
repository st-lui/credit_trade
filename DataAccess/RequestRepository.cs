 using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class RequestRepository : IRepository<request>
	{
		private DataContext db;

		public RequestRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<request> GetAll()
		{
			return db.requests;
		}

		public void Add(request item)
		{
			db.requests.Add(item);
		}

		public void Delete(request item)
		{
			db.requests.Remove(item);
		}

		public request Get(int id)
		{
			return db.requests.Include("buyer").Include("request_rows").Include("user.warehouse.postoffice.post").SingleOrDefault(x => x.id == id);
		}

		public void Change(request item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public request CreateRequest(DateTime date, string username, string comment, int user_id, int buyer_id)
		{
			return new request()
			{
				date = date,
				username = username,
				comment = comment,
				user_id = user_id,
				buyer_id = buyer_id,
				paid = false,
				pay_date = null,
				cost = 0
			};
		}

		public request CreateRequest(DateTime date, string username, string comment, int user_id, int buyer_id, IEnumerable<request_rows> requestRows)
		{
			var request = new request()
			{
				date = date,
				username = username,
				comment = comment,
				user_id = user_id,
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

		public void MakePay(request request, DateTime date)
		{
			request.paid = true;
			request.pay_date = date;
			warehouse warehouse = request.user.warehouse;
			if (!db.Entry(warehouse).Collection(x => x.leftovers).IsLoaded)
				db.Entry(warehouse).Collection(x => x.leftovers).Load();
			foreach (var request_row in request.request_rows)
			{
				leftover foundLeftover = warehouse.leftovers.SingleOrDefault(x => x.good_id == request_row.goods_id.Value);
				if (foundLeftover != null)
				{
					foundLeftover.expenditure -= request_row.amount;
					db.Entry(foundLeftover).State=EntityState.Modified;
				}
			}
		}

		public void AddRequestRow(request request, request_rows requestRow)
		{
			request.request_rows.Add(requestRow);
			requestRow.request = request;
		}

		public void CalculateCost(request request)
		{
			request.cost = 0;
			foreach (var requestRow in request.request_rows)
			{
				request.cost += requestRow.amount * requestRow.price;
			}
		}

		public IList<request> GetRequestsByDate(List<int> buyers, DateTime start, DateTime finish)
		{
			finish = finish.AddDays(1);
			return db.requests.Include("user.warehouse.postoffice.post").Where(x => x.date >= start && x.date < finish && buyers.Contains(x.buyer_id.Value)).ToList();
		}
		public IList<request> GetPenaltyRequestsByDate(List<int> buyers, DateTime start, DateTime finish)
		{
			finish = finish.AddDays(1);
			return db.requests.Include("user.warehouse.postoffice.post").Where(x => (!x.paid.Value || x.paid.Value && x.pay_date.Value>=finish) && SqlFunctions.DateAdd("day",30,x.date)<finish && buyers.Contains(x.buyer_id.Value)).ToList();
		}
	}
}