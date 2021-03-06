﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.Attributes
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class DataRelationMapAttribute : DataMapAttribute
	{
		protected int? _NestedSchemeId = null;
		protected Type _ItemType;


		public DataRelationMapAttribute(string relationName)
			: base(relationName)
		{ }

		public DataRelationMapAttribute(string relationName, int schemeId)
			: base(relationName, schemeId)
		{ }

		public DataRelationMapAttribute(string relationName, int schemeId, int nestedSchemeId)
			: base(relationName, schemeId)
		{
			_NestedSchemeId = nestedSchemeId;
		}

		public DataRelationMapAttribute(string relationName, Type itemType)
			: base(relationName)
		{
			_ItemType = itemType;
		}

		public DataRelationMapAttribute(string relationName, int schemeId, int nestedSchemeId, Type itemType)
			: base(relationName, schemeId)
		{
			_NestedSchemeId = nestedSchemeId;
			_ItemType = itemType;
		}


		public int NestedSchemeId
		{
			get { return _NestedSchemeId ?? _SchemeId; }
			set { _NestedSchemeId = value; }
		}

		public Type ItemType
		{
			get { return _ItemType; }
			set { _ItemType = value; }
		}
	}
}
