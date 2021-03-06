﻿using System;
using System.Reflection.Emit;
using System.Data;
using SimpleORM.PropertySetterGenerator;
using System.Reflection;


namespace SimpleORM
{
	public interface IPropertySetterGenerator
	{
		//void GenerateSetterMethod(
		//   ILGenerator ilOut, 
		//   Type targetClassType, 
		//   int schemeId, 
		//   DataTable schemaTable, 
		//   GetPropertyMapping getPropertyMapping,
		//   ExtractorInfoCache extractors
		//   );

		Type DataSourceType
		{ get; }

		void GenerateMethodHeader(ILGenerator ilOut, int methodIndex);

		void CreateExtractScalar(
			ILGenerator ilOut,
			PropertyInfo prop,
			FieldInfo field,
			Type sourceDataType,
			int memberIx);

		void CreateExtractNested(
			ILGenerator ilOut,
			PropertyInfo prop,
			Type relationType,
			string relationName,
			int relationSchemeId
			);

		void GenerateExtractComplex(
			ILGenerator ilOut,
			PropertyInfo prop,
			Type subType,
			MethodInfo subExtract,
			int subExtractMethodIndex);
	}
}
