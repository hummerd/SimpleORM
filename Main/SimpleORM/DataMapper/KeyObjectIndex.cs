using System;
using System.Collections.Generic;
using System.Text;


namespace SimpleORM
{
    public class KeyObjectIndex
        : Dictionary<object, List<object>>
    {
        public void AddObject(object key, object obj)
        {
            List<object> list;
            if (!TryGetValue(key, out list))
            {
                list = new List<object>();
                Add(key, list);
            }

            list.Add(obj);
        }

		public void AddRange(object key, IEnumerable<object> obj)
		{
			List<object> list;
			if (!TryGetValue(key, out list))
			{
				list = new List<object>();
				Add(key, list);
			}

			list.AddRange(obj);
		}
    }
}
