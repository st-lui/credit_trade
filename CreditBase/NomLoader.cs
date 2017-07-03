using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using CreditBase;
using PostReq.Model;

namespace PostReq.Util
{
	public class NomLoader
	{
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

		string connectionString;
		string filename, filenameClean;
		FileInfo fileInfo, fileInfoClean;
		string nomTableName = "_Reference112";
		string edIzmTableName = "_Reference80";
		string priceTableName = "_InfoRg11000";
		string partnerTableName = "_Reference122";
		string partnerNomTableName = "_Reference113";
		private string barcodeTableName = "_InfoRg11025";
		public List<Nom> NomList { get; set; }
		private NomLoader() { }

		public static NomLoader Create(string srv, string dbname,string usr, string pswd, string reg)
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
						Dictionary<SqlBinary, Tuple<DateTime, double>> priceDictionary = new Dictionary<SqlBinary, Tuple<DateTime, double>>();
						using (SqlCommand comm =new SqlCommand($@"select _Fld11001RRef,_Fld11004,s._Period from {priceTableName} s,(select _Fld11001RRef nomid, max(_Period) period from {priceTableName} where _Fld11002RRef = 0xA4AEF4CE46FB566011E3DC100178205B group by _Fld11001RRef) p where s._Fld11001RRef = p.nomid and s._Period = p.period and s._Fld11002RRef = 0xA4AEF4CE46FB566011E3DC100178205B",conn))
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
						using (SqlCommand command = new SqlCommand($"select nom._Fld2319RRef,partner._Description from {partnerNomTableName} nom,{partnerTableName} partner where nom._OwnerIDRRef=partner._IDRRef", conn))
						{
							var dataReader = command.ExecuteReader();
							while (dataReader.Read())
							{
								var binary = dataReader.GetSqlBinary(0);
								if (!partnerDictionary.ContainsKey(binary))
									partnerDictionary.Add(dataReader.GetSqlBinary(0),dataReader.GetString(1));
							}
							dataReader.Close();
						}
						//Выборка штрихкодов
						Dictionary<SqlBinary, string> barcodeDictionary = new Dictionary<SqlBinary, string>();
						using (SqlCommand command = new SqlCommand($"SELECT _Fld11028RRef,min(_Fld11026) FROM {barcodeTableName} group by _Fld11028RRef", conn))
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
						using (SqlCommand comm =new SqlCommand(
									$@"with tree (_IDRRef,_ParentIDRRef,_Description,_Code,_Fld2258RRef,_level) as 
(select _IDRRef,_ParentIDRRef,_Description,_Code,_Fld2258RRef,0 from [{nomTableName}] where _code='00000000001'
union all select [{nomTableName}]._IDRRef,[{nomTableName}]._ParentIDRRef,[{nomTableName}]._Description,[{nomTableName}]._Code,[{nomTableName}]._Fld2258RRef,tree._level+1 from [{nomTableName}],tree
where tree._IDRRef=[{nomTableName}]._ParentIDRRef and [{nomTableName}]._Code<>'С1-00001709' and [{nomTableName}]._Code<>'С1-00001897'
and [{nomTableName}]._Code<>'00000000018' and [{nomTableName}]._Code<>'00000000002' and [{nomTableName}]._Code<>'00000000049' and [{nomTableName}]._Code<>'00000000052' and [{nomTableName}]._Code<>'00000000084' and [{nomTableName}]._Code<>'00000000732')
select _IDRRef,_ParentIDRRef,_Description,_Code,_Fld2258RRef from tree;", conn))
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
}
