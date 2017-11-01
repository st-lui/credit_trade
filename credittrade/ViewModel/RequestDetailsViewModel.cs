using DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace credittrade.ViewModel
{
	public class RequestDetailsViewModel
	{
		public string id;
		public user user;
		public string buyerFio;
		public string date;
		public string cost;
		public bool fullpaid;
		public string fullpaidmessage;
		public string pay_date;
		public string paidsum;
	}
}