using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM
{
	public class StandartObjectBuilder : IObjectBuilder
	{
		#region IObjectBuilder Members

		public virtual object CreateObject(Type objectType)
		{
			return Activator.CreateInstance(objectType);
		}

		public virtual T CreateObject<T>()
			where T : new()
		{
			return new T();
		}

		#endregion
	}
}
