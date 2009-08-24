﻿using System;
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


		protected Type GenerateKeyType(string key, DataTable dtSource, List<string> columns, IPropertySetterGenerator methodGenerator, int schemeId)
		{
			string className = "DataPropertySetter_" + key;
			var tb = _ModuleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);

			MethodBuilder equals = tb.DefineMethod("Equals",
				MethodAttributes.Public | MethodAttributes.ReuseSlot |
				MethodAttributes.Virtual | MethodAttributes.HideBySig,
				typeof(bool),
				new Type[] { typeof(object) });
			ILGenerator equalsGen = equals.GetILGenerator();
			LocalBuilder locObj = equalsGen.DeclareLocal(tb);
			equalsGen.Emit(OpCodes.Ldarg_1);
			equalsGen.Emit(OpCodes.Castclass, tb);
			equalsGen.Emit(OpCodes.Stloc_0);

			foreach (var item in columns)
			{
				//Generate field
				var fb = tb.DefineField(item, dtSource.Columns[item].DataType, FieldAttributes.Public);

				//Generate mapping definition attribute on this field
				CustomAttributeBuilder attributeBuilder =
					new CustomAttributeBuilder(
							typeof(DataColumnMapAttribute).GetConstructor(new Type[2] { typeof(string), typeof(int) }),
							new object[2] { item, schemeId });
				fb.SetCustomAttribute(attributeBuilder);

				//Generate part of Equals method for field generated above
				equalsGen.Emit(OpCodes.Ldarg_0);
				equalsGen.Emit(OpCodes.Ldfld, fb);
				equalsGen.Emit(OpCodes.Ldloc_0);
				equalsGen.Emit(OpCodes.Ldfld, fb);
			}

			//Generate Equals method
			
			//Generate Equals method

			//Generate GetHashCode method

			var type = tb.CreateType();

			GenerateSetterMethod(
				type,
				schemeId,
				dtSource,
				methodGenerator
				);

			return type;
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
			MethodBuilder methodBuilder = GenerateSetterMethodDefinition(targetClassType, typeBuilder);
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

			typeBuilder.CreateType();
			result.FillMethod = typeBuilder.Assembly.GetType(typeBuilder.FullName).
				GetMethod("SetProps_" + targetClassType);
			return result;
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
		protected MethodBuilder GenerateSetterMethodDefinition(Type targetClassType, TypeBuilder typeBuilder)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("SetProps_" + targetClassType,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard, typeof(void),
				new Type[] { targetClassType, typeof(object), typeof(DataMapper), typeof(List<List<int>>), Type.GetType("System.Int32&") });

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
		protected DataMapAttribute GetMappingFromAtt(PropertyInfo prop, int schemeId)
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
		protected DataMapAttribute GetMappingFromXml(PropertyInfo prop, int schemeId)
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
		protected XmlNode FindMapping(Type propType, int schemeId, PropertyInfo prop)
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
