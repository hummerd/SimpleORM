using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;


namespace SimpleORM.MappingDataProvider
{
	public class XmlMappingDataProvider : MappingDataProviderBase
	{
		protected XmlDocument _XmlDocument;


		public override bool SetConfig(IEnumerable<string> configFiles)
		{
			_XmlDocument = null;
			XmlElement xmlRoot = null;
			XmlDocument xDoc = new XmlDocument();

			foreach (var cfgPath in configFiles)
			{
				if (string.IsNullOrEmpty(cfgPath))
				{
					continue;
				}

				if (_XmlDocument == null)
				{
					_XmlDocument = new XmlDocument();
					xmlRoot = _XmlDocument.CreateElement("Root");
					_XmlDocument.AppendChild(xmlRoot);	
				}

				xDoc.Load(cfgPath);
				XmlNode def = xDoc.SelectSingleNode("//MappingDefinition");
				xmlRoot.AppendChild(_XmlDocument.ImportNode(def, true));
			}

			return _XmlDocument != null;
		}


		protected override bool AddMappingInfo(MemberInfo member, ExtractInfo extractInfo)
		{
			bool result = false;

			if (_XmlDocument == null)
				return result;

			//Looking for node in reflected type
			Type targetType = member.ReflectedType;
			XmlNode xmlMapping = FindPropMapping(targetType, extractInfo.SchemeId, member);
			if (xmlMapping == null)
			{
				targetType = member.DeclaringType;
				xmlMapping = FindPropMapping(targetType, extractInfo.SchemeId, member);
				if (xmlMapping == null)
					return result;
			}

			result = true;

			XmlAttribute att = xmlMapping.Attributes["dataColumnName"];
			if (att == null)
			{
				att = xmlMapping.Attributes["dataRelationName"];
				Type itemType = null;
				int nestedSchemaId = extractInfo.SchemeId;

				if (att != null)
				{
					GetNestedProps(xmlMapping, ref nestedSchemaId, ref itemType);
					extractInfo.ChildTypes.Add(	new RelationExtractInfo(
							att.Value,
							member,
							new ExtractInfo(itemType, nestedSchemaId),
							GetParentKey(targetType, itemType, att.Value, extractInfo.SchemeId)));
					//return new DataRelationMapAttribute(att.Value, schemeId, nestedSchemaId, itemType);
				}
				else
				{
					att = xmlMapping.Attributes["complex"];

					if (att != null || ReflectionHelper.IsComplexType(ReflectionHelper.GetReturnType(member)))
					{
						GetNestedProps(xmlMapping, ref nestedSchemaId, ref itemType);
						extractInfo.SubTypes.Add(new RelationExtractInfo(
							null,
							member,
							new ExtractInfo(itemType, nestedSchemaId),
							null));
					}
					else
						extractInfo.MemberColumns.Add(new MemberExtractInfo(
							member.Name,
							member));
				}
			}
			else
				extractInfo.MemberColumns.Add(new MemberExtractInfo(
					String.IsNullOrEmpty(att.Value) ? member.Name : att.Value,
					member));

			return result;
		}

		protected override RefInfo GetRefInfo(ExtractInfo extractInfo)
		{
			if (_XmlDocument == null)
				return null;

			XmlNode typeMapping = FindTypeMapping(extractInfo.TargetType, extractInfo.SchemeId);
			if (typeMapping == null)
				return null;

			int[] tableIx;
			string tableName;
			GetTableID(typeMapping, out tableIx, out tableName);

			return new RefInfo(tableIx, tableName);
		}


		protected KeyInfo GetParentKey(Type parentType, Type childType, string relationName, int schemeId)
		{
			XmlNode xmlKey = FindKey(parentType, schemeId, relationName);

			if (xmlKey == null)
				return null;

			int[] tableIx;
			string tableName;
			GetTableID(xmlKey, out tableIx, out tableName);

			KeyInfo ki = new KeyInfo();
			ki.Name = relationName;
			ki.RefTable = new RefInfo(tableIx, tableName);
			ki.ChildType = childType;
			ki.ParentType = parentType;

			foreach (XmlNode clmn in xmlKey.ChildNodes)
			{
				if (clmn.NodeType == XmlNodeType.Comment)
					continue;

				string parentColumn = clmn.Attributes["name"].Value;
				ki.ParentColumns.Add(parentColumn);

				if (clmn.Attributes["foreignName"] == null ||
					string.IsNullOrEmpty(clmn.Attributes["foreignName"].Value))
					ki.ChildColumns.Add(parentColumn);
				else
					ki.ChildColumns.Add(clmn.Attributes["foreignName"].Value);
			}

			return ki;
		}

		protected void GetTableID(XmlNode node, out int[] tableIx, out string tableName)
		{
			tableIx = null;
			tableName = String.Empty;

			XmlAttribute tix = node.Attributes["tableIx"];
			if (tix != null)
			{
				string[] vals = tix.Value.Split(';');
				tableIx = new int[vals.Length];

				for (int i = 0; i < vals.Length; i++)
					tableIx[i] = int.Parse(vals[i]);
			}

			XmlAttribute tn = node.Attributes["tableName"];
			if (tn != null)
				tableName = tn.Value;
		}

		protected XmlNode FindTypeMapping(Type propType, int schemeId)
		{
			//Generate XPath Query
			string qry =
				@"/Root/MappingDefinition/TypeMapping
					[@typeName='{0}' and ((0={1} and not(@schemeId)) or @schemeId='{2}')]
				  ";

			string type = propType.Assembly.GetName().Name;
			type = type + ", " + propType.FullName;
			qry = string.Format(qry, type, schemeId, schemeId);

			return _XmlDocument.SelectSingleNode(qry);
		}

		/// <summary>
		/// Looks for mapping definition in reflected type or if it is not found, looks in declared type.
		/// </summary>
		/// <param name="propType"></param>
		/// <param name="schemeId"></param>
		/// <param name="prop"></param>
		/// <returns></returns>s
		protected XmlNode FindPropMapping(Type propType, int schemeId, MemberInfo prop)
		{
			//Generate XPath Query
			string qry =
				@"/Root/MappingDefinition/TypeMapping
					[@typeName='{0}' and ((0={1} and not(@schemeId)) or @schemeId='{2}')]
				  /PropetyMapping
					[@propertyName='{3}']
				  ";

			string type = propType.Assembly.GetName().Name;
			type = type + ", " + propType.FullName;
			qry = string.Format(qry, type, schemeId, schemeId, prop.Name);

			return _XmlDocument.SelectSingleNode(qry);
		}

		protected XmlNode FindKey(Type targetType, int schemeId, string relationName)
		{
			//Generate XPath Query
			string qry =
				@"/Root/MappingDefinition/TypeMapping
					[@typeName='{0}' and ((0={1} and not(@schemeId)) or @schemeId='{2}')]
				  /RefKey
					[@key='{3}']
				  ";

			string type = targetType.Assembly.GetName().Name;
			type = type + ", " + targetType.FullName;
			qry = string.Format(qry, type, schemeId, schemeId, relationName);

			//Looking for node
			return _XmlDocument.SelectSingleNode(qry);
		}

		/// <summary>
		/// Extracts nested type info from mapping definition
		/// </summary>
		/// <param name="xmlMapping"></param>
		/// <param name="nestedSchemeId"></param>
		/// <param name="itemType"></param>
		protected void GetNestedProps(XmlNode xmlMapping, ref int nestedSchemeId, ref Type itemType)
		{
			XmlAttribute attType = xmlMapping.Attributes["nestedItemType"];
			if (attType != null && !String.IsNullOrEmpty(attType.Value))
			{
				itemType = ReflectionHelper.GetType(attType.Value);
			}

			XmlAttribute attNestedSchemaId = xmlMapping.Attributes["nestedSchemaId"];
			if (attNestedSchemaId != null && !String.IsNullOrEmpty(attNestedSchemaId.Value))
				nestedSchemeId = int.Parse(attNestedSchemaId.Value);
		}
	}
}
