using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DbModel;

namespace credittrade.Modules
{
	public class MrcReportRow
	{
		public post parent;
		public string name;
		public List<MrcReportRow> children;
		public decimal spent, debt, debtOverdue;
	}
}