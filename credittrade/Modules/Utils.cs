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
		/// Класс, содержащий данные строки отчета
		/// </summary>
		public class ReportRow
		{
			public int warehouseId, postId;
			public string warehouseName,postName;
			public decimal debtBefore, paidBefore, debtBeforeOverdue, spent, paid, debtAfter, paidAfter, debtAfterOverdue;
			public decimal spentBefore, spentAfter;
		}
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

		//public static MemoryStream GenReport(InOutReportModel reportModel)
		//{
		//	IList<request> req = reportModel.RequestsCurrent;
		//	IList<request> penaltyRequests = reportModel.RequestsPenalty;
		//	string start=reportModel.Start;
		//	string finish = reportModel.Finish;
		//	var ms = new MemoryStream();
		//	ExcelPackage excelPackage = new ExcelPackage();
		//	Dictionary<int, decimal> debts = GetDebtForRequests(penaltyRequests);
		//	excelPackage.Workbook.Worksheets.Add("Оборотная ведомость");
		//	var sheet = excelPackage.Workbook.Worksheets[1];
		//	sheet.Cells[1, 1].Value = "Оборотная ведомость";
		//	sheet.Cells[1, 1].Style.Font.Size = 16;
		//	sheet.Cells[2, 1].Value = $"Период: {start} - {finish}";
		//	sheet.Cells[2, 1].Style.Font.Size = 13;
		//	sheet.Cells[3, 1].Value = "Склад"; sheet.Cells[3, 2].Value = "Отпущено на сумму"; sheet.Cells[3, 3].Value = "Из них погашено";
		//	sheet.Cells[3, 4].Value = "Просроченный долг";
		//	sheet.Cells[3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[3, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[3, 1, 3, 4].Style.Font.Size = 13;
		//	var groupedRequests = req.GroupBy(x => x.buyer.warehouse_id);
		//	int i = 4;
		//	decimal total = 0,paid=0,penalty=0;
		//	foreach (IGrouping<int?, request> group in groupedRequests)
		//	{
		//		string warehouseName = group.ElementAt(0).buyer.warehouse.name;
		//		int warehouseId = group.ElementAt(0).buyer.warehouse.id;
		//		decimal totalDebt = 0, paidDebt = 0,pen=0;
		//		foreach (var request in group)
		//		{
		//			totalDebt += request.cost.Value;
		//			if (request.paid.Value)
		//				paidDebt += request.cost.Value;
		//		}
		//		if (debts.ContainsKey(warehouseId))
		//			pen = debts[warehouseId];
		//		paid += paidDebt;
		//		total += totalDebt;
		//		penalty += pen;
		//		sheet.Cells[i, 1].Value = warehouseName; sheet.Cells[i, 2].Value = totalDebt; sheet.Cells[i, 3].Value = paidDebt;
		//		sheet.Cells[i, 4].Value = pen;
		//		sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//		sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//		sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//		sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//		i++;
		//	}
		//	sheet.Cells[i, 1].Value = "Итого"; sheet.Cells[i, 2].Value = total; sheet.Cells[i, 3].Value = paid;
		//	sheet.Cells[i, 4].Value = penalty;
		//	sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
		//	sheet.Cells[i, 1, i, 4].Style.Font.Size = 13;
		//	const double minWidth = 0.00;
		//	const double maxWidth = 50.00;
		//	sheet.Cells.AutoFitColumns(minWidth, maxWidth);
		//	excelPackage.SaveAs(ms);
		//	return ms;
		//}

		public static MemoryStream GenReportUfps(InOutReportModel reportModel)
		{
			var reqCurrent = reportModel.RequestsCurrent;
			var reqBefore = reportModel.RequestsBefore;
			var reqAfter = reportModel.RequestsAfter;
			var start = reportModel.Start;
			var finish = reportModel.Finish;

			DateTime finishDate = reportModel.FinishDate.AddDays(1);
			Dictionary<int, decimal> debts = GetDebtForRequests(reportModel.RequestsPenaltyAfter);
			Dictionary<int, decimal> debtsBefore = GetDebtForRequests(reportModel.RequestsPenaltyBefore);

			Dictionary<int, ReportRow> reportRows = new Dictionary<int, ReportRow>();
			//обработка заявок текущего периода
			foreach (request r in reqCurrent)
			{
				warehouse wh = r.buyer.warehouse;
				ReportRow reportRow;
				if (reportRows.ContainsKey(wh.id))
				{
					reportRow = reportRows[wh.id];
					reportRow.spent += r.cost.Value;
				}
				else
				{
					reportRow = new ReportRow();
					reportRow.warehouseId = wh.id;
					reportRow.warehouseName = wh.name;
					reportRow.postId = wh.postoffice.post.id;
					reportRow.postName = wh.postoffice.post.name;
					reportRow.spent = r.cost.Value;
					reportRows.Add(wh.id, reportRow);
				}
			}
			//обработка заявок на начало периода
			foreach (request r in reqBefore)
			{
				warehouse wh = r.buyer.warehouse;
				ReportRow reportRow;
				if (reportRows.ContainsKey(wh.id))
				{
					reportRow = reportRows[wh.id];
					reportRow.spentBefore += r.cost.Value;
					// если заказ оплачен в пред периоде, то включить его в оплаченные
					if (!(r.paid.Value && r.pay_date.Value < reportModel.StartDate))
						reportRow.paidBefore += r.cost.Value;
				}
				else
				{
					reportRow = new ReportRow();
					reportRow.warehouseId = wh.id;
					reportRow.warehouseName = wh.name;
					reportRow.postId = wh.postoffice.post.id;
					reportRow.postName = wh.postoffice.post.name;
					reportRow.spentBefore += r.cost.Value;
					// если заказ оплачен в пред периоде, то включить его в оплаченные
					if (!(r.paid.Value && r.pay_date.Value < reportModel.StartDate))
						reportRow.paidBefore += r.cost.Value;
					reportRows.Add(wh.id, reportRow);
				}
			}

			//обработка заявок на конец периода
			foreach (request r in reqAfter)
			{
				warehouse wh = r.buyer.warehouse;
				ReportRow reportRow;
				if (reportRows.ContainsKey(wh.id))
				{
					reportRow = reportRows[wh.id];
					reportRow.spentAfter += r.cost.Value;
					// если заказ оплачен в пред периоде, то включить его в оплаченные
					if (!(r.paid.Value && r.pay_date.Value < reportModel.FinishDate))
						reportRow.paidAfter += r.cost.Value;
					// если заказ оплачен в текущем периоде, то включить его в оплаченные
					if (r.paid.Value && r.pay_date.Value >= reportModel.StartDate && r.pay_date.Value < finishDate)
						reportRow.paid += r.cost.Value;

				}
				else
				{
					reportRow = new ReportRow();
					reportRow.warehouseId = wh.id;
					reportRow.warehouseName = wh.name;
					reportRow.postId = wh.postoffice.post.id;
					reportRow.postName = wh.postoffice.post.name;
					reportRow.spentAfter += r.cost.Value;
					// если заказ оплачен в пред периоде, то включить его в оплаченные
					if (!(r.paid.Value && r.pay_date.Value < reportModel.FinishDate))
						reportRow.paidAfter= r.cost.Value;
					// если заказ оплачен в текущем периоде, то включить его в оплаченные
					if (r.paid.Value && r.pay_date.Value >= reportModel.StartDate && r.pay_date.Value < finishDate)
						reportRow.paid = r.cost.Value;

					reportRows.Add(wh.id, reportRow);
				}
			}
			//обработка просроченных заказов на начало периода
			foreach (var debtBefore in debtsBefore)
			{
				int wh = debtBefore.Key;
				ReportRow reportRow;
				if (reportRows.ContainsKey(wh))
				{
					reportRow = reportRows[wh];
					reportRow.debtBeforeOverdue += debtBefore.Value;
				}
				else
				{
					reportRow = new ReportRow();
					reportRow.warehouseId = wh;
					reportRow.debtBeforeOverdue += debtBefore.Value;
					reportRows.Add(wh, reportRow);
				}
			}

			//обработка просроченных заказов на конец периода
			foreach (var debt in debts)
			{
				int wh = debt.Key;
				ReportRow reportRow;
				if (reportRows.ContainsKey(wh))
				{
					reportRow = reportRows[wh];
					reportRow.debtAfterOverdue += debt.Value;
				}
				else
				{
					reportRow = new ReportRow();
					reportRow.warehouseId = wh;
					reportRow.debtAfterOverdue += debt.Value;
					reportRows.Add(wh, reportRow);
				}
			}

			var ms = new MemoryStream();
			ExcelPackage excelPackage = new ExcelPackage();
			excelPackage.Workbook.Worksheets.Add("Оборотная ведомость");
			var sheet = excelPackage.Workbook.Worksheets[1];
			var reportRowsList = new List<ReportRow>();
			foreach (var reportRow in reportRows)
			{
				reportRowsList.Add(reportRow.Value);
			}
			int k = 0;
			var groupedByPost = reportRowsList.GroupBy(x => x.postId).OrderBy(x=>x.ElementAt(0).postName);
			foreach (IGrouping<int, ReportRow> requestsByPost in groupedByPost)
			{
				sheet.Cells[k+1, 1].Value = $"Оборотная ведомость - {requestsByPost.ElementAt(0).postName}";
				sheet.Cells[k+1, 1].Style.Font.Size = 16;
				sheet.Cells[k+2, 1].Value = $"Период: {start} - {finish}";
				sheet.Cells[k+2, 1].Style.Font.Size = 13;
				sheet.Cells[k+3, 1].Value = "Склад";
				//sheet.Cells[k+3, 2].Value = "Отпущено на сумму";
				sheet.Cells[k+3, 2].Value = "Долг на начало периода";
				sheet.Cells[k+3, 3].Value = "Просроченный долг предыдущего периода";
				sheet.Cells[k + 3, 4].Value = "Отпущено на сумму";
				sheet.Cells[k + 3, 5].Value = "Погашено";
				//sheet.Cells[k + 3, 7].Value = "Отпущено на сумму";
				sheet.Cells[k + 3, 6].Value = "Долг на конец периода";
				sheet.Cells[k + 3, 7].Value = "Просроченный долг на конец периода";
				sheet.Cells[k+3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[k+3, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[k+3, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 1, k+3, 7].Style.Font.Size = 13;

				//var groupedRequests = requestsByPost.GroupBy(x => x.buyer.warehouse_id);
				int i = k+4;
				decimal spentBefore=0,paidBefore=0,penaltyBefore=0,spentCurrent=0,paidCurrent=0,spentAfter=0,paidAfter=0,penaltyAfter=0;
				foreach (ReportRow reportRow in requestsByPost)
				{
					//decimal totalDebt = 0, paidDebt = 0,pen=0,penBefore=0;
					//foreach (var request in group)
					//{
					//	totalDebt += request.cost.Value;
					//	if (request.paid.Value)
					//		paidDebt += request.cost.Value;
					//	if (debts.ContainsKey(warehouseId))
					//		pen = debts[warehouseId];
					//	if (debtsBefore.ContainsKey(warehouseId))
					//		penBefore = debtsBefore[warehouseId];
					//}
					spentBefore += reportRow.spentBefore;
					paidBefore+= reportRow.paidBefore;
					paidCurrent += reportRow.paid;
					spentCurrent+= reportRow.spent;
					spentAfter += reportRow.spentAfter;
					paidAfter += reportRow.paidAfter;
					sheet.Cells[i, 1].Value = reportRow.warehouseName;
					//sheet.Cells[i, 2].Value = reportRow.spentBefore;
					sheet.Cells[i, 2].Value = reportRow.paidBefore;
					sheet.Cells[i, 3].Value = reportRow.debtBeforeOverdue;
					sheet.Cells[i, 4].Value = reportRow.spent;
					sheet.Cells[i, 5].Value = reportRow.paid;
					//sheet.Cells[i, 7].Value = reportRow.spentAfter;
					sheet.Cells[i, 6].Value = reportRow.paidAfter;
					sheet.Cells[i, 7].Value = reportRow.debtAfterOverdue;
					sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					//sheet.Cells[i, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					//sheet.Cells[i, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					i++;
				}
				sheet.Cells[i, 1].Value = "Итого";
				//sheet.Cells[i, 2].Value = spentBefore;
				sheet.Cells[i, 2].Value = paidBefore;
				sheet.Cells[i, 3].Value = penaltyBefore;
				sheet.Cells[i, 4].Value = spentCurrent;
				sheet.Cells[i, 5].Value = paidCurrent;
				//sheet.Cells[i, 7].Value = spentAfter;
				sheet.Cells[i, 6].Value = paidAfter;
				sheet.Cells[i, 7].Value = penaltyAfter;
				sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[i, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				//sheet.Cells[i, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 1, i, 7].Style.Font.Size = 13;
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
			var req = reportModel.RequestsCurrent;
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
			sheet.Cells[k + 1, 1].Value = $"Ведомость по товарам";// - {requestsByPost.ElementAt(0).request.buyer.warehouse.postoffice.post.name}";
			sheet.Cells[k + 1, 1].Style.Font.Size = 16;
			sheet.Cells[k + 1, 1, k + 1, 4].Merge = true;
			sheet.Cells[k + 2, 1].Value = $"Период: {start} - {finish}";
			sheet.Cells[k + 2, 1].Style.Font.Size = 13;
			sheet.Cells[k + 3, 1].Value = "№ п/п";
			sheet.Cells[k + 3, 2].Value = "Почтамт";
			sheet.Cells[k + 3, 3].Value = "Склад";
			sheet.Cells[k + 3, 4].Value = "Наименование товара";
			sheet.Cells[k + 3, 5].Value = "Цена";
			sheet.Cells[k + 3, 6].Value = "Количество";
			sheet.Cells[k + 3, 7].Value = "Сумма";
			sheet.Cells[k + 3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[k + 3, 1, k + 3, 7].Style.Font.Size = 13;
			var groupedByPost = requestRows.GroupBy(x => x.request.buyer.warehouse.postoffice.post_id).OrderBy(x => x.ElementAt(0).request.buyer.warehouse.postoffice.post.name);
			int i = k+4;
			// для подсчета количества ОПС
			int postofficeCount = 0;
			decimal totalCost = 0;
			int count = 1;
			foreach (IGrouping<int, request_rows> requestsByPost in groupedByPost)
			{
				string postName = requestsByPost.First().request.buyer.warehouse.postoffice.post.name;
				IEnumerable<IGrouping<int?,request_rows>> groupedRequests = requestsByPost.GroupBy(x => x.request.buyer.warehouse_id);
				postofficeCount += groupedRequests.Count();
				foreach (IGrouping<int?, request_rows> group in groupedRequests)
				{
					string warehouseName = group.ElementAt(0).request.buyer.warehouse.name;
					warehouseName += " " + group.ElementAt(0).request.buyer.warehouse.postoffice.idx;
					int warehouseId = group.ElementAt(0).request.buyer.warehouse.id;
					var groupedByGood = group.GroupBy(x => x.goods_id).OrderBy(x=>x.First().name);
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
						totalCost += cost;
						sheet.Cells[i, 1].Value = count++;
						sheet.Cells[i, 2].Value = postName;
						sheet.Cells[i, 3].Value = warehouseName;
						sheet.Cells[i, 4].Value = goodsName;
						sheet.Cells[i, 5].Value = price;
						sheet.Cells[i, 6].Value = amount;
						sheet.Cells[i, 7].Value = cost;
						sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
						sheet.Cells[i, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
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
			sheet.Cells[i, 4].Value = "Итого";
			sheet.Cells[i, 7].Value = totalCost;

			const double minWidth = 0.00;
			const double maxWidth = 100.00;
			sheet.Cells.AutoFitColumns(minWidth, maxWidth);
			
			sheet.Cells[i, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 4, i, 7].Style.Font.Size = 13;
			sheet.Column(1).Width = 8;
			//если почтамт в отчете один, то прячем колонку Почтамт
			if (groupedByPost.Count()== 1)
				sheet.Column(2).Hidden = true;
			//если ОПС в отчете одно, то прячем колонку Склад
			if (postofficeCount == 1)
				sheet.Column(3).Hidden = true;
			excelPackage.SaveAs(ms);
			return ms;
		}
	}
}