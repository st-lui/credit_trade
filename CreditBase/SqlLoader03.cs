
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using PostReq.Model;
using PostReq.Util;

namespace CreditBase
{
	/// <summary>
	/// Загрузчик данных номенклатуры для Республики Бурятия
	/// </summary>
	class SqlLoader03 : SqlLoader
	{
		public SqlLoader03()
		{
			reg = "03";
			priceTableName = "_InfoRg10964";
			warehouseTableName = "_Reference163";
			srv = "10.69.1.210";
			dbname = "r03-asku-work";
			usr = "r03_credit";
			pswd = "111111!!!!!!0";
		}

		public override void LoadNom()
		{
			NomLoader NL = NomLoader.Create("10.69.1.210", "r03-asku-work", "r03_credit", "111111!!!!!!0",reg);
			NL.UpdateLocalNom();
		}

		public override void LoadPricesDictionary()
		{
			WarehousePriceKindDictionary= new Dictionary<string, string>();
			PriceKindNomPrice = new Dictionary<string, Dictionary<string, decimal>>();
			string defaultPriceKind = "88A200226489576D11E37C023EEFC761";
			using (SqlConnection conn = new SqlConnection($"data source={srv};initial catalog={dbname};user={usr};password={pswd}"))
			{
				conn.Open();
				SqlCommand command=new SqlCommand($"select _Description,_Fld3221RRef from {warehouseTableName} where _marked=0x00",conn);
				SqlDataReader whReader= command.ExecuteReader();
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
						WarehousePriceKindDictionary.Add(warehouseName,priceId);
				}
				whReader.Close();
				
				command = new SqlCommand($"select _Fld10965RRef nomid,_Fld10968 price,_Fld10966RRef pricekind from {priceTableName} order by _Period",conn);
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
						nomprice=new Dictionary<string, decimal>();
						PriceKindNomPrice.Add(kindId,nomprice);
					}
					if (nomprice.ContainsKey(nomId))
						nomprice[nomId] = price;
					else
						nomprice.Add(nomId, price);
				}

			}
		
	}

		public class Node
		{
			public Nom nom;
			public List<Node> children;

			public Node(Nom nom)
			{
				this.nom = nom;
				children = new List<Node>();
			}

			public static Node Search(Node root, string parent)
			{
				if (root.nom.Id == parent)
					return root;
				foreach (var childNode in root.children)
				{
					Node x = Search(childNode, parent);
					if (x != null)
						return x;
				}
				return null;
			}

			public static bool Clear(Node root)
			{
				if (root.children.Count == 0)
				{
					if (root.nom.Price == 0)
						return true;
					//if (!root.nom.Period.HasValue)
					//	return true;
					//if ((DateTime.Today - root.nom.Period.Value).TotalDays > 180)
					//	return true;
				}
				for (int i = root.children.Count - 1; i >= 0; i--)
				{
					var childNode = root.children[i];
					bool toDelete = Clear(childNode);
					if (toDelete)
						root.children.RemoveAt(i);
				}
				if (root.children.Count == 0 && root.nom.Price == 0)
					return true;
				return false;
			}

			public static void SaveToFile(Node root, string fileName)
			{
				StreamWriter writer = new StreamWriter(new FileStream(fileName, FileMode.Create), Encoding.GetEncoding(1251));
				Stack<Node> treeStack = new Stack<Node>();
				treeStack.Push(root);
				while (treeStack.Count > 0)
				{
					Node currentNode = treeStack.Pop();
					writer.WriteLine(currentNode.nom.ToString());
					foreach (var child in currentNode.children)
					{
						treeStack.Push(child);
					}
				}
				writer.Close();
			}
		}

		public class NomLoader
		{
			string connectionString;
			string filename, filenameClean;
			FileInfo fileInfo, fileInfoClean;
			string nomTableName = "_Reference112";
			string edIzmTableName = "_Reference80";
			string priceTableName = "_InfoRg10964";
			string partnerTableName = "_Reference122";
			string partnerNomTableName = "_Reference113";
			string barcodeTableName = "_InfoRg11178";
			string priceKindsTableName = "_Reference63";

			public List<Nom> NomList { get; set; }

			public static NomLoader Create(string srv, string dbname, string usr, string pswd, string reg)
			{
				var nomLoader = new NomLoader();
				//nomLoader.connectionString = "data source=r22aufsql01;initial catalog=r22-asku-work;user=nom_reader;password=6LRZ{w.Y!LHXtY.";
				nomLoader.connectionString = $"data source={srv};initial catalog={dbname};user={usr};password={pswd}";
				nomLoader.filename = $"appdata\\nom_{reg}.txt";
				nomLoader.filenameClean = $"appdata\\nomClean_{reg}.txt";
				if (!File.Exists(nomLoader.filename))
				{
					using (File.Create(nomLoader.filename)) ;

				}
				nomLoader.fileInfo = new FileInfo(nomLoader.filename);
				if (File.Exists(nomLoader.filenameClean))
					nomLoader.fileInfoClean = new FileInfo(nomLoader.filenameClean);
				return nomLoader;
			}

			public bool CheckNomUpdate()
			{
				using (SqlConnection conn = new SqlConnection(connectionString))
				{
					try
					{
						conn.Open();
						using (SqlCommand comm = new SqlCommand($@"with tree (_IDRRef,_ParentIDRRef,_Code,_level) as 
(select _IDRRef, _ParentIDRRef, _Code, 0 from[{nomTableName}] where _code = '00000000001'
union all select[{nomTableName}]._IDRRef,[{nomTableName}]._ParentIDRRef,[{nomTableName}]._Code, tree._level + 1 from [{nomTableName}], tree 
where tree._IDRRef =[{nomTableName}]._ParentIDRRef and [{nomTableName}]._Code<>'С1-00001709' and [{nomTableName}]._Code<>'С1-00001897' 
and [{nomTableName}]._Code<>'00000000018' and [{nomTableName}]._Code<>'00000000002' and [{nomTableName}]._Code<>'00000000049' and [{nomTableName}]._Code<>'00000000052' and [{nomTableName}]._Code<>'00000000084' and [{nomTableName}]._Code<>'00000000732')
select count(_IDRRef)from tree; ", conn))
						{
							SqlDataReader dataReader = comm.ExecuteReader();
							dataReader.Read();
							int serverNomCount = dataReader.GetInt32(0);
							int localNomCount = GetLocalNomCount();
							if (serverNomCount != localNomCount)
								return true;
							else
								return false;
						}
						conn.Close();
					}
					catch (Exception e)
					{
						SimpleLogger.GetInstance().Write(e.Message);
						return false;
					}
				}
			}

			public void UpdateLocalNom()
			{
				if (CheckNomUpdate())
					using (SqlConnection conn = new SqlConnection(connectionString))
					{
						conn.Open();
						//Выборка единиц измерения
						Dictionary<SqlBinary, string> edIzmDictionary = new Dictionary<SqlBinary, string>();
						using (SqlCommand comm = new SqlCommand($"select _IDRRef,_Description from {edIzmTableName};", conn))
						{
							var dataReader = comm.ExecuteReader();
							while (dataReader.Read())
								edIzmDictionary.Add(dataReader.GetSqlBinary(0), dataReader.GetString(1));
							dataReader.Close();
						}
						//Выборка цен
						Dictionary<SqlBinary, Tuple<DateTime, double>> priceDictionary =
							new Dictionary<SqlBinary, Tuple<DateTime, double>>();
						using (
							SqlCommand comm =
								new SqlCommand(
									$@"select _Fld10965RRef,_Fld10968,s._Period from {priceTableName} s,(select _Fld10965RRef nomid, max(_Period) period from {priceTableName} where _Fld10966RRef = 0x88A200226489576D11E37C023EEFC761 group by _Fld10965RRef) p where s._Fld10965RRef = p.nomid and s._Period = p.period and s._Fld10966RRef = 0x88A200226489576D11E37C023EEFC761",
									conn))
						{
							var dataReader = comm.ExecuteReader();
							while (dataReader.Read())
							{
								var id = dataReader.GetSqlBinary(0);
								var price = (double) dataReader.GetDecimal(1);
								var date = dataReader.GetDateTime(2);
								if (!priceDictionary.ContainsKey(id))
									priceDictionary.Add(id, new Tuple<DateTime, double>(date, price));
							}
							dataReader.Close();
						}
						//Выборка поставщиков
						Dictionary<SqlBinary, string> partnerDictionary = new Dictionary<SqlBinary, string>();
						using (
							SqlCommand command =
								new SqlCommand(
									$"select nom._Fld2308RRef,partner._Description from {partnerNomTableName} nom,{partnerTableName} partner where nom._OwnerIDRRef=partner._IDRRef",
									conn))
						{
							var dataReader = command.ExecuteReader();
							while (dataReader.Read())
							{
								var binary = dataReader.GetSqlBinary(0);
								if (!partnerDictionary.ContainsKey(binary))
									partnerDictionary.Add(dataReader.GetSqlBinary(0), dataReader.GetString(1));
							}
							dataReader.Close();
						}
						//Выборка штрихкодов
						Dictionary<SqlBinary, string> barcodeDictionary = new Dictionary<SqlBinary, string>();
						using (
							SqlCommand command =
								new SqlCommand($"SELECT _Fld11181RRef,min(_Fld11179) FROM {barcodeTableName} group by _Fld11181RRef", conn))
						{
							var dataReader = command.ExecuteReader();
							while (dataReader.Read())
							{
								var binary = dataReader.GetSqlBinary(0);
								if (!barcodeDictionary.ContainsKey(binary))
									barcodeDictionary.Add(dataReader.GetSqlBinary(0), dataReader.GetString(1));
							}
							dataReader.Close();
						}
						// Выборка номенклатуры
						List<Nom> list = new List<Nom>();
						using (SqlCommand comm = new SqlCommand(
							$@"with tree (_IDRRef,_ParentIDRRef,_Description,_Code,_Fld2247RRef,_level) as 
(select _IDRRef,_ParentIDRRef,_Description,_Code,_Fld2247RRef,0 from [{nomTableName}] where _code='00000000001'
union all select [{nomTableName}]._IDRRef,[{nomTableName}]._ParentIDRRef,[{nomTableName}]._Description,[{nomTableName}]._Code,[{nomTableName}]._Fld2247RRef,tree._level+1 from [{nomTableName}],tree
where tree._IDRRef=[{nomTableName}]._ParentIDRRef and [{nomTableName}]._Code<>'С1-00001709' and [{nomTableName}]._Code<>'С1-00001897'
and [{nomTableName}]._Code<>'00000000018' and [{nomTableName}]._Code<>'00000000002' and [{nomTableName}]._Code<>'00000000049' and [{nomTableName}]._Code<>'00000000052' and [{nomTableName}]._Code<>'00000000084' and [{nomTableName}]._Code<>'00000000732')
select _IDRRef,_ParentIDRRef,_Description,_Code,_Fld2247RRef from tree;", conn))
						{
							SqlDataReader dataReader = comm.ExecuteReader();
							StreamWriter writer = new StreamWriter(new FileStream(filename, FileMode.Create), Encoding.GetEncoding(1251));

							while (dataReader.Read())
							{
								Nom nom = new Nom();
								SqlBinary binaryId = dataReader.GetSqlBinary(0);
								nom.Id = Utils.SqlBinaryToString(binaryId);
								nom.ParentId = Utils.SqlBinaryToString(dataReader.GetSqlBinary(1));
								nom.Name = dataReader.GetString(2);
								nom.Code = dataReader.GetString(3);

								if (dataReader.IsDBNull(4))
								{
									nom.EdIzmId = null;
								}
								else
								{
									var EdIzmId = dataReader.GetSqlBinary(4);
									nom.EdIzm = edIzmDictionary[EdIzmId];
								}
								if (priceDictionary.ContainsKey(binaryId))
								{
									nom.Price = priceDictionary[binaryId].Item2;
									nom.Period = new DateTime(priceDictionary[binaryId].Item1.Year - 2000, priceDictionary[binaryId].Item1.Month,
										priceDictionary[binaryId].Item1.Day);
								}
								else
								{
									nom.Price = 0;
									nom.Period = null;
								}
								if (partnerDictionary.ContainsKey(binaryId))
									nom.Partner = partnerDictionary[binaryId];
								else
									nom.Partner = null;
								if (barcodeDictionary.ContainsKey(binaryId))
									nom.Barcode = barcodeDictionary[binaryId];
								else
									nom.Barcode = null;
								list.Add(nom);
								writer.WriteLine(nom.ToString());
								writer.Flush();
							}
							writer.Close();
							dataReader.Close();
						}
						conn.Close();
						Node root = new Node(list[0]);
						for (int i = 1; i < list.Count; i++)
						{
							var node = Node.Search(root, list[i].ParentId);
							if (node != null)
								node.children.Add(new Node(list[i]));
						}
						//Node.Clear(root);
						Node.SaveToFile(root, filenameClean);
						NomList = GetLocalNom(filenameClean);

					}
				else
				{
					if (File.Exists(filenameClean))
					{
						NomList = GetLocalNom(fileInfoClean.FullName);
					}
					else
					{
						using (File.Create(filenameClean)) ;
						fileInfoClean = new FileInfo(filenameClean);
						List<Nom> list = new List<Nom>();
						list = GetLocalNom(fileInfo.FullName);
						Node root = new Node(list[0]);
						for (int i = 1; i < list.Count; i++)
						{
							var node = Node.Search(root, list[i].ParentId);
							if (node != null)
								node.children.Add(new Node(list[i]));
						}
						//Node.Clear(root);
						Node.SaveToFile(root, fileInfoClean.FullName);
						NomList = GetLocalNom(fileInfoClean.FullName);
					}
				}

			}

			public List<Nom> GetLocalNom()
			{
				//try
				//{
				var nomLines = File.ReadAllLines(fileInfo.FullName, Encoding.GetEncoding(1251));
				var nomList = new List<Nom>();
				foreach (var nomLine in nomLines)
					nomList.Add(Nom.Parse(nomLine));
				return nomList;
				//}
				//catch (Exception e)
				//{
				//	return null;
				//}

			}

			public List<Nom> GetLocalNom(string fileName)
			{
				try
				{
					var nomLines = File.ReadAllLines(fileName, Encoding.GetEncoding(1251));
					var nomList = new List<Nom>();
					foreach (var nomLine in nomLines)
						nomList.Add(Nom.Parse(nomLine));
					return nomList;
				}
				catch (Exception e)
				{
					return null;
				}

			}

			public int GetLocalNomCount()
			{
				try
				{
					int count = File.ReadAllLines(filename).Length;
					return count;
				}
				catch (IOException ioException)
				{
					return 0;
				}
			}
		}
		public override string WhatAPost(string nameFile)
		{
			string pref = "";

			if (nameFile.Contains("report_ОСП Бичурский почтамт")) pref = "42";
			if (nameFile.Contains("report_ОСП Закаменский почтамт")) pref = "43";
			if (nameFile.Contains("report_ОСП Кабанский почтамт")) pref = "44";
			if (nameFile.Contains("report_ОСП Прибайкальский почтамт")) pref = "45";
			if (nameFile.Contains("report_ОСП Северобайкальский почтамт")) pref = "46";
			if (nameFile.Contains("report_ОСП Улан-Удэнский почтамт")) pref = "47";
			if (nameFile.Contains("report_ОСП Хоринский почтамт")) pref = "48";

			return pref;

		}

		public override string GetName()
		{
			return "УФПС Бурятии";
		}
	}
}