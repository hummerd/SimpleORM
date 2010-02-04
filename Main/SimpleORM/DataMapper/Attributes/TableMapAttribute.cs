using System;
using System.Collections.Generic;
using System.Text;


namespace SimpleORM.Attributes
{
	public class TableMapAttribute : DataMapAttribute
	{
		protected int[] _TableIx;
		protected string _TableName;


		public TableMapAttribute(int[] tableIx)
		{
			_TableIx = tableIx;
		}

		public TableMapAttribute(string tableName)
		{
			_TableName = tableName;
		}

		public TableMapAttribute(int[] tableIx, int schemeId)
			: base(schemeId)
		{
			_TableIx = tableIx;
		}

		public TableMapAttribute(string tableName, int schemeId)
			: base(schemeId)
		{
			_TableName = tableName;
		}


		public int[] TableIx
		{
			get { return _TableIx; }
			set { _TableIx = value; }
		}

		public string TableName
		{
			get { return _TableName; }
			set { _TableName = value; }
		}
	}
}
