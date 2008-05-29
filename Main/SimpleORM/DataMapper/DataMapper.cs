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


namespace SimpleORM
{
	public class DataMapper
	{
		protected ModuleBuilder _ModuleBuilder;

		protected readonly Dictionary<Type, Dictionary<int, MethodInfo>> _ExtractorCache = new Dictionary<Type, Dictionary<int, MethodInfo>>();
		protected Dictionary<DataRow, object> _CreatedObjects;

		protected XmlDocument	_XmlDocument;
		protected string			_ConfigFile;

		protected IObjectBuilder _ObjectBuilder;
		protected IPropertySetterGenerator _SetterGenerator;


		public DataMapper(string configFile)
			: this(new StandartObjectBuilder(), new PropertySetterGenerator())
		{
			SetConfig(configFile);
		}

		public DataMapper(string configFile, IObjectBuilder objectBuilder)
			: this(objectBuilder, new PropertySetterGenerator())
		{
			SetConfig(configFile);
		}

		public DataMapper(IObjectBuilder objectBuilder)
			: this(objectBuilder, new PropertySetterGenerator())
		{ }

		protected DataMapper(IObjectBuilder objectBuilder, IPropertySetterGenerator setterGenerator)
		{
			_ObjectBuilder = objectBuilder;
			_SetterGenerator = setterGenerator;
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
					_Instance = new DataMapper(new StandartObjectBuilder(), /*new StandartLogger(), logLevel,*/ new PropertySetterGenerator());
				}

				return _Instance;
			}
		}

		#endregion

		#region Fill methods

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

			MethodInfo extractorMethod = null;

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

				if (extractorMethod == null)
				{
					extractorMethod = GetSetterMethod(objectType, dataRow.Table, schemeId);

					if (extractorMethod == null)
						throw new InvalidOperationException("Can not fill object without mapping definition.");
				}

				object obj;
				if (!_CreatedObjects.TryGetValue(dataRow, out obj))
				{
					obj = _ObjectBuilder.CreateObject(objectType);
					//Fill object
					CallExtractorMethod(extractorMethod, obj, dataRow);
					_CreatedObjects.Add(dataRow, obj);
				}

				objectList.Add(obj);
			}

			if (clearObjectCache)
				_CreatedObjects.Clear();
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
			//TO DO: What about schema validation? 
			//if data table schema changed but some one calls Fill method
			if (dataRow == null)
				throw new ArgumentNullException("dataRow", "Cannot fill object from null.");

			if (objectType == null && obj == null)
				throw new ArgumentNullException("objectType", "Cannot fill object of unknown type null.");

			if (objectType == null)
				objectType = obj.GetType();

			MethodInfo extractorMethod = GetSetterMethod(objectType, dataRow.Table, schemeId);

			if (extractorMethod == null)
				throw new DataMapperException("Can not fill object without mapping definition.");

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject(objectType);

			//Fill object
			CallExtractorMethod(extractorMethod, obj, dataRow);
			return obj;
		}


		protected void CallExtractorMethod(MethodInfo extractorMethod, object obj, DataRow row)
		{
			extractorMethod.Invoke(null, new object[] { obj, row, this });
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
		protected MethodInfo GetSetterMethod(Type objectType, DataTable dtSource, int schemeId)
		{
			MethodInfo extractorMethod;
			Dictionary<int, MethodInfo> schemeMethods;

			bool exists = _ExtractorCache.TryGetValue(objectType, out schemeMethods);

			if (!exists) // If extractor method does not exist for this type
			{
				extractorMethod = GenerateSetterMethod(objectType, schemeId, dtSource);
				//Add method to cache
				schemeMethods = new Dictionary<int, MethodInfo>(2);
				schemeMethods.Add(schemeId, extractorMethod);
				_ExtractorCache.Add(objectType, schemeMethods);
			}
			// If extractor method does not exist for this scheme
			else if (!schemeMethods.TryGetValue(schemeId, out extractorMethod))
			{
				extractorMethod = GenerateSetterMethod(objectType, schemeId, dtSource);
				//Add method to cache
				schemeMethods.Add(schemeId, extractorMethod);
				_ExtractorCache.Add(objectType, schemeMethods);
			}

			return extractorMethod;
		}

		/// <summary>
		/// Generates setter method using xml config or type meta info.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <returns></returns>
		protected MethodInfo GenerateSetterMethod(Type targetClassType, int schemeId, DataTable dtSource)
		{
			MethodInfo extractorMethod = null;

			//Generating Type and method declaration
			TypeBuilder typeBuilder = CreateAssemblyType(targetClassType);
			MethodBuilder methodBuilder = GenerateSetterMethodDefinition(targetClassType, typeBuilder);
			ILGenerator ilGen = methodBuilder.GetILGenerator();

			if (_XmlDocument != null)
				_SetterGenerator.GenerateSetterMethod(ilGen, targetClassType, schemeId, dtSource, GetMappingFromXml);
				//GenerateSetterMethod(ilGen, targetClassType, schemeId, dtSource, GetMappingFromXml);

			//If there is no xml config or type mapping not defined in xml
			if (extractorMethod == null)
				_SetterGenerator.GenerateSetterMethod(ilGen, targetClassType, schemeId, dtSource, GetMappingFromAtt);
				//GenerateSetterMethod(ilGen, targetClassType, schemeId, dtSource, GetMappingFromAtt);

			typeBuilder.CreateType();
			return typeBuilder.Assembly.GetType(typeBuilder.FullName).GetMethod("SetProps_" + targetClassType);
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
				CallingConventions.Standard, typeof(void), new Type[] { targetClassType, typeof(object), GetType() });

			methodBuilder.DefineParameter(1, ParameterAttributes.In, "target");
			methodBuilder.DefineParameter(2, ParameterAttributes.In, "row");
			methodBuilder.DefineParameter(3, ParameterAttributes.In, "mapper");

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

		#endregion
	}
}
