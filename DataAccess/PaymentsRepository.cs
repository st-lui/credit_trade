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
			return new payment()
			{
				amount = amount,
				request_row_id = request_row_id,
				date=date
			};
		}

		public payment CreatePayment(request_rows rr, decimal amount, DateTime date)
		{
			int request_row_id = rr.id;
			return new payment()
			{
				amount = amount,
				request_row_id = request_row_id,
				date = date
			};
		}
	}
}