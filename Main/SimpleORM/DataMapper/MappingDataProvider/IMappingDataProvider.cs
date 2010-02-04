using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SimpleORM.Attributes;
using SimpleORM.PropertySetterGenerator;


namespace SimpleORM.MappingDataProvider
{
	public interface IMappingDataProvider
	{
		bool SetConfig(IEnumerable<string> configFiles);
		bool GetExtractInfo(ExtractInfo extractInfo);
	}
}
