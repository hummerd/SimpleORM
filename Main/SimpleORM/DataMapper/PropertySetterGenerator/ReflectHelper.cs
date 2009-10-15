using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;


namespace SimpleORM.PropertySetterGenerator
{
	public static class ReflectHelper
	{
		public static List<PropertyInfo> GetProps(Type type)
		{
			Dictionary<string, PropertyInfo> distinctProps = new Dictionary<string, PropertyInfo>(30);
			PropertyInfo prop;

			foreach (var item in type.GetProperties())
			{
				string propName = item.Name;
				if (distinctProps.TryGetValue(propName, out prop))
				{
					if (item.DeclaringType == type)
					{
						distinctProps[propName] = item;
					}
				}
				else
					distinctProps.Add(propName, item);
			}

			return new List<PropertyInfo>(distinctProps.Values);
		}
	}
}
