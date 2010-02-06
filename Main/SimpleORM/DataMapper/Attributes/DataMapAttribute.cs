using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
	public class DataMapAttribute : Attribute
	{
		protected int _SchemeId = 0;
		private string _MappingName;


		public DataMapAttribute()
		{
		}

		public DataMapAttribute(int schemeId)
		{
			_SchemeId = schemeId;
		}

		public DataMapAttribute(string mappingName)
		{
			_MappingName = mappingName;
		}

		public DataMapAttribute(string mappingName, int schemeId)
		{
			_MappingName = mappingName;
			_SchemeId = schemeId;
		}


		public int SchemeId
		{
			get { return _SchemeId; }
			set { _SchemeId = value; }
		}

		public string MappingName
		{
			get { return _MappingName; }
			set { _MappingName = value; }
		}
	}
}
