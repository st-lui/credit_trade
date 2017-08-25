using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using DbModel;
using Nancy;
using NPOI.HSSF.UserModel;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace credittrade.Modules
{
	public class Utils
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
		/// <param name="requestRows"></param>
		/// <param name="rootPath">Путь к appdata на сервере</param>
		/// <returns></returns>
		public static MemoryStream GeneratePrintFormUfps(request request, IEnumerable<request_rows> requestRows, string rootPath)
		{
			if (!Directory.Exists(Path.Combine(rootPath, "files")))
				Directory.CreateDirectory(Path.Combine(rootPath, "files"));
			string filename = null;
			string templateFilename = Path.Combine(rootPath, "template_ufps.xls");
			MemoryStream ms = new MemoryStream();

			int height = 15;
			int signHeight = 3;
			int headHeight = 4;
			int dataHeight = 7;
			if (File.Exists(templateFilename))
			{
				using (Stream templateStream = new FileStream(templateFilename, FileMode.Open))
				{
					HSSFWorkbook wb = new HSSFWorkbook(templateStream);
					HSSFSheet sheet = (HSSFSheet)wb.GetSheetAt(0);
					int firstRowNum = sheet.FirstRowNum;
					int first = firstRowNum;
					int last = firstRowNum + height - 1;
					sheet.GetRow(first + headHeight).Cells[0].SetCellValue($"{request.user.warehouse.postoffice.idx} {request.user.warehouse.name} {request.user.warehouse.postoffice.post.name}");
					sheet.GetRow(first + headHeight + 1).Cells[0].SetCellValue(
						$"Заказ №{(request.id == 0 ? "" : request.id.ToString())} от {request.date:dd.MM.yyyy}");
					sheet.GetRow(first + headHeight + 2).Cells[0].SetCellValue($"ФИО покупателя: {request.buyer.fio}");

					var row = sheet.GetRow(first + dataHeight);
					HSSFCellStyle autoHeightCellStyle = (HSSFCellStyle)wb.CreateCellStyle();
					autoHeightCellStyle.CloneStyleFrom(row.Cells[0].CellStyle);
					autoHeightCellStyle.WrapText = true;
					int i = 1;
					int positionNumber = 1;
					foreach (var requestRow in requestRows)
					{
						row = sheet.CopyRow(first + dataHeight, first + dataHeight + i);
						row.Cells[0].SetCellValue(positionNumber++);
						row.Cells[1].SetCellValue("");
						row.Cells[4].CellStyle = autoHeightCellStyle;
						row.Cells[4].SetCellValue(requestRow.name);
						row.Cells[8].SetCellValue((double)requestRow.amount);
						row.Cells[9].SetCellValue((double)requestRow.price);
						row.Cells[10].SetCellValue((double)(requestRow.price * requestRow.amount));
						//sheet.ShiftRows(last[r]-signHeight+1, last[last.Count-1], 1);
						last++;
						i++;

					}
					row = sheet.GetRow(last - signHeight + 1);

					row.CreateCell(8).SetCellValue("");
					row.CreateCell(5);
					row.CreateCell(9);
					row = sheet.GetRow(last);
					row.CreateCell(8).SetCellValue("");
					row.CreateCell(5);
					row.CreateCell(9);
					sheet.GetRow(first + headHeight).Height = sheet.GetRow(first + headHeight).Height;
					sheet.GetRow(first + headHeight + 1).Height = sheet.GetRow(first + headHeight + 1).Height;
					sheet.GetRow(first + headHeight + 2).Height = sheet.GetRow(first + headHeight + 2).Height;
					wb.Write(ms);
					wb.Close();
				}


			}
			return ms;
		}

		static Dictionary<int, decimal> GetDebtForRequests(IList<request> penaltyRequests)
		{
			Dictionary<int, decimal> debtDictionary=new Dictionary<int, decimal>();
			foreach (var penaltyRequest in penaltyRequests)
			{
				if (debtDictionary.ContainsKey(penaltyRequest.buyer.warehouse.id))
					debtDictionary[penaltyRequest.buyer.warehouse.id] += penaltyRequest.cost.Value;
				else
					debtDictionary.Add(penaltyRequest.buyer.warehouse.id,penaltyRequest.cost.Value);
			}
			return debtDictionary;
		}

		public static MemoryStream GenReport(InOutReportModel reportModel)
		{
			IList<request> req = reportModel.Requests;
			IList<request> penaltyRequests = reportModel.RequestsPenalty;
			string start=reportModel.Start;
			string finish = reportModel.Finish;
			var ms = new MemoryStream();
			ExcelPackage excelPackage = new ExcelPackage();
			Dictionary<int, decimal> debts = GetDebtForRequests(penaltyRequests);
			excelPackage.Workbook.Worksheets.Add("Оборотная ведомость");
			var sheet = excelPackage.Workbook.Worksheets[1];
			sheet.Cells[1, 1].Value = "Оборотная ведомость";
			sheet.Cells[1, 1].Style.Font.Size = 16;
			sheet.Cells[2, 1].Value = $"Период: {start} - {finish}";
			sheet.Cells[2, 1].Style.Font.Size = 13;
			sheet.Cells[3, 1].Value = "Склад"; sheet.Cells[3, 2].Value = "Отпущено на сумму"; sheet.Cells[3, 3].Value = "Из них погашено";
			sheet.Cells[3, 4].Value = "Просроченный долг";
			sheet.Cells[3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 1, 3, 4].Style.Font.Size = 13;
			var groupedRequests = req.GroupBy(x => x.buyer.warehouse_id);
			int i = 4;
			decimal total = 0,paid=0,penalty=0;
			foreach (IGrouping<int?, request> group in groupedRequests)
			{
				string warehouseName = group.ElementAt(0).buyer.warehouse.name;
				int warehouseId = group.ElementAt(0).buyer.warehouse.id;
				decimal totalDebt = 0, paidDebt = 0,pen=0;
				foreach (var request in group)
				{
					totalDebt += request.cost.Value;
					if (request.paid.Value)
						paidDebt += request.cost.Value;
				}
				if (debts.ContainsKey(warehouseId))
					pen = debts[warehouseId];
				paid += paidDebt;
				total += totalDebt;
				penalty += pen;
				sheet.Cells[i, 1].Value = warehouseName; sheet.Cells[i, 2].Value = totalDebt; sheet.Cells[i, 3].Value = paidDebt;
				sheet.Cells[i, 4].Value = pen;
				sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				i++;
			}
			sheet.Cells[i, 1].Value = "Итого"; sheet.Cells[i, 2].Value = total; sheet.Cells[i, 3].Value = paid;
			sheet.Cells[i, 4].Value = penalty;
			sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 1, i, 4].Style.Font.Size = 13;
			const double minWidth = 0.00;
			const double maxWidth = 50.00;
			sheet.Cells.AutoFitColumns(minWidth, maxWidth);
			excelPackage.SaveAs(ms);
			return ms;
		}

		public static MemoryStream GenReportUfps(InOutReportModel reportModel)
		{
			var req = reportModel.Requests;
			var start = reportModel.Start;
			var finish = reportModel.Finish;
			IList<request> penaltyRequests = reportModel.RequestsPenalty;
			Dictionary<int, decimal> debts = GetDebtForRequests(penaltyRequests);
			var ms = new MemoryStream();
			ExcelPackage excelPackage = new ExcelPackage();
			excelPackage.Workbook.Worksheets.Add("Оборотная ведомость");
			var sheet = excelPackage.Workbook.Worksheets[1];

			int k = 0;
			var groupedByPost = req.GroupBy(x => x.buyer.warehouse.postoffice.post_id).OrderBy(x=>x.ElementAt(0).buyer.warehouse.postoffice.post.name);
			foreach (IGrouping<int, request> requestsByPost in groupedByPost)
			{
				sheet.Cells[k+1, 1].Value = $"Оборотная ведомость - {requestsByPost.ElementAt(0).buyer.warehouse.postoffice.post.name}";
				sheet.Cells[k+1, 1].Style.Font.Size = 16;
				sheet.Cells[k+2, 1].Value = $"Период: {start} - {finish}";
				sheet.Cells[k+2, 1].Style.Font.Size = 13;
				sheet.Cells[k+3, 1].Value = "Склад"; sheet.Cells[k+3, 2].Value = "Отпущено на сумму"; sheet.Cells[k+3, 3].Value = "Из них погашено";
				sheet.Cells[k+3, 4].Value = "Просроченный долг";
				sheet.Cells[k+3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 1, k+3, 4].Style.Font.Size = 13;

				var groupedRequests = requestsByPost.GroupBy(x => x.buyer.warehouse_id);
				int i = k+4;
				decimal total = 0, paid = 0,penalty=0;
				foreach (IGrouping<int?, request> group in groupedRequests)
				{
					string warehouseName = group.ElementAt(0).buyer.warehouse.name;
					int warehouseId = group.ElementAt(0).buyer.warehouse.id;
					decimal totalDebt = 0, paidDebt = 0,pen=0;
					foreach (var request in group)
					{
						totalDebt += request.cost.Value;
						if (request.paid.Value)
							paidDebt += request.cost.Value;
						if (debts.ContainsKey(warehouseId))
							pen = debts[warehouseId];
					}
					paid += paidDebt;
					total += totalDebt;
					penalty+= pen;
					sheet.Cells[i, 1].Value = warehouseName; sheet.Cells[i, 2].Value = totalDebt; sheet.Cells[i, 3].Value = paidDebt;
					sheet.Cells[i, 4].Value = pen;
					sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					i++;
				}
				sheet.Cells[i, 1].Value = "Итого"; sheet.Cells[i, 2].Value = total; sheet.Cells[i, 3].Value = paid;
				sheet.Cells[i, 4].Value = penalty;
				sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 1, i, 4].Style.Font.Size = 13;
				k=i+1;
			}
			const double minWidth = 0.00;
			const double maxWidth = 50.00;
			sheet.Cells.AutoFitColumns(minWidth, maxWidth);
			excelPackage.SaveAs(ms);
			return ms;
		}
		public static MemoryStream GenReportGoods(InOutReportModel reportModel)
		{
			var req = reportModel.Requests;
			var start = reportModel.Start;
			var finish = reportModel.Finish;
			var ms = new MemoryStream();
			ExcelPackage excelPackage = new ExcelPackage();
			excelPackage.Workbook.Worksheets.Add("Ведомость по товарам");
			List<request_rows> requestRows = new List<request_rows>();
			foreach (request r in req)
			{
				foreach (var rr in r.request_rows)
				{
					requestRows.Add(rr);
				}
			}
			var sheet = excelPackage.Workbook.Worksheets[1];

			int k = 0;
			var groupedByPost = requestRows.GroupBy(x => x.request.buyer.warehouse.postoffice.post_id).OrderBy(x => x.ElementAt(0).request.buyer.warehouse.postoffice.post.name);
			foreach (IGrouping<int, request_rows> requestsByPost in groupedByPost)
			{
				sheet.Cells[k + 1, 1].Value = $"Ведомость по товарам - {requestsByPost.ElementAt(0).request.buyer.warehouse.postoffice.post.name}";
				sheet.Cells[k + 1, 1].Style.Font.Size = 16;
				sheet.Cells[k + 2, 1].Value = $"Период: {start} - {finish}";
				sheet.Cells[k + 2, 1].Style.Font.Size = 13;
				sheet.Cells[k + 3, 1].Value = "Склад";
				sheet.Cells[k + 3, 2].Value = "Наименование товара";
				sheet.Cells[k + 3, 3].Value = "Цена";
				sheet.Cells[k + 3, 4].Value = "Количество";
				sheet.Cells[k + 3, 5].Value = "Сумма";
				sheet.Cells[k + 3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k + 3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k + 3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k + 3, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k + 3, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k + 3, 1, k + 3, 5].Style.Font.Size = 13;

				IEnumerable<IGrouping<int?,request_rows>> groupedRequests = requestsByPost.GroupBy(x => x.request.buyer.warehouse_id);
				int i = k + 4;
				foreach (IGrouping<int?, request_rows> group in groupedRequests)
				{
					string warehouseName = group.ElementAt(0).request.buyer.warehouse.name;
					warehouseName += " " + group.ElementAt(0).request.buyer.warehouse.postoffice.idx;
					int warehouseId = group.ElementAt(0).request.buyer.warehouse.id;
					var groupedByGood = group.GroupBy(x => x.goods_id);
					foreach (var groupbygood in groupedByGood){
						string goodsName = groupbygood.First().name;
						string price = groupbygood.First().price.ToString();
						decimal amount = 0;
						decimal cost = 0;
						foreach (request_rows rr in groupbygood)
						{
							amount += rr.amount.Value;
							cost += rr.price.Value*rr.amount.Value;
						}
						sheet.Cells[i, 1].Value = warehouseName;
						sheet.Cells[i, 2].Value = goodsName;
						sheet.Cells[i, 3].Value = price;
						sheet.Cells[i, 4].Value = amount;
						sheet.Cells[i, 5].Value = cost;
						sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						i++;
					}
				}
				//sheet.Cells[i, 1].Value = "Итого"; sheet.Cells[i, 2].Value = total; sheet.Cells[i, 3].Value = paid;
				//sheet.Cells[i, 4].Value = penalty;
				//sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[i, 1, i, 4].Style.Font.Size = 13;
				k = i + 1;
			}
			const double minWidth = 0.00;
			const double maxWidth = 50.00;
			sheet.Cells.AutoFitColumns(minWidth, maxWidth);
			excelPackage.SaveAs(ms);
			return ms;
		}
	}
}