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

		public static Stream GenReport(IList<request> req, string start, string finish)
		{
			var ms = new MemoryStream();
			ExcelPackage excelPackage = new ExcelPackage();

			excelPackage.Workbook.Worksheets.Add("Оборотная ведомость");
			var sheet = excelPackage.Workbook.Worksheets[1];
			sheet.Cells[1, 1].Value = "Оборотная ведомость";
			sheet.Cells[1, 1].Style.Font.Size = 16;
			sheet.Cells[2, 1].Value = $"Период: {start} - {finish}";
			sheet.Cells[2, 1].Style.Font.Size = 13;
			sheet.Cells[3, 1].Value = "Склад"; sheet.Cells[3, 2].Value = "Отпущено на сумму"; sheet.Cells[3, 3].Value = "Из них погашено";
			sheet.Cells[3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[3, 1, 3, 3].Style.Font.Size = 13;
			var groupedRequests = req.GroupBy(x => x.buyer.warehouse_id);
			int i = 4;
			decimal total = 0,paid=0;
			foreach (IGrouping<int?, request> group in groupedRequests)
			{
				string warehouseName = group.ElementAt(0).buyer.warehouse.name;
				decimal totalDebt = 0, paidDebt = 0;
				foreach (var request in group)
				{
					totalDebt += request.cost.Value;
					if (request.paid.Value)
						paidDebt += request.cost.Value;
				}
				paid += paidDebt;
				total += totalDebt;
				sheet.Cells[i, 1].Value = warehouseName; sheet.Cells[i, 2].Value = totalDebt; sheet.Cells[i, 3].Value = paidDebt;
				sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				i++;
			}
			sheet.Cells[i, 1].Value = "Итого"; sheet.Cells[i, 2].Value = total; sheet.Cells[i, 3].Value = paid;
			sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
			sheet.Cells[i, 1, i, 3].Style.Font.Size = 13;
			const double minWidth = 0.00;
			const double maxWidth = 50.00;
			sheet.Cells.AutoFitColumns(minWidth, maxWidth);
			excelPackage.SaveAs(ms);
			return ms;
		}

		public static Stream GenReportUfps(IList<request> req, string start, string finish)
		{
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
				sheet.Cells[k+3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[k+3, 1, k+3, 3].Style.Font.Size = 13;

				var groupedRequests = requestsByPost.GroupBy(x => x.buyer.warehouse_id);
				int i = k+4;
				decimal total = 0, paid = 0;
				foreach (IGrouping<int?, request> group in groupedRequests)
				{
					string warehouseName = group.ElementAt(0).buyer.warehouse.name;
					decimal totalDebt = 0, paidDebt = 0;
					foreach (var request in group)
					{
						totalDebt += request.cost.Value;
						if (request.paid.Value)
							paidDebt += request.cost.Value;
					}
					paid += paidDebt;
					total += totalDebt;
					sheet.Cells[i, 1].Value = warehouseName; sheet.Cells[i, 2].Value = totalDebt; sheet.Cells[i, 3].Value = paidDebt;
					sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
					i++;
				}
				sheet.Cells[i, 1].Value = "Итого"; sheet.Cells[i, 2].Value = total; sheet.Cells[i, 3].Value = paid;
				sheet.Cells[i, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
				sheet.Cells[i, 1, i, 3].Style.Font.Size = 13;
				k=i+1;
			}
			const double minWidth = 0.00;
			const double maxWidth = 50.00;
			sheet.Cells.AutoFitColumns(minWidth, maxWidth);
			excelPackage.SaveAs(ms);
			return ms;
		}
	}
}