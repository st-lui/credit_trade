using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DbModel;

namespace credittrade.Modules
{
	public class InOutReportModel
	{
		public String Start { get; set; }
		public String Finish { get; set; }
		public IList<request> Requests { get; set; }
		public IList<request> RequestsPenalty { get; set; }
	}
}