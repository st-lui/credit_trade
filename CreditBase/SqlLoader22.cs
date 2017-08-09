using System.Collections.Generic;
using System.Data.SqlClient;
using PostReq.Util;

namespace CreditBase
{
	public class SqlLoader22 : SqlLoader
	{
		public SqlLoader22()
		{
			reg = "22";
			srv = "r22aufsql01.main.russianpost.ru";
			dbname = "r22-asku-work";
			priceTableName = "_InfoRg11000";
			warehouseTableName = "_Reference163";
		}

		public override void LoadNom()
		{
			NomLoader NL = NomLoader.Create(srv, dbname, reg);
			NL.UpdateLocalNom();
		}

		public override string WhatAPost(string fileName)
		{
			string pref = "";
			if (fileName.Contains("report_Алейский почтамт")) pref = "24";
			if (fileName.Contains("report_Барнаульский почтамт")) pref = "25";
			if (fileName.Contains("report_Барнаульский УКД")) pref = "26";
			if (fileName.Contains("report_Белокурихинский УКД")) pref = "27";
			if (fileName.Contains("report_Бийский почтамт")) pref = "28";
			if (fileName.Contains("report_Бийский УКД")) pref = "29";
			if (fileName.Contains("report_Благовещенский почтамт")) pref = "30";
			if (fileName.Contains("report_Заринский почтамт")) pref = "31";
			if (fileName.Contains("report_Каменский почтамт")) pref = "32";
			if (fileName.Contains("report_Кулундинский почтамт")) pref = "33";
			if (fileName.Contains("report_Мамонтовский почтамт")) pref = "34";
			if (fileName.Contains("report_Павловский почтамт")) pref = "35";
			if (fileName.Contains("report_Первомайский почтамт")) pref = "36";
			if (fileName.Contains("report_Поспелихинский почтамт")) pref = "37";
			if (fileName.Contains("report_Рубовский УКД")) pref = "39";
			if (fileName.Contains("report_Рубцовский почтамт")) pref = "38";
			if (fileName.Contains("report_Славгородский почтамт")) pref = "40";
			if (fileName.Contains("report_Смоленский почтамт")) pref = "41";


			return pref;

		}

		public override void LoadPricesDictionary()
		{
			WarehousePriceKindDictionary = new Dictionary<string, string>();
			PriceKindNomPrice = new Dictionary<string, Dictionary<string, decimal>>();
			string defaultPriceKind = "A4AEF4CE46FB566011E3DC100178205B";
			using (SqlConnection conn = new SqlConnection($"data source={srv};initial catalog={dbname};Integrated Security=true"))
			{
				conn.Open();
				SqlCommand command = new SqlCommand($"select _Description,_Fld3233RRef from {warehouseTableName} where _marked=0x00", conn);
				SqlDataReader whReader = command.ExecuteReader();

				while (whReader.Read())
				{
					string warehouseName = whReader.GetString(0).Trim();
					string priceId = "0";
					if (!whReader.IsDBNull(1))
						priceId = Utils.SqlBinaryToString(whReader.GetSqlBinary(1));
					else
						priceId = defaultPriceKind;
					if (priceId == "00000000000000000000000000000000")
						priceId = defaultPriceKind;
					if (!WarehousePriceKindDictionary.ContainsKey(warehouseName))
						WarehousePriceKindDictionary.Add(warehouseName, priceId);
				}
				whReader.Close();

				command = new SqlCommand($"select _Fld11001RRef nomid,_Fld11004 price,_Fld11002RRef pricekind from {priceTableName} order by _Period", conn);
				var priceReader = command.ExecuteReader();
				while (priceReader.Read())
				{
					string nomId = Utils.SqlBinaryToString(priceReader.GetSqlBinary(0));
					decimal price = priceReader.GetDecimal(1);
					string kindId = Utils.SqlBinaryToString(priceReader.GetSqlBinary(2));
					Dictionary<string, decimal> nomprice;
					if (PriceKindNomPrice.ContainsKey(kindId))
					{
						nomprice = PriceKindNomPrice[kindId];
					}
					else
					{
						nomprice = new Dictionary<string, decimal>();
						PriceKindNomPrice.Add(kindId, nomprice);
					}
					if (nomprice.ContainsKey(nomId))
						nomprice[nomId] = price;
					else
						nomprice.Add(nomId, price);
				}

			}
		}
	}
}