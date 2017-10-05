using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;
using System;

namespace DataAccess
{
	public class PaysRepository : IRepository<pay>
	{
		private DataContext db;

		public PaysRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<pay> GetAll()
		{
			return db.pays;
		}

		public void Add(pay item)
		{
			db.pays.Add(item);
		}

		public void Change(pay item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(pay item)
		{
			db.pays.Remove(item);
		}

		public pay Get(int id)
		{
			return db.pays.SingleOrDefault(x => x.id == id);
		}

		public pay CreatePay(request request, DateTime date)
		{
			int request_id = request.id;

			pay pay=new pay()
			{
				request_id= request_id,
				date = date
			};
			request.pays.Add(pay);
			return pay;
		}

		public void AddPayment(pay pay,payment payment)
		{
			pay.payments.Add(payment);
			payment.pay = pay;
		}

	}
}