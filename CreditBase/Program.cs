using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Globalization;
using OfficeOpenXml;
using PostReq.Model;
using PostReq.Util;
namespace CreditBase
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(ToPost());
            //Console.WriteLine(ToPostOffice());
            //Console.WriteLine(ToWarehouses());
            //using PostReq.Model;

            //NomLoader NL = NomLoader.Create();
            //NL.UpdateLocalNom();
            //Console.WriteLine(Goods());
            //Console.ForegroundColor = ConsoleColor.Green;
            //CheckWareHouse();
            Leftovers();

            Console.WriteLine("Для выхода нажмите клавишу...");
            Console.ReadKey();
        }



        public static string ToPost()
        {
            string connStr = @"Data Source=R22Aufvdocv01\SQL;
                            Initial Catalog=credit_trade;
                            Integrated Security=False;User ID=credit;Password=123456;";
            List<string> PostsList = new List<string>() {
            "Алейский почтамт",
            "Барнаульский почтамт",
            "Барнаульский участок курьерской доставки 656990",
            "Белокурихинский участок курьерской доставки 659990",
            "Бийский почтамт",
            "Бийский участок курьерской доставки 659390",
            "Благовещенский почтамт",
            "Заринский почтамт",
            "Каменский почтамт ",
            "Кулундинский почтамт",
            "Мамонтовский почтамт",
            "Павловский почтамт",
            "Первомайский почтамт",
            "Поспелихинский почтамт",
            "Рубцовский почтамт",
            "Рубцовский участок курьерской доставки 658290",
            "Славгородский почтамт",
            "Смоленский почтамт",
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


                string connStr = @"Data Source=R22Aufvdocv01\SQL;
                            Initial Catalog=credit_trade;
                            Integrated Security=False;User ID=credit;Password=123456;";

                SqlConnection conn = new SqlConnection(connStr);

                conn.Open();


                for (int i = 2; i < Offices; i++)
                {

                    string query = string.Format(@"INSERT INTO [credit_trade].[dbo].[postoffices]
           ([idx]
           ,[name_ops]
           ,[post_id])
     VALUES
           ('{0}','{1}','{2}')", Null(listOffices.Cells["C" + i].Value), Null(listOffices.Cells["B" + i].Value), WhatAPost(Null(listOffices.Cells["A" + i].Value), conn));

                    SqlCommand sqlQueryInsert = new SqlCommand(query, conn);
                    sqlQueryInsert.ExecuteNonQuery();




                }
                conn.Close();
                conn.Dispose();





            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: Невозможно прочитать файл на диске. Описание ошибки: " + ex.Message);
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
            string connStr = @"Data Source=R22Aufvdocv01\SQL;
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
        FROM[credit_trade].[dbo].[postoffices]";
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

        public static string Goods()
        {
            //Nom Parse(string s)
            string PathNom = Path.Combine(Environment.CurrentDirectory, "appdata", "nom.txt");
            //StreamReader SR = new StreamReader(PathNom, Encoding.GetEncoding("windows-1251"));
            string[] AllLines = File.ReadAllLines(PathNom, Encoding.GetEncoding("windows-1251"));

            Dictionary<String, Nom> NomDictionary = new Dictionary<string, Nom>();
            Dictionary<String, Nom> BaseDictionary = new Dictionary<string, Nom>();

            for (int i = 0; i < AllLines.Length; i++)
            {
                Nom TempNom = Nom.Parse(AllLines[i]);
                NomDictionary.Add(TempNom.Id, TempNom);
                //Console.WriteLine(TempNom.ToString());
            }

            string connStr = @"Data Source=R22Aufvdocv01\SQL;
                            Initial Catalog=credit_trade;
                            Integrated Security=False;User ID=credit;Password=123456;";

            SqlConnection conn = new SqlConnection(connStr);
            SqlConnection connInsUpd = new SqlConnection(connStr);
            conn.Open();
            connInsUpd.Open();


            string SelectQuery = @"SELECT [id]
      ,[nom_id]
      ,[parent_id]
      ,[name]
      ,[edizm]
      ,[price]
      ,[barcode]
  FROM [credit_trade].[dbo].[goods]";
            SqlCommand sqlQuerySelect = new SqlCommand(SelectQuery, conn);

            using (SqlDataReader drNew = sqlQuerySelect.ExecuteReader())
            {
                if (drNew.HasRows)
                {
                    while (drNew.Read())
                    {
                        Nom TempFromBase = new Nom()
                        {
                            Id = drNew.GetValue(1).ToString().Trim(),
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

                    if (NomSearchBase.Price != NomSearchNom.Price)
                    {
                        string price = NomSearchNom.Price.ToString(CultureInfo.GetCultureInfo("en-US"));
                        string query = string.Format(@"UPDATE [credit_trade].[dbo].[goods] SET [price] = '{0}' WHERE  nom_id='{1}'", price, NomSearchBase.Id);

                        SqlCommand sqlQueryInsert = new SqlCommand(query, connInsUpd);
                        sqlQueryInsert.ExecuteNonQuery();
                    }
                }
                else
                {
                    string price = NomSearchNom.Price.ToString(CultureInfo.GetCultureInfo("en-US"));
                    string query = string.Format(@"INSERT INTO [credit_trade].[dbo].[goods] ([nom_id],[parent_id],[name],[edizm],[price],[barcode])
     VALUES ('{0}','{1}','{2}','{3}', {4} ,'{5}')",
           NomSearchNom.Id,
           NomSearchNom.ParentId,
           NomSearchNom.Name.Replace("'", "''"),
           NomSearchNom.EdIzm,
           price,
           NomSearchNom.Barcode);

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


        public static string CheckWareHouse()
        {
            string connStr = @"Data Source=R22Aufvdocv01\SQL;
                            Initial Catalog=credit_trade;
                            Integrated Security=False;User ID=credit;Password=123456;";

            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            SqlConnection connPref = new SqlConnection(connStr);


            //try
            //{

            string PathLeftovers = @"\\10.56.0.154\1c";

            string[] FilesLeftovers = Directory.GetFiles(PathLeftovers);
            // for (int i = 0; i < FilesLeftovers.Length; i++)
            // {
            int i = 0;
            int g = 0;

            Dictionary<string, WareHouse> DicWarehouse = new Dictionary<string, WareHouse>();
            Dictionary<string, Good> DicGood = new Dictionary<string, Good>();

            string SelectQueryWarehouse = @"SELECT [id]
      ,[name]
      ,[postoffice_id]
  FROM [credit_trade].[dbo].[warehouses]";
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
                            name_w = preffix + "_" + drNewWarehouse.GetValue(1).ToString()

                        };
                        //Console.WriteLine(TempFromBase.name_w);
                        DicWarehouse.Add(TempFromBase.name_w, TempFromBase);

                    }
                }
            }

            conn.Open();

            string SelectQueryGood = @"SELECT [id],[name] FROM [credit_trade].[dbo].[goods]";
            SqlCommand sqlQuerySelectGood = new SqlCommand(SelectQueryGood, conn);

            using (SqlDataReader drNewGood = sqlQuerySelectGood.ExecuteReader(CommandBehavior.CloseConnection))
            {
                if (drNewGood.HasRows)
                {
                    while (drNewGood.Read())
                    {
                        Good TempFromBase = new Good()
                        {
                            id_g = drNewGood.GetValue(0).ToString().Trim(),
                            name_g = drNewGood.GetValue(1).ToString()

                        };
                        //Console.WriteLine(TempFromBase.name_g);
                        DicGood.Add(TempFromBase.name_g, TempFromBase);

                    }
                }
            }
            connPref.Close();
            conn.Close();


            foreach (var OneFileLeftovers in FilesLeftovers)
            {

                Console.WriteLine("Файл - " + OneFileLeftovers);


                var ExcelOffices = new ExcelPackage(new FileInfo(OneFileLeftovers));
                var listOffices = ExcelOffices.Workbook.Worksheets[1];


                var rowCnt = listOffices.Dimension.End.Row;
                var colCnt = listOffices.Dimension.End.Column;

                int warehouse_id = 0;

                string WarehouseName = "";


                for (int col = 8; col <= colCnt - 1; col++)
                {
                    WarehouseName = Null(listOffices.Cells[8, col].Value);

                    string preffix = WhatAPost(OneFileLeftovers);
                    WarehouseName = preffix + "_" + WarehouseName;

                    if (DicWarehouse.ContainsKey(WarehouseName))
                    {
                        WareHouse wareHouse;

                        DicWarehouse.TryGetValue(WarehouseName, out wareHouse);
                        warehouse_id = Convert.ToInt32(wareHouse.id_w);

                    }
                    else
                    {

                        Console.WriteLine("Не найден склад: " + WarehouseName);
                    }

                }

                Console.WriteLine("Количество строк: " + rowCnt);
                Console.WriteLine("Количество столбцов: " + colCnt);

            }



            return "Leftovers: Успешная загрузка";
        }

        public static string Leftovers()
        {
            string connStr = @"Data Source=R22Aufvdocv01\SQL;
                            Initial Catalog=credit_trade;
                            Integrated Security=False;User ID=credit;Password=123456;";

            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            SqlConnection connPref = new SqlConnection(connStr);


            //try
            //{

            string PathLeftovers = @"\\10.56.0.154\1c";

            string[] FilesLeftovers = Directory.GetFiles(PathLeftovers);
            // for (int i = 0; i < FilesLeftovers.Length; i++)
            // {
            int i = 0;
            int g = 0;

            Dictionary<string, WareHouse> DicWarehouse = new Dictionary<string, WareHouse>();
            Dictionary<string, Good> DicGood = new Dictionary<string, Good>();

            string SelectQueryWarehouse = @"SELECT [id]
      ,[name]
      ,[postoffice_id]
  FROM [credit_trade].[dbo].[warehouses]";
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
                            name_w = preffix + "_" + drNewWarehouse.GetValue(1).ToString()

                        };
                        //Console.WriteLine(TempFromBase.name_w);
                        DicWarehouse.Add(TempFromBase.name_w, TempFromBase);

                    }
                }
                drNewWarehouse.Close();
            }
            //connPref.Close();
            conn.Open();

            string SelectQueryGood = @"SELECT [id],[name] FROM [credit_trade].[dbo].[goods]";
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
                            name_g = drNewGood.GetValue(1).ToString()

                        };
                        //Console.WriteLine(TempFromBase.name_g);
                        DicGood.Add(TempFromBase.name_g, TempFromBase);

                    }
                }
                drNewGood.Close();
            }
            
            //conn.Close();


            foreach (var OneFileLeftovers in FilesLeftovers)
            {
                i++;
                g = 0;
                Console.WriteLine("Файл - " + OneFileLeftovers);


                var ExcelOffices = new ExcelPackage(new FileInfo(OneFileLeftovers));
                var listOffices = ExcelOffices.Workbook.Worksheets[1];


                var rowCnt = listOffices.Dimension.End.Row;
                var colCnt = listOffices.Dimension.End.Column;

                int warehouse_id = 0;
                int good_id = 0;
                double amount = 0;

                string WarehouseName = "";
                string GoodName = "";

                for (int col = 8; col <= colCnt - 1; col++)
                {
                    WarehouseName = Null(listOffices.Cells[8, col].Value);

                    string preffix = WhatAPost(OneFileLeftovers);
                    WarehouseName = preffix + "_" + WarehouseName;
                    //Console.WriteLine(WarehouseName);
                    //conn.Open();

                    for (int row = 10; row <= rowCnt - 1; row++)
                    {
                        //g++;
                        //Console.WriteLine("Строка в файле - " + g);

                        GoodName = Null(listOffices.Cells[row, 3].Value);
                        GoodName = GoodName.Substring(0, GoodName.LastIndexOf(','));
                        GoodName = GoodName.Replace("\n", "");
                        if (Null(listOffices.Cells[row, col].Value) != "")
                            amount = Convert.ToDouble(Null(listOffices.Cells[row, col].Value));
                        else continue;

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
                            continue;
                            //Console.WriteLine("Не найден склад!");
                        }

                        if (DicGood.ContainsKey(GoodName))
                        {
                            Good good;

                            DicGood.TryGetValue(GoodName, out good);
                            good_id = Convert.ToInt32(good.id_g);
                            if (good_id == 0) continue;
                            //Console.WriteLine(good_id);
                        }
                        else
                        {
                            //Console.WriteLine(GoodName); Console.WriteLine("Не найден товар!");
                            continue;
                        }
                        //conn.Open();

                        string SelectQueryGoodInBase = @"SELECT [id]
      ,[warehouse_id]
      ,[good_id]
      ,[amount]
  FROM [credit_trade].[dbo].[leftovers] WHERE warehouse_id=" + warehouse_id + " AND good_id=" + good_id + "";
                        SqlCommand sqlQuerySelectGoodInBase = new SqlCommand(SelectQueryGoodInBase, conn);
                        //conn.Open();

                        using (SqlDataReader drNewGood = sqlQuerySelectGoodInBase.ExecuteReader())
                        {
                            if (drNewGood.HasRows)
                            {
                                string amountNew = amount.ToString(CultureInfo.GetCultureInfo("en-US"));
                                string query = string.Format(@"
UPDATE [credit_trade].[dbo].[leftovers]   SET [amount] = {0} WHERE [warehouse_id] = {1} AND [good_id] = {2}", amountNew, warehouse_id, good_id);


                                drNewGood.Close();
                               // conn.Close();
                                //conn.Open();
                                SqlCommand sqlQueryInsert = new SqlCommand(query, conn);
                                sqlQueryInsert.ExecuteNonQuery();
                               // conn.Close();
                            }

                            else
                            {

                                string amountNew = amount.ToString(CultureInfo.GetCultureInfo("en-US"));
                                string query = string.Format(@"
INSERT INTO [credit_trade].[dbo].[leftovers]
           ([warehouse_id]
           ,[good_id]
           ,[amount])
     VALUES
           ({0},{1},{2})
", warehouse_id, good_id, amountNew);
                                drNewGood.Close();
                                //conn.Close();
                                //conn.Open();
                                SqlCommand sqlQueryInsert = new SqlCommand(query, conn);
                                sqlQueryInsert.ExecuteNonQuery();
                                //conn.Close();

                            }
                        }

                    }

                }

                //Console.WriteLine(OneFileLeftovers.ToString());
                Console.WriteLine("Количество строк: " + rowCnt);
                Console.WriteLine("Количество столбцов: " + colCnt);
                //bool flag = true;
                //int countOffices = 0;
                //int Offices = 2;

                //while (flag)
                //{
                //    if (Null(listOffices.Cells["A" + Offices].Value).Equals(""))
                //       flag = false;
                //    else { Offices++; countOffices++; }
                //}


                //               string connStr = @"Data Source=R22Aufvdocv01\SQL;
                //                       Initial Catalog=credit_trade;
                //                       Integrated Security=False;User ID=credit;Password=123456;";

                //           SqlConnection conn = new SqlConnection(connStr);

                //           conn.Open();


                //           for (int i = 2; i < Offices; i++)
                //           {

                //               string query = string.Format(@"INSERT INTO [credit_trade].[dbo].[postoffices]
                //      ([idx]
                //      ,[name_ops]
                //      ,[post_id])
                //VALUES
                //      ('{0}','{1}','{2}')", Null(listOffices.Cells["C" + i].Value), Null(listOffices.Cells["B" + i].Value), WhatAPost(Null(listOffices.Cells["A" + i].Value), conn));

                //               SqlCommand sqlQueryInsert = new SqlCommand(query, conn);
                //               sqlQueryInsert.ExecuteNonQuery();




                //           }
                //           conn.Close();
                //           conn.Dispose();




            }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Ошибка: Описание ошибки: " + ex.Message);
            //}
            conn.Close();

            return "Leftovers: Успешная загрузка";
        }
    

        public static string WhatAPost(string nameFile)
        {
            string pref = "";
            if (nameFile.Contains("report_Алейский почтамт")) pref = "24";
            if (nameFile.Contains("report_Барнаульский почтамт")) pref = "25";
            if (nameFile.Contains("report_Барнаульский УКД")) pref = "26";
            if (nameFile.Contains("report_Белокурихинский УКД")) pref = "27";
            if (nameFile.Contains("report_Бийский почтамт")) pref = "28";
            if (nameFile.Contains("report_Бийский УКД")) pref = "29";
            if (nameFile.Contains("report_Благовещенский почтамт")) pref = "30";
            if (nameFile.Contains("report_Заринский почтамт")) pref = "31";
            if (nameFile.Contains("report_Каменский почтамт")) pref = "32";
            if (nameFile.Contains("report_Кулундинский почтамт")) pref = "33";
            if (nameFile.Contains("report_Мамонтовский почтамт")) pref = "34";
            if (nameFile.Contains("report_Павловский почтамт")) pref = "35";
            if (nameFile.Contains("report_Первомайский почтамт")) pref = "36";
            if (nameFile.Contains("report_Поспелихинский почтамт")) pref = "37";
            if (nameFile.Contains("report_Рубовский УКД")) pref = "39";
            if (nameFile.Contains("report_Рубцовский почтамт")) pref = "38";
            if (nameFile.Contains("report_Славгородский почтамт")) pref = "40";
            if (nameFile.Contains("report_Смоленский почтамт")) pref = "41";


            return pref;

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

    }
}
