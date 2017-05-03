using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DataAccess;
using DbModel;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Session;
using Nancy.TinyIoc;

namespace credittrade
{
	public class Bootstrapper : DefaultNancyBootstrapper
	{
		// The bootstrapper enables you to reconfigure the composition of the framework,
		// by overriding the various methods and properties.
		// For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

		public class User : Nancy.Security.IUserIdentity
		{
			private string name;
			private user user;
			public User(string name,user u)
			{
				this.name = name;
				user = u;
			}
			
			public string UserName
			{
				get { return this.name; }
			}
			public string Ip { get; set; }
			public int Index { get; set; }
			public IEnumerable<string> Claims
			{
				get
				{
					return new string[] { "users" };
				}
			}

			public user DbUser
			{
				get { return user; }
			}

			public override string ToString()
			{
				return string.Format("[User: UserName={1}, Ip={2}]", UserName, Ip);
			}
		}

		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);
			CookieBasedSessions.Enable(pipelines);
			Nancy.Security.Csrf.Enable(pipelines);
			pipelines.BeforeRequest += (ctx) =>
			{

				var iis_ctx = System.Web.HttpContext.Current;
				object ip = null;
				if (iis_ctx != null)
					ip = iis_ctx.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
				string userIp = String.Empty;
				bool proxy = false;
				if (ip != null)
				{

					userIp = ip.ToString();
					proxy = true;
				}
				if (string.IsNullOrEmpty(userIp))
					userIp = ctx.Request.UserHostAddress;

				//Util.log.Info("Request from ip={0}\t PROXY={1}", userIp, proxy);
				var dnsEntry = System.Net.Dns.GetHostEntry(userIp);
				string hostName=dnsEntry.HostName;
				Regex regex = new Regex("r22-/d{6}-.*");
				string idx = "658101";
				if (regex.IsMatch(hostName))
					idx = hostName.Substring(4,6);

				UnitOfWork unitOfWork = new UnitOfWork();
				var usr = unitOfWork.Users.Get(idx);
				if (usr != null)
				{
					User user = new User(usr.username,usr);
					ctx.CurrentUser = user;
					ctx.Items.Add("unitofwork",unitOfWork);
				}
				else
				{
					ctx.CurrentUser = null;
				}
				return null;
			};
			pipelines.AfterRequest += (ctx) => {
				if (ctx.CurrentUser == null)
				{
					ctx.Response.StatusCode = HttpStatusCode.Unauthorized;
					ctx.Response.Headers.Add("WWW-Authenticate", "Basic realm=RUSSIANPOST");
				}
			};
		}
	}
}