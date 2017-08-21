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
						var postList = unitOfWork.Posts.GetAll().Where(x => x.parent_id==postid);
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
					// просроченные заявки на конец периода
					IList<request> reqsPenalty = unitOfWork.Requests.GetPenaltyRequestsByDate(buyerIds, start, finish);
					// просроченные заявки на начало периода
					IList<request> reqsPenaltyBefore = unitOfWork.Requests.GetPenaltyRequestsByDate(buyerIds, SqlDateTime.MinValue.Value, start.AddDays(-1));

					InOutReportModel reportModel = new InOutReportModel();
					reportModel.Start = Request.Form["date_start"];
					reportModel.Finish = Request.Form["date_finish"];
					reportModel.Requests = reqs;
					reportModel.RequestsBefore = reqsBefore;
					reportModel.RequestsPenalty = reqsPenalty;
					reportModel.BeforeRequestsPenalty = reqsPenaltyBefore;
					ms = Utils.GenReportUfps(reportModel);
					//return Response.FromStream(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
					//return Response.FromByteArray(ms.GetBuffer(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");


					return Response.FromByteArray(ms.GetBuffer(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
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
			}
		};
	}
}


public static class Extensions
{
	public static Response FromByteArray(this IResponseFormatter formatter, byte[] body, string contentType = null)
	{
		return new ByteArrayResponse(body, contentType);
	}
}
