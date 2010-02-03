﻿using System;
using System.Collections.Generic;
using System.Text;


namespace SimpleORM
{
	public class RefInfo
	{
		protected int[] _TableID;
		protected string _TableName;


		public RefInfo(int[] tableId, string tableName)
		{
			_TableID = tableId;
			_TableName = tableName;
		}


		public int[] TableID
		{
			get
			{
				return _TableID;
			}
			set
			{
				_TableID = value;
			}
		}

		public string TableName
		{
			get
			{
				return _TableName;
			}
			set
			{
				_TableName = value;
			}
		}


		public bool EmptyOrRefersTo(int id, string name)
		{
			if (TableID == null && String.IsNullOrEmpty(TableName))
				return true;

			return RefersTo(id, name);
		}

		public bool RefersTo(int id, string name)
		{
			if ((TableID != null && Array.IndexOf(TableID, id) >= 0) || name == TableName)
			{
				return true;
			}

			return false;
		}
	}
}
