using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using DataAccess;
using DbModel;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Session;
using Nancy.TinyIoc;
using System.Data;
using System.Data.SqlClient;
using System.IO;

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
			private IEnumerable<string> claims;

			public User(string name, user u)
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
				get { return claims; }
				set { claims = value; }
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
		protected override void ConfigureConventions(NancyConventions conventions)
		{
			base.ConfigureConventions(conventions);

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("fonts", @"fonts")
			);
		}
		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);
			StaticConfiguration.DisableErrorTraces = false;
			CookieBasedSessions.Enable(pipelines);
			Nancy.Security.Csrf.Enable(pipelines);
			pipelines.BeforeRequest += (ctx) =>
			{
				int userId = 0;
				string authHeader = ctx.Request.Headers.Authorization;
				var unitOfWork = new UnitOfWork();


				if (!string.IsNullOrEmpty(authHeader))
				{
					var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);

					// RFC 2617 sec 1.2, "scheme" name is case-insensitive
					if (authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) && authHeaderVal.Parameter != null)
					{
						var credentials = authHeaderVal.Parameter;
						var encoding = Encoding.GetEncoding("iso-8859-1");
						credentials = encoding.GetString(Convert.FromBase64String(credentials));

						int separator = credentials.IndexOf(':');
						string name = credentials.Substring(0, separator);
						string password = credentials.Substring(separator + 1);
						var usr = unitOfWork.Users.Get(name, password);
						if (usr != null)
						{
							User user = new User(usr.username, usr);
							if (usr.admin.HasValue && usr.admin.Value)
								user.Claims = new[] { "Admin" };
							ctx.CurrentUser = user;
							userId = usr.id;
							ctx.Items.Add("unitofwork", unitOfWork);
							ctx.Request.Cookies.Add("userId", usr.id.ToString());
						}
					}
				}

				try
				{
					userId = int.Parse((string)ctx.Request.Cookies["userId"]);
				}
				catch
				{
				}
				if (userId == 0)
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
					SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
					builder.UserID = "sa";
					builder.Password = "hf6k:tsb9v";
					builder.DataSource = "R54-W12R2-XX.main.russianpost.ru\\ufps";
					builder.InitialCatalog = "POSTITEMS";
					string idx = "0";

					using (SqlConnection sqlConnection = new SqlConnection(builder.ConnectionString))
					{
						sqlConnection.Open();
						SqlCommand command = new SqlCommand("select idx from ipzone where ip = @userIp",sqlConnection);
						command.Parameters.AddWithValue("@userIp", userIp);
						var reader = command.ExecuteReader();
						
						if (reader.HasRows)
						{
							reader.Read();
							idx = reader.GetString(0);
						}
						sqlConnection.Close();
					}
					//Util.log.Info("Request from ip={0}\t PROXY={1}", userIp, proxy);
					string hostName;
					try
					{
						var dnsEntry = System.Net.Dns.GetHostEntry(userIp);
						hostName = dnsEntry.HostName;
					}
					catch
					{
						hostName = "";
					}
					Regex regex = new Regex("r22-/d{6}-.*");
					
					if (regex.IsMatch(hostName))
						idx = hostName.Substring(4, 6);

					
					var usr = unitOfWork.Users.Get(idx);
					if (usr != null)
					{
						User user = new User(usr.username, usr);
						if (usr.admin.HasValue && usr.admin.Value)
							user.Claims = new[] { "Admin" };
						ctx.CurrentUser = user;
						ctx.Items.Add("unitofwork", unitOfWork);
						ctx.Request.Cookies.Add("userId", usr.id.ToString());
					}
					else
					{
						ctx.CurrentUser = null;
					}
				}
				else
				{

				}
				return null;
			};
			pipelines.AfterRequest += (ctx) =>
			{
				if (ctx.Response.StatusCode == HttpStatusCode.Forbidden)
				{
					ctx.Response.StatusCode = HttpStatusCode.Unauthorized;
					ctx.Response.Headers.Add("WWW-Authenticate", "Basic realm=RUSSIANPOST");
				}
				if (ctx.CurrentUser == null)
				{
					ctx.Response.StatusCode = HttpStatusCode.Unauthorized;
					ctx.Response.Headers.Add("WWW-Authenticate", "Basic realm=RUSSIANPOST");
				}
			};
		}
	}
}