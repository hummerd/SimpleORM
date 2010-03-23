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
	public delegate bool ObjectFilter<T>(IDataRecord record, T obj);
	public delegate bool ObjectFilter(IDataRecord record, object obj);

	/// <summary>
	/// Class for transforming flat data into objects.
	/// </summary>
	public class DataMapper
	{
		protected static DataMapper _Instance = new DataMapper(new StandartObjectBuilder(), null);
		
		public static DataMapper Default
		{
			get
			{
				return _Instance;
			}
		}


		protected Dictionary<DataRow, object> _CreatedObjects;
		protected IObjectBuilder _ObjectBuilder;
		protected MappingGenerator _DMCodeGenerator;

		#region ctor
		public DataMapper()
			: this(new StandartObjectBuilder(), null)
		{ ; }

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
		#endregion


		/// <summary>
		/// If you want to manage objects creation set this property to your own ObjectBuilder.
		/// </summary>
		public IObjectBuilder ObjectBuilder
		{
			get { return _ObjectBuilder; }
			set { _ObjectBuilder = value; }
		}

		/// <summary>
		/// Sets file name for generated assembly. You can save assembly on disk later by calling SaveGeneratedAsm method.
		/// You can use this assembly for debug purposes.
		/// </summary>
		/// <remarks>Can be assigned only before any method was generated (i.e. before any call to FillXXXXXX method).</remarks>
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


		/// <summary>
		/// Saves generated assembly on disk to application current directory.
		/// You can use this assembly for debug purposes.
		/// </summary>
		/// <remarks>Can be called only if GeneratedFileName property was assigned before any method was generated (i.e. before any call to FillXXXXXX method).</remarks>
		public void SaveGeneratedAsm()
		{
			_DMCodeGenerator.SaveGeneratedAsm();
		}

		/// <summary>
		/// Set path to xml mapping file. Single file version.
		/// </summary>
		/// <param name="configFile"></param>
		public void SetConfig(string configFile)
		{
			_DMCodeGenerator.SetConfig(new List<string> { configFile });
		}

		/// <summary>
		/// Set path to xml mapping file. Multiple files version.
		/// </summary>
		/// <param name="configFile"></param>
		public void SetConfigEx(IEnumerable<string> configFiles)
		{
			_DMCodeGenerator.SetConfig(configFiles);
		}

		/// <summary>
		/// Reset all generated code. After call to this method all setter methods will be generated once again. 
		/// </summary>
		public void ClearCache()
		{
			_DMCodeGenerator.ClearCache();
		}


		#region Fill object list from reader simple
		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway.</param>
		public void FillObjectList<TObject>(IDataReader reader, IList objectList)
			where TObject : class
		{
			FillObjectList<TObject>(reader, objectList, 0, null, false, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		public void FillObjectList<TObject>(IDataReader reader, IList objectList, int schemeId)
			where TObject : class
		{
			FillObjectList<TObject>(reader, objectList, schemeId, null, false, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="conn">Connection to close after work is done</param>
		public void FillObjectList<TObject>(IDataReader reader, IList objectList, IDbConnection conn)
			where TObject : class
		{
			FillObjectList<TObject>(reader, objectList, 0, conn, true, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway.</param>
		public void FillObjectList<TObject>(IDataReader reader, IList objectList, int schemeId, IDbConnection conn)
			where TObject : class
		{
			FillObjectList<TObject>(reader, objectList, schemeId, conn, true, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway.</param>
		public void FillObjectList<TObject>(IDataReader reader, IList objectList, int schemeId, IDbConnection conn, bool close, ObjectFilter<TObject> filter)
			where TObject : class
		{
			try
			{
				if (objectList == null)
					throw new ArgumentException("Destination list can not be null.", "objectList");

				if (reader == null)
					throw new ArgumentException("Cannot fill objects from null.", "reader");

				if (reader.IsClosed)
					throw new ArgumentException("Cannot fill objects from closed reader.", "reader");

				FillObjectListInternal<TObject>(reader, objectList, schemeId, conn, close, filter);
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

		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway.</param>
		public void FillObjectList(IDataReader reader, IList objectList, Type objectType, int schemeId, IDbConnection conn, bool close, ObjectFilter filter)
		{
			try
			{
				if (objectList == null)
					throw new ArgumentException("Destination list can not be null.", "objectList");

				if (reader == null)
					throw new ArgumentException("Cannot fill objects from null.", "reader");

				if (reader.IsClosed)
					throw new ArgumentException("Cannot fill objects from closed reader.", "reader");

				if (objectList != null && objectType == null)
					objectType = ReflectionHelper.GetListItemType(objectList.GetType());

				if (objectType == null && objectList == null)
					throw new ArgumentNullException("objectType", "Cannot fill object of unknown type.");

				FillObjectListInternal(reader, objectList, objectType, schemeId, conn, close, filter);
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

		#endregion

		#region Fill object list from reader complex
		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// This overload uses default schemeId (0). DO NOT closes reader after work is done.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		public void FillObjectListComplex<TObject>(IDataReader reader, IList objectList)
			where TObject : class
		{
			FillObjectListComplex<TObject>(reader, objectList, 0, null, false, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// DO NOT closes reader after work is done.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		public void FillObjectListComplex<TObject>(IDataReader reader, IList objectList, int schemeId)
			where TObject : class
		{
			FillObjectListComplex<TObject>(reader, objectList, schemeId, null, false, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// This overload uses default schemeId (0). Closes reader and conn after work is done.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="conn">Connection to close after work is done</param>
		public void FillObjectListComplex<TObject>(IDataReader reader, IList objectList, IDbConnection conn)
			where TObject : class
		{
			FillObjectListComplex<TObject>(reader, objectList, 0, conn, true, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// Closes reader and conn after work is done.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		public void FillObjectListComplex<TObject>(IDataReader reader, IList objectList, int schemeId, IDbConnection conn)
			where TObject : class
		{
			FillObjectListComplex<TObject>(reader, objectList, schemeId, conn, true, null);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway and will be present in child collections(if such exists)</param>
		public void FillObjectListComplex<TObject>(IDataReader reader, IList objectList, int schemeId, IDbConnection conn, bool close, ObjectFilter<TObject> filter)
			where TObject : class
		{
			try
			{
				if (objectList == null)
					throw new ArgumentException("Destination list can not be null.", "objectList");

				if (reader == null)
					throw new ArgumentException("Cannot fill objects from null.", "reader");

				if (reader.IsClosed)
					throw new ArgumentException("Cannot fill objects from closed reader.", "reader");

				FillObjectListComplexInternal<TObject>(reader, objectList, schemeId, filter);
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

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway and will be present in child collections(if such exists)</param>
		public void FillObjectListComplex(IDataReader reader, IList objectList, Type objectType, int schemeId, IDbConnection conn, bool close, ObjectFilter filter)
		{
			try
			{
				if (objectList == null)
					throw new ArgumentException("Destination list can not be null.", "objectList");

				if (reader == null)
					throw new ArgumentException("Cannot fill objects from null.", "reader");

				if (reader.IsClosed)
					throw new ArgumentException("Cannot fill objects from closed reader.", "reader");

				if (objectList != null && objectType == null)
					objectType = ReflectionHelper.GetListItemType(objectList.GetType());

				if (objectType == null && objectList == null)
					throw new ArgumentNullException("objectType", "Cannot fill object of unknown type.");

				FillObjectListComplexInternal(reader, objectList, objectType, schemeId, filter);
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
		#endregion

		#region Fill object from reader
		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// Do not populates object children. Calls IDataReader.Read() before filling object.
		/// Do not closes reader.
		/// </summary>
		/// <typeparam name="TObject">Type of object that will be filled from reader.</typeparam>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null.</param>
		/// <returns>Filled object</returns>
		public TObject FillObject<TObject>(IDataReader reader, TObject obj)
			where TObject : class
		{
			return FillObject<TObject>(reader, obj, 0, true, null, false);
		}

		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// Do not populates object children. Calls IDataReader.Read() before filling object.
		/// Closes reader and connection after work is done.
		/// </summary>
		/// <typeparam name="TObject">Type of object that will be filled from reader.</typeparam>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null.</param>
		/// <param name="conn">Connection to close after work is done.</param>
		/// <returns>Filled object</returns>
		public TObject FillObject<TObject>(IDataReader reader, TObject obj, IDbConnection conn)
			where TObject : class
		{
			return FillObject<TObject>(reader, obj, 0, true, conn, true);
		}

		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// Do not populates object children. Calls IDataReader.Read() before filling object.
		/// Do not closes reader.
		/// </summary>
		/// <typeparam name="TObject">Type of object that will be filled from reader.</typeparam>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		/// <returns>Filled object</returns>
		public TObject FillObject<TObject>(IDataReader reader, TObject obj, int schemeId)
			where TObject : class
		{
			return FillObject<TObject>(reader, obj, schemeId, true, null, false) ;
		}

		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// </summary>
		/// <typeparam name="TObject">Type of object that will be filled from reader.</typeparam>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		/// <param name="read">If this parameter is true call IDataReader.Read() before filling object, otherwise do not call IDataReader.Read().</param>
		/// <param name="conn">Connection to close after work is done.</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed.</param>
		/// <returns>Filled object</returns>
		public TObject FillObject<TObject>(IDataReader reader, TObject obj, int schemeId, bool read, IDbConnection conn, bool close)
			where TObject : class
		{
			try
			{
				if (reader == null)
					throw new ArgumentNullException("reader", "Cannot fill object from null.");

				if (reader.IsClosed)
					throw new ArgumentException("reader", "Cannot fill object from closed reader.");

				return FillObjectInternal<TObject>(reader, obj, schemeId, read);
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

		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// </summary>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null if objectType is specified.</param>
		/// <param name="objectType">Type of object that will be filled from reader.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		/// <param name="read">If this parameter is true call IDataReader.Read() before filling object, otherwise do not call IDataReader.Read().</param>
		/// <param name="conn">Connection to close after work is done.</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed.</param>
		/// <returns>Filled object</returns>
		public object FillObject(IDataReader reader, object obj, Type objectType, int schemeId, bool read, IDbConnection conn, bool close)
		{
			try
			{
				if (reader == null)
					throw new ArgumentNullException("reader", "Cannot fill object from null.");

				if (reader.IsClosed)
					throw new ArgumentNullException("reader", "Cannot fill object from null.");

				if (objectType == null && obj != null)
					objectType = obj.GetType();

				if (objectType == null)
					throw new ArgumentNullException("objectType", "Cannot fill object of unknown type.");

				return FillObjectInternal(reader, obj, objectType, schemeId, read);
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
		#endregion

		#region Fill object list from data rows
		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection). This method uses default schemeId - 0.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataCollection">DataView to extract data.</param>
		/// <param name="objectList">Collection to populate.</param>
		public void FillObjectList<TObject>(DataView dataCollection, IList objectList)
			where TObject : class
		{
			FillObjectListInternal<DataRowView, TObject>(dataCollection, objectList, 0,
				drv => drv.Row,
				true);
		}

		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection). This method uses default schemeId - 0.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataCollection">Collection of data row objects.</param>
		/// <param name="objectList">Collection to populate.</param>
		public void FillObjectList<TObject>(DataRowCollection dataCollection, IList objectList)
			where TObject : class
		{
			FillObjectListInternal<DataRow, TObject>(dataCollection, objectList, 0, null, true);
		}

		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection). This method uses default schemeId - 0.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataCollection">Collection of data row objects.</param>
		/// <param name="objectList">Collection to populate.</param>
		public void FillObjectList<TObject>(ICollection dataCollection, IList objectList)
			where TObject : class
		{
			FillObjectListInternal<DataRow, TObject>(dataCollection, objectList, 0, null, true);
		}

		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection).
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataCollection">DataView to extract data.</param>
		/// <param name="objectList">Collection to populate.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		public void FillObjectList<TObject>(DataView dataCollection, IList objectList, int schemeId)
			where TObject : class
		{
			FillObjectListInternal<DataRowView, TObject>(objectList, dataCollection, schemeId,
				drv => drv.Row,
				true);
		}

		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection).
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataCollection">Collection of data row objects.</param>
		/// <param name="objectList">Collection to populate.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		public void FillObjectList<TObject>(DataRowCollection dataCollection, IList objectList, int schemeId)
			where TObject : class
		{
			FillObjectListInternal<DataRow, TObject>(dataCollection, objectList, schemeId, null, true);
		}

		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection).
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataCollection">Collection of data row objects.</param>
		/// <param name="objectList">Collection to populate.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		public void FillObjectList<TObject>(ICollection dataCollection, IList objectList, int schemeId)
		{
			FillObjectListInternal<DataRow, TObject>(dataCollection, objectList, schemeId, null, true);
		}

		/// <summary>
		/// Fills objectList with objects extracted from dataCollection and populates object children.
		/// dataCollection must be collection of DataRow objects. To extract object children there must be 
		/// DataRelation objects in DataSet(that holds DataRows from dataCollection).
		/// </summary>
		/// <param name="dataCollection">Collection of data row objects.</param>
		/// <param name="objectList">Collection to populate.</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		public void FillObjectList(ICollection dataCollection, IList objectList, Type objectType, int schemeId)
		{
			FillObjectListInternal<DataRow>(objectList, objectType, dataCollection, schemeId, null, true);
		}
		#endregion		

		#region Fill object from data row
		/// <summary>
		/// Fills obj with data extracted from dataRow and populates object children.
		/// To extract object children there must be DataRelation objects in DataSet 
		/// (that holds specified dataRow).
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataRow">Data row to extract data from.</param>
		/// <param name="obj">Object to fill. Can be null in this case there will be created new object of TObject type.</param>
		/// <returns></returns>
		public TObject FillObject<TObject>(DataRow dataRow, TObject obj)
			where TObject : class
		{
			return FillObject<TObject>(dataRow, obj, 0);
		}

		/// <summary>
		/// Fills obj with data extracted from dataRow and populates object children.
		/// To extract object children there must be DataRelation objects in DataSet 
		/// (that holds specified dataRow).
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataRow">Data row to extract data from.</param>
		/// <param name="obj">Object to fill. Can be null in this case there will be created new object of TObject type.</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <returns></returns>
		public TObject FillObject<TObject>(DataRow dataRow, TObject obj, int schemeId)
			where TObject : class
		{
			if (dataRow == null)
				throw new ArgumentNullException("dataRow", "Cannot fill object from null.");

			return FillObjectInternal<TObject>(dataRow, obj, schemeId);
		}

		/// <summary>
		/// Fills obj with data extracted from dataRow and populates object children.
		/// To extract object children there must be DataRelation objects in DataSet 
		/// (that holds specified dataRow).
		/// </summary>
		/// <param name="dataRow">Data row to extract data from.</param>
		/// <param name="obj">Object to fill. Can be null in this case there will be created new object of objectType.</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <returns>Filled object</returns>
		public object FillObject(DataRow dataRow, object obj, Type objectType, int schemeId)
		{
			if (dataRow == null)
				throw new ArgumentNullException("dataRow", "Cannot fill object from null.");

			if (objectType == null && obj != null)
				objectType = obj.GetType();

			if (objectType == null)
				throw new ArgumentNullException("objectType", "Cannot fill object of unknown type.");

			return FillObjectInternal(dataRow, obj, objectType, schemeId);
		} 
		#endregion


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


		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway.</param>
		protected void FillObjectListInternal<TObject>(IDataReader reader, IList objectList, int schemeId, IDbConnection conn, bool close, ObjectFilter<TObject> filter)
			where TObject : class
		{
			var objectType = typeof(TObject);
			List<List<int>> columnIndexes = null;

			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(
				objectType,
				schemeId
				);

			if (extractInfo == null)
				throw new InvalidOperationException("Can not fill object without mapping definition.");

			DataTable schemeTable = null;
			FillMethodDef method = null;
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

				TObject obj = _ObjectBuilder.CreateObject<TObject>();
				//Fill object
				CallExtractorMethod(method, obj, reader, columnIndexes);

				if (filter == null || filter(reader, obj))
					objectList.Add(obj);
			}
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Do not populates object children.
		/// </summary>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="conn">Connection to close after work is done</param>
		/// <param name="close">True to close connection and reader after work is done. If this parameter is false neither conn nor reader will be closed</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway.</param>
		protected void FillObjectListInternal(IDataReader reader, IList objectList, Type objectType, int schemeId, IDbConnection conn, bool close, ObjectFilter filter)
		{
			List<List<int>> columnIndexes = null;

			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(
				objectType,
				schemeId
				);

			if (extractInfo == null)
				throw new InvalidOperationException("Can not fill object without mapping definition.");

			DataTable schemeTable = null;
			FillMethodDef method = null;
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

				if (filter == null || filter(reader, obj))
					objectList.Add(obj);
			}
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway and will be present in child collections(if such exists)</param>
		protected void FillObjectListComplexInternal<TObject>(
			IDataReader reader,
			IList objectList,
			int schemeId,
			ObjectFilter<TObject> filter)
		{
			List<List<int>> columnIndexes = null;
			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(typeof(TObject), schemeId);

			if (!extractInfo.CheckTableIndex())
				throw new DataMapperException("You forgot to set TableMap for attribute mapping or tableIx/tableName attributes for xml mapping.");

			Dictionary<ExtractInfo,
				KeyObjectIndex> tempPrimary = new Dictionary<ExtractInfo, KeyObjectIndex>(); //table name //pk //object
			Dictionary<ExtractInfo,
				KeyObjectIndex> tempForeign = new Dictionary<ExtractInfo, KeyObjectIndex>(); //table name //pk //object

			int tableIx = 0;
			do
			{
				//if (!reader.Read())
				//    continue;

				DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
				List<ExtractInfo> assosiatedEI = extractInfo.FindByTable(tableIx, schemeTable.TableName);

				int eiCount = assosiatedEI.Count;
				if (eiCount <= 0)
					System.Diagnostics.Debug.WriteLine("Warning! No type to extract from table " + schemeTable.TableName);
				else
				{
					for (int i = 0; i < eiCount; i++)
					{
						ExtractInfo currentEI = assosiatedEI[i];
						FillMethodDef extractMethod = null;
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

						if (currentEI == extractInfo)
						{
							ExtractPrimaryObjects<TObject>(
							   reader,
							   primaryKeys,
							   foreignKeys,
							   out pkObjects,
							   out fkObjects,
							   columnIndexes,
							   objectList,
							   extractMethod,
							   filter);
						}
						else
						{
							ExtractNotPrimaryObjects(
							   reader,
							   currentEI.TargetType,
							   primaryKeys,
							   foreignKeys,
							   out pkObjects,
							   out fkObjects,
							   columnIndexes,
							   extractMethod);
						}

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
					} //end for
				}

				tableIx++;
			} while (reader.NextResult());

			if (extractInfo.LinkMethod != null)
				CallLinkObjects(extractInfo, tempPrimary, tempForeign);
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList</typeparam>
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway and will be present in child collections(if such exists)</param>
		protected void ExtractPrimaryObjects<TObject>(
			IDataReader reader,
			List<KeyInfo> pkInfo,
			List<KeyInfo> fkInfo,
			out KeyObjectIndex pkObjects,
			out KeyObjectIndex fkObjects,
			List<List<int>> columnIndexes,
			IList objectList,
			FillMethodDef method,
			ObjectFilter<TObject> filter
			)
		{
			pkObjects = new KeyObjectIndex(typeof(TObject));
			fkObjects = new KeyObjectIndex(typeof(TObject));

			//do
			while (reader.Read())
			{
				TObject obj = _ObjectBuilder.CreateObject<TObject>();
				//Fill object
				CallExtractorMethod(method, obj, reader, columnIndexes);
				ExtractObjectRelations(reader, obj, typeof(TObject), pkInfo, fkInfo, pkObjects, fkObjects);

				if (filter == null || filter(reader, obj))
				{
					objectList.Add(obj);
				}
			} 
		}

		/// <summary>
		/// Fills objectList with objects extracted from reader. Populates all children according to specified mapping.
		/// </summary> 
		/// <param name="reader">Reader with data</param>
		/// <param name="objectList">Collection to populate</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <param name="filter">This filter can exclude some objects from objectList collection. 
		/// Note: objects will be created anyway and will be present in child collections(if such exists)</param>
		protected void FillObjectListComplexInternal(
			IDataReader reader,
			IList objectList,
			Type objectType,
			int schemeId,
			ObjectFilter filter)
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
				//if (!reader.Read())
				//    continue;

				DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
				List<ExtractInfo> assosiatedEI = extractInfo.FindByTable(tableIx, schemeTable.TableName);

				int eiCount = assosiatedEI.Count;
				if (eiCount <= 0)
					System.Diagnostics.Debug.WriteLine("Warning! No type to extract from table " + schemeTable.TableName);
				else
				{
					for (int i = 0; i < eiCount; i++)
					{
						ExtractInfo currentEI = assosiatedEI[i];
						FillMethodDef extractMethod = null;
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

						if (currentEI == extractInfo)
						{
							ExtractPrimaryObjects(
							   reader,
							   objectType,
							   primaryKeys,
							   foreignKeys,
							   out pkObjects,
							   out fkObjects,
							   columnIndexes,
							   objectList,
							   extractMethod,
							   filter);
						}
						else
						{
							ExtractNotPrimaryObjects(
							   reader,
							   currentEI.TargetType,
							   primaryKeys,
							   foreignKeys,
							   out pkObjects,
							   out fkObjects,
							   columnIndexes,
							   extractMethod);
						}

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
					} //end for
				}
				tableIx++;
			} while (reader.NextResult());

			CallLinkObjects(extractInfo, tempPrimary, tempForeign);
		}

		protected void ExtractPrimaryObjects(
			IDataReader reader,
			Type objectType,
			List<KeyInfo> pkInfo,
			List<KeyInfo> fkInfo,
			out KeyObjectIndex pkObjects,
			out KeyObjectIndex fkObjects,
			List<List<int>> columnIndexes,
			IList objectList,
			FillMethodDef method,
			ObjectFilter filter)
		{
			pkObjects = new KeyObjectIndex(objectType);
			fkObjects = new KeyObjectIndex(objectType);

			//do
			while (reader.Read())
			{
				object obj = _ObjectBuilder.CreateObject(objectType);
				//Fill object
				CallExtractorMethod(method, obj, reader, columnIndexes);
				ExtractObjectRelations(reader, obj, objectType, pkInfo, fkInfo, pkObjects, fkObjects);

				if (filter == null || filter(reader, obj))
				{
					objectList.Add(obj);
				}
			} 
		}

		protected void ExtractNotPrimaryObjects(
			IDataReader reader,
			Type objectType,
			List<KeyInfo> pkInfo,
			List<KeyInfo> fkInfo,
			out KeyObjectIndex pkObjects,
			out KeyObjectIndex fkObjects,
			List<List<int>> columnIndexes,
			FillMethodDef method
			)
		{
			pkObjects = new KeyObjectIndex(objectType);
			fkObjects = new KeyObjectIndex(objectType);

			//do
			while (reader.Read())
			{
				object obj = _ObjectBuilder.CreateObject(objectType);
				//Fill object
				CallExtractorMethod(method, obj, reader, columnIndexes);
				ExtractObjectRelations(reader, obj, objectType, pkInfo, fkInfo, pkObjects, fkObjects);
			} 
		}

		protected void ExtractObjectRelations(
			IDataReader reader,
			object obj,
			Type objectType,
			List<KeyInfo> pkInfo,
			List<KeyInfo> fkInfo,
			KeyObjectIndex pkObjects,
			KeyObjectIndex fkObjects
			)
		{
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
		}

		protected void CallLinkObjects(
			ExtractInfo extractInfo,
            Dictionary<ExtractInfo, KeyObjectIndex> tempPrimary,
            Dictionary<ExtractInfo, KeyObjectIndex> tempForeign)
		{
			extractInfo.LinkMethod.Invoke(null, new object[] { extractInfo, tempPrimary, tempForeign, new Dictionary<ExtractInfo, object>(20) });
		}

		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// </summary>
		/// <typeparam name="TObject">Type of object that will be filled from reader.</typeparam>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		/// <param name="read">If this parameter is true call IDataReader.Read() before filling object, otherwise do not call IDataReader.Read().</param>
		/// <returns>Filled object</returns>
		protected TObject FillObjectInternal<TObject>(IDataReader reader, TObject obj, int schemeId, bool read)
			where TObject : class
		{
			DataTable schemeTable = GetTableFromSchema(reader.GetSchemaTable());
			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfoWithMethod(
				typeof(TObject),
				schemeId,
				schemeTable,
				DataReaderPSG.TypeOfDataSource
				);

			if (extractInfo == null)
				throw new DataMapperException("Can not fill object without mapping definition.");

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject<TObject>();

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

		/// <summary>
		/// Fills specified object (or creates new one) with data from reader.
		/// </summary>
		/// <param name="reader">Reader with data.</param>
		/// <param name="obj">Object to fill. Can be null if objectType is specified.</param>
		/// <param name="objectType">Type of object that will be filled from reader.</param>
		/// <param name="schemeId">Id of mapping scheme.</param>
		/// <param name="read">If this parameter is true call IDataReader.Read() before filling object, otherwise do not call IDataReader.Read().</param>
		/// <returns>Filled object</returns>
		protected object FillObjectInternal(IDataReader reader, object obj, Type objectType, int schemeId, bool read)
		{
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

		protected void FillObjectListInternal<TRowItem, TObject>(
			ICollection dataCollection, 
			IList objectList, 
			int schemeId, 
			DataRowExtractor<TRowItem> rowExtractor, 
			bool clearObjectCache)
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (dataCollection == null)
				throw new ArgumentException("Cannot fill objects from null.", "dataCollection");

			Type listType = objectList.GetType();
			//if (objectType == null && listType.IsGenericType)
			//    objectType = listType.GetGenericArguments()[0];

			//if (objectType == null)
			//    throw new ArgumentException("Cannot fill object of unknown type.", "objectType");

			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(typeof(TObject), schemeId);
			FillMethodDef extractMethod = null;
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
					obj = _ObjectBuilder.CreateObject<TObject>();
					//Fill object
					CallExtractorMethod(extractMethod, obj, dataRow, columnIndexes);
					_CreatedObjects.Add(dataRow, obj);
				}

				objectList.Add(obj);
			}

			if (clearObjectCache)
				_CreatedObjects.Clear();
		}

		protected void FillObjectListInternal<TRowItem>(
			IList objectList, 
			Type objectType, 
			ICollection dataCollection, 
			int schemeId, 
			DataRowExtractor<TRowItem> rowExtractor, 
			bool clearObjectCache)
		{
			if (objectList == null)
				throw new ArgumentException("Destination list can not be null.", "objectList");

			if (dataCollection == null)
				throw new ArgumentException("Cannot fill objects from null.", "dataCollection");

			Type listType = objectList.GetType();
			if (objectType == null && listType.IsGenericType)
				objectType = listType.GetGenericArguments()[0];

			if (objectType == null)
				throw new ArgumentException("Cannot fill object of unknown type.", "objectType");

			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfo(objectType, schemeId);
			FillMethodDef extractMethod = null;
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

		/// <summary>
		/// Fills obj with data extracted from dataRow and populates object children.
		/// To extract object children there must be DataRelation objects in DataSet 
		/// (that holds specified dataRow).
		/// </summary>
		/// <typeparam name="TObject">Type of objects that will be added to collection objectList.</typeparam>
		/// <param name="dataRow">Data row to extract data from.</param>
		/// <param name="obj">Object to fill. Can be null in this case there will be created new object of TObject type.</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <returns></returns>
		protected TObject FillObjectInternal<TObject>(DataRow dataRow, TObject obj, int schemeId)

		{
			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfoWithMethod(
				typeof(TObject),
				schemeId,
				dataRow.Table,
				DataTablePSG.TypeOfDataSource
				);

			FillMethodDef extractMethod;
			if (extractInfo == null || !extractInfo.FillMethod.TryGetValue(DataTablePSG.TypeOfDataSource, out extractMethod))
				throw new DataMapperException("Can not fill object without mapping definition.");

			_CreatedObjects = new Dictionary<DataRow, object>(1);

			List<List<int>> columnIndexes = extractInfo.GetSubColumnsIndexes(dataRow.Table);

			//If there is no instance create it
			if (obj == null)
				obj = _ObjectBuilder.CreateObject<TObject>();

			//Fill object
			CallExtractorMethod(extractMethod, obj, dataRow, columnIndexes);
			return obj;
		} 

		/// <summary>
		/// Fills obj with data extracted from dataRow and populates object children.
		/// To extract object children there must be DataRelation objects in DataSet 
		/// (that holds specified dataRow).
		/// </summary>
		/// <param name="dataRow">Data row to extract data from.</param>
		/// <param name="obj">Object to fill. Can be null in this case there will be created new object of objectType.</param>
		/// <param name="objectType">Type of objects that will be added to collection objectList</param>
		/// <param name="schemeId">Id of mapping scheme</param>
		/// <returns>Filled object</returns>
		protected object FillObjectInternal(DataRow dataRow, object obj, Type objectType, int schemeId)
		{
			ExtractInfo extractInfo = _DMCodeGenerator.CreateExtractInfoWithMethod(
				objectType,
				schemeId,
				dataRow.Table,
				DataTablePSG.TypeOfDataSource
				);

			FillMethodDef extractMethod;
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

		protected bool CallExtractorMethod(
			FillMethodDef extractorMethod, 
			object obj, 
			object data, 
			List<List<int>> subColumns)
		{
			int clmn = 0;
			object[] created = new object[_DMCodeGenerator.GeneratedMethodCount];
			return extractorMethod(obj, data, this, subColumns, ref clmn, created);
			//return (bool)extractorMethod.Invoke(null, new object[] { obj, data, this, subColumns, clmn, created });
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


		public class KeyObjectIndex	: Dictionary<object, IList>
		{
			protected Type _ListType;


			public KeyObjectIndex(Type objectType)
			{
				_ListType = objectType;
			}


			public void AddObject(object key, object obj)
			{
				IList list;
				if (!TryGetValue(key, out list))
				{
					list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_ListType));
					Add(key, list);
				}

				list.Add(obj);
			}

			public void AddRange(object key, object objList)
			{
				IList list;
				if (!TryGetValue(key, out list))
				{
					list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_ListType));
					Add(key, list);
				}

				var m = typeof(List<>).MakeGenericType(_ListType).GetMethod("AddRange");
				m.Invoke(list, new object[] { objList });
				//list.AddRange(obj);
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
			{
				return Activator.CreateInstance<T>();
			}

			#endregion
		}
	}
}
