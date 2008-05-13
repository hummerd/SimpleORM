using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGenerator
{
	public class StandartObjectBuilder : IObjectBuilder
	{
		#region IObjectBuilder Members

		public object CreateObject(Type objectType)
		{
			return Activator.CreateInstance(objectType);
		}

		#endregion
	}
}
