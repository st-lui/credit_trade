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
namespace credittrade.Modules
{
	public class AdminModule:NancyModule
	{
		public AdminModule():base("/admin")
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
					return View["buyer_add",model];
				}
				
			};

			Post["/buyers/add"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					string limit = Request.Form.limit;
					limit=limit.Replace(',', '.');
					decimal lim = decimal.Parse(limit,CultureInfo.GetCultureInfo("en-US"));
					var b = unitOfWork.Users.CreateBuyer(currentUser, Request.Form.fio,Request.Form.warehouse,Request.Form.contract_number,Request.Form.contract_date,lim);
					var br = unitOfWork.Buyers;
					br.Add(b);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/admin");
				}
			};
			Get["/buyers/edit/{buyer_id}"]=p=>
			{
				using (UnitOfWork unitOfWork = (UnitOfWork) Context.Items["unitofwork"])
				{
					user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
					model.Buyer= unitOfWork.Buyers.Get((int)p["buyer_id"]);
					model.Warehouses = unitOfWork.Posts.GetWarehouses(currentUser.warehouse.postoffice.post_id);
					if (model.Buyer ==null)
						return new NotFoundResponse();
					return View["buyer_edit",model];
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
					decimal lim = decimal.Parse(limit,CultureInfo.GetCultureInfo("en-US"));
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
					if (currentUser.warehouse.postoffice.idx == "656700")
					{
						var postList = unitOfWork.Posts.GetAll().Where(x => x.name.Contains("почтамт"));
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
		}
	}
}