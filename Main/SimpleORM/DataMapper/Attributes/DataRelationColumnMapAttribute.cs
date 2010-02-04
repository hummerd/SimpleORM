using System;
using System.Collections.Generic;
using System.Text;


namespace SimpleORM.Attributes
{
	public class DataRelationColumnMapAttribute : DataMapAttribute
	{
		protected string _ParentColumn;
		protected string _ChildColumn;


		public DataRelationColumnMapAttribute(string columnName)
		{
			_ParentColumn = columnName;
			_ChildColumn = columnName;
		}

		public DataRelationColumnMapAttribute(string parentColumn, string childColumn)
		{
			_ParentColumn = parentColumn;
			_ChildColumn = childColumn;
		}


		public string ParentColumn
		{
			get { return _ParentColumn; }
			set { _ParentColumn = value; }
		}

		public string ChildColumn
		{
			get { return _ChildColumn; }
			set { _ChildColumn = value; }
		}
	}
}
