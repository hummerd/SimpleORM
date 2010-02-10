using System;
using System.Reflection;


namespace SimpleORM
{
	public static class ReflectionHelper
	{
		public static Type GetType(string typePath)
		{
			var parts = typePath.Split(',');
			return Assembly.Load(parts[0].Trim()).GetType(parts[1].Trim());
		}

		/// <summary>
		/// Helper method. Returns first generic argument type for first generic subtype.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Type GetListItemType(Type listType)
		{
			while (listType != null)
			{
				if (listType.IsGenericType)
					break;

				listType = listType.BaseType;
			}

			if (listType == null)
				return null;

			return listType.GetGenericArguments()[0];
		}

		public static bool IsComplexType(Type type)
		{
			if (type.IsClass && type != typeof(string))
				return true;

			return false;
		}

		public static Type GetReturnType(MemberInfo member)
		{
			if (member is FieldInfo)
				return ((FieldInfo)member).FieldType;

			if (member is PropertyInfo)
				return ((PropertyInfo)member).PropertyType;

			return null;
		}
	}
}
