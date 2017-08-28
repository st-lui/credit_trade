using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using OfficeOpenXml;
using PostReq.Model;
using PostReq.Util;
namespace CreditBase
{
	public class SimpleLogger
	{
		private static string logName = "log.txt";
		private static SimpleLogger current;

		public static SimpleLogger GetInstance()
		{
			return current ?? (current = new SimpleLogger());
		}

		public void Write(string message)
		{
			StreamWriter writer = new StreamWriter(logName, true);
			writer.WriteLine($"{DateTime.Now:g} {message}");
			writer.Close();
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			//Console.WriteLine(ToPost());
			//Console.WriteLine(ToPostOffice());
			//Console.WriteLine(ToWarehouses());
			List<SqlLoaderCreator> creators = new List<SqlLoaderCreator>()
			{
				new SqlLoaderCreator75(),
				new SqlLoaderCreator03(),
				new SqlLoaderCreator42(),
				new SqlLoaderCreator22(),
			};
			foreach (var sqlLoaderCreator in creators)
			{
				SqlLoader sqlLoader = sqlLoaderCreator.FactoryMethod();
				try
				{
					//LeftoversFrom1c();

					try
					{
						//nomLoader.connectionString = "data source=r22aufsql01;initial catalog=r22-asku-work;user=nom_reader;password=6LRZ{w.Y!LHXtY.";
						sqlLoader.LoadNom();


					}
					catch (Exception ee)
					{
						SimpleLogger.GetInstance().Write(ee.ToString());
					}
					SimpleLogger.GetInstance().Write(Goods(sqlLoader.reg));
					//Console.ForegroundColor = ConsoleColor.Green;
					//CheckWareHouse();
					sqlLoader.LoadPricesDictionary();
					Leftovers(sqlLoader.reg, sqlLoader.WhatAPost, sqlLoader.WarehousePriceKindDictionary, sqlLoader.PriceKindNomPrice);
					SimpleLogger.GetInstance().Write("Работа завершена");
				}
				catch (Exception e)
				{
					SimpleLogger.GetInstance().Write(e.ToString());
				}
			}
			RecalcPrices();
		}

		public static void RecalcPrices()
		{
			SimpleLogger.GetInstance().Write($"Начат пересчет цен");
			string connStr = @"Data Source=r54web02\sql;
							Initial Catalog=credit_trade;
							Integrated Security=False;User ID=credit;Password=123456;";

			SqlConnection conn = new SqlConnection(connStr);
			conn.Open();
			Dictionary<long, Leftover> leftovers = new Dictionary<long, Leftover>();
			using (SqlCommand command = new SqlCommand($"select l.id,l.good_id,l.warehouse_id,l.amount,l.expenditure,l.price,g.name,p.id from leftovers l,goods g,warehouses w,postoffices po,posts p where l.good_id=g.id and l.warehouse_id=w.id and po.id=w.postoffice_id and po.post_id =p.id", conn))
			{
				if (conn.State == ConnectionState.Closed)
					conn.Open();
				using (SqlDataReader dataReader = command.ExecuteReader())
				{
					while (dataReader.Read())
					{
						var leftover = new Leftover(dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetInt32(2),
							dataReader.GetDecimal(3), dataReader.GetDecimal(4), dataReader.GetDecimal(5), dataReader.GetString(6).Replace((char)160, ' '), dataReader.GetInt32(7).ToString());
						long leftoverKey = (long)leftover.warehouse_id * 1000000 + leftover.good_id;
						leftovers.Add(leftoverKey, leftover);
					}
					dataReader.Close();
				}
			}
			List<Request> requests = new List<Request>();
			List<RequestRow> requestRows = new List<RequestRow>();
			using (SqlCommand reqCommand = new SqlCommand("select r.id,b.warehouse_id,cost from requests r,buyers b where b.id=r.buyer_id", conn))
			{
				using (SqlDataReader requestReader = reqCommand.ExecuteReader())
				{
					while (requestReader.Read())
					{
						int requestId = requestReader.GetInt32(0);
						int warehouseId = requestReader.GetInt32(1);
						decimal requestCost = requestReader.GetDecimal(2);
						decimal requestCostChange = 0;
						Request request = new Request();
						request.id = requestId;
						request.cost=requestCost;
						using (SqlConnection conn2 = new SqlConnection(connStr))
						{
							conn2.Open();
							using (SqlCommand requestRowCommand = new SqlCommand($"select id, price,amount,goods_id from request_rows where request_id={requestId}", conn2))
							{
								using (SqlDataReader requestRowReader = requestRowCommand.ExecuteReader())
								{
									while (requestRowReader.Read())
									{
										int requestRowId = requestRowReader.GetInt32(0);
										decimal requestRowPrice = requestRowReader.GetDecimal(1);
										decimal amount = requestRowReader.GetDecimal(2);
										int good_id = requestRowReader.GetInt32(3);
										long leftoverId = (long)warehouseId * 1000000 + good_id;
										//if (leftovers.ContainsKey(leftoverId))
										{
											var leftover = leftovers[leftoverId];
											if (leftover.price != requestRowPrice)
											{
												RequestRow requestRow = new RequestRow();
												requestRow.id = requestRowId;
												requestRow.oldprice = requestRowPrice;
												requestRow.price = leftover.price;
												requestRow.cost = requestRow.price * amount;
												requestRows.Add(requestRow);
											}
										}
									}
								}
							}
							conn2.Close();
						}
						requests.Add(request);
					}
				}
			}
			conn.Close();
			if (conn.State == ConnectionState.Closed)
				conn.Open();

			var transaction = conn.BeginTransaction();
			foreach (var requestRow in requestRows)
			{
				SimpleLogger.GetInstance().Write($"Обновление цены request_row id:{requestRow.id} старая цена: {requestRow.oldprice}новая цена: {requestRow.price}");
				using (SqlCommand command = new SqlCommand())
				{
					command.Connection = conn;
					command.Transaction = transaction;
					string priceStr = requestRow.price.ToString(CultureInfo.GetCultureInfo("en-US"));
					command.CommandText = $"update request_rows set price={priceStr} where id={requestRow.id}";
					command.ExecuteNonQuery();
				}

			}
			transaction.Commit();
			transaction = conn.BeginTransaction();
			var conn3 = new SqlConnection(connStr);
			conn3.Open();
			foreach (var request in requests)
			{
				decimal newCost = 0;
				using (SqlCommand command = new SqlCommand($"select sum(s.cost) from(select(rr.price * rr.amount) cost from request_rows rr where request_id = {request.id}) s;",conn3))
				{
					newCost=(decimal)command.ExecuteScalar();
				}
				if (newCost != request.cost)
				{
					SimpleLogger.GetInstance().Write($"Обновление стоимости заказа id:{request.id} новая стоимость:{newCost} старая стоимость: {request.cost}");
					using (SqlCommand command = new SqlCommand())
					{
						command.Connection = conn;
						command.Transaction = transaction;
						string newCostStr = newCost.ToString(CultureInfo.GetCultureInfo("en-US"));
						command.CommandText = $"update requests set cost = {newCostStr} where id={request.id}";
						command.ExecuteNonQuery();
					}
				}
			}
			transaction.Commit();
			conn.Close();
			conn3.Close();
			//todo обновление бд
			SimpleLogger.GetInstance().Write($"Завершен пересчет цен");

		}
		public static void LeftoversFrom1c()
		{
			SimpleLogger.GetInstance().Write($"Начато формирование остатков");
			Process process = new Process();
			process.StartInfo = new ProcessStartInfo("postoffice.bat");
			process.Start();
			process.WaitForExit();
			SimpleLogger.GetInstance().Write($"Завершено формирование остатков");
		}

		public static string Goods(string reg_code)
		{
			//Nom Parse(string s)
			string PathNom = Path.Combine(Environment.CurrentDirectory, "appdata", $"nom_{reg_code}.txt");
			//StreamReader SR = new StreamReader(PathNom, Encoding.GetEncoding("windows-1251"));
			string[] AllLines = File.ReadAllLines(PathNom, Encoding.GetEncoding("windows-1251"));

			Dictionary<String, Nom> NomDictionary = new Dictionary<string, Nom>();
			Dictionary<String, Nom> BaseDictionary = new Dictionary<string, Nom>();

			for (int i = 0; i < AllLines.Length; i++)
			{
				Nom TempNom = Nom.Parse(AllLines[i]);
				NomDictionary.Add(TempNom.Id + "_" + reg_code, TempNom);
				//Console.WriteLine(TempNom.ToString());
			}

			string connStr = @"Data Source=r54web02\sql;
							Initial Catalog=credit_trade;
							Integrated Security=False;User ID=credit;Password=123456;";

			SqlConnection conn = new SqlConnection(connStr);
			SqlConnection connInsUpd = new SqlConnection(connStr);
			conn.Open();
			connInsUpd.Open();


			string SelectQuery = $@"SELECT [id]
	  ,[nom_id]
	  ,[parent_id]
	  ,[name]
	  ,[edizm]
	  ,[price]
	  ,[barcode]
  FROM [credit_trade].[dbo].[goods] where reg_code='{reg_code}'";
			SqlCommand sqlQuerySelect = new SqlCommand(SelectQuery, conn);

			using (SqlDataReader drNew = sqlQuerySelect.ExecuteReader())
			{
				if (drNew.HasRows)
				{
					while (drNew.Read())
					{
						Nom TempFromBase = new Nom()
						{
							Id = drNew.GetValue(1).ToString().Trim() + "_" + reg_code,
							ParentId = drNew.GetValue(2).ToString().Trim(),
							Name = drNew.GetValue(3).ToString(),
							EdIzm = drNew.GetValue(4).ToString(),
							Price = Convert.ToDouble(drNew.GetValue(5).ToString()),
							Barcode = drNew.GetValue(6).ToString()
						};
						BaseDictionary.Add(TempFromBase.Id, TempFromBase);
					}
				}
			}

			//NomDictionary
			//BaseDictionary
			int count = 0;
			foreach (var itemNom in NomDictionary)
			{
				count++;
				//Console.WriteLine(count);
				Nom NomSearchNom = itemNom.Value;
				if (BaseDictionary.ContainsKey(itemNom.Key))
				{
					Nom NomSearchBase;

					BaseDictionary.TryGetValue(itemNom.Key, out NomSearchBase);

					if (NomSearchBase.Price != NomSearchNom.Price || NomSearchBase.Name != NomSearchNom.Name)
					{
						string price = NomSearchNom.Price.ToString(CultureInfo.GetCultureInfo("en-US"));
						string[] nomIdSplit = NomSearchBase.Id.Split('_');
						if (NomSearchBase.Name != NomSearchNom.Name)
							SimpleLogger.GetInstance().Write($"Изменение имени номенклатуры:старое\t{NomSearchBase.Name}\tновое\t{NomSearchNom.Name}");
						string query = $"UPDATE [credit_trade].[dbo].[goods] SET [price] = '{price}',name='{NomSearchNom.Name.Replace("'", "''")}' WHERE  nom_id='{nomIdSplit[0]}' and reg_code='{nomIdSplit[1]}'";

						SqlCommand sqlQueryInsert = new SqlCommand(query, connInsUpd);
						sqlQueryInsert.ExecuteNonQuery();
					}
				}
				else
				{
					string price = NomSearchNom.Price.ToString(CultureInfo.GetCultureInfo("en-US"));
					string query = string.Format(@"INSERT INTO [credit_trade].[dbo].[goods] ([nom_id],[parent_id],[name],[edizm],[price],[barcode],reg_code)
	 VALUES ('{0}','{1}','{2}','{3}', {4} ,'{5}','{6}')",
		   NomSearchNom.Id,
		   NomSearchNom.ParentId,
		   NomSearchNom.Name.Replace("'", "''"),
		   NomSearchNom.EdIzm,
		   price,
		   NomSearchNom.Barcode, reg_code);

					SqlCommand sqlQueryInsert = new SqlCommand(query, connInsUpd);
					sqlQueryInsert.ExecuteNonQuery();
				}

			}


			conn.Close();
			conn.Dispose();

			//foreach (var item in NomDictionary)
			//{
			//    Console.WriteLine(item.Key + ":" + item.Value);
			//}

			return "Goods: Успешная загрузка";
		}


		public static string Leftovers(string reg_code, Func<string, string> whatAPost, Dictionary<string, string> warehousePriceKindDictionary, Dictionary<string, Dictionary<string, decimal>> sqlLoaderPriceKindNomPrice)
		{
			string connStr = @"Data Source=r54web02\sql;
							Initial Catalog=credit_trade;
							Integrated Security=False;User ID=credit;Password=123456;";

			SqlConnection conn = new SqlConnection(connStr);
			conn.Open();

			SqlConnection connPref = new SqlConnection(connStr);


			//try
			//{

			string PathLeftovers = @"C:\1c";

			string[] FilesLeftovers = Directory.GetFiles(PathLeftovers);
			// for (int i = 0; i < FilesLeftovers.Length; i++)
			// {
			int i = 0;
			int g = 0;

			Dictionary<string, WareHouse> DicWarehouse = new Dictionary<string, WareHouse>();
			Dictionary<string, Good> DicGood = new Dictionary<string, Good>();

			string SelectQueryWarehouse = @"SELECT [id] ,[name],[postoffice_id]  FROM [credit_trade].[dbo].[warehouses]";
			SqlCommand sqlQuerySelectWarehouse = new SqlCommand(SelectQueryWarehouse, conn);

			using (SqlDataReader drNewWarehouse = sqlQuerySelectWarehouse.ExecuteReader(CommandBehavior.CloseConnection))
			{
				if (drNewWarehouse.HasRows)
				{
					while (drNewWarehouse.Read())
					{

						string SelectPOST = @"SELECT [post_id] FROM [credit_trade].[dbo].[postoffices] WHERE id=" + Convert.ToInt32(drNewWarehouse.GetValue(2).ToString().Trim()) + "";
						SqlCommand sqlSelectPOST = new SqlCommand(SelectPOST, connPref);
						string preffix = "";
						connPref.Open();
						using (SqlDataReader drPOST = sqlSelectPOST.ExecuteReader(CommandBehavior.CloseConnection))
						{
							while (drPOST.Read())
							{
								preffix = drPOST.GetValue(0).ToString();
							}

						}
						WareHouse TempFromBase = new WareHouse()
						{
							id_w = drNewWarehouse.GetValue(0).ToString().Trim(),
							name_w = preffix + "_" + drNewWarehouse.GetValue(1).ToString().Trim()

						};
						//Console.WriteLine(TempFromBase.name_w);
						DicWarehouse.Add(TempFromBase.name_w, TempFromBase);

					}
				}
				drNewWarehouse.Close();
			}
			//connPref.Close();
			conn.Open();

			string SelectQueryGood = $@"SELECT [id],[name],nom_id FROM [credit_trade].[dbo].[goods] where reg_code ='{reg_code}'";
			SqlCommand sqlQuerySelectGood = new SqlCommand(SelectQueryGood, conn);

			using (SqlDataReader drNewGood = sqlQuerySelectGood.ExecuteReader())
			{
				if (drNewGood.HasRows)
				{
					while (drNewGood.Read())
					{
						Good TempFromBase = new Good()
						{
							id_g = drNewGood.GetValue(0).ToString().Trim(),
							name_g = drNewGood.GetValue(1).ToString().Replace((char)160, ' '),
							id_nom = drNewGood.GetString(2)

						};
						//Console.WriteLine(TempFromBase.name_g);
						DicGood.Add(TempFromBase.name_g, TempFromBase);

					}
				}
				drNewGood.Close();
			}

			//conn.Close();

			Dictionary<long, Leftover> leftovers = new Dictionary<long, Leftover>();
			using (SqlCommand command = new SqlCommand($"select l.id,l.good_id,l.warehouse_id,l.amount,l.expenditure,l.price,g.name,p.id from leftovers l,goods g,warehouses w,postoffices po,posts p where l.good_id=g.id and l.warehouse_id=w.id and po.id=w.postoffice_id and po.post_id =p.id and g.reg_code={reg_code}", conn))
			{
				if (conn.State == ConnectionState.Closed)
					conn.Open();
				using (SqlDataReader dataReader = command.ExecuteReader())
				{
					while (dataReader.Read())
					{
						var leftover = new Leftover(dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetInt32(2),
							dataReader.GetDecimal(3), dataReader.GetDecimal(4), dataReader.GetDecimal(5), dataReader.GetString(6).Replace((char)160, ' '), dataReader.GetInt32(7).ToString());
						long leftoverKey = (long)leftover.warehouse_id * 1000000 + leftover.good_id;
						leftovers.Add(leftoverKey, leftover);
					}
					dataReader.Close();
				}
			}
			foreach (var OneFileLeftovers in FilesLeftovers)
			{
				i++;
				g = 0;
				SimpleLogger.GetInstance().Write("Файл - " + OneFileLeftovers);

				string preffix = whatAPost(OneFileLeftovers);
				if (string.IsNullOrEmpty(preffix))
					continue;
				var ExcelOffices = new ExcelPackage(new FileInfo(OneFileLeftovers));
				var listOffices = ExcelOffices.Workbook.Worksheets[1];


				var rowCnt = listOffices.Dimension.End.Row;
				var colCnt = listOffices.Dimension.End.Column;

				int warehouse_id = 0;
				int good_id = 0;
				decimal amount = 0;

				string WarehouseName = "";
				string GoodName = "";
				int startCol = 1;
				for (int col = 1; col <= colCnt - 1; col++)
				{
					string colVal = Null(listOffices.Cells[8, col].Value);
					if (colVal == "Номенклатура, Ед. изм.")
					{
						startCol = col;
						break;
					}
				}
				HashSet<string> excelGoodsSet = new HashSet<string>();
				for (int row = 10; row <= rowCnt - 1; row++)
				{
					GoodName = Null(listOffices.Cells[row, startCol].Value);
					GoodName = GoodName.Substring(0, GoodName.LastIndexOf(','));
					GoodName = GoodName.Replace("\n", "");
					excelGoodsSet.Add(GoodName);
				}
				for (int col = startCol + 5; col <= colCnt - 1; col++)
				{
					WarehouseName = Null(listOffices.Cells[8, col].Value).Trim();
					string wh_name = WarehouseName;
					WarehouseName = preffix + "_" + WarehouseName;
					//Console.WriteLine(WarehouseName);
					//conn.Open();
					if (DicWarehouse.ContainsKey(WarehouseName))
					{
						WareHouse wareHouse;

						DicWarehouse.TryGetValue(WarehouseName, out wareHouse);
						warehouse_id = Convert.ToInt32(wareHouse.id_w);
						//Console.WriteLine(WarehouseName);
						//Console.WriteLine(warehouse_id);
					}
					else
					{
						SimpleLogger.GetInstance().Write($"Не найден склад {WarehouseName}!");
						continue;
					}
					string priceKind = warehousePriceKindDictionary[wh_name];
					var priceDictionary = sqlLoaderPriceKindNomPrice[priceKind];
					for (int row = 10; row <= rowCnt - 1; row++)
					{
						//g++;
						//Console.WriteLine("Строка в файле - " + g);

						GoodName = Null(listOffices.Cells[row, startCol].Value);
						GoodName = GoodName.Substring(0, GoodName.LastIndexOf(','));
						GoodName = GoodName.Replace("\n", "");
						if (Null(listOffices.Cells[row, col].Value) != "")
							amount = Convert.ToDecimal(Null(listOffices.Cells[row, col].Value));
						else
							amount = 0;
						Good good;
						if (DicGood.ContainsKey(GoodName))
						{
							good = DicGood[GoodName];
							good_id = int.Parse(good.id_g);
						}
						else
						{
							//SimpleLogger.GetInstance().Write($"Не найден товар {GoodName}!");
							continue;
						}

						decimal price = 0;
						if (priceDictionary.ContainsKey(good.id_nom))
							price = priceDictionary[good.id_nom];

						//conn.Open();
						long leftoverKey = (long)warehouse_id * 1000000 + good_id;
						if (leftovers.ContainsKey(leftoverKey))
						{
							if (leftovers[leftoverKey].amount != amount)
							{
								leftovers[leftoverKey].amount = amount;
								leftovers[leftoverKey].sqlKey = 'u';
							}
							if (leftovers[leftoverKey].price != price)
							{
								leftovers[leftoverKey].price = price;
								leftovers[leftoverKey].sqlKey = 'u';
							}
						}
						else
						if (amount != 0)
						{
							var leftover = new Leftover(0, good_id, warehouse_id, amount, 0, price, "", "");
							leftover.sqlKey = 'a';
							leftovers.Add(leftoverKey, leftover);
						}
					}
					if (col % 10 == 0)
						SimpleLogger.GetInstance().Write($"Обработано {col} столбцов из {colCnt}");
				}
				foreach (var leftover in leftovers)
				{
					if (leftover.Value.warehouseName == preffix)
						if (!excelGoodsSet.Contains(leftover.Value.goodName))
						{
							if (leftover.Value.amount != 0)
							{
								SimpleLogger.GetInstance().Write(
									$"Товар отсутствует в excel id={leftover.Value.id} name={leftover.Value.goodName} Текущее количество {leftover.Value.amount} Id Склада {leftover.Value.warehouse_id}");
								leftover.Value.amount = 0;
								leftover.Value.sqlKey = 'u';

							}
						}
				}
				SimpleLogger.GetInstance().Write("Количество строк: " + rowCnt);
				SimpleLogger.GetInstance().Write("Количество столбцов: " + colCnt);
			}
			//}
			//catch (Exception ex)
			//{
			//    Console.WriteLine("Ошибка: Описание ошибки: " + ex.Message);
			//}
			if (conn.State == ConnectionState.Closed)
				conn.Open();

			var transaction = conn.BeginTransaction();
			foreach (var leftover in leftovers)
			{
				if (leftover.Value.sqlKey == 'u')
				{
					using (SqlCommand command = new SqlCommand())
					{
						command.Connection = conn;
						command.Transaction = transaction;
						string amountStr = leftover.Value.amount.ToString(CultureInfo.GetCultureInfo("en-US"));
						string priceStr = leftover.Value.price.ToString(CultureInfo.GetCultureInfo("en-US"));
						command.CommandText = $"update leftovers set amount={amountStr},price={priceStr} where id={leftover.Value.id}";
						command.ExecuteNonQuery();
					}
				}
				else
				if (leftover.Value.sqlKey == 'a')
				{
					using (SqlCommand command = new SqlCommand())
					{
						command.Connection = conn;
						command.Transaction = transaction;
						string amountStr = leftover.Value.amount.ToString(CultureInfo.GetCultureInfo("en-US"));
						string priceStr = leftover.Value.price.ToString(CultureInfo.GetCultureInfo("en-US"));
						command.CommandText = $"insert into leftovers (amount,expenditure,good_id,warehouse_id,price) values ({amountStr},0,{leftover.Value.good_id},{leftover.Value.warehouse_id},{priceStr})";
						command.ExecuteNonQuery();
					}
				}
			}
			transaction.Commit();
			conn.Close();

			return "Leftovers: Успешная загрузка";
		}

		//public static string WhatAPost(string nameFile)
		//{
		//	string pref = "";
		//	if (nameFile.Contains("report_Алейский почтамт")) pref = "24";
		//	if (nameFile.Contains("report_Барнаульский почтамт")) pref = "25";
		//	if (nameFile.Contains("report_Барнаульский УКД")) pref = "26";
		//	if (nameFile.Contains("report_Белокурихинский УКД")) pref = "27";
		//	if (nameFile.Contains("report_Бийский почтамт")) pref = "28";
		//	if (nameFile.Contains("report_Бийский УКД")) pref = "29";
		//	if (nameFile.Contains("report_Благовещенский почтамт")) pref = "30";
		//	if (nameFile.Contains("report_Заринский почтамт")) pref = "31";
		//	if (nameFile.Contains("report_Каменский почтамт")) pref = "32";
		//	if (nameFile.Contains("report_Кулундинский почтамт")) pref = "33";
		//	if (nameFile.Contains("report_Мамонтовский почтамт")) pref = "34";
		//	if (nameFile.Contains("report_Павловский почтамт")) pref = "35";
		//	if (nameFile.Contains("report_Первомайский почтамт")) pref = "36";
		//	if (nameFile.Contains("report_Поспелихинский почтамт")) pref = "37";
		//	if (nameFile.Contains("report_Рубовский УКД")) pref = "39";
		//	if (nameFile.Contains("report_Рубцовский почтамт")) pref = "38";
		//	if (nameFile.Contains("report_Славгородский почтамт")) pref = "40";
		//	if (nameFile.Contains("report_Смоленский почтамт")) pref = "41";

		//	if (nameFile.Contains("report_ОСП Бичурский почтамт")) pref = "42";
		//	if (nameFile.Contains("report_ОСП Закаменский почтамт")) pref = "43";
		//	if (nameFile.Contains("report_ОСП Кабанский почтамт")) pref = "44";
		//	if (nameFile.Contains("report_ОСП Прибайкальский почтамт")) pref = "45";
		//	if (nameFile.Contains("report_ОСП Северобайкальский почтамт")) pref = "46";
		//	if (nameFile.Contains("report_ОСП Улан-Удэнский почтамт")) pref = "47";
		//	if (nameFile.Contains("report_ОСП Хоринский почтамт")) pref = "48";

		//	if (nameFile.Contains("report_Анжеро-Судженский почтамт.xlsx")) pref = "50";
		//	if (nameFile.Contains("report_Беловский почтамт.xlsx")) pref = "51";
		//	if (nameFile.Contains("report_Кемеровский почтамт.xlsx")) pref = "52";
		//	if (nameFile.Contains("report_Ленинск-Кузнецкий почтамт.xlsx")) pref = "53";
		//	if (nameFile.Contains("report_Мариинский почтамт.xlsx")) pref = "54";
		//	if (nameFile.Contains("report_Междуреченский почтамт.xlsx")) pref = "55";
		//	if (nameFile.Contains("report_Новокузнецкий почтамт.xlsx")) pref = "56";
		//	if (nameFile.Contains("report_Прокопьевский почтамт.xlsx")) pref = "57";
		//	if (nameFile.Contains("report_Таштагольский почтамт.xlsx")) pref = "58";
		//	if (nameFile.Contains("report_Тисульский почтамт.xlsx")) pref = "59";
		//	if (nameFile.Contains("report_Топкинский почтамт.xlsx")) pref = "60";
		//	if (nameFile.Contains("report_Тяжинский почтамт.xlsx")) pref = "61";
		//	if (nameFile.Contains("report_Юргинский почтамт.xlsx")) pref = "62";
		//	if (nameFile.Contains("report_Яшкинский почтамт.xlsx")) pref = "63";

		//	return pref;

		//}

		public static string ToPost()
		{
			string connStr = @"Data Source=r54web02\sql;
							Initial Catalog=credit_trade;
							Integrated Security=False;User ID=credit;Password=123456;";
			List<string> PostsList = new List<string>() {
"ОСП Агинский почтамт",
"ОСП Борзинский почтамт",
"ОСП Краснокаменский почтамт",
"ОСП Могочинский почтамт",
"ОСП Нерчинский почтамт",
"ОСП Петровск-Забайкальский почтамт",
"ОСП Приаргунский почтамт",
"ОСП Улетовский почтамт",
"ОСП Читинский почтамт",
"ОСП Шилкинский почтамт"
};



			SqlConnection conn = new SqlConnection(connStr);

			conn.Open();

			for (int i = 0; i < PostsList.Count; i++)
			{


				string query = string.Format(@"INSERT INTO [credit_trade].[dbo].[posts]
		   ([name])
	 VALUES
		   ('{0}')", PostsList[i]);

				SqlCommand sqlQueryInsert = new SqlCommand(query, conn);
				sqlQueryInsert.ExecuteNonQuery();

			}
			conn.Close();
			conn.Dispose();

			return "Post: Успешная загрузка";

		}

		public static string Null(object x)
		{
			return x == null ? "" : x.ToString();
		}

		public static string ToPostOffice()
		{
			try
			{

				string FileOffices = @"PostOffice.xlsx";


				var ExcelOffices = new ExcelPackage(new FileInfo(FileOffices));
				var listOffices = ExcelOffices.Workbook.Worksheets[1];

				bool flag = true;
				int countOffices = 0;
				int Offices = 2;

				while (flag)
				{
					if (Null(listOffices.Cells["A" + Offices].Value).Equals(""))
						flag = false;
					else { Offices++; countOffices++; }
				}


				string connStr = @"Data Source=r54web02\sql;
							Initial Catalog=credit_trade;
							Integrated Security=False;User ID=credit;Password=123456;";

				SqlConnection conn = new SqlConnection(connStr);

				conn.Open();


				for (int i = 2; i < Offices; i++)
				{

					string query = string.Format($@"INSERT INTO [credit_trade].[dbo].[postoffices]([idx],[name_ops],[post_id]) output inserted.id VALUES
		   ('{Null(listOffices.Cells["C" + i].Value)}','{Null(listOffices.Cells["B" + i].Value)}','{WhatAPost(Null(listOffices.Cells["A" + i].Value), conn)}')");

					SqlCommand sqlQueryInsert = new SqlCommand(query, conn);
					int postOfficeId = (int)sqlQueryInsert.ExecuteScalar();
					SqlCommand sqlQueryInsertWarehouse = new SqlCommand($"insert into warehouses (postoffice_id,name) values ({postOfficeId},'{Null(listOffices.Cells["B" + i].Value)}')", conn);
					sqlQueryInsertWarehouse.ExecuteNonQuery();
				}
				conn.Close();
				conn.Dispose();





			}
			catch (Exception ex)
			{
				SimpleLogger.GetInstance().Write("Ошибка: Невозможно прочитать файл на диске. Описание ошибки: " + ex.Message);
			}


			return "PostOffice: Успешная загрузка.";
		}

		public static string WhatAPost(string namePost, SqlConnection connect)
		{


			string NumberPost = "";
			string SelectQuery = @"SELECT [privilegies]
	  ,[id]
	  ,[name]
  FROM [credit_trade].[dbo].[posts] WHERE name='" + namePost + "'";
			SqlCommand sqlQuerySelect = new SqlCommand(SelectQuery, connect);

			using (SqlDataReader drNew = sqlQuerySelect.ExecuteReader())
			{
				while (drNew.Read())
				{
					NumberPost = drNew.GetValue(1).ToString().Trim();

				}
			}


			return NumberPost;
		}

		public static string ToWarehouses()
		{
			string connStr = @"Data Source=r54web02\sql;
							Initial Catalog=credit_trade;
							Integrated Security=False;User ID=credit;Password=123456;";

			SqlConnection conn = new SqlConnection(connStr);
			SqlConnection connIns = new SqlConnection(connStr);
			conn.Open();
			connIns.Open();


			string SelectQuery = @"SELECT [id]
	  ,[idx]
	  ,[name_ops]
	  ,[post_id]
		FROM[credit_trade].[dbo].[postoffices] where post_id >=50";
			SqlCommand sqlQuerySelect = new SqlCommand(SelectQuery, conn);

			using (SqlDataReader drNew = sqlQuerySelect.ExecuteReader())
			{
				while (drNew.Read())
				{
					string query = string.Format(@"INSERT INTO [credit_trade].[dbo].[warehouses]
		   ([name]
		   ,[postoffice_id])
	 VALUES
		   ('{0}','{1}')", drNew.GetValue(2).ToString(), drNew.GetValue(0).ToString());

					SqlCommand sqlQueryInsert = new SqlCommand(query, connIns);
					sqlQueryInsert.ExecuteNonQuery();

				}
			}




			conn.Close();
			conn.Dispose();
			return "Warehouses: Успешная загрузка";
		}

	}

	public class WareHouse
	{
		public string id_w { set; get; }
		public string name_w { set; get; }
	}

	public class Good
	{
		public string id_g { set; get; }
		public string name_g { set; get; }
		public string id_nom { set; get; }
	}

	public class Request
	{
		public int id { get; set; }
		public decimal cost { get; set; }
	}

	public class RequestRow
	{
		public int id { get; set; }
		public decimal price { get; set; }
		public decimal oldprice { get; set; }
		public decimal cost { get; set; }
	}
	public class Leftover
	{
		public Leftover(int id, int goodId, int warehouseId, decimal amount, decimal expenditure, decimal price, string goodName, string warehouseName)
		{
			this.id = id;
			warehouse_id = warehouseId;
			this.warehouseName = warehouseName;
			good_id = goodId;
			this.amount = amount;
			this.expenditure = expenditure;
			this.price = price;
			this.goodName = goodName;
			sqlKey = '\0';
		}

		public int id { get; set; }
		public int warehouse_id { get; set; }
		public int good_id { get; set; }
		public decimal amount { get; set; }
		public decimal expenditure { get; set; }
		public decimal price { get; set; }
		public char sqlKey { get; set; }
		public string goodName { get; set; }
		public string warehouseName { get; set; }
	}

}
