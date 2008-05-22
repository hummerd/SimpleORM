using System;
using System.Collections.Generic;
using System.ComponentModel;
using SimpleORM.Attributes;


namespace DataMapperTest
{
	public enum TestEnum
	{ 
		None = 0,
		First = 1,
		Second = 2
	}

	public class TesterEnumProp
	{
		private TestEnum _EnumProp;
		[DataColumnMap("Field4")]
		public TestEnum EnumProp
		{
			get { return _EnumProp; }
			set { _EnumProp = value; }
		}
	}

	public class TesterNullableProp
	{
		private DateTime? _NullableProp;
		[DataColumnMap("Field3")]
		public DateTime? NullableProp
		{
			get { return _NullableProp; }
			set { _NullableProp = value; }
		}
	}

	public class TesterNullablePropNI
	{
		private bool? _NullableProp;
		[DataColumnMapAttribute("Field1")]
		public bool? NullableProp
		{
			get { return _NullableProp; }
			set { _NullableProp = value; }
		}
	}

	public class TesterValueProp
	{
		private int _ValueProp;
		[DataColumnMapAttribute("Field1")]
		public int ValueProp
		{
			get { return _ValueProp; }
			set { _ValueProp = value; }
		}
	}

	public class TesterValuePropNI
	{
		private bool _ValueProp;
		[DataColumnMapAttribute("Field1")]
		public bool ValueProp
		{
			get { return _ValueProp; }
			set { _ValueProp = value; }
		}
	}

	public class TesterRefProp
	{
		private string _RefProp;
		[DataColumnMapAttribute("Field2")]
		public string RefProp
		{
			get { return _RefProp; }
			set { _RefProp = value; }
		}
	}

	public class TesterStructProp
	{
		private DateTime _StructProp;
		[DataColumnMapAttribute("Field3")]
		public DateTime StructProp
		{
			get { return _StructProp; }
			set { _StructProp = value; }
		}
	}

	public class TesterAll
	{
		private int _ValueProp;
		[DataColumnMapAttribute("Field1")]
		public int ValueProp
		{
			get { return _ValueProp; }
			set { _ValueProp = value; }
		}

		private bool _ValuePropNI;
		[DataColumnMapAttribute("Field1")]
		public bool ValuePropNI
		{
			get { return _ValuePropNI; }
			set { _ValuePropNI = value; }
		}

		private string _RefProp;
		[DataColumnMapAttribute("Field2")]
		public string RefProp
		{
			get { return _RefProp; }
			set { _RefProp = value; }
		}

		private DateTime _StructProp;
		[DataColumnMapAttribute("Field3")]
		public DateTime StructProp
		{
			get { return _StructProp; }
			set { _StructProp = value; }
		}

		private DateTime? _NullableProp;
		[DataColumnMapAttribute("Field3")]
		public DateTime? NullableProp
		{
			get { return _NullableProp; }
			set { _NullableProp = value; }
		}

		private bool? _NullablePropBool;
		[DataColumnMapAttribute("Field1")]
		public bool? NullablePropBool
		{
			get { return _NullablePropBool; }
			set { _NullablePropBool = value; }
		}

		private TesterAllList _TesterList;
		[DataRelationMap("Relation1")]
		public TesterAllList TesterList
		{
			get { return _TesterList; }
			set { _TesterList = value; }
		}

		//private TesterAllBindingCollection _TesterBindingList;
		//[DataRelationMap("Relation1")]
		//public TesterAllBindingCollection TesterBindingList
		//{
		//   get { return _TesterBindingList; }
		//   set { _TesterBindingList = value; }
		//}

		//private ArrayList _TesterObjectList;
		//[DataRelationMap("Relation1", typeof(TesterAll))]
		//public ArrayList TesterObjectList
		//{
		//   get { return _TesterObjectList; }
		//   set { _TesterObjectList = value; }
		//}
	}

	public class TesterAllList : List<TesterAll>
	{
	
	}

	public class TesterAllBindingCollection : BindingList<TesterAll>
	{

	}
}
