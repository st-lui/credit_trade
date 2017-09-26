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
					model.requests = unitOfWork.Users.GetRequests(currentUser);
					return View["index", model];
				}
			};
			Get["/requests/view/{request_id}"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					model.request = unitOfWork.Requests.Get(p.request_id);

					return View["request_view", model];
				}
			};
			Get["/requests/print/{request_id}"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					request request = unitOfWork.Requests.Get(p.request_id);
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
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					model.request = unitOfWork.Requests.Get(p.request_id);

					return View["request_makepay", model];
				}
			};
			Post["/requests/makepay/{request_id}"] = p =>
			{
				this.ValidateCsrfToken();
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime pay_date = Request.Form.pay_date;
					var request = unitOfWork.Requests.Get(p.request_id);
					unitOfWork.Requests.MakePay(request, pay_date);
					unitOfWork.Requests.Change(request);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/");
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

			Post["/report_goods.xlsx"] = param =>
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
					ms = Utils.GenReportGoods(reportModel," выданным в кредит");
					ms.Position = 0;
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
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

			Post["/report_goods_paid.xlsx"] = param =>
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
					ms.Position = 0;
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
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

			Post["/report_leftovers.xlsx"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					int warehouseId = int.Parse(Request.Form["post"]);
					IList<leftover> lefts = unitOfWork.Leftovers.GetWithGoodsForWareHouse(warehouseId);
					LeftoversReportModel reportModel = new LeftoversReportModel();
					reportModel.leftovers = lefts;
					var ms = Utils.GenReportLeftovers(reportModel,"");
					ms.Position = 0;
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
				}
			};
		}
	}
}