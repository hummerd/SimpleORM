using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using SimpleORM.Exception;
using SimpleORM.PropertySetterGenerator;


namespace SimpleORM
{
	public delegate DataRow DataRowExtractor<T>(T obj);


	public class DataMapper
	{
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


		protected Dictionary<DataRow, object> _CreatedObjects;
		protected IObjectBuilder _ObjectBuilder;
		protected MappingGenerator _DMCodeGenerator;


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

		public DataMapper(IEnumerable<string> configFiles)
			: this(new StandartObjectBuilder(), null)
		{
			SetConfigEx(configFiles);
		}

		public DataMapper(IEnumerable<string> configFiles, IObjectBuilder objectBuilder)
			: this(objectBuilder, null)
		{
			SetConfigEx(configFiles);
		}

		public DataMapper(IObjectBuilder objectBuilder)
			: this(objectBuilder, null)
		{ }


		protected DataMapper(IObjectBuilder objectBuilder, Dictionary<Type, IPropertySetterGenerator> setterGenerators)
		{
			_ObjectBuilder = objectBuilder;
			_DMCodeGenerator = new MappingGenerator(setterGenerators);
		}


		public IObjectBuilder ObjectBuilder
		{
			get { return _ObjectBuilder; }
			set { _ObjectBuilder = value; }
		}

		
		public string GeneratedFileName
		{
			get
			{
				return _DMCodeGenerator.GeneratedFileName;
			}
			set
			{
				_DMCodeGenerator.GeneratedFileName = value;
			}
		}


		public void SaveGeneratedAsm()
		{
			_DMCodeGenerator.SaveGeneratedAsm();
		}

		public void SetConfig(string configFile)
		{
			_DMCodeGenerator.SetConfig(new List<string> { configFile });
		}

		public void SetConfigEx(IEnumerable<string> configFiles)
		{
			_DMCodeGenerator.SetConfig(configFiles);
		}

		public void ClearCache()
		{
			_DMCodeGenerator.ClearCache();
		}

		
		public void FillObjectList<TObject>(IList objectList, IDataReader reader)
			where TObject : class
		{
			FillObjectList<TObject>(objectList, reader, null, 0, false);
		}

		public void FillObjectList<TObject>(IList objectList, IDataReader reader, IDbConnection conn, int schemeId, bool close)
			where TObject : class
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (reader == null)
				throw new ArgumentException("Cannot fill objects from null.", "reader");

			var objectType = typeof(TObject);
			List<List<int>> columnIndexes = null;

			//Create new cache only if we manage objects cache
			//if (clearObjectCache)
			//   _CreatedObjects = new Dictionary<DataRow, object>();
			try
			{
				ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(
					objectType,
					schemeId
					);

				if (extractInfo == null)
					throw new InvalidOperationException("Can not fill object without mapping definition.");

				DataTable schemeTable = null;
				MethodInfo method = null;
				extractInfo.FillMethod.TryGetValue(DataReaderPSG.TypeOfDataSource, out method);

				while (reader.Read())
				{
					if (method == null)
					{
						schemeTable = GetTableFromSchema(reader.GetSchemaTable());
						_DMCodeGenerator.GenerateSetterMethod(
							extractInfo,
							schemeTable,
							DataReaderPSG.TypeOfDataSource
							);

						if (!extractInfo.FillMethod.TryGetValue(DataReaderPSG.TypeOfDataSource, out method))
							throw new InvalidOperationException("Failed to create setter method.");
					}

					if (columnIndexes == null)
					{
						if (schemeTable == null)
							schemeTable = GetTableFromSchema(reader.GetSchemaTable());

						columnIndexes = extractInfo.GetSubColumnsIndexes(schemeTable);
					}

					object obj = _ObjectBuilder.CreateObject(objectType);
					//Fill object
					CallExtractorMethod(method, obj, reader, columnIndexes);
					objectList.Add(obj);
				}
			}
			finally
			{
				if (close)
				{
					if (reader != null)
						reader.Close();

					if (conn != null)
						conn.Close();
				}
			}
		}

		public void FillObjectListComplex<TObject>(IList objectList, IDataReader reader, int schemeId)
			where TObject : class
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (reader == null)
				throw new ArgumentException("Cannot fill objects from null.", "reader");

			var objectType = typeof(TObject);
			FillObjectsInternal(reader, objectType, schemeId, objectList);
		}



		public TObject FillObject<TObject>(IDataReader reader, IDbConnection conn, TObject obj)
			where TObject : class
		{
			return FillObject(reader, conn, typeof(TObject), obj, 0, true, true) as TObject;
		}

		public TObject FillObject<TObject>(IDataReader reader, TObject obj)
			where TObject : class
		{
			return FillObject(reader, null, typeof(TObject), obj, 0, false, false) as TObject;
		}

		public TObject FillObject<TObject>(IDataReader reader, TObject obj, int schemeId)
			where TObject : class
		{
			return FillObject(reader, null, typeof(TObject), obj, schemeId, false, false) as TObject;
		}

		public object FillObject(IDataReader reader, IDbConnection conn, Type objectType, object obj, int schemeId, bool read, bool close)
		{
			try
			{
				if (reader == null)
					throw new ArgumentNullException("reader", "Cannot fill object from null.");

				if (objectType == null && obj == null)
					throw new ArgumentNullException("objectType", "Cannot fill object of unknown type null.");

				if (objectType == null)
					objectType = obj.GetType();

				DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
				ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfoWithMethod(
					objectType,
					schemeId,
					schemeTable,
					DataReaderPSG.TypeOfDataSource
					);

				if (extractInfo == null)
					throw new DataMapperException("Can not fill object without mapping definition.");

				//If there is no instance create it
				if (obj == null)
					obj = _ObjectBuilder.CreateObject(objectType);

				List<List<int>> columnIndexes = extractInfo.GetSubColumnsIndexes(schemeTable);

				bool hasData = true;
				if (read)
					hasData = reader.Read();

				//Fill object
				if (hasData)
					CallExtractorMethod(
						extractInfo.FillMethod[DataReaderPSG.TypeOfDataSource], 
						obj, 
						reader, 
						columnIndexes);

				return obj;
			}
			finally
			{
				if (close)
				{
					if (reader != null)
						reader.Close();

					if (conn != null)
						conn.Close();
				}
			}
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

			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfoWithMethod(
				objectType,
				schemeId,
				dataRow.Table,
				DataTablePSG.TypeOfDataSource
				);

			MethodInfo extractMethod;
			if (extractInfo == null || !extractInfo.FillMethod.TryGetValue(DataTablePSG.TypeOfDataSource, out extractMethod))
				throw new DataMapperException("Can not fill object without mapping definition.");

			_CreatedObjects = new Dictionary<DataRow, object>(1);

			List<List<int>> columnIndexes = extractInfo.GetSubColumnsIndexes(dataRow.Table);

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject(objectType);

			//Fill object
			CallExtractorMethod(extractMethod, obj, dataRow, columnIndexes);
			return obj;
		}


		protected void FillObjectsInternal(
			IDataReader reader,
			Type objectType,
			int schemeId,
			IList objectList)
		{
			List<List<int>> columnIndexes = null;
			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(objectType, schemeId);

			Dictionary<ExtractInfo,
				KeyObjectIndex> tempPrimary = new Dictionary<ExtractInfo, KeyObjectIndex>(); //table name //pk //object
			Dictionary<ExtractInfo,
				KeyObjectIndex> tempForeign = new Dictionary<ExtractInfo, KeyObjectIndex>(); //table name //pk //object

			int tableIx = 0;
			do
			{
				if (!reader.Read())
					continue;

				DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
				List<ExtractInfo> assosiatedEI = extractInfo.FindByTable(tableIx, schemeTable.TableName);

				for (int i = 0; i < assosiatedEI.Count; i++)
				{
					ExtractInfo currentEI = assosiatedEI[i];
					MethodInfo extractMethod = null;
					currentEI.FillMethod.TryGetValue(DataReaderPSG.TypeOfDataSource, out extractMethod);

					if (extractMethod == null)
					{
						_DMCodeGenerator.GenerateSetterMethod(
							currentEI,
							schemeTable,
							DataReaderPSG.TypeOfDataSource
						);

						if (!currentEI.FillMethod.TryGetValue(DataReaderPSG.TypeOfDataSource, out extractMethod))
							throw new InvalidOperationException("Failed to create setter method.");
					}

					columnIndexes = currentEI.GetSubColumnsIndexes(schemeTable);

					List<KeyInfo> primaryKeys = currentEI.GetPrimaryKeys();
					primaryKeys.ForEach(pk =>
						pk.InitParentColumnIndexes(schemeTable)
						);

					List<KeyInfo> foreignKeys = currentEI.GetForeignKeys().FindAll(fk =>
						fk.RefTable.EmptyOrRefersTo(tableIx, schemeTable.TableName));

					foreignKeys.ForEach(fk =>
						fk.InitChildColumnIndexes(schemeTable)
						);

					KeyObjectIndex pkObjects;
					KeyObjectIndex fkObjects;

					ExtractObjects(
					   reader,
					   currentEI.TargetType,
					   primaryKeys,
					   foreignKeys,
					   out pkObjects,
					   out fkObjects,
					   columnIndexes,
					   currentEI == extractInfo ? objectList : null,
					   extractMethod);

					KeyObjectIndex koi;
					if (tempPrimary.TryGetValue(currentEI, out koi))
					{
						foreach (var item in pkObjects)
							koi.AddRange(item.Key, item.Value);
					}
					else
						tempPrimary.Add(currentEI, pkObjects);


					if (tempForeign.TryGetValue(currentEI, out koi))
					{
						foreach (var item in fkObjects)
							koi.AddRange(item.Key, item.Value);
					}
					else
						tempForeign.Add(currentEI, fkObjects);
				}

				tableIx++;
			} while (reader.NextResult());

			LinkObjects(extractInfo, tempPrimary, tempForeign);
		}

		protected void ExtractObjects(
			IDataReader reader,
			Type targetType,
			List<KeyInfo> pkInfo,
			List<KeyInfo> fkInfo,
			out KeyObjectIndex pkObjects,
			out KeyObjectIndex fkObjects,
			List<List<int>> columnIndexes,
			IList objectList,
			MethodInfo method
			)
		{
			pkObjects = new KeyObjectIndex();
			fkObjects = new KeyObjectIndex();

			do
			{
				object obj = _ObjectBuilder.CreateObject(targetType);
				//Fill object
				CallExtractorMethod(method, obj, reader, columnIndexes);

				if (pkInfo != null && pkInfo.Count > 0)
				{
					for (int i = 0; i < pkInfo.Count; i++)
					{
						KeyInfo pki = pkInfo[i];
						object pk = _ObjectBuilder.CreateObject(pki.ParentKeyExtractInfo.TargetType);
						bool hasNulls = CallExtractorMethod(pki.GetParentKeyExtractorMethod(), pk, reader, pki.ParentColumnIndexes);
						pkObjects.AddObject(pk, obj);
					}
				}

				if (fkInfo != null && fkInfo.Count > 0)
				{
					for (int i = 0; i < fkInfo.Count; i++)
					{
						KeyInfo fki = fkInfo[i];
						object fk = _ObjectBuilder.CreateObject(fki.ChildKeyExtractInfo.TargetType);
						bool hasNulls = CallExtractorMethod(fki.GetChildKeyExtractorMethod(), fk, reader, fki.ChildColumnIndexes);
						//according to ansi null comparsion, nothing can be equal to this key
						if (!hasNulls)
							fkObjects.AddObject(fk, obj);
					}
				}

				if (objectList != null)
				{
					objectList.Add(obj);
				}
			} while (reader.Read());
		}

		protected void LinkObjects(
			ExtractInfo extractInfo,
			Dictionary<ExtractInfo, KeyObjectIndex> tempPrimary,
			Dictionary<ExtractInfo, KeyObjectIndex> tempForeign)
		{
			LinkObjects(
				extractInfo, 
				tempPrimary,
				tempForeign,
				new Dictionary<ExtractInfo ,object>(20)
				);
		}

		protected void LinkObjects(
			ExtractInfo extractInfo,
            Dictionary<ExtractInfo, KeyObjectIndex> tempPrimary,
            Dictionary<ExtractInfo, KeyObjectIndex> tempForeign,
			Dictionary<ExtractInfo, object> filled)
		{
			if (filled.ContainsKey(extractInfo))
				return;

			filled.Add(extractInfo, null);
			Dictionary<object, List<object>> pkObjects = tempPrimary[extractInfo];

			for (int i = 0; i < extractInfo.ChildTypes.Count; i++)
			{
				RelationExtractInfo childEI = extractInfo.ChildTypes[i];
				LinkObjects(childEI.RelatedExtractInfo, tempPrimary, tempForeign, filled);

				Dictionary<object, List<object>> fkObjects = tempForeign[childEI.RelatedExtractInfo];

				foreach (var item in pkObjects)
				{
                    List<object> parentList = item.Value;

					List<object> children;
					fkObjects.TryGetValue(item.Key, out children);
					
                    foreach (var parent in parentList)
                    {
						PropertyInfo pi = childEI.Member as PropertyInfo;
						IList lst = pi.GetValue(parent, null) as IList;

						if (lst == null)
						{
							lst = (IList)Activator.CreateInstance(pi.PropertyType);
							pi.SetValue(parent, lst, null); 
						}

						if (children != null)
							for (int k = 0; k < children.Count; k++)
								lst.Add(children[k]);
                    }
				}
			}
		}

		protected void FillObjectListInternal<TRowItem>(IList objectList, Type objectType, ICollection dataCollection, int schemeId, DataRowExtractor<TRowItem> rowExtractor, bool clearObjectCache)
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (dataCollection == null)
				throw new ArgumentException("Cannot fill objects from null.", "dataCollection");

			Type listType = objectList.GetType();
			if (objectType == null && listType.IsGenericType)
				objectType = listType.GetGenericArguments()[0];

			if (objectType == null)
				throw new ArgumentException("Cannot fill object of unknown type null.", "objectType");

			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(objectType, schemeId);
			MethodInfo extractMethod = null;
			extractInfo.FillMethod.TryGetValue(DataTablePSG.TypeOfDataSource, out extractMethod);

			List<List<int>> columnIndexes = null; 

			//Trying to increase internal capacity of object container
			MethodInfo setCapacity = listType.GetMethod("set_Capacity", new Type[] { typeof(int) });
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

				if (extractMethod == null)
				{
					_DMCodeGenerator.GenerateSetterMethod(
						extractInfo,
						dataRow.Table,
						DataTablePSG.TypeOfDataSource
					);

					if (!extractInfo.FillMethod.TryGetValue(DataTablePSG.TypeOfDataSource, out extractMethod))
						throw new InvalidOperationException("Failed to create setter method.");
				}

				if (columnIndexes == null)
					columnIndexes = extractInfo.GetSubColumnsIndexes(dataRow.Table);

				object obj;
				if (!_CreatedObjects.TryGetValue(dataRow, out obj))
				{
					obj = _ObjectBuilder.CreateObject(objectType);
					//Fill object
					CallExtractorMethod(extractMethod, obj, dataRow, columnIndexes);
					_CreatedObjects.Add(dataRow, obj);
				}

				objectList.Add(obj);
			}

			if (clearObjectCache)
				_CreatedObjects.Clear();
		}

		protected bool CallExtractorMethod(MethodInfo extractorMethod, object obj, object data, List<List<int>> subColumns)
		{
			int clmn = 0;
			object[] created = new object[_DMCodeGenerator.GeneratedMethodCount];
			return (bool)extractorMethod.Invoke(null, new object[] { obj, data, this, subColumns, clmn, created });
		}

		/// <summary>
		/// Returns empty table with schema specified by schemeTable
		/// </summary>
		/// <param name="schemeTable"></param>
		/// <returns></returns>
		protected DataTable GetTableFromSchema(DataTable schemeTable)
		{
			DataTable result = new DataTable();

			foreach (DataRow dr in schemeTable.Rows)
			{
				result.Columns.Add(
					dr["ColumnName"].ToString(),
					(Type)dr["DataType"]);
			}

			result.TableName = result.Columns[0].ColumnName;
			return result;
		}


		public class KeyObjectIndex	: Dictionary<object, List<object>>
		{
			public void AddObject(object key, object obj)
			{
				List<object> list;
				if (!TryGetValue(key, out list))
				{
					list = new List<object>();
					Add(key, list);
				}

				list.Add(obj);
			}

			public void AddRange(object key, IEnumerable<object> obj)
			{
				List<object> list;
				if (!TryGetValue(key, out list))
				{
					list = new List<object>();
					Add(key, list);
				}

				list.AddRange(obj);
			}
		}

		public class StandartObjectBuilder : IObjectBuilder
		{
			#region IObjectBuilder Members

			public virtual object CreateObject(Type objectType)
			{
				return Activator.CreateInstance(objectType);
			}

			public virtual T CreateObject<T>()
				where T : new()
			{
				return new T();
			}

			#endregion
		}
	}
}
