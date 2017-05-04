﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web.Helpers;
using DataAccess;
using DbModel;
using Nancy;
using Nancy.Security;

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
				model.username = Context.CurrentUser.UserName;
				return View["index", model];
			};
			Get["/buyers/list"] = p =>
			{
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var buyers = unitOfWork.Users.GetBuyers(currentUser);
					model.Buyers = buyers;
					return View["buyers", model];
				}
			};
			Get["/buyers/add"] = p =>
			{
				return View["buyer_add"];
			};
			Post["/buyers/add"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var b = unitOfWork.Users.CreateBuyer(currentUser, Request.Form.fio);
					var br = unitOfWork.Buyers;
					br.Add(b);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/buyers/list");
				}
			};
			Get["requests/add"] = p =>
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

			Post["requests/add"] = p =>
			{
				this.ValidateCsrfToken();
				user currentUser = ((Bootstrapper.User)Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork)Context.Items["unitofwork"])
				{
					var request_info = Request.Form.request_info;
					int buyer_id = Request.Form.buyer;
					dynamic request_data= Json.Decode(request_info);
					request request= unitOfWork.Requests.CreateRequest(DateTime.Now, currentUser.username, "", currentUser.id, buyer_id);
					decimal cost = 0;
					foreach (KeyValuePair<string,dynamic> row in request_data)
					{
						leftover leftover = unitOfWork.Leftovers.GetWithGoods(int.Parse(row.Value["id"]));
						request_rows rr = unitOfWork.RequestRows.CreateRequestRows(row.Value["amount"],leftover.good.name,leftover.good.edizm,leftover.good_id,leftover.good.price,leftover.good.barcode);
						leftover.expenditure += rr.amount;
						unitOfWork.Leftovers.Change(leftover);
						cost += rr.amount.Value * rr.price.Value;
						unitOfWork.Requests.AddRequestRow(request,rr);
					}
					request.cost = cost;
					unitOfWork.Requests.Add(request);
					unitOfWork.SaveChanges();
					return Response.AsRedirect("~/");
				}
			};
		}
	}
}