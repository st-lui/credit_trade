using System.Dynamic;
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
		}
	}
}