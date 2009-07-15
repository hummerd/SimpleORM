using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class ComplexDataMapAttribute : DataRelationMapAttribute
	{
		public ComplexDataMapAttribute()
			: base(null)
		{ }

		public ComplexDataMapAttribute(int nestedSchemeId, Type itemType)
			: base(null, 0, nestedSchemeId, itemType)
		{ }

		public ComplexDataMapAttribute(int schemeId, int nestedSchemeId, Type itemType)
			: base(null, schemeId, nestedSchemeId, itemType)
		{
		}
	}
}
