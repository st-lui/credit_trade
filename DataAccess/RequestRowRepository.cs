using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DbModel;

namespace DataAccess
{
	public class RequestRowRepository : IRepository<request_rows>
	{
		private DataContext db;

		public RequestRowRepository(DataContext db)
		{
			this.db = db;
		}

		public IEnumerable<request_rows> GetAll()
		{
			return db.request_rows;
		}

		public void Add(request_rows item)
		{
			db.request_rows.Add(item);
		}

		public void Change(request_rows item)
		{
			db.Entry(item).State = EntityState.Modified;
		}

		public void Delete(request_rows item)
		{
			db.request_rows.Remove(item);
		}

		public request_rows Get(int id)
		{
			return db.request_rows.SingleOrDefault(x => x.id == id);
		}

		public request_rows CreateRequestRows(decimal amount, string name, string ed_izm, int goods_id, decimal price, string barcode)
		{
			return new request_rows() {
				amount = amount,
				barcode = barcode,
				ed_izm = ed_izm,
				goods_id = goods_id,
				name = name,
				price = price };
		}

		public request_rows CreateRequestRows(decimal amount, int goods_id,IEnumerable<good> goodsList,decimal leftoverPrice)
		{
			var good = goodsList.First(x => x.id == goods_id);
			request_rows requestRow= new request_rows()
			{
				amount = amount,
				barcode = good.barcode,
				ed_izm = good.edizm,
				goods_id = goods_id,
				name = good.name,
				
price = leftoverPrice
				//price = leftoverPrice == 0 ? good.price : leftoverPrice
			};
			return requestRow;
		}
	}
}