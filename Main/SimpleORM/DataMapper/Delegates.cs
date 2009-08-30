using System;
using System.Collections.Generic;
using System.Text;
using SimpleORM.Attributes;
using System.Data;
using System.Reflection;


namespace SimpleORM
{
	public delegate DataMapAttribute GetPropertyMapping(MemberInfo prop, int schemeId);
	public delegate DataRow DataRowExtractor<T>(T obj);
}
