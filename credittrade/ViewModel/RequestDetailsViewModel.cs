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
		public bool partial_paid;
		public string paid_cost;
		public string returned_cost;
		public List<request_rows> request_rows;
	}
}