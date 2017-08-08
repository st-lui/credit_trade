using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PostReq.Model
{
	public class Nom
	{
		public string Id { get; set; }
		public string ParentId { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
		public Byte[] EdIzmId { get; set; }
		public string EdIzm { get; set; }
		public double Price { get; set; }
		public DateTime? Period { get; set; }
		public String Partner { get; set; }
		public string Barcode { get; set; }

		public override string ToString()
		{
			string stringRep = "";
			stringRep += Id;
			stringRep += (char)1;
			stringRep += ParentId;
			stringRep += (char)1;
			stringRep += Name;
			stringRep += (char)1;
			stringRep += Code;
			stringRep += (char)1;
			if (EdIzm == null)
				stringRep += "null";
			else
				stringRep += EdIzm;
			stringRep += (char)1; ;
			stringRep += Price.ToString("F2", CultureInfo.InvariantCulture);
			stringRep += (char)1;
			if (Period.HasValue)
				stringRep += Period.Value.ToString("yyyy.MM.dd");
			else
				stringRep += "null";
			stringRep += (char)1;
			if (Partner != null)
				stringRep += Partner;
			else
				stringRep += "null";
			stringRep += (char)1;
			if (Partner != null)
				stringRep += Barcode;
			else
				stringRep += "null";
			stringRep = stringRep.Replace("\n", "");
			return stringRep;
		}

		public static Nom Parse(string s)
		{
			var nom = new Nom();
			var split = s.Split((char)1);
			nom.Id = split[0];
			nom.ParentId = split[1];
			nom.Name = split[2];
			nom.Code = split[3];
			if (split[4] == "null")
				nom.EdIzm = null;
			else
				nom.EdIzm = split[4];
			double d = 0;
			if (double.TryParse(split[5], NumberStyles.Any, CultureInfo.InvariantCulture, out d))
				nom.Price = d;
			else
				nom.Price = 0.0;
			if (split[6] == "null")
				nom.Period = null;
			else
				nom.Period = DateTime.Parse(split[6]);
			if (split[7] == "null")
				nom.Partner = null;
			else
				nom.Partner = split[7];
			if (split[8] == "null")
				nom.Barcode = null;
			else
				nom.Barcode = split[8];
			return nom;
		}
	}
}
