using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class ComplexDataMapAttribute : DataMapAttribute
	{
		private int _NestedSchemeId = 0;
		private Type _ItemType;

		
		
		public int NestedSchemeId
		{
			get { return _NestedSchemeId; }
			set { _NestedSchemeId = value; }
		}

		public Type ItemType
		{
			get { return _ItemType; }
			set { _ItemType = value; }
		}

		//public override bool Equals(object obj)
		//{
		//   return ItemType;
		//}
	}
}
