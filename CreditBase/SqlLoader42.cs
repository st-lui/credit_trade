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
	/// Загрузчик данных номенклатуры для Кемеровской области
	/// </summary>
	class SqlLoader42 : SqlLoader
	{
		public SqlLoader42()
		{
			reg = "42";
			srv = "R421C8ASKBDB00.main.russianpost.ru";
			dbname = "r42-asku-work";
			priceTableName= "_InfoRg11044";
			warehouseTableName = "_Reference165";
		}

		public override void LoadNom()
		{
			NomLoader NL = NomLoader.Create("R421C8ASKBDB00.main.russianpost.ru", "r42-asku-work",reg);
			NL.UpdateLocalNom();
		}

		public override void LoadPricesDictionary()
		{
			WarehousePriceKindDictionary = new Dictionary<string, string>();
			PriceKindNomPrice = new Dictionary<string, Dictionary<string, decimal>>();
			string defaultPriceKind = "82322C59E545699111E358BB32E464FC";
			using (SqlConnection conn = new SqlConnection($"data source={srv};initial catalog={dbname};Integrated Security=true"))
			{
				conn.Open();
				SqlCommand command = new SqlCommand($"select _Description,_Fld3256RRef from {warehouseTableName} where _marked=0x00", conn);
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

				command = new SqlCommand($"select _Fld11045RRef nomid,_Fld11048 price,_Fld11046RRef pricekind from {priceTableName} order by _Period", conn);
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
			string nomTableName = "_Reference114";
			string edIzmTableName = "_Reference82";
			string priceTableName = "_InfoRg11044";
			string partnerTableName = "_Reference124";
			string partnerNomTableName = "_Reference115";
			string barcodeTableName = "_InfoRg11069";
			string priceKindsTableName="_Reference65";

			public List<Nom> NomList { get; set; }

			public static NomLoader Create(string srv, string dbname, string reg)
			{
				var nomLoader = new NomLoader();
				//nomLoader.connectionString = "data source=r22aufsql01;initial catalog=r22-asku-work;user=nom_reader;password=6LRZ{w.Y!LHXtY.";
				nomLoader.connectionString = $"data source={srv};initial catalog={dbname};Integrated Security=true";
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
									$@"select _Fld11045RRef,_Fld11048,s._Period from {priceTableName} s,(select _Fld11045RRef nomid, max(_Period) period from {priceTableName} where _Fld11046RRef = 0x82322C59E545699111E358BB32E464FC group by _Fld11045RRef) p where s._Fld11045RRef = p.nomid and s._Period = p.period and s._Fld11046RRef = 0x82322C59E545699111E358BB32E464FC",
									conn))
						{
							var dataReader = comm.ExecuteReader();
							while (dataReader.Read())
							{
								var id = dataReader.GetSqlBinary(0);
								var price = (double)dataReader.GetDecimal(1);
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
									$"select nom._Fld2342RRef,partner._Description from {partnerNomTableName} nom,{partnerTableName} partner where nom._OwnerIDRRef=partner._IDRRef",
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
								new SqlCommand($"SELECT _Fld11072RRef,min(_Fld11070) FROM {barcodeTableName} group by _Fld11072RRef", conn))
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
							$@"with tree (_IDRRef,_ParentIDRRef,_Description,_Code,_Fld2281RRef,_level) as 
(select _IDRRef,_ParentIDRRef,_Description,_Code,_Fld2281RRef,0 from [{nomTableName}] where _code='00000000001'
union all select [{nomTableName}]._IDRRef,[{nomTableName}]._ParentIDRRef,[{nomTableName}]._Description,[{nomTableName}]._Code,[{nomTableName}]._Fld2281RRef,tree._level+1 from [{nomTableName}],tree
where tree._IDRRef=[{nomTableName}]._ParentIDRRef and [{nomTableName}]._Code<>'С1-00001709' and [{nomTableName}]._Code<>'С1-00001897'
and [{nomTableName}]._Code<>'00000000018' and [{nomTableName}]._Code<>'00000000002' and [{nomTableName}]._Code<>'00000000049' and [{nomTableName}]._Code<>'00000000052' and [{nomTableName}]._Code<>'00000000084' and [{nomTableName}]._Code<>'00000000732')
select _IDRRef,_ParentIDRRef,_Description,_Code,_Fld2281RRef from tree;", conn))
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

			if (nameFile.Contains("report_Анжеро-Судженский почтамт.xlsx")) pref = "50";
			if (nameFile.Contains("report_Беловский почтамт.xlsx")) pref = "51";
			if (nameFile.Contains("report_Кемеровский почтамт.xlsx")) pref = "52";
			if (nameFile.Contains("report_Ленинск-Кузнецкий почтамт.xlsx")) pref = "53";
			if (nameFile.Contains("report_Мариинский почтамт.xlsx")) pref = "54";
			if (nameFile.Contains("report_Междуреченский почтамт.xlsx")) pref = "55";
			if (nameFile.Contains("report_Новокузнецкий почтамт.xlsx")) pref = "56";
			if (nameFile.Contains("report_Прокопьевский почтамт.xlsx")) pref = "57";
			if (nameFile.Contains("report_Таштагольский почтамт.xlsx")) pref = "58";
			if (nameFile.Contains("report_Тисульский почтамт.xlsx")) pref = "59";
			if (nameFile.Contains("report_Топкинский почтамт.xlsx")) pref = "60";
			if (nameFile.Contains("report_Тяжинский почтамт.xlsx")) pref = "61";
			if (nameFile.Contains("report_Юргинский почтамт.xlsx")) pref = "62";
			if (nameFile.Contains("report_Яшкинский почтамт.xlsx")) pref = "63";

			return pref;

		}

		
	}

}