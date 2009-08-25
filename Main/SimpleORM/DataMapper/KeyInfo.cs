using System;
using System.Collections.Generic;
using System.Reflection;


namespace SimpleORM
{
	public class KeyInfo
	{
		protected string			_Name;
		protected MethodInfo		_FillMethod;
		protected List<string>	_Columns;
		protected Type				_KeyType;


		public KeyInfo()
		{
			_Columns = new List<string>();
		}


		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}
		
		public MethodInfo FillMethod
		{
			get { return _FillMethod; }
			set { _FillMethod = value; }
		}

		public List<string> Columns
		{
			get { return _Columns; }
			set { _Columns = value; }
		}

		public Type KeyType
		{
			get { return _KeyType; }
			set { _KeyType = value; }			
		}
	}
}
