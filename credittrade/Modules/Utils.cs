using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using DbModel;
using Nancy;
using NPOI.HSSF.UserModel;

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
		public static MemoryStream GeneratePrintFormUfps(request request, IEnumerable<request_rows> requestRows,string rootPath)
		{
			if (!Directory.Exists(Path.Combine(rootPath,"files")))
				Directory.CreateDirectory(Path.Combine(rootPath, "files"));
			string filename = null;
			string templateFilename = Path.Combine(rootPath,"template_ufps.xls");
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
						row.Cells[8].SetCellValue((double) requestRow.amount);
						row.Cells[9].SetCellValue((double) requestRow.price);
						row.Cells[10].SetCellValue((double) (requestRow.price* requestRow.amount));
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
	}
}