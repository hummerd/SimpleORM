using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;


namespace SimpleORM.PropertySetterGenerator
{
	public class ExtractorInfoCache : 
		Dictionary<Type,								//target object type (Entity type)
			Dictionary<int, ExtractInfo>>	//scheme
	{
		public bool TryGetExtractInfo(
			Type targetType, 
			int schemeId,
			out ExtractInfo extractInfo)
		{
			extractInfo = null;

			//Dictionary<Type, Dictionary<int, ExtractInfo>> extractorSheme;
			//if (!TryGetValue(targetType, out extractorSheme))
			//   return false;

			Dictionary<int, ExtractInfo> schemeExtractInfo;
			if (!TryGetValue(targetType, out schemeExtractInfo))
				return false;

			if (!schemeExtractInfo.TryGetValue(schemeId, out extractInfo))
				return false;

			return true;
		}

		public void Add(
			Type targetType,
			int schemeId,
			ExtractInfo extractInfo)
		{
			//Dictionary<Type, Dictionary<int, ExtractInfo>> extractorSheme;
			//if (!TryGetValue(targetType, out extractorSheme))
			//{
			//   extractorSheme = new Dictionary<Type, Dictionary<int, ExtractInfo>>();
			//   Add(targetType, extractorSheme);
			//}

			Dictionary<int, ExtractInfo> schemeExtractInfo;
			if (!TryGetValue(targetType, out schemeExtractInfo))
			{
				schemeExtractInfo = new Dictionary<int, ExtractInfo>();
				Add(targetType, schemeExtractInfo);		
			}

			if (schemeExtractInfo.ContainsKey(schemeId))
			{
				schemeExtractInfo[schemeId] = extractInfo;
			}
			else
			{
				schemeExtractInfo.Add(schemeId, extractInfo);
			}
		}

		public bool Contains(
			Type targetType,
			int schemeId)
		{
			Dictionary<int, ExtractInfo> schemeExtractInfo;
			if (!TryGetValue(targetType, out schemeExtractInfo))
				return false;

			return schemeExtractInfo.ContainsKey(schemeId);
		}
	}
}
