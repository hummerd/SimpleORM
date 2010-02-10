using System;
using System.Collections.Generic;
using System.Text;
using SimpleORM.Attributes;
using System.Reflection;


namespace SimpleORM.MappingDataProvider
{
	public class AttMappingDataProvider : MappingDataProviderBase, IMappingDataProvider
	{
		public override bool SetConfig(IEnumerable<string> configFiles)
		{
			bool result = true;

			foreach (var cfgPath in configFiles)
			{
				result &= string.IsNullOrEmpty(cfgPath);
			}

			return result;
		}


		protected override bool AddMappingInfo(MemberInfo member, ExtractInfo extractInfo)
		{
			bool result = false;

			object[] attrs = member.GetCustomAttributes(typeof(DataMapAttribute), true);
			if (attrs == null || attrs.Length <= 0)
				return result;

			foreach (object att in attrs)
			{
				DataColumnMapAttribute columnMap = att as DataColumnMapAttribute;
				if (columnMap != null && 
					columnMap.SchemeId == extractInfo.SchemeId &&
					att.GetType() == typeof(DataColumnMapAttribute))
				{
					result = true;
					extractInfo.MemberColumns.Add(
						new MemberExtractInfo(string.IsNullOrEmpty(columnMap.MappingName) ? member.Name : columnMap.MappingName, member));
				}

				ComplexDataMapAttribute complexMap = att as ComplexDataMapAttribute;
				if (complexMap != null && 
					complexMap.SchemeId == extractInfo.SchemeId &&
					att.GetType() == typeof(ComplexDataMapAttribute))
				{
					result = true;
					extractInfo.SubTypes.Add(
						new RelationExtractInfo(
							complexMap.MappingName,
							member,
							new ExtractInfo(complexMap.ItemType, complexMap.NestedSchemeId),
							null
							)
						);
				}

				DataRelationMapAttribute relationMap = att as DataRelationMapAttribute;
				if (relationMap != null && 
					relationMap.SchemeId == extractInfo.SchemeId &&
					att.GetType() == typeof(DataRelationMapAttribute))
				{
					result = true;
					extractInfo.ChildTypes.Add(
						new RelationExtractInfo(
							relationMap.MappingName,
							member,
							new ExtractInfo(relationMap.ItemType, relationMap.NestedSchemeId),
							GetParentKey(extractInfo.TargetType, relationMap, extractInfo.SchemeId, attrs)
							)
						);
				}
			} //end of foreach

			return result;
		}

		protected override RefInfo GetRefInfo(ExtractInfo extractInfo)
		{
			object[] attrs = extractInfo.TargetType.GetCustomAttributes(typeof(TableMapAttribute), true);

			if (attrs == null || attrs.Length <= 0)
				return null;

			TableMapAttribute tm = attrs[0] as TableMapAttribute;
			return new RefInfo(tm.TableIx, tm.TableName);
		}


		protected KeyInfo GetParentKey(Type parentType, DataRelationMapAttribute relationMap, int schemeId, object[] attrs)
		{
			TableMapAttribute tableId = Array.Find(attrs, a =>
				a is TableMapAttribute) as TableMapAttribute ?? new TableMapAttribute((int[])null);

			KeyInfo ki = new KeyInfo();
			ki.Name = relationMap.MappingName;
			ki.RefTable = new RefInfo(tableId.TableIx, tableId.TableName);
			ki.ChildType = relationMap.ItemType;
			ki.ParentType = parentType;

			foreach (object att in attrs)
			{
				DataRelationColumnMapAttribute relColumnMap = att as DataRelationColumnMapAttribute;
				if (relColumnMap == null || relColumnMap.SchemeId != schemeId)
					continue;

				ki.ParentColumns.Add(relColumnMap.ParentColumn);
				ki.ChildColumns.Add(relColumnMap.ChildColumn);
			}

			return ki;
		}
	}
}
