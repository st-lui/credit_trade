using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlTypes;

namespace PostReq.Util
{
	public class Utils
	{
		public enum FormMode
		{
			New,Edit,Copy
		}
		public enum RequestView
		{
			Postamt,Ufps
		}

		public static string SqlBinaryToString(SqlBinary value)
		{
			string stringRep = "";
			var byteArray = value.Value;
			Array.ForEach(byteArray, b => stringRep += b.ToString("X2"));
			return stringRep;
		}
	}
}
