using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class DataColumnMapAttribute : DataMapAttribute
	{
		public DataColumnMapAttribute(string columnName)
			: base(columnName)
		{ }

		public DataColumnMapAttribute(string columnName, int schemeId)
			: base(columnName, schemeId)
		{ }
	}
}
