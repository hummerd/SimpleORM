using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml;
using SimpleORM.Attributes;
using SimpleORM.Exception;


namespace SimpleORM.PropertySetterGenerator
{
	public class DataMapperCodeGenerator
	{
		protected readonly ExtractorInfoCache _ExtractInfoCache = new ExtractorInfoCache();
					
		protected ModuleBuilder		_ModuleBuilder;
		protected AssemblyBuilder	_AsmBuilder;
		protected Dictionary<Type, IPropertySetterGenerator> _SetterGenerators;
		protected XmlDocument		_XmlDocument;
		protected string			_ConfigFile;
		protected KeyClassGenerator _KeyGenerator;


		public DataMapperCodeGenerator(Dictionary<Type, IPropertySetterGenerator> setterGenerators)
		{
			if (setterGenerators == null)
			{
				_SetterGenerators = new Dictionary<Type, IPropertySetterGenerator>(2);
				_SetterGenerators.Add(DataTablePSG.TypeOfDataSource, new DataTablePSG());
				_SetterGenerators.Add(DataReaderPSG.TypeOfDataSource, new DataReaderPSG());
			}
			else
			{
				_SetterGenerators = setterGenerators;
			}
		}


		/// <summary>
		/// Sets mapping definitions
		/// </summary>
		/// <param name="configFile"></param>
		public void SetConfig(string configFile)
		{
			if (String.IsNullOrEmpty(configFile))
			{
				_XmlDocument = null;
			}
			else
			{
				_ConfigFile = configFile;
				_XmlDocument = new XmlDocument();
				_XmlDocument.Load(_ConfigFile);
			}
		}

		/// <summary>
		/// Clears generated mapping
		/// </summary>
		public void ClearCache()
		{
			_ExtractInfoCache.Clear();
			_ModuleBuilder = null;
		}

		public void GenerateExtractorMethod(ExtractInfo info, Type targetType)
		{
			TypeBuilder typeBuilder = CreateAssemblyType(targetType);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("SetProps_" + targetType,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard, typeof(void),
				new Type[] { 
					targetType,								// object
					typeof(IDataReader),					// reader
					typeof(DataMapper),					// mapper
					typeof(List<List<int>>),			// columns
					Type.GetType("System.Int32&"),	// columnSetIx
					typeof(List<List<int>>),			// keyColumns
				});

			methodBuilder.DefineParameter(1, ParameterAttributes.In, "target");
			methodBuilder.DefineParameter(2, ParameterAttributes.In, "reader");
			methodBuilder.DefineParameter(3, ParameterAttributes.In, "mapper");
			methodBuilder.DefineParameter(4, ParameterAttributes.In, "columnsList");
			methodBuilder.DefineParameter(5, ParameterAttributes.Out, "columnsIx");
			methodBuilder.DefineParameter(6, ParameterAttributes.In, "keyColumns");

			Type objListType = typeof(List<>).MakeGenericType(targetType);
			ILGenerator ilGen = methodBuilder.GetILGenerator();
			LocalBuilder locObjBuilder = ilGen.DeclareLocal(typeof(IObjectBuilder));
			LocalBuilder locObj = ilGen.DeclareLocal(targetType);

			//Declare and init
			Type pkType = null;
			LocalBuilder locPkDict = null;

			if (info.PrimaryKeyInfo != null)
			{
				pkType = info.PrimaryKeyInfo.KeyType;
				Type pkDictType = typeof(Dictionary<,>).MakeGenericType(pkType, targetType);
				locPkDict = ilGen.DeclareLocal(pkDictType);

				ilGen.Emit(OpCodes.Newobj, pkDictType);
				ilGen.Emit(OpCodes.Stloc, locPkDict);
				
				ilGen.Emit(OpCodes.Ldarg, 123); //tempResult
				ilGen.Emit(OpCodes.Ldloc, locPkDict);
				ilGen.Emit(OpCodes.Stind_Ref);
			}
			
			List<LocalBuilder> locKeys = new List<LocalBuilder>();
			List<LocalBuilder> locKeyLists = new List<LocalBuilder>();
			List<LocalBuilder> locKeyDicts = new List<LocalBuilder>();

			foreach (var item in info.ForeignKeysInfo)
			{
				LocalBuilder locKey = ilGen.DeclareLocal(item.KeyType);
				LocalBuilder locKeyList = ilGen.DeclareLocal(typeof(List<>).MakeGenericType(item.KeyType));
				Type fkDictType = typeof(Dictionary<,>).MakeGenericType(item.KeyType, objListType);
				LocalBuilder locKeyDict = ilGen.DeclareLocal(fkDictType);

				ilGen.Emit(OpCodes.Newobj, fkDictType);
				ilGen.Emit(OpCodes.Stloc, locKeyDict);

				ilGen.Emit(OpCodes.Ldarg, 124); //fkIndex
				ilGen.Emit(OpCodes.Ldloc, locKeyDict);
				//ilGen.Emit(OpCodes.Callvirt, _ListAdd);

				locKeys.Add(locKey);
				locKeyLists.Add(locKeyList);
				locKeyDicts.Add(locKeyDict);
			}
			
			Label lblStart = ilGen.DefineLabel();
			ilGen.MarkLabel(lblStart);
			
			ilGen.Emit(OpCodes.Ldloc, locObjBuilder);
			//ilGen.Emit(OpCodes.Callvirt, _CreateObject);
			ilGen.Emit(OpCodes.Stloc, locObj);

			ilGen.Emit(OpCodes.Ldloc, locObj);	// object
			ilGen.Emit(OpCodes.Ldarg_1);			// reader
			ilGen.Emit(OpCodes.Ldarg_2);			// mapper
			ilGen.Emit(OpCodes.Ldarg_3);			// columnsList
			ilGen.Emit(OpCodes.Ldarg, 4);			// columnsIx
			ilGen.Emit(OpCodes.Call, info.FillMethod[typeof(IDataReader)]);

			//if (topLevel)
			//{
			//   objectList.Add(obj);
			//}
			Label lblNotTopLevel = ilGen.DefineLabel();
			ilGen.Emit(OpCodes.Ldarg, 5); // topLevel
			ilGen.Emit(OpCodes.Brfalse, lblNotTopLevel); // topLevel
			ilGen.Emit(OpCodes.Ldarg, 6); // objectList
			ilGen.Emit(OpCodes.Ldloc, locObj);
			//ilGen.Emit(OpCodes.Callvirt, _ListAdd);
			ilGen.MarkLabel(lblNotTopLevel);

			KeyInfo pkInfo = info.PrimaryKeyInfo;
			int keyIx = 0;
			if (pkInfo != null)
			{			
				LocalBuilder locPk = ilGen.DeclareLocal(info.PrimaryKeyInfo.KeyType);
				ilGen.Emit(OpCodes.Newobj, info.PrimaryKeyInfo.KeyType);
				ilGen.Emit(OpCodes.Stloc, locPk);

				ilGen.Emit(OpCodes.Ldloc, locPk);	// object
				ilGen.Emit(OpCodes.Ldarg_1);			// reader
				ilGen.Emit(OpCodes.Ldarg_2);			// mapper
				ilGen.Emit(OpCodes.Ldarg, 5);			// keyColumns
				ilGen.Emit(OpCodes.Ldc_I4, keyIx++);// columnsIx
				ilGen.Emit(OpCodes.Call, info.PrimaryKeyInfo.FillMethod);
				
				ilGen.Emit(OpCodes.Ldloc, locPkDict);
				ilGen.Emit(OpCodes.Ldloc, locPk);
				ilGen.Emit(OpCodes.Ldloc, locObj);
				//ilGen.Emit(OpCodes.Callvirt, _DictAdd);
			}
			
			//List<LocalBuilder> fkDicts = new List<LocalBuilder>();
			for (int i = 0; i < info.ForeignKeysInfo.Count; i++)
			{
				Label lblFkPresent = ilGen.DefineLabel();

				ilGen.Emit(OpCodes.Newobj, info.ForeignKeysInfo[i].KeyType);
				ilGen.Emit(OpCodes.Stloc, locKeys[i]);

				//object fk = _ObjectBuilder.CreateObject(item.KeyType);
				//CallExtractorMethod(item.FillMethod, fk, reader, columnIndexes);
				ilGen.Emit(OpCodes.Ldloc, locKeys[i]);	//object
				ilGen.Emit(OpCodes.Ldarg_1);				// reader
				ilGen.Emit(OpCodes.Ldarg_2);				// mapper
				ilGen.Emit(OpCodes.Ldarg, 5);				// keyColumns
				ilGen.Emit(OpCodes.Ldc_I4, keyIx++);	// columnsIx
				ilGen.Emit(OpCodes.Call, info.ForeignKeysInfo[i].FillMethod);

				//List<object> fko;
				//if (!fkObjects.TryGetValue(fk, out fko))
				//{
				//   fko = new List<object>();
				//   fkObjects.Add(fk, fko);
				//}
				//
				//fko.Add(obj);
				ilGen.Emit(OpCodes.Ldloc, locKeyDicts[i]);
				ilGen.Emit(OpCodes.Ldloc, locKeys[i]);
				ilGen.Emit(OpCodes.Ldloca, locKeyLists[i]);
				//ilGen.Emit(OpCodes.Callvirt, _TryGetValue);
				ilGen.Emit(OpCodes.Brtrue, lblFkPresent); //br to fko.Add(obj);
				ilGen.Emit(OpCodes.Newobj, objListType);
				ilGen.Emit(OpCodes.Stloc, locKeyLists[i]);

				ilGen.Emit(OpCodes.Ldloc, locKeyDicts[i]);
				ilGen.Emit(OpCodes.Ldloc, locKeys[i]);
				ilGen.Emit(OpCodes.Ldloc, locKeyLists[i]);
				//ilGen.Emit(OpCodes.Callvirt, _DictAdd);

				ilGen.MarkLabel(lblFkPresent);

				ilGen.Emit(OpCodes.Ldloc, locKeyLists[i]);
				ilGen.Emit(OpCodes.Ldloc, locKeys[i]);
				//ilGen.Emit(OpCodes.Callvirt, _ListAdd);
			}

			ilGen.Emit(OpCodes.Ldarg_1);	// reader
			//ilGen.Emit(OpCodes.Callvirt, _Read);
			ilGen.Emit(OpCodes.Brtrue, lblStart);

			ilGen.Emit(OpCodes.Ret);
		}


		public ExtractInfo CreateExtractInfoWithMethod(Type targetClassType, int schemeId, DataTable dtSource, Type generatorSourceType)
		{
			ExtractInfo result = CreateExtractInfo(targetClassType, schemeId);
			if (!result.FillMethod.ContainsKey(generatorSourceType))
				GenerateSetterMethod(result, dtSource, generatorSourceType);

			return result;
		}

		public ExtractInfo CreateExtractInfo(Type targetClassType, int schemeId)
		{
			//Check cache
			ExtractInfo result;
			if (_ExtractInfoCache.TryGetExtractInfo(targetClassType, schemeId, out result))
				return result;

			//Cache miss, create new one
			result = new ExtractInfo(targetClassType, schemeId);
			_ExtractInfoCache.Add(targetClassType, schemeId, result);

			List<PropertyInfo> props = ReflectHelper.GetProps(targetClassType);

			foreach (PropertyInfo prop in props)
			{
				DataMapAttribute mapping = GetMappingMethod(targetClassType, schemeId)
					(prop, schemeId);

				if (mapping == null)
					continue;

				if (mapping.GetType() == typeof(DataColumnMapAttribute))
				{
					DataColumnMapAttribute clmnmap = (DataColumnMapAttribute)mapping;

					result.MemberColumns.Add(
						new MemberExtractInfo(clmnmap.MappingName, prop)
						);
				}
				else if (mapping.GetType() == typeof(DataRelationMapAttribute))
				{
					DataRelationMapAttribute rmap = (DataRelationMapAttribute)mapping;

					if (!typeof(IList).IsAssignableFrom(prop.PropertyType))
						throw new DataMapperException("Cannot set nested objects for collection that does not implement IList (" + prop.Name + ").");

					if (rmap.ItemType == null)
						rmap.ItemType =  GetListItemType(prop.PropertyType);

					if (rmap.ItemType == null)
						throw new DataMapperException("Cannot resolve type of items in collection(" + prop.Name + "). " +
							"Try to set it via ItemType property of DataRelationMapAttribute.");

					result.ChildTypes.Add(
						new RelationExtractInfo(
							rmap.MappingName,
							prop,
							CreateExtractInfo(rmap.ItemType, rmap.NestedSchemeId)
							)
						);
				}
				else if (mapping.GetType() == typeof(ComplexDataMapAttribute))
				{
					ComplexDataMapAttribute cmap = (ComplexDataMapAttribute)mapping;

					if (cmap.ItemType == null)
						cmap.ItemType = prop.PropertyType;

					result.SubTypes.Add(
						new RelationExtractInfo(
							cmap.MappingName,
							prop,
							CreateExtractInfo(cmap.ItemType, cmap.NestedSchemeId)
							)
						);
				}
			}
			
			FieldInfo[] fields = targetClassType.GetFields();
			//result.Fields = fields;

			foreach (FieldInfo field in fields)
			{
				DataColumnMapAttribute mapping = GetMappingMethod(targetClassType, schemeId)
					(field, schemeId) as DataColumnMapAttribute;

				if (mapping == null)
					continue;

				result.MemberColumns.Add(
					new MemberExtractInfo(
						mapping.MappingName,
						field
						)
					);
			}

			//Extract info about primary Key
			result.PrimaryKeyInfo = GetPrimaryKey(targetClassType, schemeId);

			//Extract info about foreign keys
			result.ForeignKeysInfo.AddRange(
				GetForeignKeys(targetClassType, schemeId)
				);

			return result;
		}

		/// <summary>
		/// Generates setter method using xml config or type meta info.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <returns></returns>
		public ExtractInfo GenerateSetterMethod(ExtractInfo extractInfo, DataTable dtSource, Type generatorSourceType)
		{
			IPropertySetterGenerator methodGenerator = _SetterGenerators[generatorSourceType];

			// First process complex types
			foreach (RelationExtractInfo item in extractInfo.SubTypes)
			{
				GenerateSetterMethod(
					item.ExtractInfo,
					dtSource,
					generatorSourceType
					);
			}

			//Generating Type and method declaration
			TypeBuilder typeBuilder = CreateAssemblyType(extractInfo.TargetType);
			MethodBuilder methodBuilder = GenerateSetterMethodDefinition(
				extractInfo.TargetType, typeBuilder, methodGenerator.DataSourceType);
			ILGenerator ilGen = methodBuilder.GetILGenerator();

			methodGenerator.GenerateMethodHeader(ilGen);

			int propIx = 0;
			foreach (MemberExtractInfo mei in extractInfo.MemberColumns)
			{
				methodGenerator.CreateExtractScalar(
					ilGen,
					mei.Member as PropertyInfo,
					mei.Member as FieldInfo,
					mei.MapName,
					dtSource,
					propIx++
					);
			}

			foreach (RelationExtractInfo rei in extractInfo.ChildTypes)
			{
				methodGenerator.CreateExtractNested(
					ilGen,
					rei.Member as PropertyInfo,
					rei.ExtractInfo.TargetType,
					rei.MapName,
					rei.ExtractInfo.SchemeId
					);
			}

			foreach (RelationExtractInfo rei in extractInfo.SubTypes)
			{
				methodGenerator.GenerateExtractComplex(
					ilGen,
					rei.Member as PropertyInfo,
					rei.ExtractInfo.TargetType,
					rei.ExtractInfo.FillMethod[methodGenerator.DataSourceType]
					);
			}

			ilGen.Emit(OpCodes.Ret);
			Type type = typeBuilder.CreateType();
			extractInfo.FillMethod[methodGenerator.DataSourceType] = type.GetMethod("SetProps_" + extractInfo.TargetType);

			//Extract info about primary Key
			if (extractInfo.PrimaryKeyInfo != null)
			{
				extractInfo.PrimaryKeyInfo = GenerateKey(
					extractInfo.PrimaryKeyInfo,
					extractInfo.SchemeId,
					dtSource,
					generatorSourceType);
			}

			//Extract info about foreign keys
			List<KeyInfo> foreignKeysInfo = new List<KeyInfo>();
			foreach (KeyInfo fk in extractInfo.ForeignKeysInfo)
			{
				foreignKeysInfo.Add(
					GenerateKey(fk, extractInfo.SchemeId, dtSource, generatorSourceType)
					);
			}

			extractInfo.ForeignKeysInfo = foreignKeysInfo;

			return extractInfo;
		}


		protected GetPropertyMapping GetMappingMethod(Type targetClassType, int schemeId)
		{
			if (_XmlDocument != null && IsXmlMappingExists(targetClassType, schemeId))
			{
				return GetMappingFromXml;
			}
			else //If there is no xml config or type mapping not defined in xml
			{
				return GetMappingFromAtt;
			}
		}

		/// <summary>
		/// Checks if xml mapping for specified type exists
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="schemeId"></param>
		/// <returns></returns>
		protected bool IsXmlMappingExists(Type targetClassType, int schemeId)
		{
			if (_XmlDocument == null)
				return false;

			//Generate XPath Query
			string qry = "/MappingDefinition/TypeMapping{0}";
			string typeName = targetClassType.Assembly.GetName().Name;
			typeName = typeName + ", " + targetClassType.FullName;
			string typeClause = "[@typeName=\"" + typeName + "\" and @schemeId=\"" + schemeId + "\"]";
			qry = String.Format(qry, typeClause);

			//Looking for node
			return _XmlDocument.SelectSingleNode(qry) != null;
		}

		/// <summary>
		/// Helper method. Returns first generic argument type for first generic subtype.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		protected Type GetListItemType(Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType)
					break;

				type = type.BaseType;
			}

			if (type == null)
				return null;

			return type.GetGenericArguments()[0];
		}
		
		protected KeyInfo GetPrimaryKey(Type targetClassType, int schemeId)
		{
			return null;
		}

		protected List<KeyInfo> GetForeignKeys(Type targetClassType, int schemeId)
		{
			return new List<KeyInfo>();
		}

		protected KeyInfo GenerateKey(KeyInfo pk, int schemeId, DataTable dtSource, Type generatorSourceType)
		{
			IPropertySetterGenerator methodGenerator = _SetterGenerators[generatorSourceType];

			pk.KeyType = _KeyGenerator.GenerateKeyType(
				pk.Name,
				dtSource,
				pk.Columns,
				methodGenerator,
				schemeId
				);

			pk.FillMethod = CreateExtractInfoWithMethod(
				pk.KeyType,
				schemeId,
				dtSource,
				generatorSourceType
				).FillMethod[generatorSourceType];

			return pk;
		}

		/// <summary>
		/// Creates dynamic assembly for holding generated type with setter methods.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <returns></returns>
		protected TypeBuilder CreateAssemblyType(Type targetClassType)
		{
			if (_ModuleBuilder == null)
			{
				_AsmBuilder = Thread.GetDomain().DefineDynamicAssembly(
					new AssemblyName("DataPropertySetterAsm"), AssemblyBuilderAccess.Run);
				_ModuleBuilder = _AsmBuilder.DefineDynamicModule("DataPropertySetterMod");
			}

			string className = "DataPropertySetter_" + targetClassType.FullName;
			string newClassName = className;
			int i = 0;
			while (_ModuleBuilder.GetType(newClassName) != null)
				newClassName = className + i++;

			return _ModuleBuilder.DefineType(newClassName, TypeAttributes.Class | TypeAttributes.Public);
		}

		/// <summary>
		/// Creates method definition for holding setter method.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="typeBuilder"></param>
		/// <returns></returns>
		protected MethodBuilder GenerateSetterMethodDefinition(
			Type targetClassType, 
			TypeBuilder typeBuilder,
			Type dataSourceType)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("SetProps_" + targetClassType,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard, typeof(void),
				new Type[] { targetClassType, dataSourceType, typeof(DataMapper), typeof(List<List<int>>), Type.GetType("System.Int32&") });

			methodBuilder.DefineParameter(1, ParameterAttributes.In, "target");
			methodBuilder.DefineParameter(2, ParameterAttributes.In, "row");
			methodBuilder.DefineParameter(3, ParameterAttributes.In, "mapper");
			methodBuilder.DefineParameter(4, ParameterAttributes.In, "columnsList");
			methodBuilder.DefineParameter(5, ParameterAttributes.Out, "columnsIx");

			return methodBuilder;
		}

		/// <summary>
		/// Retrieves mapping info for specified property from type meta info (attributes).
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="schemeId"></param>
		/// <returns></returns>
		protected DataMapAttribute GetMappingFromAtt(MemberInfo prop, int schemeId)
		{
			object[] attrs = prop.GetCustomAttributes(true);
			if (attrs != null && attrs.Length > 0)
			{
				DataMapAttribute mappingAtt = Array.Find(attrs, delegate(object att)
				{
					DataMapAttribute propAtt = att as DataMapAttribute;
					return propAtt != null && propAtt.SchemeId == schemeId;
				}) as DataMapAttribute;

				if (mappingAtt != null &&
					 mappingAtt is DataColumnMapAttribute &&
					 String.IsNullOrEmpty(mappingAtt.MappingName))
					mappingAtt.MappingName = prop.Name;

				return mappingAtt;
			}

			return null;
		}

		/// <summary>
		/// Retrieves mapping info for specified property from xml config file.
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="schemeId"></param>
		/// <returns></returns>
		protected DataMapAttribute GetMappingFromXml(MemberInfo prop, int schemeId)
		{
			//Looking for node in reflected type
			XmlNode xmlMapping = FindMapping(prop.ReflectedType, schemeId, prop);
			if (xmlMapping == null)
			{
				xmlMapping = FindMapping(prop.DeclaringType, schemeId, prop);
				if (xmlMapping == null)
					return null;
			}

			//Create mapping class
			XmlAttribute att = xmlMapping.Attributes["dataColumnName"];
			if (att == null)
			{
				att = xmlMapping.Attributes["dataRelationName"];
				Type itemType = null;
				int nestedSchemaId = schemeId;

				if (att != null)
				{
					GetNestedProps(xmlMapping, ref nestedSchemaId, ref itemType);
					return new DataRelationMapAttribute(att.Value, schemeId, nestedSchemaId, itemType);
				}
				else
				{
					att = xmlMapping.Attributes["complex"];

					if (att != null)
					{
						GetNestedProps(xmlMapping, ref nestedSchemaId, ref itemType);
						return new ComplexDataMapAttribute(nestedSchemaId, itemType);
					}
					else
						return new DataColumnMapAttribute(prop.Name, schemeId);
				}
			}
			else
				return new DataColumnMapAttribute(
					String.IsNullOrEmpty(att.Value) ? prop.Name : att.Value, schemeId);
		}

		/// <summary>
		/// Looks for mapping definition in reflected type or if it is not found, looks in declared type.
		/// </summary>
		/// <param name="propType"></param>
		/// <param name="schemeId"></param>
		/// <param name="prop"></param>
		/// <returns></returns>
		protected XmlNode FindMapping(Type propType, int schemeId, MemberInfo prop)
		{
			//Generate XPath Query
			string qry = "/MappingDefinition/TypeMapping{0}/PropetyMapping{1}";
			string type = propType.Assembly.GetName().Name;
			type = type + ", " + propType.FullName;
			string typeClause = "[@typeName=\"" + type + "\" and @schemeId=\"" + schemeId + "\"]";
			string propClause = "[@propertyName = \"" + prop.Name + "\"]";
			qry = String.Format(qry, typeClause, propClause);

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
				string[] typeInfo = attType.Value.Split(',');
				itemType = Assembly.Load(typeInfo[0].Trim()).GetType(typeInfo[1].Trim());
			}

			XmlAttribute attNestedSchemaId = xmlMapping.Attributes["nestedSchemaId"];
			if (attNestedSchemaId != null && !String.IsNullOrEmpty(attNestedSchemaId.Value))
				nestedSchemeId = int.Parse(attNestedSchemaId.Value);
		}
	}
}
