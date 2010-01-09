using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SimpleORM.Attributes;


namespace SimpleORM
{
	/// <summary>
	/// Class that represents information about mapping TargetType with SchemeId.
	/// Also class contains references to generated fill methods (FillMethod dictionary).
	/// </summary>
	public class ExtractInfo
	{
		protected Type									_TargetType;
		protected int									_SchemeId;
		protected Dictionary<Type, MethodInfo> _FillMethod;
		protected List<MemberExtractInfo>		_PropColumns;
		protected List<RelationExtractInfo>		_SubTypes;
		protected List<RelationExtractInfo>		_ChildTypes;
		protected KeyInfo					_PrimaryKeyInfo;
		protected List<KeyInfo>			_ForeignKeysInfo;
		protected int						_TableID = -1;
		protected string					_TableName;
		protected IList<PropertyInfo> _Props;
		protected IList<FieldInfo>		_Fields;


		public ExtractInfo(Type targetType, int schemeId)
		{
			_TargetType = targetType;
			_SchemeId = schemeId;
			_FillMethod = new Dictionary<Type, MethodInfo>();
			_PropColumns = new List<MemberExtractInfo>();
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
		
		public int TableID
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


		public List<ExtractInfo> FindByTable(int id, string name)
		{
			List<ExtractInfo> result = new List<ExtractInfo>();

			if (id == TableID || name == TableName)
			{
				result.Add(this);
				return result;
			}

			foreach (RelationExtractInfo item in ChildTypes)
			{
				ExtractInfo ei = item.ExtractInfo;
				if (id == ei.TableID || name == ei.TableName)
					result.Add(ei);
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
