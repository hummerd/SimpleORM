using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.Attributes
{
	/// <summary>
	/// Maps object property or field to specified column
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class DataColumnMapAttribute : DataMapAttribute
	{
		public DataColumnMapAttribute()
		{ }

		public DataColumnMapAttribute(int schemeId)
			: base(schemeId)
		{ }

		public DataColumnMapAttribute(string columnName)
			: base(columnName)
		{ }

		public DataColumnMapAttribute(string columnName, int schemeId)
			: base(columnName, schemeId)
		{ }
	}
}
