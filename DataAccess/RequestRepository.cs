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
			return db.requests.Include("buyer").Include("pays").Include("request_rows.payments").Include("user.warehouse.postoffice.post").SingleOrDefault(x => x.id == id);
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
			///TODO: предусмотреть полную оплату при существующей частичной
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
					db.Entry(foundLeftover).State = EntityState.Modified;
				}
			}
		}
		/// <summary>
		///  Оплата остатка товара по заказу
		/// </summary>
		/// <param name="request">Заказ</param>
		/// <param name="date">Дата оплаты</param>
		/// <param name="payments">Репозиторий платежей</param>
		/// <param name="pays">Репозиторий оплат</param>
		public void MakePay(request request, DateTime date, PaymentsRepository payments, PaysRepository pays)
		{
			pay pay = pays.CreatePay(request, date);
			foreach (var request_row in request.request_rows)
			{
				decimal amount = request_row.amount.Value - request_row.paid_amount - request_row.return_amount;
				MakePartialPay(request, request_row, amount, date, payments, pay);
			}
			pays.CalculateCost(pay);
			pays.Add(pay);
		}

		/// <summary>
		///  Возврат остатка товара по заказу
		/// </summary>
		/// <param name="request">Заказ</param>
		/// <param name="date">Дата оплаты</param>
		/// <param name="payments">Репозиторий платежей</param>
		/// <param name="pays">Репозиторий оплат</param>
		public void MakePayWithReturn(request request, DateTime date, PaymentsRepository payments, PaysRepository pays)
		{
			pay pay = pays.CreatePay(request, date);
			foreach (var request_row in request.request_rows)
			{
				decimal amount = request_row.amount.Value - request_row.paid_amount - request_row.return_amount;
				amount *= -1;
				MakePartialPay(request, request_row, amount, date, payments,pay);
			}
			pays.CalculateCost(pay);
			pays.Add(pay);
		}
		/// <summary>
		/// Создание частичного платежа или возврата
		/// </summary>
		/// <param name="request">Заказ</param>
		/// <param name="request_row">Строка заказа</param>
		/// <param name="amount">Количество товара, если положительное, то продажа, если отрицательное, то возврат</param>
		/// <param name="date">Дата частичного платежа</param>
		/// <param name="payments">Репозиторий частичных платежей</param>
		/// <param name="pay">Оплата</param>
		public void MakePartialPay(request request, request_rows request_row, decimal amount, DateTime date, PaymentsRepository payments, pay pay)
		{
			payment payment = payments.CreatePayment(request_row, amount, date,pay);
			payments.Add(payment);
			if (payment.amount > 0)
				request_row.paid_amount += payment.amount;
			else
			if (payment.amount < 0)
				request_row.return_amount += -payment.amount;
			db.Entry(request_row).State = EntityState.Modified;
			warehouse warehouse = request.user.warehouse;
			if (!db.Entry(warehouse).Collection(x => x.leftovers).IsLoaded)
				db.Entry(warehouse).Collection(x => x.leftovers).Load();
			leftover foundLeftover = warehouse.leftovers.SingleOrDefault(x => x.good_id == request_row.goods_id.Value);
			if (foundLeftover != null)
			{
				foundLeftover.expenditure -= payment.amount;
				db.Entry(foundLeftover).State = EntityState.Modified;
			}
			MakePayIfPaid(request, date);
		}

		/// <summary>
		/// Проверка полной оплаты заказа по количеству в частичных платежах
		/// </summary>
		/// <param name="request"></param>
		/// <param name="date"></param>
		public void MakePayIfPaid(request request, DateTime date)
		{
			bool paid = true;
			foreach (request_rows rr in request.request_rows)
				if (rr.paid_amount + rr.return_amount != rr.amount)
				{
					paid = false;
					break;
				}
			if (paid)
			{
				request.paid = true;
				request.pay_date = date;
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
				request.cost += Math.Round(requestRow.amount.Value * requestRow.price.Value,2,MidpointRounding.AwayFromZero);
			}
		}

		public IList<request> GetRequestsByDate(List<int> buyers, DateTime start, DateTime finish)
		{
			finish = finish.AddDays(1);
			return db.requests.Include("user.warehouse.postoffice.post").Where(x => x.date >= start && x.date < finish && buyers.Contains(x.buyer_id.Value)).ToList();
		}

		public IList<request> GetRequestsWithRowsByDate(List<int> buyers, DateTime start, DateTime finish)
		{
			finish = finish.AddDays(1);
			return db.requests.Include("request_rows").Include("user.warehouse.postoffice.post").Where(x => x.date >= start && x.date < finish && buyers.Contains(x.buyer_id.Value)).ToList();
		}

		public IList<request> GetPaidRequestsWithRowsByDate(List<int> buyers, DateTime start, DateTime finish)
		{
			finish = finish.AddDays(1);
			return db.requests.Include("request_rows").Include("user.warehouse.postoffice.post").Where(x => x.paid.Value && x.pay_date >= start && x.pay_date < finish && buyers.Contains(x.buyer_id.Value)).ToList();
		}

		public IList<request> GetPenaltyRequestsByDate(List<int> buyers, DateTime start, DateTime finish)
		{
			finish = finish.AddDays(1);
			return db.requests.Include("user.warehouse.postoffice.post").Where(x => x.date.Value >= start && x.date < finish && (!x.paid.Value || x.paid.Value && x.pay_date.Value >= finish) && SqlFunctions.DateAdd("day", 30, x.date) < finish && buyers.Contains(x.buyer_id.Value)).ToList();
		}
	}
}