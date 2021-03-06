﻿using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleORM;


namespace DataMapperTest
{
	/// <summary>
	///This is a test class for CodeGenerator.DataMapper and is intended
	///to contain all CodeGenerator.DataMapper Unit Tests
	///</summary>
	[TestClass]
	public class DataMapperTest
	{
		private DataTable _DateTable = new DataTable();
		private DateTime _CurrentDate;

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
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//
		[TestInitialize]
		public void MyTestInitialize()
		{
			DataSet dsTest = new DataSet();

			_DateTable = new DataTable();
			_DateTable.Columns.Add(new DataColumn("Field1", typeof(int)));
			_DateTable.Columns.Add(new DataColumn("Field2", typeof(string)));
			_DateTable.Columns.Add(new DataColumn("Field3", typeof(DateTime)));
			_DateTable.Columns.Add(new DataColumn("Field4", typeof(int)));
			_DateTable.Columns.Add(new DataColumn("ParentId", typeof(int)));

			_DateTable.Rows.Add(72, "Hey!", _CurrentDate = DateTime.Now, 1, 1);
			_DateTable.Rows.Add(34, "Twice", DateTime.MaxValue, 2, 2);
			_DateTable.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
			_DateTable.Rows.Add(0, "Twice", DateTime.MaxValue, 0, 0);

			dsTest.Tables.Add(_DateTable);

			DataTable childTable = new DataTable();
			childTable.Columns.Add(new DataColumn("Field1", typeof(int)));
			childTable.Columns.Add(new DataColumn("Field2", typeof(string)));
			childTable.Columns.Add(new DataColumn("Field3", typeof(DateTime)));
			childTable.Columns.Add(new DataColumn("ParentId", typeof(int)));

			childTable.Rows.Add(2, "Child 1", DateTime.Now, 1);
			childTable.Rows.Add(3, "Child 2", DateTime.MaxValue, 1);
			childTable.Rows.Add(4, "Child 3", DateTime.MaxValue, 2);
			childTable.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

			dsTest.Tables.Add(childTable);
			dsTest.Relations.Add("Relation1",
				_DateTable.Columns["ParentId"],
				childTable.Columns["ParentId"],
				false);
		}

		//Use TestCleanup to run code after each test has run
		//
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion



		[TestMethod]
		public void PerformanseTest()
		{
			int rowCount = 100000;

			DataTable dtPerfTest = new DataTable();
			dtPerfTest.Columns.Add(new DataColumn("Field1", typeof(int)));
			dtPerfTest.Columns.Add(new DataColumn("Field2", typeof(string)));
			dtPerfTest.Columns.Add(new DataColumn("Field3", typeof(DateTime)));

			dtPerfTest.BeginLoadData();
			for (int i = 0; i < rowCount; i++)
				dtPerfTest.Rows.Add(i, i.ToString(), DateTime.Now);
			dtPerfTest.EndLoadData();

			List<TesterAll> tester = new List<TesterAll>(100000);

			DateTime dtStart = DateTime.Now;
			DataMapper.Default.FillObjectList<TesterAll>(dtPerfTest.Rows, tester, 1);
			TimeSpan span = DateTime.Now - dtStart;

			testContextInstance.WriteLine("Time for creation of " + rowCount + " objects is: " + span);
			testContextInstance.WriteLine("Time for creation of one object is: " + new TimeSpan(span.Ticks / rowCount));
		}

		[TestMethod]
		public void FillObjectSchemeDegradeTest()
		{
			//This call will generate full schema
			FillObjectTest();

			//now tying to load from degrade scheme 
			DataTable dt = _DateTable.Copy();
			dt.Columns.RemoveAt(4);
			dt.Columns.RemoveAt(3);
			dt.Columns.RemoveAt(2);
			dt.Columns.RemoveAt(1);

			TesterAll tester = new TesterAll();
			//DataMapper.Default.ClearCache();
			//DataMapper.Default.SetConfig(null);
			//DataMapper.Default.SaveGeneratedAsm("asm1.dll");

			DataMapper.Default.FillObject(dt.Rows[0], tester, 0);
			
			if (tester.ValueProp != 72 ||
				 tester.ValuePropNI != true ||
				 tester.RefProp != null ||
				 tester.StructProp != DateTime.MinValue ||
				 tester.NullableProp != null ||
				 tester.NullablePropBool != true ||
				 tester.TesterArrayList.Count != 0 ||
				 tester.TesterList.Count != 0 ||
				 tester.CmplProp.StructProp != DateTime.MinValue
				)
				Assert.Fail("FillObjectTest fails.");
		}

		[TestMethod]
		public void FillObjectTest_ComplexPropLevel2()
		{
			TesterComplexProp2 tester = new TesterComplexProp2();
			DataMapper.Default.ClearCache();
			//DataMapper.Default.GeneratedFileName = "recur.dll";
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);
			//DataMapper.Default.SaveGeneratedAsm();

			if (tester.CmplProp == null)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp.EnumProp != TestEnum.First)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp.ValueProp != 72)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp.CmplProp1 == null)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp.CmplProp1 != null && tester.CmplProp.ValueProp != 72)
				Assert.Fail("FillObjectTest_ComplexProp fails.");
			
			if (tester.CmplProp2 == null)
			   Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp2.StructProp != _CurrentDate)
			   Assert.Fail("FillObjectTest_ComplexProp fails.");
		}

		[TestMethod]
		public void FillObjectTest_ComplexProp()
		{
			TesterComplexProp tester = new TesterComplexProp();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.EnumProp != TestEnum.First)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.ValueProp != 72)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp1 == null)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp1 != null && tester.CmplProp1.ValueProp != 72)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp2 == null)
				Assert.Fail("FillObjectTest_ComplexProp fails.");

			if (tester.CmplProp2.StructProp != _CurrentDate)
				Assert.Fail("FillObjectTest_ComplexProp fails.");
		}

		[TestMethod]
		public void FillObjectTest_EnumProp()
		{
			TesterEnumProp tester = new TesterEnumProp();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.EnumProp != TestEnum.First)
				Assert.Fail("FillObjectTest_EnumProp fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.EnumProp != TestEnum.None)
				Assert.Fail("FillObjectTest_EnumProp with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTest_NullableProp()
		{
			TesterNullableProp tester = new TesterNullableProp();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.NullableProp != _CurrentDate)
				Assert.Fail("FillObjectTest_NullableProp fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.NullableProp != null)
				Assert.Fail("FillObjectTest_NullableProp with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTest_NullablePropNI()
		{
			TesterNullablePropNI tester = new TesterNullablePropNI();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.NullableProp != true)
				Assert.Fail("FillObjectTest_NullablePropNI fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.NullableProp != null)
				Assert.Fail("FillObjectTest_NullablePropNI with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTest_ValueProp()
		{
			TesterValueProp tester = new TesterValueProp();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.ValueProp != 72)
				Assert.Fail("FillObjectTest_ValueProp fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.ValueProp != default(int))
				Assert.Fail("FillObjectTest_ValueProp with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTest_ValuePropNI()
		{
			TesterValuePropNI tester = new TesterValuePropNI();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.ValueProp != true)
				Assert.Fail("FillObjectTest_ValuePropNI fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.ValueProp != default(bool))
				Assert.Fail("FillObjectTest_ValuePropNI with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTest_RefProp()
		{
			TesterRefProp tester = new TesterRefProp();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.RefProp != "Hey!")
				Assert.Fail("FillObjectTest_RefProp fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.RefProp != null)
				Assert.Fail("FillObjectTest_RefProp with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTest_StructProp()
		{
			TesterStructProp tester = new TesterStructProp();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.StructProp != _CurrentDate)
				Assert.Fail("FillObjectTest_StructProp fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.StructProp != default(DateTime))
				Assert.Fail("FillObjectTest_StructProp with DBNull fails.");
		}

		[TestMethod]
		public void FillObjectTestSubclassing()
		{
			TesterAllSub tester = new TesterAllSub();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);
			DataMapper.Default.GeneratedFileName = "gen.dll";
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.ValueProp != 1 ||
				 tester.ValuePropNI != true ||
				 tester.RefProp != "Hey!" ||
				 tester.StructProp != _CurrentDate ||
				 tester.NullableProp != _CurrentDate ||
				 tester.NullablePropBool != true ||
				 tester.TesterArrayList.Count != 2 ||
				 tester.TesterList.Count != 2 ||
				 tester.CmplProp.StructProp != _CurrentDate
				)
				Assert.Fail("FillObjectTest fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.ValueProp != default(int) ||
				 tester.ValuePropNI != default(bool) ||
				 tester.RefProp != null ||
				 tester.StructProp != default(DateTime) ||
				 tester.NullableProp != null ||
				 tester.NullablePropBool != null ||
				 tester.TesterArrayList.Count != 2 ||
				 tester.TesterList.Count != 2 ||
				 tester.CmplProp.StructProp != default(DateTime)
				)
				Assert.Fail("FillObjectTest with DBNull fails.");
		}
		
		[TestMethod]
		public void FillObjectTest()
		{
			TesterAll tester = new TesterAll();
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);


			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);
			if (tester.ValueProp != 72 ||
				 tester.ValuePropNI != true ||
				 tester.RefProp != "Hey!" ||
				 tester.StructProp != _CurrentDate ||
				 tester.NullableProp != _CurrentDate ||
				 tester.NullablePropBool != true ||
				 tester.TesterArrayList.Count != 2 ||
				 tester.TesterList.Count != 2 ||
				 tester.CmplProp.StructProp != _CurrentDate ||
				 tester.StrProp != "72" ||
				 tester.EnumProp != TestEnum.First
				)
				Assert.Fail("FillObjectTest fails.");


			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);
			if (tester.ValueProp != default(int) ||
				 tester.ValuePropNI != default(bool) ||
				 tester.RefProp != null ||
				 tester.StructProp != default(DateTime) ||
				 tester.NullableProp != null ||
				 tester.NullablePropBool != null ||
				 tester.TesterArrayList.Count != 2 ||
				 tester.TesterList.Count != 2 ||
				 tester.CmplProp.StructProp != default(DateTime) ||
				 tester.StrProp != null ||
				 tester.EnumProp != TestEnum.None
				)
				Assert.Fail("FillObjectTest with DBNull fails.");


			DataMapper.Default.FillObject(_DateTable.Rows[3], tester, 0);
			if (tester.ValueProp != 0 ||
				 tester.ValuePropNI != false ||
				 tester.RefProp != "Twice" ||
				 tester.StructProp != DateTime.MaxValue ||
				 tester.NullableProp != DateTime.MaxValue ||
				 tester.NullablePropBool != false ||
				 tester.StrProp != "0"
				//result.TesterArrayList.Count != 1 ||
				//result.TesterList.Count != 1
				)
				Assert.Fail("FillObjectsTest fails.");
		}

		[TestMethod]
		public void FillObjectsTest()
		{
			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(String.Empty);

			List<TesterAll> objs = new List<TesterAll>(_DateTable.Rows.Count);
			DataMapper.Default.FillObjectList<TesterAll>(_DateTable.Rows, objs);

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
		}

		[TestMethod]
		public void FillObjectTestXML()
		{
			TesterAll tester = new TesterAll();

			DataMapper.Default.ClearCache();
			DataMapper.Default.SetConfig(@"..\..\..\DataMapperTest\datatable.mapping");
			DataMapper.Default.FillObject(_DateTable.Rows[0], tester, 0);

			if (tester.ValueProp != 72 ||
				 tester.ValuePropNI != true ||
				 tester.RefProp != "Hey!" ||
				 tester.StructProp != _CurrentDate ||
				 tester.NullableProp != _CurrentDate ||
				 tester.NullablePropBool != true ||
				 tester.TesterArrayList.Count != 2 ||
				 tester.TesterList.Count != 2 ||
				 tester.CmplProp.StructProp != _CurrentDate
				)
				Assert.Fail("FillObjectTest fails.");

			DataMapper.Default.FillObject(_DateTable.Rows[2], tester, 0);

			if (tester.ValueProp != default(int) ||
				 tester.ValuePropNI != default(bool) ||
				 tester.RefProp != null ||
				 tester.StructProp != default(DateTime) ||
				 tester.NullableProp != null ||
				 tester.NullablePropBool != null ||
				 tester.TesterArrayList.Count != 2 ||
				 tester.TesterList.Count != 2 ||
				 tester.CmplProp.StructProp != default(DateTime)
				)
				Assert.Fail("FillObjectTest with DBNull fails.");
		}
	}
}
