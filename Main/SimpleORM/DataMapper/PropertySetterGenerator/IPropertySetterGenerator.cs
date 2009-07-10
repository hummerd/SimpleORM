using System;
using System.Reflection.Emit;
using System.Data;


namespace SimpleORM
{
	public interface IPropertySetterGenerator
	{
		void GenerateSetterMethod(
			ILGenerator ilGen, 
			Type targetClassType, 
			int schemeId, 
			DataTable schemaTable, 
			GetPropertyMapping getPropertyMapping,
			ExtractInfo extractInfo);
	}
}
