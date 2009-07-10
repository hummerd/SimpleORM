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
using SimpleORM.PropertySetterGenerator;


namespace SimpleORM
{
	public class DataMapper
	{
		protected ModuleBuilder _ModuleBuilder;

		protected readonly 
			Dictionary<Type,		//target object type (Entity type)
				Dictionary<Type,	//extractor type DataTable or IDataReader
					Dictionary<int, ExtractInfo>>> _ExtractorCache = new Dictionary<Type, Dictionary<Type, Dictionary<int, ExtractInfo>>>();

		protected Dictionary<DataRow, object> _CreatedObjects;

		protected XmlDocument	_XmlDocument;
		protected string			_ConfigFile;

		protected IObjectBuilder _ObjectBuilder;
		protected Dictionary<Type, IPropertySetterGenerator> _SetterGenerators;


		public DataMapper(string configFile)
			: this(new StandartObjectBuilder(), null)
		{
			SetConfig(configFile);
		}

		public DataMapper(string configFile, IObjectBuilder objectBuilder)
			: this(objectBuilder, null)
		{
			SetConfig(configFile);
		}

		public DataMapper(IObjectBuilder objectBuilder)
			: this(objectBuilder, null)
		{ }

		protected DataMapper(IObjectBuilder objectBuilder, Dictionary<Type, IPropertySetterGenerator> setterGenerators)
		{
			_ObjectBuilder = objectBuilder;

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


		public IObjectBuilder ObjectBuilder
		{
			get { return _ObjectBuilder; }
			set { _ObjectBuilder = value; }
		}


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

		public void ClearCache()
		{
			_ExtractorCache.Clear();
			_ModuleBuilder = null;
		}


		#region Static facade

		protected static DataMapper _Instance;

		public static DataMapper Default
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = new DataMapper(new StandartObjectBuilder(), null);
				}

				return _Instance;
			}
		}

		#endregion

		#region Fill methods

		public void FillObjectList<TObject>(IList objectList, IDataReader reader)
			where TObject : class
		{
			FillObjectList<TObject>(objectList, reader, 0, true);
		}

		public void FillObjectList<TObject>(IList objectList, IDataReader reader, int schemeId, bool clearObjectCache)
			where TObject : class
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (reader == null)
				throw new ArgumentException("Cannot fill objects from null.", "reader");

			var objectType = typeof(TObject);

			//Create new cache only if we manage objects cache
			//if (clearObjectCache)
			//   _CreatedObjects = new Dictionary<DataRow, object>();
			ExtractInfo extractInfo = null;

			while (reader.Read())
			{
				if (extractInfo == null)
				{
					extractInfo = GetSetterMethod(
						objectType,
						typeof(IDataReader),
						GetTableFromSchema(reader.GetSchemaTable()), 
						schemeId);
					if (extractInfo == null || extractInfo.FillMethod == null)
						throw new InvalidOperationException("Can not fill object without mapping definition.");
				}

				object obj = _ObjectBuilder.CreateObject(objectType);
				//Fill object
				CallExtractorMethod(extractInfo.FillMethod, obj, reader, null);
				objectList.Add(obj);
			}
		}

		public TObject FillObject<TObject>(IDataReader reader, TObject obj)
			where TObject : class
		{
			return FillObject(reader, typeof(TObject), obj, 0) as TObject;
		}

		public TObject FillObject<TObject>(IDataReader reader, TObject obj, int schemeId)
			where TObject : class
		{
			return FillObject(reader, typeof(TObject), obj, schemeId) as TObject;
		}

		public object FillObject(IDataReader reader, Type objectType, object obj, int schemeId)
		{
			if (reader == null)
				throw new ArgumentNullException("reader", "Cannot fill object from null.");

			if (objectType == null && obj == null)
				throw new ArgumentNullException("objectType", "Cannot fill object of unknown type null.");

			if (objectType == null)
				objectType = obj.GetType();

			ExtractInfo extractInfo = GetSetterMethod(
				objectType,
				typeof(IDataReader),
				GetTableFromSchema(reader.GetSchemaTable()), 
				schemeId);

			if (extractInfo == null || extractInfo.FillMethod == null)
				throw new DataMapperException("Can not fill object without mapping definition.");

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject(objectType);

			//Fill object
			CallExtractorMethod(extractInfo.FillMethod, obj, reader, null);
			return obj;
		}


		public void FillObjectList<TObject>(IList objectList, DataView dataCollection)
			where TObject : class
		{
			FillObjectListInternal<DataRowView>(objectList, typeof(TObject), dataCollection, 0,
				delegate(DataRowView drv)
				{ return drv.Row; },
				true);
		}

		public void FillObjectList<TObject>(IList objectList, DataRowCollection dataCollection)
			where TObject : class
		{
			FillObjectListInternal<DataRow>(objectList, typeof(TObject), dataCollection, 0, null, true);
		}

		public void FillObjectList<TObject>(IList objectList, ICollection dataCollection)
			where TObject : class
		{
			FillObjectListInternal<DataRow>(objectList, typeof(TObject), dataCollection, 0, null, true);
		}

		public void FillObjectList<TObject>(IList objectList, DataView dataCollection, int schemeId)
			where TObject : class
		{
			FillObjectListInternal<DataRowView>(objectList, typeof(TObject), dataCollection, schemeId,
				delegate(DataRowView drv)
				{ return drv.Row; },
				true);
		}

		public void FillObjectList<TObject>(IList objectList, DataRowCollection dataCollection, int schemeId)
			where TObject : class
		{
			FillObjectListInternal<DataRow>(objectList, typeof(TObject), dataCollection, schemeId, null, true);
		}

		public void FillObjectList<TObject>(IList objectList, ICollection dataCollection, int schemeId)
			where TObject : class
		{
			FillObjectListInternal<DataRow>(objectList, typeof(TObject), dataCollection, schemeId, null, true);
		}

		public void FillObjectList(IList objectList, Type objectType, ICollection dataCollection, int schemeId)
		{
			FillObjectListInternal<DataRow>(objectList, objectType, dataCollection, schemeId, null, true);
		}

		/// <summary>
		/// Do not use this method!!! This method is for internal use only!!!
		/// </summary>
		/// <param name="objectList"></param>
		/// <param name="objectType"></param>
		/// <param name="dataCollection"></param>
		/// <param name="schemeId"></param>
		public void FillObjectListNested(IList objectList, Type objectType, ICollection dataCollection, int schemeId)
		{
			FillObjectListInternal<DataRow>(objectList, objectType, dataCollection, schemeId, null, false);
		}


		public TObject FillObject<TObject>(DataRow dataRow, TObject obj)
			where TObject : class
		{
			return FillObject(dataRow, typeof(TObject), obj, 0) as TObject;
		}

		public TObject FillObject<TObject>(DataRow dataRow, TObject obj, int schemeId)
			where TObject : class
		{
			return FillObject(dataRow, typeof(TObject), obj, schemeId) as TObject;
		}

		public object FillObject(DataRow dataRow, Type objectType, object obj, int schemeId)
		{
			if (dataRow == null)
				throw new ArgumentNullException("dataRow", "Cannot fill object from null.");

			if (objectType == null && obj == null)
				throw new ArgumentNullException("objectType", "Cannot fill object of unknown type null.");

			if (objectType == null)
				objectType = obj.GetType();

			ExtractInfo extractInfo = GetSetterMethod(
				objectType, 
				typeof(DataTable),
				dataRow.Table, 
				schemeId);

			if (extractInfo == null || extractInfo.FillMethod == null)
				throw new DataMapperException("Can not fill object without mapping definition.");

			List<int> columnIndexes = ColumnsIndexes(dataRow.Table, extractInfo.PropColumns);

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject(objectType);

			//Fill object
			CallExtractorMethod(extractInfo.FillMethod, obj, dataRow, columnIndexes);
			return obj;
		}


		protected void FillObjectListInternal<TRowItem>(IList objectList, Type objectType, ICollection dataCollection, int schemeId, DataRowExtractor<TRowItem> rowExtractor, bool clearObjectCache)
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (dataCollection == null)
				throw new ArgumentException("Cannot fill objects from null.", "dataCollection");

			Type listType = objectList.GetType();
			if (listType.IsGenericType)
				objectType = listType.GetGenericArguments()[0];

			if (objectType == null)
				throw new ArgumentException("Cannot fill object of unknown type null.", "objectType");

			ExtractInfo extractInfo = null;
			List<int> columnIndexes = null;

			//Trying to increase internal capacity of object container
			MethodInfo setCapacity = objectList.GetType().GetMethod("set_Capacity", new Type[] { typeof(int) });
			if (setCapacity != null)
				setCapacity.Invoke(objectList, new object[] { dataCollection.Count + objectList.Count });

			//Create new cache only if we manage objects cache
			if (clearObjectCache)
				_CreatedObjects = new Dictionary<DataRow, object>(dataCollection.Count);

			//If there is no instance create it
			foreach (TRowItem row in dataCollection)
			{
				DataRow dataRow;
				if (rowExtractor == null)
					dataRow = row as DataRow;
				else
					dataRow = rowExtractor(row);

				if (extractInfo == null)
				{
					extractInfo = GetSetterMethod(
						objectType,
						typeof(DataTable),
						dataRow.Table, 
						schemeId);

					if (extractInfo == null || extractInfo.FillMethod == null)
						throw new InvalidOperationException("Can not fill object without mapping definition.");

					columnIndexes = ColumnsIndexes(dataRow.Table, extractInfo.PropColumns);
				}

				object obj;
				if (!_CreatedObjects.TryGetValue(dataRow, out obj))
				{
					obj = _ObjectBuilder.CreateObject(objectType);
					//Fill object
					CallExtractorMethod(extractInfo.FillMethod, obj, dataRow, columnIndexes);
					_CreatedObjects.Add(dataRow, obj);
				}

				objectList.Add(obj);
			}

			if (clearObjectCache)
				_CreatedObjects.Clear();
		}

		protected List<int> ColumnsIndexes(DataTable table, List<string> columns)
		{
			List<int> result = new List<int>(columns.Count);
			for (int i = 0; i < columns.Count; i++)
				result.Add(table.Columns.IndexOf(columns[i]));

			return result;
		}

		protected void CallExtractorMethod(MethodInfo extractorMethod, object obj, object data, IList<int> columns)
		{
			extractorMethod.Invoke(null, new object[] { obj, data, this, columns });
		}

		#endregion

		#region Generator methods

		/// <summary>
		/// Returns setter method for specified type.
		/// </summary>
		/// <remarks>If this is first call for specified type then setter method is generated and placed into cache.
		/// If setter method exists in cache then it just retriveid from there.</remarks>
		/// <param name="objectType"></param>
		/// <param name="dtSource"></param>
		/// <param name="schemeId"></param>
		/// <returns></returns>
		protected ExtractInfo GetSetterMethod(Type objectType, Type extractorType, DataTable dtSource, int schemeId)
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
				schemeMethods = new Dictionary<int, ExtractInfo>(2) { {schemeId, result} };
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
					new Dictionary<int, ExtractInfo>(2) { {schemeId, result} }
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

		protected ExtractInfo GenerateComplexSetterMethod(Type targetClassType, int schemeId, DataTable dtSource, IPropertySetterGenerator methodGenerator)
		{
			Stack<ComplexDataMapAttribute> relatedObjects = FindSubObjects(targetClassType, schemeId, null);
			while (relatedObjects.Count > 0)
			{
				ComplexDataMapAttribute dma = relatedObjects.Pop();
				GenerateSetterMethod(dma.ItemType, dma.NestedSchemeId, dtSource, methodGenerator);
			}

			return GenerateSetterMethod(targetClassType, schemeId, dtSource, methodGenerator);
		}

		protected Stack<ComplexDataMapAttribute> FindSubObjects(Type targetClassType, int schemeId, Stack<ComplexDataMapAttribute> result)
		{
			if (result == null)
				result = new Stack<ComplexDataMapAttribute>();

			List<ComplexDataMapAttribute> localComplex = new List<ComplexDataMapAttribute>();

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

				if (!localComplex.Contains(mapping))
					localComplex.Add(mapping);
			}

			foreach (var item in localComplex)
			{
				if (result.Contains(item))
					throw new DataMapperException("Can not extract complex objects with cyclic references");
				else
					result.Push(item);
			}

			foreach (var item in localComplex)
			{
				FindSubObjects(item.ItemType, item.NestedSchemeId, result);
			}

			return result;
		}
		
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
		/// Creates dynamic assembly for holding generated type with setter methods.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <returns></returns>
		protected TypeBuilder CreateAssemblyType(Type targetClassType)
		{
			if (_ModuleBuilder == null)
			{
				AssemblyBuilder asmBuilder = Thread.GetDomain().DefineDynamicAssembly(
					new AssemblyName("DataPropertySetterAsm"), AssemblyBuilderAccess.Run);
				_ModuleBuilder = asmBuilder.DefineDynamicModule("DataPropertySetterMod");
			}

			string className = "DataPropertySetter_" + targetClassType.FullName;
			string newClassName = className;
			int i = 0;
			while (_ModuleBuilder.GetType(newClassName) != null)
				newClassName = className + i;

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
				CallingConventions.Standard, typeof(void), new Type[] { targetClassType, typeof(object), GetType(), typeof(IList<int>) });

			methodBuilder.DefineParameter(1, ParameterAttributes.In, "target");
			methodBuilder.DefineParameter(2, ParameterAttributes.In, "row");
			methodBuilder.DefineParameter(3, ParameterAttributes.In, "mapper");
			methodBuilder.DefineParameter(4, ParameterAttributes.In, "columns");

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
			//Generate XPath Query
			string qry = "/MappingDefinition/TypeMapping{0}/PropetyMapping{1}";
			string type = prop.DeclaringType.Assembly.GetName().Name;
			type = type + ", " + prop.DeclaringType.FullName;
			string typeClause = "[@typeName = \"" + type + "\" and @schemeId = \"" + schemeId + "\"]";
			string propClause = "[@propertyName = \"" + prop.Name + "\"]";
			qry = String.Format(qry, typeClause, propClause);

			//Looking for node
			XmlNode xmlMapping = _XmlDocument.SelectSingleNode(qry);
			if (xmlMapping == null)
				return null;

			//Create mapping class
			XmlAttribute att = xmlMapping.Attributes["dataColumnName"];
			if (att == null)
			{
				att = xmlMapping.Attributes["dataRelationName"];

				if (att != null)
				{
					Type itemType = null;
					int nestedSchemaId = schemeId;
					
					XmlAttribute attType = xmlMapping.Attributes["nestedItemType"];
					if (attType != null && !String.IsNullOrEmpty(attType.Value))
					{
						string[] typeInfo = attType.Value.Split(',');
						itemType = Assembly.Load(typeInfo[0].Trim()).GetType(typeInfo[1].Trim());
					}

					XmlAttribute attNestedSchemaId = xmlMapping.Attributes["nestedSchemaId"];
					if (attNestedSchemaId != null && !String.IsNullOrEmpty(attNestedSchemaId.Value))
						nestedSchemaId = int.Parse(attNestedSchemaId.Value);

					return new DataRelationMapAttribute(att.Value, schemeId, nestedSchemaId, itemType);
				}
				else
					return new DataColumnMapAttribute(prop.Name, schemeId);
			}
			else
				return new DataColumnMapAttribute(
					String.IsNullOrEmpty(att.Value) ? prop.Name : att.Value, schemeId);
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
			string typeClause = "[@typeName = \"" + type + "\" and @schemeId = \"" + schemeId + "\"]";
			qry = String.Format(qry, typeClause);

			//Looking for node
			return _XmlDocument.SelectSingleNode(qry) != null;
		}

		protected DataTable GetTableFromSchema(DataTable schemeTable)
		{
			DataTable result = new DataTable();

			foreach (DataRow dr in schemeTable.Rows)
			{
				result.Columns.Add(
					dr["ColumnName"].ToString(),
					(Type)dr["DataType"]);
			}

			return result;
		}

		#endregion
	}
}
