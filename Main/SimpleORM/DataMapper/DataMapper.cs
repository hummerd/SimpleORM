using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using SimpleORM.Exception;
using SimpleORM.PropertySetterGenerator;


namespace SimpleORM
{
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
		protected DataMapperCodeGenerator _DMCodeGenerator;

		
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
			_DMCodeGenerator = new DataMapperCodeGenerator(setterGenerators);
		}


		public IObjectBuilder ObjectBuilder
		{
			get { return _ObjectBuilder; }
			set { _ObjectBuilder = value; }
		}


		public void SetConfig(string configFile)
		{
			_DMCodeGenerator.SetConfig(configFile);
		}

		public void ClearCache()
		{
			_DMCodeGenerator.ClearCache();
		}

		//public void SaveGeneratedAsm(string path)
		//{
		//   _AsmBuilder.Save(path);
		//}
		
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
			List<List<int>> columnIndexes = null;

			//Create new cache only if we manage objects cache
			//if (clearObjectCache)
			//   _CreatedObjects = new Dictionary<DataRow, object>();
			ExtractInfo extractInfo = null;

			while (reader.Read())
			{
				if (extractInfo == null)
				{
					DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
					extractInfo = _DMCodeGenerator.GetSetterMethod(
						objectType,
						typeof(IDataReader),
						schemeTable,
						schemeId);

					if (extractInfo == null || extractInfo.FillMethod == null)
						throw new InvalidOperationException("Can not fill object without mapping definition.");

					columnIndexes = GetSubColumnsIndexes(schemeTable, extractInfo);
				}

				object obj = _ObjectBuilder.CreateObject(objectType);
				//Fill object
				CallExtractorMethod(extractInfo.FillMethod, obj, reader, columnIndexes);
				objectList.Add(obj);
			}
		}

		public void FillObjectListComplex<TObject>(IList objectList, IDataReader reader, int schemeId, bool clearObjectCache)
			where TObject : class
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (reader == null)
				throw new ArgumentException("Cannot fill objects from null.", "reader");

			var objectType = typeof(TObject);
			FillObjectsInternal(reader, objectType, schemeId, objectList);
		}

		protected void FillObjectsInternal(
			IDataReader reader,
			Type objectType,
			int schemeId,
			IList objectList)
		{
			List<List<int>> columnIndexes = null;
			ExtractInfo extractInfo = null;
			bool topLevel = false;
			Dictionary<string, Dictionary<object, object>> tempResult = new Dictionary<string, Dictionary<object, object>>(); //table name //pk //object
			Dictionary<string, Dictionary<object, List<object>>> fkIndex = new Dictionary<string, Dictionary<object, List<object>>>(); //table name //pk //object

			Dictionary<object, object> pkObjects = new Dictionary<object, object>();
			Dictionary<object, List<object>> fkObjects = new Dictionary<object, List<object>>();

			do
			{
				bool hasData = reader.Read();
				if (!hasData)
					continue;

				ExtractFillInfo(reader, objectType, schemeId, out extractInfo, out columnIndexes, out topLevel);


			} while (reader.NextResult());

			LinkObjects(tempResult, fkIndex);
		}

		protected void ExtractObjects(
			IDataReader reader,
			Type objectType,
			int schemeId,
			ExtractInfo extractInfo,
			out IDictionary tempResult,
			List<IDictionary> fkIndex,
			List<List<int>> columnIndexes,
			IList objectList,
			bool topLevel)
		{
			Dictionary<object, object> pkObjects = new Dictionary<object, object>();
			Dictionary<object, List<object>> fkObjects = new Dictionary<object, List<object>>();

			do
			{
				object obj = _ObjectBuilder.CreateObject(objectType);
				//Fill object
				CallExtractorMethod(extractInfo.FillMethod, obj, reader, columnIndexes);

				KeyInfo pkInfo = extractInfo.PrimaryKeyInfo;
				if (pkInfo != null)
				{
					object pk = _ObjectBuilder.CreateObject<DataTable>();
					CallExtractorMethod(pkInfo.FillMethod, pk, reader, columnIndexes);
					pkObjects.Add(pk, obj);
				}

				List<KeyInfo> fkInfo = extractInfo.ForeignKeysInfo;
				if (fkInfo.Count > 0)
				{
					foreach (var item in fkInfo)
					{
						object fk = _ObjectBuilder.CreateObject(item.KeyType);
						CallExtractorMethod(item.FillMethod, fk, reader, columnIndexes);

						List<object> fko;
						if (!fkObjects.TryGetValue(fk, out fko))
						{
							fko = new List<object>();
							fkObjects.Add(fk, fko);
						}

						fko.Add(obj);
					}
				}

				if (topLevel)
				{
					objectList.Add(obj);
				}
			} while (reader.Read());

			tempResult = pkObjects;
			fkIndex.Add(fkObjects);
		}

		protected bool ExtractFillInfo(
			IDataReader reader,
			Type objectType,
			int schemeId,
			out ExtractInfo extractInfo,
			out List<List<int>> columnIndexes,
			out bool topLevel)
		{
			topLevel = false;

			DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
			string tableName = String.IsNullOrEmpty(schemeTable.TableName) ? schemeTable.Columns[0].ColumnName : schemeTable.TableName;

			extractInfo = _DMCodeGenerator.GetSetterMethod(
				objectType,
				typeof(IDataReader),
				schemeTable,
				schemeId);

			if (extractInfo == null || extractInfo.FillMethod == null)
				throw new InvalidOperationException("Can not fill object without mapping definition.");

			columnIndexes = GetSubColumnsIndexes(schemeTable, extractInfo);

			//if no pk and no fk and not top level object
			if (extractInfo.PrimaryKeyInfo == null &&
				 extractInfo.ForeignKeysInfo.Count <= 0 &&
				!topLevel)
				return false;

			return true;
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

			DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
			ExtractInfo extractInfo = _DMCodeGenerator.GetSetterMethod(
				objectType,
				typeof(IDataReader),
				schemeTable,
				schemeId);

			if (extractInfo == null || extractInfo.FillMethod == null)
				throw new DataMapperException("Can not fill object without mapping definition.");

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject(objectType);

			List<List<int>> columnIndexes = GetSubColumnsIndexes(schemeTable, extractInfo);

			//Fill object
			CallExtractorMethod(extractInfo.FillMethod, obj, reader, columnIndexes);
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

			ExtractInfo extractInfo = _DMCodeGenerator.GetSetterMethod(
				objectType,
				typeof(DataTable),
				dataRow.Table,
				schemeId);

			if (extractInfo == null || extractInfo.FillMethod == null)
				throw new DataMapperException("Can not fill object without mapping definition.");

			_CreatedObjects = new Dictionary<DataRow, object>(1);

			List<List<int>> columnIndexes = GetSubColumnsIndexes(dataRow.Table, extractInfo);

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject(objectType);

			//Fill object
			CallExtractorMethod(extractInfo.FillMethod, obj, dataRow, columnIndexes);
			return obj;
		}


		protected void LinkObjects(Dictionary<string, Dictionary<object, object>> tempResult, Dictionary<string, Dictionary<object, List<object>>> fkIndex)
		{
			throw new NotImplementedException();
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

			ExtractInfo extractInfo = null;
			//List<int> columnIndexes = null;
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

				if (extractInfo == null)
				{
					extractInfo = _DMCodeGenerator.GetSetterMethod(
						objectType,
						typeof(DataTable),
						dataRow.Table,
						schemeId);

					if (extractInfo == null || extractInfo.FillMethod == null)
						throw new InvalidOperationException("Can not fill object without mapping definition.");

					columnIndexes = GetSubColumnsIndexes(dataRow.Table, extractInfo);
					//columnIndexes = ColumnsIndexes(dataRow.Table, extractInfo.PropColumns);
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

		protected List<List<int>> GetSubColumnsIndexes(DataTable table, ExtractInfo extractInfo)
		{
			return GetSubColumnsIndexes(table, extractInfo, null);
		}

		protected List<List<int>> GetSubColumnsIndexes(DataTable table, ExtractInfo extractInfo, List<List<int>> result)
		{
			if (result == null)
				result = new List<List<int>>();

			result.Add(GetColumnsIndexes(table, extractInfo.PropColumns));

			foreach (var item in extractInfo.SubTypes)
				GetSubColumnsIndexes(table, item, result);

			return result;
		}

		protected List<int> GetColumnsIndexes(DataTable table, List<string> columns)
		{
			List<int> result = new List<int>(columns.Count);
			for (int i = 0; i < columns.Count; i++)
				result.Add(table.Columns.IndexOf(columns[i]));

			return result;
		}

		protected void CallExtractorMethod(MethodInfo extractorMethod, object obj, object data, List<List<int>> subColumns)
		{
			int clmn = 0;
			extractorMethod.Invoke(null, new object[] { obj, data, this, subColumns, clmn });
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

			return result;
		}
	}
}
