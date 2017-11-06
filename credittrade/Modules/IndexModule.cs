using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web;
using System.Web.Helpers;
using DataAccess;
using DbModel;
using Nancy;
using Nancy.Security;
using System.Globalization;
using System.IO;
using System.Linq;
using credittrade.ViewModel;

namespace credittrade.Modules
{
	public class IndexModule : NancyModule
	{
		public IndexModule()
		{
			this.RequiresAuthentication();
			dynamic model = new ExpandoObject();
			Get["/"] = _ =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					model.username = currentUser.username;
					var requests = unitOfWork.Users.GetRequests(currentUser);
					List<RequestViewModel> list = new List<RequestViewModel>();
					foreach (request req in requests)
					{
						RequestViewModel rvm = new RequestViewModel();
						rvm.id = req.id.ToString();
						rvm.buyerFio = req.buyer.fio;
						rvm.date = req.date.Value.ToString("dd.MM.yyyy");
						rvm.cost = req.cost.ToString();
						rvm.fullpaid = req.paid.Value;
						rvm.fullpaidmessage = rvm.fullpaid ? "Да" : "Нет";
						rvm.pay_date = req.pay_date.HasValue ? req.pay_date.Value.ToString("dd.MM.yyyy HH:mm") : "-";
                        rvm.paidsum= "-";
                        if (!rvm.fullpaid)
                            rvm.paidsum = req.pays.Where(x => x.cost > 0).Sum(x => x.cost).ToString("f2");
						string returned = req.pays.Where(x => x.cost > 0).Sum(x => x.cost).ToString("f2");
						list.Add(rvm);
					}

					model.requests = list;
					return View["index", model];
				}
			};
			Get["/requests/view/{request_id}"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					model.request = unitOfWork.Requests.Get(p.request_id);
					if (model.request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					return View["request_view", model];
				}
			};
			Get["/requests/print/{request_id}"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					request request = unitOfWork.Requests.Get(p.request_id);
					if (request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					model.request = request;
					return View["request_print", model];
				}
			};

			//Get["/buyers/list"] = p =>
			//{
			//	user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
			//	using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
			//	{
			//		var buyers = unitOfWork.Users.GetBuyers(currentUser);
			//		model.Buyers = buyers;
			//		return View["buyers", model];
			//	}
			//};
			//Get["/buyers/add"] = p =>
			//{	
			//	return View["buyer_add"];
			//};
			//Post["/buyers/add"] = p =>
			//{
			//	this.ValidateCsrfToken();
			//	user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
			//	using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
			//	{
			//		var b = unitOfWork.Users.CreateBuyer(currentUser, Request.Form.fio);
			//		var br = unitOfWork.Buyers;
			//		br.Add(b);
			//		unitOfWork.SaveChanges();
			//		return Response.AsRedirect("~/buyers/list");
			//	}
			//};
			Get["/requests/add"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var leftovers = unitOfWork.Warehouses.GetLeftovers(currentUser.warehouse);
					model.leftovers = leftovers;
					model.buyers = unitOfWork.Buyers.GetBuyers(currentUser.warehouse_id.Value);
					return View["request_add", model];
				}
			};

			Post["/requests/add"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var request_info = Request.Form.request_info;
					int buyer_id = Request.Form.buyer;
					DateTime dt = Request.Form.datecreated;
					dynamic request_data = Json.Decode(request_info);
					request request = unitOfWork.Requests.CreateRequest(dt, currentUser.username, "", currentUser.id, buyer_id);
					decimal cost = 0;
					foreach (KeyValuePair<string, dynamic> row in request_data)
					{
						leftover leftover = unitOfWork.Leftovers.GetWithGoods(int.Parse(row.Value["id"]));
						request_rows rr = unitOfWork.RequestRows.CreateRequestRows(row.Value["amount"], leftover.good.name, leftover.good.edizm, leftover.good_id,
							//leftover.price == 0 ? leftover.good.price : leftover.price, leftover.good.barcode);
							leftover.price, leftover.good.barcode);
						leftover.expenditure += rr.amount;
						unitOfWork.Leftovers.Change(leftover);
						unitOfWork.Requests.AddRequestRow(request, rr);
					}
					unitOfWork.Requests.CalculateCost(request);
					unitOfWork.Requests.Add(request);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/");
				}
			};
			Get["/requests/makepay/{request_id}"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					request request = unitOfWork.Requests.Get(p.request_id);
					if (request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					RequestDetailsViewModel rdvm = new RequestDetailsViewModel();
					rdvm.id = request.id.ToString();
					rdvm.user = request.user;
					rdvm.buyerFio = request.buyer.fio;
					rdvm.date = request.date.Value.ToString("dd.MM.yyyy HH:mm");
					decimal cost = 0;
					rdvm.request_rows = new List<request_rows>();
					foreach (request_rows rr in request.request_rows)
					{
						if (rr.amount - rr.paid_amount - rr.return_amount > 0)
						{
							request_rows new_rr = new request_rows();
							new_rr.id = rr.id;
							new_rr.amount = rr.amount - rr.paid_amount - rr.return_amount;
							new_rr.barcode = rr.barcode;
							new_rr.ed_izm = rr.ed_izm;
							new_rr.goods_id = rr.goods_id;
							new_rr.name = rr.name;
							new_rr.price = rr.price;
							cost += Math.Round(new_rr.price.Value * new_rr.amount.Value, 2, MidpointRounding.AwayFromZero);
							rdvm.request_rows.Add(new_rr);
						}
					}
					rdvm.cost = cost.ToString("f2");
					rdvm.paid_cost = request.pays.Where(x => x.cost > 0).Sum(x => x.cost).ToString("f2");
					rdvm.returned_cost = request.pays.Where(x => x.cost < 0).Sum(x => x.cost).ToString("f2");
					if (rdvm.paid_cost != "")
						rdvm.partial_paid = true;
					model.request = rdvm;
					return View["request_makepay", model];
				}
			};
			Post["/requests/makepay/{request_id}"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime pay_date = Request.Form.pay_date;
					var request = unitOfWork.Requests.Get(p.request_id);
					if (request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					unitOfWork.Requests.MakePay(request, pay_date, unitOfWork.Payments, unitOfWork.Pays);
					unitOfWork.Requests.Change(request);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/");
				}
			};

			Get["/requests/makepay_partial/{request_id}"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					model.request = unitOfWork.Requests.Get(p.request_id);
					if (model.request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					return View["request_makepay_partial", model];
				}
			};

			Post["/requests/makepay_partial/{request_id}"] = p =>
			{
				//this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var request_info = Request.Form.request_info;
					DateTime datecreated = Request.Form.datecreated;
					dynamic request_data = Json.Decode(request_info);
					decimal cost = 0;
					request request = unitOfWork.Requests.Get(p.request_id);
					if (request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					pay pay = unitOfWork.Pays.CreatePay(request, datecreated);
					foreach (KeyValuePair<string, dynamic> payment in request_data)
					{
						unitOfWork.Requests.MakePartialPay(request, request.request_rows.First(x => x.id == int.Parse(payment.Value["id"])), payment.Value["amount"], datecreated, unitOfWork.Payments, pay);
					}
					unitOfWork.Pays.CalculateCost(pay);
					unitOfWork.SaveChanges();
					return pay.id.ToString();
				}
			};

			Get["/requests/print_partial/{pay_id}"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					pay pay = unitOfWork.Pays.GetWithData(p.pay_id);
					if (pay.request.user.warehouse_id != currentUser.warehouse_id)
						return View["access_denied"];
					model.pay = pay;
					return View["request_print_partial", model];
				}
			};


			Get["/reports"] = p =>
			{
				return View["reports"];
			};
			Get["/report_goods"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					warehouse warehouse = currentUser.warehouse;
					model.Post = warehouse.name;
					model.Post_id = warehouse.id;
					var today = DateTime.Today;
					string StartDate = new DateTime(today.Year, today.Month, 1).ToString("d", CultureInfo.InvariantCulture);
					string FinishDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)).ToString("d", CultureInfo.InvariantCulture);
					model.StartDate = StartDate;
					model.FinishDate = FinishDate;
					model.Layout = "layout.cshtml";

				}
				return View["report_goods", model];
			};

			Post["/report_goods"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime start = DateTime.Parse(Request.Form["date_start"]);
					DateTime finish = DateTime.Parse(Request.Form["date_finish"]);
					int warehouseId = int.Parse(Request.Form["post"]);
					MemoryStream ms = new MemoryStream();
					warehouse warehouse = unitOfWork.Warehouses.GetWithBuyers(warehouseId);

					List<buyer> buyersPost = warehouse.buyers.ToList();
					var buyerIds = buyersPost.Select(x => x.id).ToList();
					IList<request> reqs = unitOfWork.Requests.GetRequestsWithRowsByDate(buyerIds, start, finish);
					InOutReportModel reportModel = new InOutReportModel();
					reportModel.Start = Request.Form["date_start"];
					reportModel.Finish = Request.Form["date_finish"];
					reportModel.RequestsCurrent = reqs;
					ms = Utils.GenReportGoods(reportModel, " выданным в кредит");
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attachment; filename=report_goods.xlsx");
				}
			};

			Get["/report_goods_paid"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					warehouse warehouse = currentUser.warehouse;
					model.Post = warehouse.name;
					model.Post_id = warehouse.id;
					var today = DateTime.Today;
					string StartDate = new DateTime(today.Year, today.Month, 1).ToString("d", CultureInfo.InvariantCulture);
					string FinishDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)).ToString("d", CultureInfo.InvariantCulture);
					model.StartDate = StartDate;
					model.FinishDate = FinishDate;
					model.Layout = "layout.cshtml";

				}
				return View["report_goods_paid", model];
			};

			Post["/report_goods_paid"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime start = DateTime.Parse(Request.Form["date_start"]);
					DateTime finish = DateTime.Parse(Request.Form["date_finish"]);
					int warehouseId = int.Parse(Request.Form["post"]);
					MemoryStream ms = new MemoryStream();
					warehouse warehouse = unitOfWork.Warehouses.GetWithBuyers(warehouseId);

					List<buyer> buyersPost = warehouse.buyers.ToList();
					var buyerIds = buyersPost.Select(x => x.id).ToList();
					IList<request> reqs = unitOfWork.Requests.GetPaidRequestsWithRowsByDate(buyerIds, start, finish);
					InOutReportModel reportModel = new InOutReportModel();
					reportModel.Start = Request.Form["date_start"];
					reportModel.Finish = Request.Form["date_finish"];
					reportModel.RequestsCurrent = reqs;
					ms = Utils.GenReportGoods(reportModel, " оплаченным");
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attachment; filename=report_goods_paid.xlsx");
				}
			};
			Get["/report_leftovers"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					warehouse warehouse = currentUser.warehouse;
					model.Post = warehouse.name;
					model.Post_id = warehouse.id;
					var today = DateTime.Today;
					string StartDate = new DateTime(today.Year, today.Month, 1).ToString("d", CultureInfo.InvariantCulture);
					string FinishDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)).ToString("d", CultureInfo.InvariantCulture);
					model.StartDate = StartDate;
					model.FinishDate = FinishDate;
					model.Layout = "layout.cshtml";

				}
				return View["report_leftovers", model];
			};

			Post["/report_leftovers"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					int warehouseId = int.Parse(Request.Form["post"]);
					IList<leftover> lefts = unitOfWork.Leftovers.GetWithGoodsForWareHouse(warehouseId);
					LeftoversReportModel reportModel = new LeftoversReportModel();
					reportModel.leftovers = lefts;
					var ms = Utils.GenReportLeftovers(reportModel, "");
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attachment; filename=report_leftovers.xlsx");
				}
			};
		}
	}
}