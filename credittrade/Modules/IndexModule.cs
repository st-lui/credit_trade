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
				user currentUser = ((Bootstrapper.User) Context.CurrentUser).DbUser;
				using (UnitOfWork unitOfWork = (UnitOfWork) Context.Items["unitofwork"])
				{
					var leftovers = unitOfWork.Warehouses.GetLeftovers(currentUser.warehouse);
					model.leftovers = leftovers;
					return View["request_add", model];
				}
			};
		}
	}
}