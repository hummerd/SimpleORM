using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml;
using SimpleORM.Attributes;


namespace SimpleORM
{
	public class DataMapper
	{
		protected ModuleBuilder _ModuleBuilder;

		protected readonly Dictionary<Type, Dictionary<int, MethodInfo>> _ExtractorCache = new Dictionary<Type, Dictionary<int, MethodInfo>>();
		protected Dictionary<DataRow, object> _CreatedObjects;

		protected XmlDocument _XmlDocument;


		protected IObjectBuilder _ObjectBuilder;
		protected ILogger _Logger;
		protected LogLevel _LogLevel;


		public DataMapper(IObjectBuilder objectBuilder, ILogger logger, LogLevel logLevel)
		{
			_ObjectBuilder = objectBuilder;
			_Logger = logger;
			_LogLevel = logLevel;
		}


		public IObjectBuilder ObjectBuilder
		{
			get { return _ObjectBuilder; }
			set { _ObjectBuilder = value; }
		}

		public ILogger Logger
		{
			get { return _Logger; }
			set { _Logger = value; }
		}


		public void SetConfig(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				_XmlDocument = null;
				WriteLogInfo("Config file reseted.");
			}
			else
			{
				_XmlDocument = new XmlDocument();
				_XmlDocument.Load(path);
				WriteLogInfo("Config file for data mapper set to: " + path);
			}
		}

		public void ClearCache()
		{
			_ExtractorCache.Clear();
			_ModuleBuilder = null;
			WriteLogInfo("Cache cleared.");
		}


		protected void WriteLogInfo(string message)
		{
			if (_LogLevel == LogLevel.ErrorsAndInfo)
				_Logger.WriteEntry(message, LogSeverity.Information);
		}


		#region Static facade

		protected static DataMapper _Instance;

		public static DataMapper Default
		{
			get
			{
				if (_Instance == null)
				{
					LogLevel logLevel = LogLevel.None;
#if DEBUG
					logLevel = LogLevel.ErrorsAndInfo;
#endif
					_Instance = new DataMapper(new StandartObjectBuilder(), new StandartLogger(), logLevel);
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
				throw new ArgumentException("Cannot fill object from null.", "dataRow");

			if (objectType == null && obj == null)
				throw new ArgumentException("Cannot fill object of unknown type null.", "objectType");

			if (objectType == null)
				objectType = obj.GetType();

			MethodInfo extractorMethod = GetSetterMethod(objectType, dataRow.Table, schemeId);

			if (extractorMethod == null)
				throw new InvalidOperationException("Can not fill object without mapping definition.");

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
				GenerateSetterMethod(ilGen, targetClassType, schemeId, dtSource, GetMappingFromXml);

			//If there is no xml config or type mapping not defined in xml
			if (extractorMethod == null)
				GenerateSetterMethod(ilGen, targetClassType, schemeId, dtSource, GetMappingFromAtt);

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
				CallingConventions.Standard, typeof(void), new Type[] { targetClassType, typeof(DataRow), GetType() });

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
				return (DataMapAttribute)Array.Find(attrs, delegate(object att)
				{
					DataMapAttribute propAtt = att as DataMapAttribute;
					return propAtt != null && propAtt.SchemeId == schemeId;
				});
			else
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
				if (att == null)
					throw new InvalidOperationException("Invalid xml config. Each PropetyMapping node must have either dataColumnName attribute or dataRelationName attribute");

				return new DataRelationMapAttribute(att.Value, schemeId);
			}
			else
				return new DataColumnMapAttribute(att.Value, schemeId);
		}

		/// <summary>
		/// Generates setter method for specofoed type.
		/// </summary>
		/// <param name="ilGen"></param>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <param name="getPropertyMapping"></param>
		/// <returns></returns>
		protected void GenerateSetterMethod(ILGenerator ilGen, Type targetClassType, int schemeId, DataTable dtSource, GetPropertyMapping getPropertyMapping)
		{
			ilGen.DeclareLocal(typeof(object));

			//Common methods and fields used for generating code
			MethodInfo getRowItem = typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(int) });
			MethodInfo getChildRows = typeof(DataRow).GetMethod("GetChildRows", new Type[] { typeof(string) });
			MethodInfo getType = typeof(Type).GetMethod("GetTypeFromHandle");
			MethodInfo changeType = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
			FieldInfo dbNullValue = typeof(DBNull).GetField("Value");
			MethodInfo setNested = typeof(DataMapper).GetMethod("FillObjectListNested");

			PropertyInfo[] props = targetClassType.GetProperties();

			foreach (PropertyInfo prop in props)
			{
				DataMapAttribute mapping = getPropertyMapping(prop, schemeId);
				if (mapping == null)
					continue;

				if (mapping is DataColumnMapAttribute)
				{
					DataColumnMapAttribute mapAtt = mapping as DataColumnMapAttribute;
					int column = dtSource.Columns.IndexOf(mapAtt.MappingName);
					if (column < 0)
						continue;

					Type propType = prop.PropertyType;
					bool isNullable = propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>);

					if (isNullable && dtSource.Columns[column].DataType == propType.GetGenericArguments()[0])
						GenerateSetNullableProperty(ilGen, column, dbNullValue, getRowItem,
							targetClassType.GetMethod("set_" + prop.Name), propType, propType.GetGenericArguments()[0]);
					else if (isNullable && dtSource.Columns[column].DataType != propType.GetGenericArguments()[0])
						GenerateSetNullablePropertyNI(ilGen, column, dbNullValue, getType, changeType, getRowItem,
							targetClassType.GetMethod("set_" + prop.Name), propType, propType.GetGenericArguments()[0]);
					else if (propType.IsValueType && dtSource.Columns[column].DataType == propType)
						GenerateSetValueProperty(ilGen, column, dbNullValue, getRowItem,
							targetClassType.GetMethod("set_" + prop.Name), propType);
					else if (propType.IsValueType && dtSource.Columns[column].DataType != propType)
						GenerateSetValuePropertyNI(ilGen, column, dbNullValue, getType, changeType, getRowItem,
							targetClassType.GetMethod("set_" + prop.Name), propType);
					else
						GenerateSetRefProperty(ilGen, column, dbNullValue, getRowItem,
							targetClassType.GetMethod("set_" + prop.Name), propType);
				}
				else
				{
					Type propType = prop.PropertyType;

					if (!typeof(IList).IsAssignableFrom(propType))
						throw new InvalidOperationException("Cannot set nested objects for collection that does not implement IList (" + prop.Name + ").");

					DataRelationMapAttribute mapAtt = mapping as DataRelationMapAttribute;
					Type itemType = mapAtt.ItemType;
					if (itemType == null)
						itemType = GetItemType(propType);

					if (itemType == null)
						throw new InvalidOperationException("Cannot resolve type of items in collection(" + prop.Name + "). " +
							"Try to set it via ItemType property of DataRelationMapAttribute.");

					GenerateSetNestedProperty(ilGen, getChildRows, mapAtt.MappingName, getType,
						targetClassType.GetMethod("get_" + prop.Name), targetClassType.GetMethod("set_" + prop.Name),
						propType, itemType, mapAtt.NestedSchemeId, setNested);
				}
			}

			ilGen.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Helper method. Returns first generic argument type for first generic subtype.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		protected Type GetItemType(Type type)
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

		#endregion

		#region IL generators

		/// <summary>
		/// Generates part of a setter method to set property of type Nullable.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="column"></param>
		/// <param name="dbNullValue"></param>
		/// <param name="getItem"></param>
		/// <param name="setProp"></param>
		/// <param name="propType"></param>
		/// <param name="subType"></param>
		protected void GenerateSetNullableProperty(
			ILGenerator ILout,
			int column,
			FieldInfo dbNullValue,
			MethodInfo getItem,
			MethodInfo setProp,
			Type propType,
			Type subType)
		{
			#region Algorithm
			/*	Pseudo algorithm
		 * if (val == DBNull.Value)
		 *		set = null;
		 * else
		 *		set = (PropType)val
		 */
			#endregion

			#region IL ildisasm
			/*
			 .maxstack 3
			 .locals init (
				  [0] object val,
				  [1] valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime> CS$0$0000)
			 L_0000: ldarg.1 
			 L_0001: ldc.i4.2 
			 L_0002: callvirt instance object [System.Data]System.Data.DataRow::get_Item(int32)
			 L_0007: stloc.0 
			 L_0008: ldloc.0 
			 L_0009: ldsfld class [mscorlib]System.DBNull [mscorlib]System.DBNull::Value
			 L_000e: bne.un.s L_0020
			 L_0010: ldarg.0 
			 L_0011: ldloca.s CS$0$0000
			 L_0013: initobj [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime>
			 L_0019: ldloc.1 
			 L_001a: callvirt instance void CodeGenerator.MyTest::set_Date(valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime>)
			 L_001f: ret 
			 L_0020: ldarg.0 
			 L_0021: ldloc.0 
			 L_0022: unbox.any [mscorlib]System.DateTime
			 L_0027: newobj instance void [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime>::.ctor(!0)
			 L_002c: callvirt instance void CodeGenerator.MyTest::set_Date(valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime>)
			 L_0031: ret 
		*/
			#endregion

			LocalBuilder loc = ILout.DeclareLocal(propType);

			Label lblElse = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldc_I4, column);
			ILout.EmitCall(OpCodes.Call, getItem, null);
			ILout.Emit(OpCodes.Stloc_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldsfld, dbNullValue);

			ILout.Emit(OpCodes.Bne_Un, lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloca, loc.LocalIndex);
			ILout.Emit(OpCodes.Initobj, propType);
			ILout.Emit(OpCodes.Ldloc, loc.LocalIndex);
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);

			ILout.Emit(OpCodes.Br, lblEnd);
			
			ILout.MarkLabel(lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Unbox_Any, subType);
			ILout.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.MarkLabel(lblEnd);
		}

		/// <summary>
		/// Generates part of a setter method to set property of type Nullable if 
		/// property sub type is not same like column data type.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="column"></param>
		/// <param name="dbNullValue"></param>
		/// <param name="getType"></param>
		/// <param name="changeType"></param>
		/// <param name="getItem"></param>
		/// <param name="setProp"></param>
		/// <param name="propType"></param>
		/// <param name="subType"></param>
		protected void GenerateSetNullablePropertyNI(
			ILGenerator ILout,
			int column,
			FieldInfo dbNullValue,
			MethodInfo getType,
			MethodInfo changeType,
			MethodInfo getItem,
			MethodInfo setProp,
			Type propType,
			Type subType)
		{
			#region Algorithm
			/*	Pseudo algorithm
		 * if (val == DBNull.Value)
		 *		set = null;
		 * else
		 *		set = Convert.ChangeType(val, prop.Type);
		 */
			#endregion

			#region IL ildisasm
			/*
			.maxstack 3
			.locals init (
			  [0] object val,
			  [1] valuetype [mscorlib]System.Nullable`1<bool> CS$0$0000)
			L_0000: ldarg.1 
			L_0001: ldc.i4.0 
			L_0002: callvirt instance object [System.Data]System.Data.DataRow::get_Item(int32)
			L_0007: stloc.0 
			L_0008: ldloc.0 
			L_0009: ldsfld class [mscorlib]System.DBNull [mscorlib]System.DBNull::Value
			L_000e: bne.un.s L_0020
			L_0010: ldarg.0 
			L_0011: ldloca.s CS$0$0000
			L_0013: initobj [mscorlib]System.Nullable`1<bool>
			L_0019: ldloc.1 
			L_001a: callvirt instance void CodeGenerator.MyTest::set_BoolProp(valuetype [mscorlib]System.Nullable`1<bool>)
			L_001f: ret 
			L_0020: ldarg.0 
			L_0021: ldloc.0 
			L_0022: ldtoken bool
			L_0027: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			L_002c: call object [mscorlib]System.Convert::ChangeType(object, class [mscorlib]System.Type)
			L_0031: unbox.any bool
			L_0036: newobj instance void [mscorlib]System.Nullable`1<bool>::.ctor(!0)
			L_003b: callvirt instance void CodeGenerator.MyTest::set_BoolProp(valuetype [mscorlib]System.Nullable`1<bool>)
			L_0040: ret 
		*/
			#endregion

			LocalBuilder loc = ILout.DeclareLocal(propType);

			Label lblElse = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldc_I4, column);
			ILout.EmitCall(OpCodes.Call, getItem, null);
			ILout.Emit(OpCodes.Stloc_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldsfld, dbNullValue);
			ILout.Emit(OpCodes.Bne_Un, lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloca, loc.LocalIndex);
			ILout.Emit(OpCodes.Initobj, propType);
			ILout.Emit(OpCodes.Ldloc, loc.LocalIndex);
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.Emit(OpCodes.Br, lblEnd);
			ILout.MarkLabel(lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldtoken, subType);
			ILout.EmitCall(OpCodes.Call, getType, null);
			ILout.EmitCall(OpCodes.Call, changeType, null);
			ILout.Emit(OpCodes.Unbox_Any, subType);
			ILout.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.MarkLabel(lblEnd);
		}

		/// <summary>
		/// Generates part of a setter method to set property of value type.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="column"></param>
		/// <param name="dbNullValue"></param>
		/// <param name="getItem"></param>
		/// <param name="setProp"></param>
		/// <param name="propType"></param>
		protected void GenerateSetValueProperty(
			ILGenerator ILout,
			int column,
			FieldInfo dbNullValue,
			MethodInfo getItem,
			MethodInfo setProp,
			Type propType)
		{
			#region Algorithm
			/*	Pseudo algorithm
		 * if (val == DBNull.Value)
		 *		set = default(prop.Type);
		 * else
		 *		set = val;
		 */
			#endregion

			#region IL ildisasm
			/*
			.maxstack 2
			.locals init (
			  [0] object val)
			L_0000: ldarg.1 
			L_0001: ldc.i4.0 
			L_0002: callvirt instance object [System.Data]System.Data.DataRow::get_Item(int32)
			L_0007: stloc.0 
			L_0008: ldloc.0 
			L_0009: ldsfld class [mscorlib]System.DBNull [mscorlib]System.DBNull::Value
			L_000e: bne.un.s L_0018
			L_0010: ldarg.0 
			L_0011: ldc.i4.0 
			L_0012: callvirt instance void CodeGenerator.MyTest::set_Prop1(int32)
			L_0017: ret 
			L_0018: ldarg.0 
			L_0019: ldloc.0 
			L_001a: unbox.any int32
			L_001f: callvirt instance void CodeGenerator.MyTest::set_Prop1(int32)
			L_0024: ret 
		*/
			#endregion

			LocalBuilder loc = ILout.DeclareLocal(propType);

			Label lblElse = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldc_I4, column);
			ILout.EmitCall(OpCodes.Call, getItem, null);
			ILout.Emit(OpCodes.Stloc_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldsfld, dbNullValue);
			ILout.Emit(OpCodes.Bne_Un, lblElse);
			ILout.Emit(OpCodes.Ldarg_0);

			//init to default
			if (propType.IsPrimitive) // for primitive types
			{
				ILout.Emit(OpCodes.Ldc_I4, 0);
			}
			else // for structs types
			{
				ILout.Emit(OpCodes.Ldloca, loc.LocalIndex);
				ILout.Emit(OpCodes.Initobj, propType);
				ILout.Emit(OpCodes.Ldloc, loc.LocalIndex);
			}

			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.Emit(OpCodes.Br, lblEnd);
			ILout.MarkLabel(lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Unbox_Any, propType);
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.MarkLabel(lblEnd);
		}

		/// <summary>
		/// Generates part of a setter method to set property of value type if 
		/// property type is not same like column data type.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="column"></param>
		/// <param name="dbNullValue"></param>
		/// <param name="getType"></param>
		/// <param name="changeType"></param>
		/// <param name="getItem"></param>
		/// <param name="setProp"></param>
		/// <param name="propType"></param>
		protected void GenerateSetValuePropertyNI(
			ILGenerator ILout,
			int column,
			FieldInfo dbNullValue,
			MethodInfo getType,
			MethodInfo changeType,
			MethodInfo getItem,
			MethodInfo setProp,
			Type propType)
		{
			#region Algorithm
			/*	Pseudo algorithm
		 * if (val == DBNull.Value)
		 *		set = default(prop.Type);
		 * else
		 *		set = Convert.ChangeType(val, prop.Type);
		 */
			#endregion

			#region IL ildisasm
			/*
			.maxstack 3
			.locals init (
			  [0] object val)
			L_0000: ldarg.1 
			L_0001: ldc.i4.0 
			L_0002: callvirt instance object [System.Data]System.Data.DataRow::get_Item(int32)
			L_0007: stloc.0 
			L_0008: ldloc.0 
			L_0009: ldsfld class [mscorlib]System.DBNull [mscorlib]System.DBNull::Value
			L_000e: bne.un.s L_0018
			L_0010: ldarg.0 
			L_0011: ldc.i4.0 
			L_0012: callvirt instance void CodeGenerator.MyTest::set_BoolProp2(bool)
			L_0017: ret 
			L_0018: ldarg.0 
			L_0019: ldloc.0 
			L_001a: ldtoken bool
			L_001f: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			L_0024: call object [mscorlib]System.Convert::ChangeType(object, class [mscorlib]System.Type)
			L_0029: unbox.any bool
			L_002e: callvirt instance void CodeGenerator.MyTest::set_BoolProp2(bool)
			L_0033: ret 
		*/
			#endregion

			LocalBuilder loc = ILout.DeclareLocal(propType);

			Label lblElse = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldc_I4, column);
			ILout.EmitCall(OpCodes.Call, getItem, null);
			ILout.Emit(OpCodes.Stloc_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldsfld, dbNullValue);
			ILout.Emit(OpCodes.Bne_Un, lblElse);
			ILout.Emit(OpCodes.Ldarg_0);

			//init to default
			if (propType.IsPrimitive) // for primitive types
			{
				ILout.Emit(OpCodes.Ldc_I4, 0);
			}
			else // for structs types
			{
				ILout.Emit(OpCodes.Ldloca, loc.LocalIndex);
				ILout.Emit(OpCodes.Initobj, propType);
				ILout.Emit(OpCodes.Ldloc, loc.LocalIndex);
			}

			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.Emit(OpCodes.Br, lblEnd);
			ILout.MarkLabel(lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldtoken, propType);
			ILout.EmitCall(OpCodes.Call, getType, null);
			ILout.EmitCall(OpCodes.Call, changeType, null);
			ILout.Emit(OpCodes.Unbox_Any, propType);
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.MarkLabel(lblEnd);
		}

		/// <summary>
		/// Generates part of a setter method to set property of reference type.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="column"></param>
		/// <param name="dbNullValue"></param>
		/// <param name="getItem"></param>
		/// <param name="setProp"></param>
		/// <param name="propType"></param>
		protected void GenerateSetRefProperty(
			ILGenerator ILout,
			int column,
			FieldInfo dbNullValue,
			MethodInfo getItem,
			MethodInfo setProp,
			Type propType)
		{
			#region Algorithm
			/*	Pseudo algorithm
		 * if (val == DBNull.Value)
		 *		set = null;
		 * else
		 *		set = val;
		 */
			#endregion

			#region IL ildisasm
			/*
			.maxstack 2
			.locals init (
			  [0] object val)
			L_0000: ldarg.1 
			L_0001: ldc.i4.1 
			L_0002: callvirt instance object [System.Data]System.Data.DataRow::get_Item(int32)
			L_0007: stloc.0 
			L_0008: ldloc.0 
			L_0009: ldsfld class [mscorlib]System.DBNull [mscorlib]System.DBNull::Value
			L_000e: bne.un.s L_0018
			L_0010: ldarg.0 
			L_0011: ldnull 
			L_0012: callvirt instance void CodeGenerator.MyTest::set_Prop2(string)
			L_0017: ret 
			L_0018: ldarg.0 
			L_0019: ldloc.0 
			L_001a: castclass string
			L_001f: callvirt instance void CodeGenerator.MyTest::set_Prop2(string)
			L_0024: ret 
		*/
			#endregion

			Label lblElse = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldc_I4, column);
			ILout.EmitCall(OpCodes.Call, getItem, null);
			ILout.Emit(OpCodes.Stloc_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Ldsfld, dbNullValue);
			ILout.Emit(OpCodes.Bne_Un, lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldnull);
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.Emit(OpCodes.Br, lblEnd);
			ILout.MarkLabel(lblElse);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldloc_0);
			ILout.Emit(OpCodes.Castclass, propType);
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);
			ILout.MarkLabel(lblEnd);
		}

		/// <summary>
		/// Generates part of a setter method to set nested list.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="getChildRows"></param>
		/// <param name="relationName"></param>
		/// <param name="getType"></param>
		/// <param name="getList"></param>
		/// <param name="setList"></param>
		/// <param name="propType"></param>
		/// <param name="itemType"></param>
		/// <param name="nestedSchemaId"></param>
		/// <param name="setNested"></param>
		protected void GenerateSetNestedProperty(
			ILGenerator ILout,
			MethodInfo getChildRows,
			string relationName,
			MethodInfo getType,
			MethodInfo getList,
			MethodInfo setList,
			Type propType,
			Type itemType,
			int nestedSchemaId,
			MethodInfo setNested
			)
		{
			#region Algorithm
			//DataRow[] drChilds = dr.GetChildRows("RelationName");
			//if (drChilds.Length <= 0)
			//   return;
			//
			//List<MyTest> newList;
			//if (mt.NestedList == null)
			//{
			//   newList = (List<MyTest>)Activator.CreateInstance(typeof(List<MyTest>));
			//   mt.NestedList = newList;
			//}
			//else
			//   newList = mt.NestedList;
			//
			//ExtractNested(newList, drChilds, 234);
			#endregion

			#region IL ildisasm
			/*
			.maxstack 3
			.locals init (
			  [0] class [System.Data]System.Data.DataRow[] drChilds,
			  [1] class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> newList)
			L_0000: ldarg.1 
			L_0001: ldstr "RelationName"
			L_0006: callvirt instance class [System.Data]System.Data.DataRow[] [System.Data]System.Data.DataRow::GetChildRows(string)
			L_000b: stloc.0 
			L_000c: ldloc.0 
			L_000d: ldlen 
			L_000e: conv.i4 
			L_000f: ldc.i4.0 
		 * 
			L_0010: bgt.s L_0013
			L_0012: ret
		 * 
			L_0013: ldarg.0 
			L_0014: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
		 * 
			L_0019: brtrue.s L_0039
			L_001b: ldtoken [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			L_0020: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			L_0025: call object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
			L_002a: castclass [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			L_002f: stloc.1 
			L_0030: ldarg.0 
			L_0031: ldloc.1 
			L_0032: callvirt instance void CodeGenerator.MyTest::set_NestedList(class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>)
		   
		   L_0037: br.s L_0040
		 * 
			L_0039: ldarg.0 
			L_003a: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			L_003f: stloc.1 
			
		   L_0040: ldloc.1 
			L_0041: brfalse.s L_0053
			L_0043: ldloc.1 
			L_0044: ldloc.0 
			L_0045: ldlen 
			L_0046: conv.i4 
			L_0047: ldloc.1 
			L_0048: callvirt instance int32 [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>::get_Count()
			L_004d: add 
			L_004e: callvirt instance void [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>::set_Capacity(int32)
		 *	
		   L_0053: ldarg.0 
			L_0054: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			L_0059: ldloc.0 
			L_005a: ldc.i4 0xea
			L_005f: call void CodeGenerator.Program::ExtractNested(class [mscorlib]System.Collections.IList, class [mscorlib]System.Collections.Generic.IEnumerable`1<class [System.Data]System.Data.DataRow>, int32)
			L_0064: ret 
		*/
			#endregion

			MethodInfo createInst = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) });
			//MethodInfo setCapacity = null;
			//MethodInfo getCount = null;

			if (itemType == null)
				itemType = propType.GetGenericArguments()[0];

			//bool isList = propType.IsGenericType && typeof(List<>) == propType.GetGenericTypeDefinition();

			//if (isList)
			//{
			//   setCapacity = itemType.GetMethod("set_Capacity", new Type[] { typeof(int) });
			//   getCount = itemType.GetMethod("get_Count");
			//}

			Label lblElse1 = ILout.DefineLabel();
			Label lblElse2 = ILout.DefineLabel();
			Label lblAfterFirstIf = ILout.DefineLabel();
			//Label lblAfterSecondIf = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			LocalBuilder locRows = ILout.DeclareLocal(typeof(DataRow[]));
			LocalBuilder loc = ILout.DeclareLocal(propType);

			ILout.Emit(OpCodes.Ldarg_1);								//L_0000: ldarg.1 
			ILout.Emit(OpCodes.Ldstr, relationName);				//L_0001: ldstr "RelationName"
			ILout.EmitCall(OpCodes.Call, getChildRows, null);	//L_0006: callvirt instance class [System.Data]System.Data.DataRow[] [System.Data]System.Data.DataRow::GetChildRows(string)
			ILout.Emit(OpCodes.Stloc, locRows);						//L_000b: stloc.0 
			ILout.Emit(OpCodes.Ldloc, locRows);						//L_000c: ldloc.0 
			ILout.Emit(OpCodes.Ldlen);									//L_000d: ldlen 
			ILout.Emit(OpCodes.Conv_I4);								//L_000e: conv.i4		
			ILout.Emit(OpCodes.Ldc_I4_0);								//L_000f: ldc.i4.0 

			ILout.Emit(OpCodes.Bgt, lblElse1);						//L_0010: bgt.s L_0013
			ILout.Emit(OpCodes.Br, lblEnd);							//L_0012: ret

			ILout.MarkLabel(lblElse1);
			ILout.Emit(OpCodes.Ldarg_0);								//L_0013: ldarg.0 
			ILout.EmitCall(OpCodes.Callvirt, getList, null);	//L_0014: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()

			ILout.Emit(OpCodes.Brtrue, lblElse2);					//L_0019: brtrue.s L_0039
			ILout.Emit(OpCodes.Ldtoken, propType);					//L_001b: ldtoken [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			ILout.EmitCall(OpCodes.Call, getType, null);			//L_0020: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			ILout.EmitCall(OpCodes.Call, createInst, null);		//L_0025: call object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
			ILout.Emit(OpCodes.Castclass, propType);				//L_002a: castclass [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			ILout.Emit(OpCodes.Stloc, loc);							//L_002f: stloc.1 
			ILout.Emit(OpCodes.Ldarg_0);								//L_0030: ldarg.0 
			ILout.Emit(OpCodes.Ldloc, loc);							//L_0031: ldloc.1 
			ILout.EmitCall(OpCodes.Callvirt, setList, null);	//L_0032: callvirt instance void CodeGenerator.MyTest::set_NestedList(class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>)

			ILout.Emit(OpCodes.Br, lblAfterFirstIf);				//L_0037: br.s L_0040

			ILout.MarkLabel(lblElse2);
			ILout.Emit(OpCodes.Ldarg_0);								//L_0039: ldarg.0 
			ILout.EmitCall(OpCodes.Callvirt, getList, null);	//L_003a: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			ILout.Emit(OpCodes.Stloc, loc);							//L_003f: stloc.1 

			ILout.MarkLabel(lblAfterFirstIf);

			//if (isList)
			//{
			//   ILout.Emit(OpCodes.Ldloc, loc);							//L_0040: ldloc.1

			//   ILout.Emit(OpCodes.Brfalse, lblAfterSecondIf);		//L_0041: brfalse.s L_0053
			//   ILout.Emit(OpCodes.Ldloc, loc);							//L_0043: ldloc.1 
			//   ILout.Emit(OpCodes.Ldloc, locRows);						//L_0044: ldloc.0 	
			//   ILout.Emit(OpCodes.Ldlen);									//L_0045: ldlen 
			//   ILout.Emit(OpCodes.Conv_I4);								//L_0046: conv.i4
			//   ILout.Emit(OpCodes.Ldloc, loc);							//L_0047: ldloc.1 
			//   ILout.EmitCall(OpCodes.Callvirt, getCount, null);	//L_0048: callvirt instance int32 [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>::get_Count()
			//   ILout.Emit(OpCodes.Add);									//L_004d: add 
			//   ILout.EmitCall(OpCodes.Callvirt, setCapacity, null); //L_004e: callvirt instance void [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>::set_Capacity(int32)

			//   ILout.MarkLabel(lblAfterSecondIf);
			//}

			ILout.Emit(OpCodes.Ldarg_2);
			ILout.Emit(OpCodes.Ldarg_0);								//L_0053: ldarg.0 
			ILout.EmitCall(OpCodes.Callvirt, getList, null);	//L_0054: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			ILout.Emit(OpCodes.Ldtoken, itemType);
			ILout.EmitCall(OpCodes.Call, getType, null);
			ILout.Emit(OpCodes.Ldloc, locRows);						//L_0059: ldloc.0 
			ILout.Emit(OpCodes.Ldc_I4, nestedSchemaId);			//L_005a: ldc.i4 0xea
			ILout.EmitCall(OpCodes.Callvirt, setNested, null);	//L_005f: call void CodeGenerator.Program::ExtractNested(class [mscorlib]System.Collections.IList, class [mscorlib]System.Collections.Generic.IEnumerable`1<class [System.Data]System.Data.DataRow>, int32)
			ILout.MarkLabel(lblEnd);									//L_0064: ret 
		}
		#endregion
	}
}
