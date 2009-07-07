using System;


namespace SimpleORM
{
	public interface IPropertySetterGenerator
	{
		void GenerateSetterMethod(System.Reflection.Emit.ILGenerator ilGen, Type targetClassType, int schemeId, System.Data.DataTable schemaTable, GetPropertyMapping getPropertyMapping);
	}
}
