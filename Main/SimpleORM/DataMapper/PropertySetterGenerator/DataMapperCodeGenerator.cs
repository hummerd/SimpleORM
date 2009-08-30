using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml;
using SimpleORM.Attributes;


namespace SimpleORM.PropertySetterGenerator
{
	public class DataMapperCodeGenerator
	{
		protected readonly
			Dictionary<Type,		//target object type (Entity type)
				Dictionary<Type,	//extractor type DataTable or IDataReader
					Dictionary<int, ExtractInfo>>> _ExtractorCache = new Dictionary<Type, Dictionary<Type, Dictionary<int, ExtractInfo>>>();

		protected ModuleBuilder		_ModuleBuilder;
		protected AssemblyBuilder	_AsmBuilder;
		protected Dictionary<Type, IPropertySetterGenerator> _SetterGenerators;
		protected XmlDocument		_XmlDocument;
		protected string				_ConfigFile;
		protected KeyClassGenerator _KeyGenerator;


		public DataMapperCodeGenerator(Dictionary<Type, IPropertySetterGenerator> setterGenerators)
		{
			if (setterGenerators == null)
			{
				_SetterGenerators = new Dictionary<Type, IPropertySetterGenerator>(2);
				_SetterGenerators.Add(typeof(DataTable), new DataRowPSG());
				_SetterGenerators.Add(typeof(IDataReader), new DataReaderPSG());
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
			_ExtractorCache.Clear();
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
			ilGen.Emit(OpCodes.Call, info.FillMethod);

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

		/// <summary>
		/// Returns setter method for specified type.
		/// </summary>
		/// <remarks>If this is first call for specified type then setter method is generated and placed into cache.
		/// If setter method exists in cache then it just retriveid from there.</remarks>
		/// <param name="objectType"></param>
		/// <param name="dtSource"></param>
		/// <param name="schemeId"></param>
		/// <returns></returns>
		public ExtractInfo GetSetterMethod(Type objectType, Type extractorType, DataTable dtSource, int schemeId)
		{
			IPropertySetterGenerator methodGenerator = _SetterGenerators[extractorType];

			ExtractInfo result;
			Dictionary<Type, Dictionary<int, ExtractInfo>> extractorSchemas;
			Dictionary<int, ExtractInfo> schemeMethods;

			bool extractorTypeExists = _ExtractorCache.TryGetValue(objectType, out extractorSchemas);

			if (!extractorTypeExists) // If extractor method does not exist for this type
			{
				result = GenerateSetterMethod(objectType, schemeId, dtSource, methodGenerator);
				//Add method to cache
				schemeMethods = new Dictionary<int, ExtractInfo>(2) { { schemeId, result } };
				_ExtractorCache.Add(
					objectType,
					new Dictionary<Type, Dictionary<int, ExtractInfo>>() { { extractorType, schemeMethods } }
					);
			}
			// if extracot of specified type does not exists		
			else if (!extractorSchemas.TryGetValue(extractorType, out schemeMethods))
			{
				result = GenerateSetterMethod(objectType, schemeId, dtSource, methodGenerator);
				extractorSchemas.Add(
					extractorType,
					new Dictionary<int, ExtractInfo>(2) { { schemeId, result } }
					);
			}
			// If extractor method does not exist for this scheme
			else if (!schemeMethods.TryGetValue(schemeId, out result))
			{
				result = GenerateSetterMethod(objectType, schemeId, dtSource, methodGenerator);
				schemeMethods.Add(schemeId, result);
			}

			return result;
		}


		//protected ExtractInfo GenerateComplexSetterMethod(Type targetClassType, int schemeId, DataTable dtSource, IPropertySetterGenerator methodGenerator)
		//{
		//   Stack<ComplexDataMapAttribute> relatedObjects = FindSubObjects(targetClassType, schemeId, null);
		//   while (relatedObjects.Count > 0)
		//   {
		//      ComplexDataMapAttribute dma = relatedObjects.Pop();
		//      GenerateSetterMethod(dma.ItemType, dma.NestedSchemeId, dtSource, methodGenerator);
		//   }

		//   return GenerateSetterMethod(targetClassType, schemeId, dtSource, methodGenerator);
		//}

		//protected Stack<ComplexDataMapAttribute> FindSubObjects(Type targetClassType, int schemeId, Stack<ComplexDataMapAttribute> result)
		//{
		//   if (result == null)
		//      result = new Stack<ComplexDataMapAttribute>();

		//   List<ComplexDataMapAttribute> localComplex = new List<ComplexDataMapAttribute>();

		//   bool useXmlMapping = _XmlDocument != null && IsXmlMappingExists(targetClassType, schemeId);
		//   PropertyInfo[] props = targetClassType.GetProperties();

		//   foreach (PropertyInfo prop in props)
		//   {
		//      ComplexDataMapAttribute mapping = useXmlMapping ?
		//         GetMappingFromXml(prop, schemeId) as ComplexDataMapAttribute : 
		//         GetMappingFromAtt(prop, schemeId) as ComplexDataMapAttribute;

		//      if (mapping == null)
		//         continue;

		//      if (mapping.ItemType == null)
		//         mapping.ItemType = prop.PropertyType;

		//      //if (!localComplex.Contains(mapping))
		//      //   localComplex.Add(mapping);
		//   }

		//   foreach (var item in localComplex)
		//      if (result.Contains(item))
		//         throw new DataMapperException("Can not extract complex objects with cyclic references");

		//   foreach (var item in localComplex)
		//      result.Push(item);

		//   foreach (var item in localComplex)
		//      FindSubObjects(item.ItemType, item.NestedSchemeId, result);

		//   return result;
		//}

		/// <summary>
		/// Generates setter method using xml config or type meta info.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <returns></returns>
		protected ExtractInfo GenerateSetterMethod(Type targetClassType, int schemeId, DataTable dtSource, IPropertySetterGenerator methodGenerator)
		{
			ExtractInfo result = new ExtractInfo();

			result.SubTypes = GetSubTypes(targetClassType, schemeId, dtSource, methodGenerator);

			//Generating Type and method declaration
			TypeBuilder typeBuilder = CreateAssemblyType(targetClassType);
			MethodBuilder methodBuilder = GenerateSetterMethodDefinition(
				targetClassType, typeBuilder, methodGenerator.DataSourceType);
			ILGenerator ilGen = methodBuilder.GetILGenerator();

			if (_XmlDocument != null && IsXmlMappingExists(targetClassType, schemeId))
			{
				methodGenerator.GenerateSetterMethod(
					ilGen, targetClassType, schemeId, dtSource, GetMappingFromXml, result);
			}
			else //If there is no xml config or type mapping not defined in xml
			{
				methodGenerator.GenerateSetterMethod(
					ilGen, targetClassType, schemeId, dtSource, GetMappingFromAtt, result);
			}

			Type type = typeBuilder.CreateType();
			result.FillMethod = type.GetMethod("SetProps_" + targetClassType);

			//Extract info about primary Key
			KeyInfo pk = GetPrimaryKey(targetClassType, schemeId);
			if (pk != null)
			{
				result.PrimaryKeyInfo = GenerateKey(pk, schemeId, dtSource, methodGenerator);
			}

			//Extract info about foreign keys
			List<KeyInfo> foreignKeys = GetForeignKeys(targetClassType, schemeId);
			foreach (var item in foreignKeys)
			{
				result.ForeignKeysInfo.Add(
					GenerateKey(pk, schemeId, dtSource, methodGenerator)
					);
			}

			return result;
		}

		protected KeyInfo GetPrimaryKey(Type targetClassType, int schemeId)
		{
			return null;
		}

		protected List<KeyInfo> GetForeignKeys(Type targetClassType, int schemeId)
		{
			return new List<KeyInfo>();
		}

		protected KeyInfo GenerateKey(KeyInfo pk, int schemeId, DataTable dtSource, IPropertySetterGenerator methodGenerator)
		{
			pk.KeyType = _KeyGenerator.GenerateKeyType(
				pk.Name,
				dtSource,
				pk.Columns,
				methodGenerator,
				schemeId
				);

			pk.FillMethod = GenerateSetterMethod(
				pk.KeyType,
				schemeId,
				dtSource,
				methodGenerator
				).FillMethod;

			return pk;
		}


		/// <summary>
		/// Get list of sub types assosiated with targetClassType. Sub type is type marked as complex in mapping schema.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <param name="methodGenerator"></param>
		/// <returns></returns>
		protected List<ExtractInfo> GetSubTypes(Type targetClassType, int schemeId, DataTable dtSource, IPropertySetterGenerator methodGenerator)
		{
			List<ExtractInfo> result = new List<ExtractInfo>();

			bool useXmlMapping = _XmlDocument != null && IsXmlMappingExists(targetClassType, schemeId);
			PropertyInfo[] props = targetClassType.GetProperties();

			foreach (PropertyInfo prop in props)
			{
				ComplexDataMapAttribute mapping = useXmlMapping ?
					GetMappingFromXml(prop, schemeId) as ComplexDataMapAttribute :
					GetMappingFromAtt(prop, schemeId) as ComplexDataMapAttribute;

				if (mapping == null)
					continue;

				if (mapping.ItemType == null)
					mapping.ItemType = prop.PropertyType;

				result.Add(
					GenerateSetterMethod(
						mapping.ItemType,
						mapping.NestedSchemeId,
						dtSource,
						methodGenerator));
			}

			return result;
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

		/// <summary>
		/// Checks if xml mapping for specified type exists
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="schemeId"></param>
		/// <returns></returns>
		protected bool IsXmlMappingExists(Type type, int schemeId)
		{
			//Generate XPath Query
			string qry = "/MappingDefinition/TypeMapping{0}";
			string typeName = type.Assembly.GetName().Name;
			typeName = typeName + ", " + type.FullName;
			string typeClause = "[@typeName=\"" + typeName + "\" and @schemeId=\"" + schemeId + "\"]";
			qry = String.Format(qry, typeClause);

			//Looking for node
			return _XmlDocument.SelectSingleNode(qry) != null;
		}
	}
}
