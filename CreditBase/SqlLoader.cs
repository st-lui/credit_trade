namespace CreditBase
{
	public abstract class SqlLoader
	{
		public string reg,srv,dbname,usr,pswd;
		public string nomTableName,edIzmTableName,priceTableName,partnerTableName,partnerNomTableName,barcodeTableName;
		public abstract void LoadNom();
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