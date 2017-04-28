using System.Collections.Generic;

namespace DataAccess
{
	public interface IRepository<T> where T :class
	{
		IEnumerable<T> GetAll();
		void Add(T item);
		void Change(T item);
		void Delete(T item);
		T Get(int id);
	}
}