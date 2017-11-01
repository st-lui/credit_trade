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

		public pay GetWithData(int id)
		{
			return db.pays.Include("payments.request_rows").Include("request.buyer").Include("request.user.warehouse.postoffice.post").SingleOrDefault(x => x.id == id);
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

		/// <summary>
		/// Расчет стоимости частичного платежа
		/// </summary>
		/// <param name="pay">Частичнный платеж для расчета стоимости</param>
		public void CalculateCost(pay pay)
		{
			pay.cost = 0;
			foreach (var payment in pay.payments)
			{
				pay.cost += Math.Round(payment.amount* payment.request_rows.price.Value, 2, MidpointRounding.AwayFromZero);
			}
		}

	}
}