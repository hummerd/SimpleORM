using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SimpleORM.Attributes;
using System.Data;


namespace SimpleORM
{
	/// <summary>
	/// Class that represents information about mapping TargetType with SchemeId.
	/// Also class contains references to generated fill methods (FillMethod dictionary).
	/// </summary>
	public class ExtractInfo : ICloneable
	{
		protected Type							_TargetType;
		protected int							_SchemeId;
		protected Dictionary<Type, MethodInfo>	_FillMethod;
		protected List<MemberExtractInfo>		_PropColumns;
		protected List<RelationExtractInfo>		_SubTypes;
		protected List<RelationExtractInfo>		_ChildTypes;
		protected List<KeyInfo>					_PrimaryKeysInfo;
		protected List<KeyInfo>					_ForeignKeysInfo;
		protected RefInfo				_RefTable;
		protected IList<PropertyInfo>	_Props;
		protected IList<FieldInfo>		_Fields;


		public ExtractInfo(Type targetType, int schemeId)
		{
			_TargetType = targetType;
			_SchemeId = schemeId;
			_FillMethod = new Dictionary<Type, MethodInfo>();
			_PropColumns = new List<MemberExtractInfo>();
			_PrimaryKeysInfo = new List<KeyInfo>();
			_ForeignKeysInfo = new List<KeyInfo>();
			_SubTypes = new List<RelationExtractInfo>();
			_ChildTypes = new List<RelationExtractInfo>();
		}

		
		public Type TargetType
		{
			get
			{
				return _TargetType;
			}
			set
			{
				_TargetType = value;
			}
		}
		
		public int SchemeId
		{
			get
			{
				return _SchemeId;
			}
			set
			{
				_SchemeId = value;
			}
		}

		public Dictionary<Type, MethodInfo> FillMethod
		{
			get { return _FillMethod; }
		}
		
		/// <summary>
		/// List of DB columns that will be mapped to object
		/// (in the same order as it goes in entity type)
		/// First must be enumerated props then fields
		/// </summary>
		public List<MemberExtractInfo> MemberColumns
		{
			get { return _PropColumns; }
			set { _PropColumns = value; }
		}

		//public IList<PropertyInfo> Props
		//{
		//   get
		//   {
		//      return _Props;
		//   }
		//   set
		//   {
		//      _Props = value;
		//   }
		//}

		//public IList<FieldInfo> Fields
		//{
		//   get
		//   {
		//      return _Fields;
		//   }
		//   set
		//   {
		//      _Fields = value;
		//   }
		//}

		public List<RelationExtractInfo> SubTypes
		{
			get { return _SubTypes; }
			set { _SubTypes = value; }
		}

		public List<RelationExtractInfo> ChildTypes
		{
			get { return _ChildTypes; }
			set { _ChildTypes = value; }
		}

		public List<KeyInfo> PrimaryKeyInfo
		{
			get { return _PrimaryKeysInfo; }
			set { _PrimaryKeysInfo = value; }
		}

		public List<KeyInfo> ForeignKeysInfo
		{
			get { return _ForeignKeysInfo; }
			set { _ForeignKeysInfo = value; }
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


		public ExtractInfo Copy()
		{
			return Clone() as ExtractInfo;
		}

		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}

		#endregion

		public List<List<int>> GetSubColumnsIndexes(DataTable table)
		{
			return GetSubColumnsIndexes(table, null);
		}
		
		public List<ExtractInfo> FindByTable(int id, string name)
		{
			List<ExtractInfo> result = new List<ExtractInfo>();

			if (_RefTable.RefersTo(id, name))
			{
				result.Add(this);
				return result;
			}

			foreach (RelationExtractInfo item in ChildTypes)
			{
#warning here can be infinite recursion
				result.AddRange(
					item.ExtractInfo.FindByTable(id, name)
					);
			}

			return result;
		}


		public override string ToString()
		{
			return String.Format(
				"ExtractInfo for {0}, scheme {1}",
				TargetType,
				SchemeId
				);
		}


		protected List<List<int>> GetSubColumnsIndexes(DataTable table, List<List<int>> result)
		{
			if (result == null)
				result = new List<List<int>>();

			result.Add(GetColumnsIndexes(table, MemberColumns));

			foreach (var item in SubTypes)
				item.ExtractInfo.GetSubColumnsIndexes(table, result);

			return result;
		}

		protected List<int> GetColumnsIndexes(DataTable table, List<MemberExtractInfo> columns)
		{
			List<int> result = new List<int>(columns.Count);
			for (int i = 0; i < columns.Count; i++)
				result.Add(table.Columns.IndexOf(columns[i].MapName));

			return result;
		}
	}

	public class RelationExtractInfo : MemberExtractInfo
	{
		protected ExtractInfo	_ExtractInfo;


		public RelationExtractInfo(string mapName, MemberInfo member, ExtractInfo extractInfo)
			: base(mapName, member)
		{
			_ExtractInfo = extractInfo;
		}


		public ExtractInfo ExtractInfo
		{
			get
			{
				return _ExtractInfo;
			}
			set
			{
				_ExtractInfo = value;
			}
		}		
	}

	public class MemberExtractInfo
	{
		protected string			_MapName;
		protected MemberInfo		_Member;


		public MemberExtractInfo(string mapName, MemberInfo member)
		{
			_MapName = mapName;
			_Member = member;
		}


		public string MapName
		{
			get
			{
				return _MapName;
			}
			set
			{
				_MapName = value;
			}
		}

		public MemberInfo Member
		{ 
			get
			{
				return _Member;
			}
			set
			{
				_Member = value;
			}
		}
	}
}
