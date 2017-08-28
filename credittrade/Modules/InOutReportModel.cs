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
		public DateTime StartDate { get; set; }
		public DateTime FinishDate { get; set; }
		public IList<request> RequestsAfter { get; internal set; }
		public IList<request> RequestsCurrent { get; set; }
		public IList<request> RequestsBefore { get; internal set; }
		public IList<request> RequestsPenaltyAfter { get; set; }
		public IList<request> RequestsPenaltyBefore { get; internal set; }
	}
}