using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;


namespace SimpleORM
{
	public class ExtractInfo
	{
		protected MethodInfo				_FillMethod;
		protected List<string>			_PropColumns;
		protected List<ExtractInfo>	_SubTypes;
		protected KeyInfo					_PrimaryKeyInfo;
		protected List<KeyInfo>			_ForeignKeysInfo;


		public ExtractInfo()
		{
			_PropColumns = new List<string>();
			_ForeignKeysInfo = new List<KeyInfo>();
		}

		
		public MethodInfo FillMethod
		{
			get { return _FillMethod; }
			set { _FillMethod = value; }
		}

		public List<string> PropColumns
		{
			get { return _PropColumns; }
			set { _PropColumns = value; }
		}

		public List<ExtractInfo> SubTypes
		{
			get { return _SubTypes; }
			set { _SubTypes = value; }
		}

		public KeyInfo PrimaryKeyInfo
		{
			get { return _PrimaryKeyInfo; }
			set { _PrimaryKeyInfo = value; }
		}

		public List<KeyInfo> ForeignKeysInfo
		{
			get { return _ForeignKeysInfo; }
			set { _ForeignKeysInfo = value; }
		}
	}
}
