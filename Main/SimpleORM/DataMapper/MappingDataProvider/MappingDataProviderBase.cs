using System;
using System.Collections.Generic;
using System.Reflection;


namespace SimpleORM.MappingDataProvider
{
	public abstract class MappingDataProviderBase : IMappingDataProvider
	{
		public virtual bool SetConfig(IEnumerable<string> configFiles)
		{ 
			return false; 
		}

		public bool GetExtractInfo(ExtractInfo extractInfo)
		{
			bool result = false;

			ForEachMember(extractInfo.TargetType, m =>
				result |= AddMappingInfo(m, extractInfo)
				);

			RefInfo refTable = GetRefInfo(extractInfo);
			if (refTable != null)
			{
				extractInfo.RefTable = refTable;
				result = true;
			}

			return result;
		}


		protected abstract bool AddMappingInfo(MemberInfo member, ExtractInfo extractInfo);

		protected abstract RefInfo GetRefInfo(ExtractInfo extractInfo);


		protected void ForEachMember(Type type, Action<MemberInfo> action)
		{
			List<PropertyInfo> props = GetProps(type);
			props.ForEach(p =>
				action(p));

			FieldInfo[] fields = type.GetFields();
			Array.ForEach(fields, f =>
				action(f));
		}

		protected List<PropertyInfo> GetProps(Type type)
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
