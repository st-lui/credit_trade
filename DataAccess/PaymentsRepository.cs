using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;
using System;

namespace DataAccess
{
	public class PaymentsRepository : IRepository<payment>
	{
		private DataContext db;

		public PaymentsRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<payment> GetAll()
		{
			return db.payments;
		}

		public void Add(payment item)
		{
			db.payments.Add(item);
		}

		public void Change(payment item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(payment item)
		{
			db.payments.Remove(item);
		}

		public payment Get(int id)
		{
			return db.payments.SingleOrDefault(x => x.id == id);
		}

		public payment CreatePayment(request request, good good, decimal amount, DateTime date)
		{
			var requestRow = request.request_rows.Where(x => x.goods_id.Value == good.id).SingleOrDefault();
			if (requestRow == null)
				return null;
			int request_row_id = requestRow.id;
			payment p =new payment()
			{
				amount = amount,
				request_row_id = request_row_id,
				date=date
			};
			requestRow.payments.Add(p);
			return p;
		}

		public payment CreatePayment(request_rows rr, decimal amount, DateTime date)
		{
			int request_row_id = rr.id;
			payment p=new payment()
			{
				amount = amount,
				request_row_id = request_row_id,
				date = date
			};
			rr.payments.Add(p);
			return p;
		}
		/// <summary>
		/// Создает объект частиный платеж, устанавливает связь с объектами строка заказа и оплата
		/// </summary>
		/// <param name="rr"></param>
		/// <param name="amount"></param>
		/// <param name="date"></param>
		/// <param name="pay"></param>
		/// <returns></returns>
		public payment CreatePayment(request_rows rr, decimal amount, DateTime date,pay pay)
		{
			int request_row_id = rr.id;
			payment p = new payment()
			{
				amount = amount,
				request_row_id = request_row_id,
				date = date,
				pay_id=pay.id
			};
			rr.payments.Add(p);
			pay.payments.Add(p);
			return p;
		}
	}
}