using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SimpleORM.Attributes;


namespace SimpleORM.PropertySetterGenerator
{
	public class MemberMapInfo
	{
		public MemberMapInfo(MemberInfo member, DataMapAttribute mapping)
		{
			Member = member;
			Mapping = mapping;
		}


		public MemberInfo Member
		{ get; set; }

		public DataMapAttribute Mapping
		{ get; set; }


		public bool IsFieldSimpleMapping()
		{
			return
				Member is FieldInfo &&
				Mapping is DataColumnMapAttribute;
		}

		public bool IsPropSimpleMapping()
		{
			return
				Member is PropertyInfo &&
				Mapping is DataColumnMapAttribute;
		}

		public bool IsPropRelationMapping()
		{
			return
				Member is PropertyInfo &&
				Mapping is DataRelationMapAttribute;
		}

		public bool IsPropComplexMapping()
		{
			return
				Member is PropertyInfo &&
				Mapping is ComplexDataMapAttribute;
		}
	}

	public class MemberMapInfoCollection : List<MemberMapInfo>
	{
		
	}
}
