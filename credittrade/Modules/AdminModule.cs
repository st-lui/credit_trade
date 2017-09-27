using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using DataAccess;
using DbModel;
using Nancy;
using Nancy.Security;
using System.Globalization;
using System.IO;
using System.Data.SqlTypes;

namespace credittrade.Modules
{
	public class AdminModule : NancyModule
	{
		public AdminModule() : base("/admin")
		{
			this.RequiresClaims("Admin");
			dynamic model = new ExpandoObject();
			Get["/"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var buyers = unitOfWork.Posts.GetBuyers(currentUser.warehouse.postoffice.post_id);
					model.Buyers = buyers;
					return View["buyers", model];
				}
			};

			Get["/buyers/add"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var warehouses = unitOfWork.Posts.GetWarehouses(currentUser.warehouse.postoffice.post_id);
					model.Warehouses = warehouses;
					return View["buyer_add", model];
				}

			};

			Post["/buyers/add"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					string limit = Request.Form.limit;
					limit = limit.Replace(',', '.');
					decimal lim = decimal.Parse(limit, CultureInfo.GetCultureInfo("en-US"));
					var b = unitOfWork.Users.CreateBuyer(currentUser, Request.Form.fio, Request.Form.warehouse,
						Request.Form.contract_number, Request.Form.contract_date, lim);
					var br = unitOfWork.Buyers;
					br.Add(b);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/admin");
				}
			};
			Get["/buyers/edit/{buyer_id}"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					model.Buyer = unitOfWork.Buyers.Get((int)p["buyer_id"]);
					model.Warehouses = unitOfWork.Posts.GetWarehouses(currentUser.warehouse.postoffice.post_id);
					if (model.Buyer == null)
						return new NotFoundResponse();
					return View["buyer_edit", model];
				}

			};

			Post["/buyers/edit/{buyer_id}"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					buyer Buyer = unitOfWork.Buyers.Get((int)p["buyer_id"]);
					if (Buyer == null)
						return new NotFoundResponse();
					string limit = Request.Form.limit;
					limit = limit.Replace(',', '.');
					decimal lim = decimal.Parse(limit, CultureInfo.GetCultureInfo("en-US"));
					Buyer.fio = Request.Form.fio;
					Buyer.warehouse_id = Request.Form.warehouse;
					Buyer.contract_number = Request.Form.contract_number;
					Buyer.contract_date = Request.Form.contract_date;
					Buyer.limit = lim;
					unitOfWork.Buyers.Change(Buyer);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/admin");
				}
			};

			Get["/reports"] = p =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					if (currentUser.warehouse.postoffice.post.privilegies == 1)
					{
						int postid = currentUser.warehouse.postoffice.post.id;
						var postList = unitOfWork.Posts.GetAll().Where(x => x.parent_id == postid);
						Dictionary<string, string> debt = new Dictionary<string, string>();
						foreach (post post in postList)
						{
							decimal debtValue = 0;
							var warehouses = unitOfWork.Posts.GetWarehouses(post.id);

							foreach (var wh in warehouses)
							{
								decimal cost = unitOfWork.Warehouses.GetNotPaidRequests(wh.id).Sum(x => x.cost).Value;
								if (cost > 0)
									debtValue += cost;
							}
							debt.Add(post.name, debtValue.ToString(CultureInfo.GetCultureInfo("ru-RU")));
						}
						model.Debt = debt;
						return View["report_ufps", model];
					}
					else
					{
						post post = currentUser.warehouse.postoffice.post;
						model.Post = post.name;
						var warehouses = unitOfWork.Posts.GetWarehouses(post.id);
						Dictionary<string, string> debt = new Dictionary<string, string>();
						foreach (var wh in warehouses)
						{
							var cost = unitOfWork.Warehouses.GetNotPaidRequests(wh.id).Sum(x => x.cost);
							if (cost > 0)
								debt.Add($"{wh.postoffice.idx} {wh.name}", cost.Value.ToString(CultureInfo.GetCultureInfo("ru-RU")));
						}
						model.Debt = debt;
						return View["report1", model];
					}
				}

			};
			Get["/report_mrc"] = param =>
			{
				return Response.AsRedirect("~/admin/report_mrc/7");
			};
			Get["/report_mrc/{mon}"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					List<MrcReportRow> reportRows = new List<MrcReportRow>();
					var parents = unitOfWork.Posts.GetAll().Where(x => x.privilegies == 1).OrderBy(x => x.name);
					int mon = (int)param["mon"];
					DateTime finish = DateTime.Today;
					DateTime start = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
					switch (mon)
					{
						case 6:start = new DateTime(2017, 6, 1, 0, 0, 0); finish = new DateTime(2017, 6, 30, 23, 59, 59); break;
						case 7:start = new DateTime(2017, 7, 1, 0, 0, 0); finish = new DateTime(2017, 7, 31, 23, 59, 59); break;
						case 8:start = new DateTime(2017, 8, 1, 0, 0, 0); finish = new DateTime(2017, 8, 31, 23, 59, 59); break;
						case 9: start = new DateTime(2017, 9, 1, 0, 0, 0); finish = DateTime.Today.AddDays(1).AddSeconds(-1); break;
					}
					foreach (var ufps in parents)
					{
						MrcReportRow reportRow = new MrcReportRow();
						reportRow.parent = ufps;
						reportRow.name = ufps.name;
						reportRow.children = new List<MrcReportRow>();
						var postList = unitOfWork.Posts.GetAll().Where(x => x.parent_id == ufps.id);
						decimal spent = 0, debt = 0, debtover = 0;
						foreach (post post in postList)
						{
							MrcReportRow reportRow_child = new MrcReportRow();
							reportRow_child.parent = post;
							reportRow_child.name = post.name;
							List<buyer> buyersPost = new List<buyer>();
							IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(post.id);
							buyersPost.AddRange(buyers);
							var buyerIds = buyersPost.Select(x => x.id).ToList();
							IList<request> reqs = unitOfWork.Requests.GetRequestsByDate(buyerIds, start, finish);
							IList<request> reqsPen = unitOfWork.Requests.GetPenaltyRequestsByDate(buyerIds, SqlDateTime.MinValue.Value, finish);
							foreach (request req in reqs)
							{
								reportRow_child.spent += req.cost.Value;
								if (!req.paid.Value || req.pay_date > finish)
								{
									reportRow_child.debt += req.cost.Value;
								}
								if (req.paid.Value && req.pay_date.Value >= start && req.pay_date <= finish)
									reportRow_child.paid += req.cost.Value;
							}
							foreach (request reqPen in reqsPen)
								reportRow_child.debtOverdue += reqPen.cost.Value;
							reportRow.spent += reportRow_child.spent;
							reportRow.debt += reportRow_child.debt;
							reportRow.paid += reportRow_child.paid;
							reportRow.debtOverdue += reportRow_child.debtOverdue;
							reportRow.children.Add(reportRow_child);
						}
						reportRows.Add(reportRow);
					}

					model.reportRows = reportRows;
					string monStr = "";
					switch (mon)
					{
						case 6:monStr = "Июнь 2017";break;
						case 7:
							monStr = "Июль 2017"; break;
						case 8:
							monStr = "Август 2017"; break;
						case 9: monStr = "Сентябрь 2017"; break;
					}
					model.monStr = monStr;
					return View["report_mrc", model];
				}
			};
			Get["/report_sells"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					post post = currentUser.warehouse.postoffice.post;
					model.Post = post.name;
					model.Post_id = post.id;
					var today = DateTime.Today;
					string StartDate = new DateTime(today.Year, today.Month, 1).ToString("d", CultureInfo.InvariantCulture);
					string FinishDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)).ToString("d", CultureInfo.InvariantCulture);
					model.StartDate = StartDate;
					model.FinishDate = FinishDate;

				}
				return View["report2", model];
			};
			Post["/report_sells"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime start = DateTime.Parse(Request.Form["date_start"]);
					DateTime finish = DateTime.Parse(Request.Form["date_finish"]);
					int postId = int.Parse(Request.Form["post"]);
					MemoryStream ms = new MemoryStream();
					post post = unitOfWork.Posts.Get(postId);
					List<buyer> buyersPost = new List<buyer>();
					//если уфпс
					if (post.privilegies == 1)
					{
						foreach (post p in post.children)
						{
							IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(p.id);
							buyersPost.AddRange(buyers);
						}
					}
					// иначе почтамт
					else
					{
						IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(post.id);
						buyersPost.AddRange(buyers);
					}
					var buyerIds = buyersPost.Select(x => x.id).ToList();
					// заявки периода
					IList<request> reqs = unitOfWork.Requests.GetRequestsByDate(buyerIds, start, finish);
					// заявки до периода
					IList<request> reqsBefore = unitOfWork.Requests.GetRequestsByDate(buyerIds, SqlDateTime.MinValue.Value, start.AddDays(-1));
					// заявки на конец периода
					IList<request> reqsAfter = unitOfWork.Requests.GetRequestsByDate(buyerIds, SqlDateTime.MinValue.Value, finish);
					// просроченные заявки на конец периода
					IList<request> reqsPenalty = unitOfWork.Requests.GetPenaltyRequestsByDate(buyerIds, SqlDateTime.MinValue.Value, finish);
					// просроченные заявки на начало периода
					IList<request> reqsPenaltyBefore = unitOfWork.Requests.GetPenaltyRequestsByDate(buyerIds, SqlDateTime.MinValue.Value, start.AddDays(-1)); 

					InOutReportModel reportModel = new InOutReportModel();
					reportModel.Start = Request.Form["date_start"];
					reportModel.Finish = Request.Form["date_finish"];
					reportModel.StartDate = start;
					reportModel.FinishDate = finish;
					reportModel.RequestsBefore = reqsBefore;
					reportModel.RequestsCurrent = reqs;
					reportModel.RequestsAfter = reqsAfter;
					reportModel.RequestsPenaltyAfter = reqsPenalty;
					reportModel.RequestsPenaltyBefore = reqsPenaltyBefore;
					ms = Utils.GenReportUfps(reportModel);
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attachment; filename=report_sells.xlsx");
				}
			};


			Get["/report_goods"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					post post = currentUser.warehouse.postoffice.post;
					model.Post = post.name;
					model.Post_id = post.id;
					var today = DateTime.Today;
					string StartDate = new DateTime(today.Year, today.Month, 1).ToString("d", CultureInfo.InvariantCulture);
					string FinishDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)).ToString("d", CultureInfo.InvariantCulture);
					model.StartDate = StartDate;
					model.FinishDate = FinishDate;

				}
				return View["report_goods", model];
			};

			Post["/report_goods"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime start = DateTime.Parse(Request.Form["date_start"]);
					DateTime finish = DateTime.Parse(Request.Form["date_finish"]);
					int postId = int.Parse(Request.Form["post"]);
					MemoryStream ms = new MemoryStream();
					post post = unitOfWork.Posts.Get(postId);

					List<buyer> buyersPost = new List<buyer>();
					if (post.privilegies == 1)
					{
						foreach (post p in post.children)
						{
							IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(p.id);
							buyersPost.AddRange(buyers);
						}
					}
					// иначе почтамт
					else
					{
						IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(post.id);
						buyersPost.AddRange(buyers);
					}
					var buyerIds = buyersPost.Select(x => x.id).ToList();
					IList<request> reqs = unitOfWork.Requests.GetRequestsWithRowsByDate(buyerIds, start, finish);
					InOutReportModel reportModel = new InOutReportModel();
					reportModel.Start = Request.Form["date_start"];
					reportModel.Finish = Request.Form["date_finish"];
					reportModel.RequestsCurrent = reqs;
					ms = Utils.GenReportGoods(reportModel," выданным в кредит");
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attachment; filename=report_goods.xlsx");
				}
			};

			Get["/report_goods_paid"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					post post = currentUser.warehouse.postoffice.post;
					model.Post = post.name;
					model.Post_id = post.id;
					var today = DateTime.Today;
					string StartDate = new DateTime(today.Year, today.Month, 1).ToString("d", CultureInfo.InvariantCulture);
					string FinishDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)).ToString("d", CultureInfo.InvariantCulture);
					model.StartDate = StartDate;
					model.FinishDate = FinishDate;

				}
				return View["report_goods_paid", model];
			};

			Post["/report_goods_paid"] = param =>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					DateTime start = DateTime.Parse(Request.Form["date_start"]);
					DateTime finish = DateTime.Parse(Request.Form["date_finish"]);
					int postId = int.Parse(Request.Form["post"]);
					MemoryStream ms = new MemoryStream();
					post post = unitOfWork.Posts.Get(postId);

					List<buyer> buyersPost = new List<buyer>();
					if (post.privilegies == 1)
					{
						foreach (post p in post.children)
						{
							IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(p.id);
							buyersPost.AddRange(buyers);
						}
					}
					// иначе почтамт
					else
					{
						IEnumerable<buyer> buyers = unitOfWork.Posts.GetBuyers(post.id);
						buyersPost.AddRange(buyers);
					}
					var buyerIds = buyersPost.Select(x => x.id).ToList();
					IList<request> reqs = unitOfWork.Requests.GetPaidRequestsWithRowsByDate(buyerIds, start, finish);
					InOutReportModel reportModel = new InOutReportModel();
					reportModel.Start = Request.Form["date_start"];
					reportModel.Finish = Request.Form["date_finish"];
					reportModel.RequestsCurrent = reqs;
					ms = Utils.GenReportGoods(reportModel, " оплаченным");
					return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attachment; filename=report_paid.xlsx");
				}
			};

		}
	}
}
public class ByteArrayResponse : Response
{
	/// <summary>
	/// Byte array response
	/// </summary>
	/// <param name="body">Byte array to be the body of the response</param>
	/// <param name="contentType">Content type to use</param>
	public ByteArrayResponse(byte[] body, string contentType = null)
	{
		this.ContentType = contentType ?? "application/octet-stream";

		this.Contents = stream =>
		{
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(body);
				writer.Close();
			}
		};
	}
}


public static class Extensions
{
	public static Response FromByteArray(this IResponseFormatter formatter, byte[] body, string contentType = null)
	{
		int zeros = 0;	
		for (int i = body.Length - 1; body[i] == 0; i--)
		{
			zeros++;
		}
		byte[] newBody = new byte[body.Length - zeros];
		for (int i = 0; i < body.Length - zeros; i++)
			newBody[i] = body[i];
		return new ByteArrayResponse(newBody, contentType);
	}
	public static Response FromStream(this IResponseFormatter formatter, Stream sourceStream, string contentType,string contentDisposition)
	{
		sourceStream.Position = 0;
		Response r = formatter.FromStream(sourceStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
		r.Headers.Add("content-disposition",contentDisposition);
		return r;
	}
}
