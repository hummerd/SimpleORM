using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace SimpleORM
{
	public class ExtractInfo
	{
		protected MethodInfo _FillMethod;
		protected List<string> _PropColumns;
		protected Dictionary<Type, ExtractInfo> _RelatedInfo;


		public ExtractInfo()
		{
			_PropColumns = new List<string>();
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

		public Dictionary<Type, ExtractInfo> RelatedInfo
		{
			get { return _RelatedInfo; }
			set { _RelatedInfo = value; }
		}
	}
}
