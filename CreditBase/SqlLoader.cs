using System.Collections.Generic;

namespace CreditBase
{
	public abstract class SqlLoader
	{
		public string reg,srv,dbname,usr,pswd;
		public string nomTableName,edIzmTableName,priceTableName,partnerTableName,partnerNomTableName,barcodeTableName,warehouseTableName;
		public abstract void LoadNom();
		public abstract string WhatAPost(string fileName);
		public Dictionary<string, string> WarehousePriceKindDictionary;
		public Dictionary<string, Dictionary<string, decimal>> PriceKindNomPrice;
		public abstract void LoadPricesDictionary();
	}

	public abstract class SqlLoaderCreator
	{
		public abstract SqlLoader FactoryMethod();
	}

	class SqlLoaderCreator42 : SqlLoaderCreator
	{
		public override SqlLoader FactoryMethod()
		{
			return new SqlLoader42();
		}
	}

	class SqlLoaderCreator03 : SqlLoaderCreator
	{
		public override SqlLoader FactoryMethod()
		{
			return new SqlLoader03();
		}
	}
}