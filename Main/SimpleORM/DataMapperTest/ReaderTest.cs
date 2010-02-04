using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleORM;


namespace DataMapperTest
{
	/// <summary>
	/// Summary description for ReaderTest
	/// </summary>
	[TestClass]
	public class ReaderTest
	{
		private DataSet _DataSet = new DataSet();
		private DataSet _Hierarchy = new DataSet();
		private DateTime _CurrentDate;


		public ReaderTest()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		[TestInitialize()]
		public void MyTestInitialize() 
		{
			var _DateTable = new DataTable();
			_DateTable.Columns.Add(new DataColumn("Field1", typeof(int)));
			_DateTable.Columns.Add(new DataColumn("Field2", typeof(string)));
			_DateTable.Columns.Add(new DataColumn("Field3", typeof(DateTime)));
			_DateTable.Columns.Add(new DataColumn("Field4", typeof(int)));
			_DateTable.Columns.Add(new DataColumn("ParentId", typeof(int)));

			_CurrentDate = DateTime.Now;
			_DateTable.Rows.Add(72, "Hey!", _CurrentDate, 1, 1);
			_DateTable.Rows.Add(34, "Twice", DateTime.MaxValue, 2, 2);
			_DateTable.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
			_DateTable.Rows.Add(0, "Twice", DateTime.MaxValue, 0, 0);

			_DataSet.Tables.Add(_DateTable);

			DataTable childTable = new DataTable();
			childTable.Columns.Add(new DataColumn("Field1", typeof(int)));
			childTable.Columns.Add(new DataColumn("Field2", typeof(string)));
			childTable.Columns.Add(new DataColumn("Field3", typeof(DateTime)));
			childTable.Columns.Add(new DataColumn("ParentId", typeof(int)));

			childTable.Rows.Add(2, "Child 1", DateTime.Now, 1);
			childTable.Rows.Add(3, "Child 2", DateTime.MaxValue, 1);
			childTable.Rows.Add(4, "Child 3", DateTime.MaxValue, 2);
			childTable.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

			_DataSet.Tables.Add(childTable);


	
			
			
			DataTable parent = new DataTable();
			parent.Columns.Add(new DataColumn("PARENT", typeof(int)));
			parent.Columns.Add(new DataColumn("Id", typeof(int)));
			parent.Columns.Add(new DataColumn("Name", typeof(string)));
			parent.Columns.Add(new DataColumn("Date", typeof(DateTime)));
			parent.Columns.Add(new DataColumn("EntityTypeId", typeof(int)));
			parent.Columns.Add(new DataColumn("EntityTypeName", typeof(string)));

			parent.Rows.Add(DBNull.Value, 1, "Name1", _CurrentDate, 1, "Type1");
			parent.Rows.Add(DBNull.Value, 2, "Name2", _CurrentDate.AddDays(1), 2, "Type2");
			parent.Rows.Add(DBNull.Value, 3, "Name3", DBNull.Value, 1, "Type1");



			DataTable child1 = new DataTable();
			child1.Columns.Add(new DataColumn("CHILD1", typeof(int)));
			child1.Columns.Add(new DataColumn("Id", typeof(int)));
			child1.Columns.Add(new DataColumn("Name", typeof(string)));
			child1.Columns.Add(new DataColumn("Date", typeof(DateTime)));
			child1.Columns.Add(new DataColumn("ParentId", typeof(int)));
			child1.Columns.Add(new DataColumn("EntityTypeId", typeof(int)));
			child1.Columns.Add(new DataColumn("EntityTypeName", typeof(string)));

			child1.Rows.Add(DBNull.Value, 1, "Child11", _CurrentDate, 1, 1, "Type1");
			child1.Rows.Add(DBNull.Value, 2, "Child12", _CurrentDate.AddDays(1), 1, 2, "Type2");
			child1.Rows.Add(DBNull.Value, 3, "Child13", DBNull.Value, 2, 1, "Type1");



			DataTable child2 = new DataTable();
			child2.Columns.Add(new DataColumn("CHILD2", typeof(int)));
			child2.Columns.Add(new DataColumn("Name", typeof(string)));
			child2.Columns.Add(new DataColumn("Date", typeof(DateTime)));
			child2.Columns.Add(new DataColumn("ParentId", typeof(int)));
			child2.Columns.Add(new DataColumn("EntityTypeId", typeof(int)));
			child2.Columns.Add(new DataColumn("EntityTypeName", typeof(string)));
			child2.Columns.Add(new DataColumn("Id", typeof(int)));

			child2.Rows.Add(DBNull.Value, "Child21", _CurrentDate, 1, 1, "Type1", 4);
			child2.Rows.Add(DBNull.Value, "Child22", _CurrentDate.AddDays(1), 2, 2, "Type2", 5);
			child2.Rows.Add(DBNull.Value, "Child23", DBNull.Value, 2, 1, "Type1", 6);


			DataTable child2child = new DataTable();
			child2child.Columns.Add(new DataColumn("CHILD2CHILD", typeof(int)));
			child2child.Columns.Add(new DataColumn("Name", typeof(string)));
			child2child.Columns.Add(new DataColumn("Date", typeof(DateTime)));
			child2child.Columns.Add(new DataColumn("Id", typeof(int)));
			child2child.Columns.Add(new DataColumn("ParentId", typeof(int)));
			child2child.Columns.Add(new DataColumn("EntityTypeId", typeof(int)));
			child2child.Columns.Add(new DataColumn("EntityTypeName", typeof(string)));

			child2child.Rows.Add(DBNull.Value, "Child221", _CurrentDate, 4, 4, 1, "Type1");
			child2child.Rows.Add(DBNull.Value, "Child222", _CurrentDate.AddDays(1), 5, 5, 2, "Type2");
			child2child.Rows.Add(DBNull.Value, "Child223", DBNull.Value, 6, 5, 1, "Type1");

			_Hierarchy.Tables.AddRange(new DataTable[] { 
				parent,
				child1,
				child2,
				child2child
			});
		}
		
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion


		[TestMethod]
		public void FillObjectListComplexReaderTest()
		{
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(@"..\..\..\DataMapperTest\hierarchy.mapping");

			List<Parent> objs = new List<Parent>();

			var reader = _Hierarchy.CreateDataReader();
			DataMapper.Default.FillObjectListComplex<Parent>(objs, reader, 0);
			reader.Close();

			if (objs[0].Id != 1  ||
				 objs[0].EntityType == null ||
				 objs[0].Name.Length < 3 ||
				 objs[0].Childs1.Count != 2 ||
				 objs[0].Childs2.Count != 1 || //4
				 objs[0].Childs2[0].Childs2.Count != 1
				)
				Assert.Fail("HierarchyTest fails.");

			if (objs[1].Id != 2 ||
				 objs[1].EntityType == null ||
				 objs[1].Name.Length < 3 ||
				 objs[1].Childs1.Count != 1 ||
				 objs[1].Childs2.Count != 2 ||//5,6
				 objs[1].Childs2[0].Id != 5 ||
				 objs[1].Childs2[0].EntityType == null ||
				 objs[1].Childs2[0].Childs2.Count != 2 ||
				 objs[1].Childs2[1].Id != 6 ||
				 objs[1].Childs2[1].EntityType == null ||
				 objs[1].Childs2[1].Childs2.Count != 0
				)
				Assert.Fail("HierarchyTest fails.");

			if (objs[2].Id != 3 ||
				 objs[2].EntityType == null ||
				 objs[2].Name.Length < 3 ||
				 objs[2].Childs1.Count != 0 ||
				 objs[2].Childs2.Count != 0
				)
				Assert.Fail("HierarchyTest fails.");
		}

		[TestMethod]
		public void FillObjectsReaderNestedTest()
		{
			DataMapper.Default.ClearCache();
			//DataMapper.Default.GeneratedFileName = "ng.dll";
			DataMapper.Default.SetConfig(@"..\..\..\DataMapperTest\data.mapping");

			List<TesterAll> objs = new List<TesterAll>();

			var reader = _DataSet.CreateDataReader();
			DataMapper.Default.FillObjectListComplex<TesterAll>(objs, reader, 0);
			//DataMapper.Default.SaveGeneratedAsm();
			if (objs[0].ValueProp != 72 ||
				 objs[0].ValuePropNI != true ||
				 objs[0].RefProp != "Hey!" ||
				 objs[0].StructProp != _CurrentDate ||
				 objs[0].NullableProp != _CurrentDate ||
				 objs[0].NullablePropBool != true ||
				 objs[0].TesterArrayList.Count != 2 ||
				 objs[0].TesterList.Count != 2
				)
				Assert.Fail("FillObjectsTest fails.");

			if (objs[1].ValueProp != 34 ||
				 objs[1].ValuePropNI != true ||
				 objs[1].RefProp != "Twice" ||
				 objs[1].StructProp != DateTime.MaxValue ||
				 objs[1].NullableProp != DateTime.MaxValue ||
				 objs[1].NullablePropBool != true ||
				 objs[1].TesterArrayList.Count != 1 ||
				 objs[1].TesterList.Count != 1
				)
				Assert.Fail("FillObjectsTest fails.");

			if (objs[2].ValueProp != default(int) ||
				 objs[2].ValuePropNI != default(bool) ||
				 objs[2].RefProp != null ||
				 objs[2].StructProp != default(DateTime) ||
				 objs[2].NullableProp != null ||
				 objs[2].NullablePropBool != null ||
				 objs[2].TesterArrayList.Count != 0 ||
				 objs[2].TesterList.Count != 0
				)
				Assert.Fail("FillObjectsTest with DBNull fails.");

			reader.Close();

			reader = _DataSet.CreateDataReader();
			objs.Clear();
			DataMapper.Default.FillObjectListComplex<TesterAll>(objs, reader, 0);
			reader.Close();
		}

		[TestMethod]
		public void FillObjectsReaderTest()
		{
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);

			List<TesterAll> objs = new List<TesterAll>();

			var reader = _DataSet.CreateDataReader();
			var conn = new DBConnMock();
			DataMapper.Default.FillObjectList<TesterAll>(objs, reader, conn, 0, true);

			Assert.IsTrue(reader.IsClosed);
			Assert.IsTrue(conn.State == ConnectionState.Closed);

			if (objs[0].ValueProp != 72 ||
				 objs[0].ValuePropNI != true ||
				 objs[0].RefProp != "Hey!" ||
				 objs[0].StructProp != _CurrentDate ||
				 objs[0].NullableProp != _CurrentDate ||
				 objs[0].NullablePropBool != true
				//objs[0].TesterArrayList.Count != 2 ||
				//objs[0].TesterList.Count != 2
				)
				Assert.Fail("FillObjectsTest fails.");

			if (objs[1].ValueProp != 34 ||
				 objs[1].ValuePropNI != true ||
				 objs[1].RefProp != "Twice" ||
				 objs[1].StructProp != DateTime.MaxValue ||
				 objs[1].NullableProp != DateTime.MaxValue ||
				 objs[1].NullablePropBool != true
				//objs[1].TesterArrayList.Count != 1 ||
				//objs[1].TesterList.Count != 1
				)
				Assert.Fail("FillObjectsTest fails.");

			if (objs[2].ValueProp != default(int) ||
				 objs[2].ValuePropNI != default(bool) ||
				 objs[2].RefProp != null ||
				 objs[2].StructProp != default(DateTime) ||
				 objs[2].NullableProp != null ||
				 objs[2].NullablePropBool != null
				// objs[2].TesterArrayList.Count != 0 ||
				// objs[2].TesterList.Count != 0
				)
				Assert.Fail("FillObjectsTest with DBNull fails.");

			reader.Close();

			reader = _DataSet.CreateDataReader();
			objs.Clear();
			DataMapper.Default.FillObjectList<TesterAll>(objs, reader);
			reader.Close();
		}

		[TestMethod]
		public void FillObjectReaderTest()
		{
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);

			TesterAll result = new TesterAll();
			var reader = _DataSet.CreateDataReader();
			reader.Read();
			DataMapper.Default.FillObject(reader, result);

			if (result.ValueProp != 72 ||
				result.ValuePropNI != true ||
				result.RefProp != "Hey!" ||
				result.StructProp != _CurrentDate ||
				result.NullableProp != _CurrentDate ||
				result.NullablePropBool != true ||
				result.StrProp != "72" ||
				result.EnumProp != TestEnum.First
				//result.TesterArrayList.Count != 2 ||
				//result.TesterList.Count != 2
				)
				Assert.Fail("FillObjectTest fails.");

			reader.Read();
			result = DataMapper.Default.FillObject<TesterAll>(reader, null);
			if (result.ValueProp != 34 ||
				 result.ValuePropNI != true ||
				 result.RefProp != "Twice" ||
				 result.StructProp != DateTime.MaxValue ||
				 result.NullableProp != DateTime.MaxValue ||
				 result.NullablePropBool != true ||
				 result.StrProp != "34" ||
				 result.EnumProp != TestEnum.Second
				//result.TesterArrayList.Count != 1 ||
				//result.TesterList.Count != 1
				)
				Assert.Fail("FillObjectsTest fails.");

			reader.Read();
			result = DataMapper.Default.FillObject<TesterAll>(reader, null);
			if (result.ValueProp != default(int) ||
				 result.ValuePropNI != default(bool) ||
				 result.RefProp != null ||
				 result.StructProp != default(DateTime) ||
				 result.NullableProp != null ||
				 result.NullablePropBool != null ||
				 result.StrProp != null ||
				 result.EnumProp != TestEnum.None
				//result.TesterArrayList.Count != 1 ||
				//result.TesterList.Count != 1
				)
				Assert.Fail("FillObjectsTest fails.");

			reader.Read();
			result = DataMapper.Default.FillObject<TesterAll>(reader, null);
			if (result.ValueProp != 0 ||
				 result.ValuePropNI != false ||
				 result.RefProp != "Twice" ||
				 result.StructProp != DateTime.MaxValue ||
				 result.NullableProp != DateTime.MaxValue ||
				 result.NullablePropBool != false ||
				 result.StrProp != "0"
				//result.TesterArrayList.Count != 1 ||
				//result.TesterList.Count != 1
				)
				Assert.Fail("FillObjectsTest fails.");

			reader.Close();
		}

		[TestMethod]
		public void FillObjectReaderSchemeDegradeTest()
		{
			//This call will generate full schema
			FillObjectReaderTest();

			//now tying to load from degrade scheme 
			DataTable dt = _DataSet.Tables[0].Copy();
			dt.Columns.RemoveAt(4);
			dt.Columns.RemoveAt(3);
			dt.Columns.RemoveAt(2);
			dt.Columns.RemoveAt(1);

			TesterAll tester = new TesterAll();
			//DataMapper.Default.ClearCache();
			//DataMapper.Default.SetConfig(null);
			//DataMapper.Default.SaveGeneratedAsm("asm1.dll");

			var reader = dt.CreateDataReader();
			reader.Read();

			DataMapper.Default.FillObject(reader, tester);

			if (tester.ValueProp != 72 ||
				 tester.ValuePropNI != true ||
				 tester.RefProp != null ||
				 tester.StructProp != DateTime.MinValue ||
				 tester.NullableProp != null ||
				 tester.NullablePropBool != true ||
				//tester.TesterArrayList != null ||
				//tester.TesterList != null ||
				 tester.CmplProp.StructProp != DateTime.MinValue
				)
				Assert.Fail("FillObjectTest fails.");
		}

		[TestMethod]
		public void FillObjectTestSubclassing()
		{
			TesterAllSub tester = new TesterAllSub();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			var reader = _DataSet.CreateDataReader();
			reader.Read();
			DataMapper.Default.FillObject(reader, tester, 0);

			if (tester.ValueProp != 1 ||
				 tester.ValuePropNI != true ||
				 tester.RefProp != "Hey!" ||
				 tester.StructProp != _CurrentDate ||
				 tester.NullableProp != _CurrentDate ||
				 tester.NullablePropBool != true ||
				 tester.CmplProp.StructProp != _CurrentDate
				)
				Assert.Fail("FillObjectTest fails.");

			reader.Read();
			reader.Read();
			DataMapper.Default.FillObject(reader, tester, 0);

			if (tester.ValueProp != default(int) ||
				 tester.ValuePropNI != default(bool) ||
				 tester.RefProp != null ||
				 tester.StructProp != default(DateTime) ||
				 tester.NullableProp != null ||
				 tester.NullablePropBool != null ||
				 tester.CmplProp.StructProp != default(DateTime)
				)
				Assert.Fail("FillObjectTest with DBNull fails.");

			reader.Close();
		}
	}
}
