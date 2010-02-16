using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using SimpleORM.Exception;


namespace SimpleORM
{
	/// <summary>
	/// Class that represents information about mapping TargetType with SchemeId.
	/// Also class contains references to generated fill methods (FillMethod dictionary).
	/// </summary>
	public class ExtractInfo : ICloneable
	{
		protected MethodInfo					_LinkMethod;
		protected Type							_TargetType;
		protected int							_SchemeId;
		protected Dictionary<Type, MethodInfo>	_FillMethod;
		protected Dictionary<Type, int>			_MethodIndex;
		protected List<MemberExtractInfo>		_PropColumns;
		protected List<RelationExtractInfo>		_SubTypes;
		protected List<RelationExtractInfo>		_ChildTypes;
		protected List<RelationExtractInfo>		_RelationsFromParent;
		protected RefInfo						_RefTable;
		protected IList<PropertyInfo>			_Props;
		protected IList<FieldInfo>				_Fields;


		public ExtractInfo(Type targetType, int schemeId)
		{
			_TargetType = targetType;
			_SchemeId = schemeId;
			_FillMethod = new Dictionary<Type, MethodInfo>();
			_MethodIndex = new Dictionary<Type, int>();
			_PropColumns = new List<MemberExtractInfo>();
			_RelationsFromParent = new List<RelationExtractInfo>();
			_SubTypes = new List<RelationExtractInfo>();
			_ChildTypes = new List<RelationExtractInfo>();
		}


		public MethodInfo LinkMethod
		{
			get
			{
				return _LinkMethod;
			}
			set
			{
				_LinkMethod = value;
			}
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

		public Dictionary<Type, int> MethodIndex
		{
			get { return _MethodIndex; }
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

		public List<RelationExtractInfo> RelationsFromParent
		{
			get { return _RelationsFromParent; }
			set { _RelationsFromParent = value; }
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


		public void ForAllTree(Action<ExtractInfo> action)
		{
			ForAllTree(new List<ExtractInfo>(), action);
		}

		public void ResolveForeign()
		{
			ResolveForeign(new List<ExtractInfo>());
		}

		public bool CheckTableIndex()
		{
			var allEI = GetWholeChildTree();
			for (int i = 0; i < allEI.Count; i++)
			{
				if (!allEI[i].RefTable.IsEmpty())
					return true;
			}

			return false;
		}

		public List<KeyInfo> GetPrimaryKeys()
		{
			return GetKeys(true);
		}

		public List<KeyInfo> GetForeignKeys()
		{
			return GetKeys(false);
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
			var subTree = GetWholeSubTree();
			var result = new List<List<int>>();

			for (int i = 0; i < subTree.Count; i++)
			{
				result.Add(subTree[i].GetColumnsIndexes(table));
			}

			return result;
		}
		
		public List<ExtractInfo> FindByTable(int id, string name)
		{
			List<ExtractInfo> result = new List<ExtractInfo>();
			List<ExtractInfo> all = GetWholeChildTree();

			foreach (ExtractInfo item in all)
			{
				if (item.RefTable.RefersTo(id, name))
					result.Add(item);
			}

			return result;
		}

		public List<ExtractInfo> GetWholeChildTree()
		{
			List<ExtractInfo> result = new List<ExtractInfo>();
			result.Add(this);
			
			foreach (RelationExtractInfo item in ChildTypes)
			{
				if (!result.Contains(item.RelatedExtractInfo))
					result.AddRange(
						item.RelatedExtractInfo.GetWholeChildTree());
			}

			return result;
		}

		public List<ExtractInfo> GetWholeSubTree()
		{
			List<ExtractInfo> result = new List<ExtractInfo>();
			result.Add(this);

			foreach (RelationExtractInfo item in SubTypes)
			{
				if (!result.Contains(item.RelatedExtractInfo))
					result.AddRange(
						item.RelatedExtractInfo.GetWholeSubTree());
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			ExtractInfo ei = obj as ExtractInfo;
			if (ei == null)
				return false;

			return 
				ei.TargetType == TargetType &&
				ei.SchemeId == SchemeId;
		}

		public override int GetHashCode()
		{
			return 
				SchemeId ^
				TargetType.GetHashCode();
		}

		public override string ToString()
		{
			return String.Format(
				"ExtractInfo for {0}, scheme {1}",
				TargetType,
				SchemeId
				);
		}


		protected void ForAllTree(List<ExtractInfo> resolved, Action<ExtractInfo> action)
		{
			if (resolved.Contains(this))
				return;

			resolved.Add(this);
			action(this);

			foreach (var item in ChildTypes)
			{
				action(item.RelatedExtractInfo);
			}

			foreach (var item in SubTypes)
			{
				action(item.RelatedExtractInfo);
			}
		}

		protected void ResolveForeign(List<ExtractInfo> resolved)
		{
			if (resolved.Contains(this))
				return;

			resolved.Add(this);

			foreach (var item in ChildTypes)
			{
				var ei = item.RelatedExtractInfo;
				ei.RelationsFromParent.Add(item);
				ei.ResolveForeign(resolved);
			}

			foreach (var item in SubTypes)
			{
				item.RelatedExtractInfo.ResolveForeign(resolved);
			}
		}

		protected List<KeyInfo> GetKeys(bool primary)
		{
			List<KeyInfo> result = new List<KeyInfo>(ChildTypes.Count);
			var rels = primary ? ChildTypes : RelationsFromParent;

			foreach (var item in rels)
			{
				var key = item.KeyInfo;
				if (key == null)
					throw new DataMapperException("Can not fin key for relation " + item.MapName);

				if (!result.Contains(key))
					result.Add(key);
			}

			return result;
		}

		protected List<int> GetColumnsIndexes(DataTable table)
		{
			var mc = MemberColumns;
			List<int> result = new List<int>(mc.Count);
			for (int i = 0; i < mc.Count; i++)
				result.Add(table.Columns.IndexOf(mc[i].MapName));

			return result;
		}
	}

	public class RelationExtractInfo : MemberExtractInfo
	{
		protected ExtractInfo _RelatedExtractInfo;
		protected KeyInfo _PrimaryKeyInfo;


		public RelationExtractInfo(string mapName, MemberInfo member, ExtractInfo relatedExtractInfo, KeyInfo primaryKey)
			: base(mapName, member)
		{
			_RelatedExtractInfo = relatedExtractInfo;
			_PrimaryKeyInfo = primaryKey;
		}


		public ExtractInfo RelatedExtractInfo
		{
			get
			{
				return _RelatedExtractInfo;
			}
			set
			{
				_RelatedExtractInfo = value;
			}
		}

		public KeyInfo KeyInfo
		{
			get
			{
				return _PrimaryKeyInfo;
			}
			set
			{
				_PrimaryKeyInfo = value;
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
