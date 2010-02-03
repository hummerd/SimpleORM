using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;


namespace SimpleORM
{
	public class KeyInfo
	{
		protected string		_Name;
		protected Type			_ParentType;
		protected Type			_ChildType;
		protected ExtractInfo	_ParentKeyExtractInfo;
		protected ExtractInfo	_ChildKeyExtractInfo;
		protected Type			_GeneratorSourceType;
		protected List<string>	_ParentColumns;
		protected List<string>	_ChildColumns;
		protected List<List<int>> _ParentColumnIndexes;
		protected List<List<int>> _ChildColumnIndexes;
		protected MethodInfo	_ParentKeyExtractMethod;
		protected MethodInfo	_ChildKeyExtractMethod;
		protected RefInfo		_RefTable;


		public KeyInfo()
		{
			_ParentColumns = new List<string>();
			_ChildColumns = new List<string>();
		}


		public Type ParentType
		{
			get { return _ParentType; }
			set { _ParentType = value; }
		}

		public Type ChildType
		{
			get { return _ChildType; }
			set { _ChildType = value; }
		}

		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}


		public ExtractInfo ParentKeyExtractInfo
		{
			get { return _ParentKeyExtractInfo; }
			set { _ParentKeyExtractInfo = value; InitParentExtractMethod(); }
		}

		public ExtractInfo ChildKeyExtractInfo
		{
			get { return _ChildKeyExtractInfo; }
			set { _ChildKeyExtractInfo = value; InitChildExtractMethod(); }
		}

		public Type GeneratorSourceType
		{
			get { return _GeneratorSourceType; }
			set 
			{ 
				_GeneratorSourceType = value; 
				InitParentExtractMethod();
				InitChildExtractMethod();
			}
		}

		public List<string> ParentColumns
		{
			get { return _ParentColumns; }
			set { _ParentColumns = value; }
		}

		public List<string> ChildColumns
		{
			get { return _ChildColumns; }
			set { _ChildColumns = value; }
		}

		public List<List<int>> ParentColumnIndexes
		{
			get { return _ParentColumnIndexes; }
		}

		public List<List<int>> ChildColumnIndexes
		{
			get { return _ChildColumnIndexes; }
		}

		public RefInfo RefTable
		{
			get
			{
				return _RefTable;
			}
			set
			{
				_RefTable = value;
			}
		}


		public MethodInfo GetParentKeyExtractorMethod()
		{
			return _ParentKeyExtractMethod;
		}

		public MethodInfo GetChildKeyExtractorMethod()
		{
			return _ChildKeyExtractMethod;
		}

		public void InitParentColumnIndexes(DataTable schemeTable)
		{
			_ParentColumnIndexes = ParentKeyExtractInfo.GetSubColumnsIndexes(schemeTable);
		}

		public void InitChildColumnIndexes(DataTable schemeTable)
		{
			_ChildColumnIndexes = ChildKeyExtractInfo.GetSubColumnsIndexes(schemeTable);
		}


		public override bool Equals(object obj)
		{
			KeyInfo ki = obj as KeyInfo;
			if (ki == null)
				return false;

			return 
				ki.ParentType.Equals(ParentType) &&
				ki.ChildType.Equals(ChildType) &&
				ki.Name.Equals(Name);
		}

		public override int GetHashCode()
		{
			return
				ParentType.GetHashCode() ^
				ChildType.GetHashCode() ^
				Name.GetHashCode();
		}


		protected void InitParentExtractMethod()
		{
			if (ParentKeyExtractInfo == null || GeneratorSourceType == null)
				return;

			_ParentKeyExtractMethod = _ParentKeyExtractInfo.FillMethod[GeneratorSourceType];
		}

		protected void InitChildExtractMethod()
		{
			if (ChildKeyExtractInfo == null || GeneratorSourceType == null)
				return;

			_ChildKeyExtractMethod = _ChildKeyExtractInfo.FillMethod[GeneratorSourceType];
		}
	}
}
