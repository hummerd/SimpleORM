﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using SimpleORM.Exception;


namespace SimpleORM.PropertySetterGenerator
{
	public class DataMapperCodeGenerator
	{
		protected ModuleBuilder _ModuleBuilder;
		protected Dictionary<Type, IPropertySetterGenerator> _SetterGenerators;
		protected int _MethodIndex;


		public DataMapperCodeGenerator(Dictionary<Type, IPropertySetterGenerator> setterGenerators, ModuleBuilder moduleBuilder)
		{
			if (setterGenerators == null)
			{
				_SetterGenerators = new Dictionary<Type, IPropertySetterGenerator>(2);
				_SetterGenerators.Add(DataTablePSG.TypeOfDataSource, new DataTablePSG());
				_SetterGenerators.Add(DataReaderPSG.TypeOfDataSource, new DataReaderPSG());
			}
			else
			{
				_SetterGenerators = setterGenerators;
			}

			_ModuleBuilder = moduleBuilder;
		}


		public int GeneratedMethodCount
		{
			get
			{
				return _MethodIndex;
			}
		}


		/// <summary>
		/// Generates setter method using xml config or type metadata (attributes).
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <returns></returns>
		public ExtractInfo GenerateSetterMethod(
			ExtractInfo extractInfo, 
			DataTable dtSource, 
			Type generatorSourceType)
		{
			return GenerateSetterMethod(extractInfo, dtSource, generatorSourceType, 0);
		}


		/// <summary>
		/// Generates setter method using xml config or type metadata (attributes).
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <returns></returns>
		protected ExtractInfo GenerateSetterMethod(
			ExtractInfo extractInfo, 
			DataTable dtSource, 
			Type generatorSourceType, 
			int extractLevel)
		{
			//Method alredy exists
			if (extractInfo.FillMethod.ContainsKey(generatorSourceType))
				return extractInfo;

			Debug.WriteLine(string.Format(
				new String('\t', extractLevel) + "Creating method for {0}, source {1}",
				extractInfo,
				generatorSourceType.Name));

			IPropertySetterGenerator methodGenerator = _SetterGenerators[generatorSourceType];

			//Generating Type and method declaration
			TypeBuilder typeBuilder = CreateAssemblyType(extractInfo.TargetType, extractInfo.SchemeId, generatorSourceType);
			MethodBuilder methodBuilder = GenerateSetterMethodDefinition(
				extractInfo.TargetType, typeBuilder, methodGenerator.DataSourceType);

			extractInfo.FillMethod[methodGenerator.DataSourceType] = methodBuilder;
			extractInfo.MethodIndex[methodGenerator.DataSourceType] = _MethodIndex++;

			ILGenerator ilGen = methodBuilder.GetILGenerator();

			//First process complex types
			foreach (RelationExtractInfo item in extractInfo.SubTypes)
			{
				GenerateSetterMethod(
					item.RelatedExtractInfo,
					dtSource,
					generatorSourceType,
					extractLevel + 1
					);
			}

			//Generate method body
			GenerateSetterMethod(ilGen, methodGenerator, extractInfo, dtSource, extractLevel);

			Type type = typeBuilder.CreateType();

			//if (extractInfo.FillMethod.ContainsKey(methodGenerator.DataSourceType))
			//    throw new DataMapperException(String.Format(
			//        "Method for type {0}, scheme {1}, source {2} generated once again.",
			//        extractInfo.TargetType,
			//        extractInfo.SchemeId,
			//        methodGenerator.DataSourceType
			//        ));

			extractInfo.FillMethod[methodGenerator.DataSourceType] =
				type.GetMethod("SetProps_" + extractInfo.TargetType);

			Debug.WriteLine(string.Format(
				new String('\t', extractLevel) + "Done with creating method for {0}",
				extractInfo));

			return extractInfo;
		}

		/// <summary>
		/// Generates setter method using xml config or type metadata (attributes).
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="schemeId"></param>
		/// <param name="dtSource"></param>
		/// <returns></returns>
		protected void GenerateSetterMethod(
			ILGenerator ilGen, 
			IPropertySetterGenerator methodGenerator, 
			ExtractInfo extractInfo, 
			DataTable dtSource, 
			int extractLevel)
		{
			methodGenerator.GenerateMethodHeader(
				ilGen,
				extractInfo.MethodIndex[methodGenerator.DataSourceType]);

			int propIx = 0;
			foreach (MemberExtractInfo mei in extractInfo.MemberColumns)
			{
				int columnIx = dtSource.Columns.IndexOf(mei.MapName);
				if (columnIx < 0)
				{
					Debug.WriteLine(string.Format(
						"Warning! Column {0} that was defined in mapping does not exists. No mapping code will be generated for member {1}",
						mei.MapName,
						mei.Member.Name));
					propIx++;
					continue;
				}

				Debug.WriteLine(string.Format(
					new String('\t', extractLevel) + "\tGenerating code that fills member {0}, index {1} with source type {2}",
					mei.Member,
					propIx,
					dtSource.Columns[columnIx].DataType
					));

				methodGenerator.CreateExtractScalar(
					ilGen,
					mei.Member as PropertyInfo,
					mei.Member as FieldInfo,
					dtSource.Columns[columnIx].DataType,
					propIx++
					);
			}

			foreach (RelationExtractInfo rei in extractInfo.ChildTypes)
			{
				Debug.WriteLine(string.Format(
					new String('\t', extractLevel) + "\tGenerating code that fills member {0} with child {1}",
					rei.Member,
					rei.RelatedExtractInfo.TargetType
					));

				methodGenerator.CreateExtractNested(
					ilGen,
					rei.Member as PropertyInfo,
					rei.RelatedExtractInfo.TargetType,
					rei.MapName,
					rei.RelatedExtractInfo.SchemeId
					);
			}

			foreach (RelationExtractInfo rei in extractInfo.SubTypes)
			{
				methodGenerator.GenerateExtractComplex(
					ilGen,
					rei.Member as PropertyInfo,
					rei.RelatedExtractInfo.TargetType,
					rei.RelatedExtractInfo.FillMethod[methodGenerator.DataSourceType],
					rei.RelatedExtractInfo.MethodIndex[methodGenerator.DataSourceType]
					);
			}

			ilGen.Emit(OpCodes.Ldloc_2);
			ilGen.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Creates dynamic assembly for holding generated type with setter methods.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <returns></returns>
		protected TypeBuilder CreateAssemblyType(
			Type targetClassType,
			int schemeId,
			Type generatorSourceType)
		{
			string prefix = "DataPropertySetter.";
			if (targetClassType.FullName.StartsWith(prefix))
				prefix = String.Empty;

			string className = prefix + targetClassType.FullName + "." + generatorSourceType.Name + "_" + schemeId;
			string newClassName = className;
			int i = 0;
			while (_ModuleBuilder.GetType(newClassName) != null)
				newClassName = className + i++;

			return _ModuleBuilder.DefineType(newClassName, TypeAttributes.Class | TypeAttributes.Public);
		}

		/// <summary>
		/// Creates method definition for holding setter method.
		/// </summary>
		/// <param name="targetClassType"></param>
		/// <param name="typeBuilder"></param>
		/// <returns></returns>
		protected MethodBuilder GenerateSetterMethodDefinition(
			Type targetClassType, 
			TypeBuilder typeBuilder,
			Type dataSourceType)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("SetProps_" + targetClassType,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard, 
				typeof(bool),
				new Type[] { 
					targetClassType, 
					dataSourceType, 
					typeof(DataMapper),
					typeof(List<List<int>>), 
					Type.GetType("System.Int32&"),
					typeof(object[])
				});

			methodBuilder.DefineParameter(1, ParameterAttributes.In, "target");
			methodBuilder.DefineParameter(2, ParameterAttributes.In, "row");
			methodBuilder.DefineParameter(3, ParameterAttributes.In, "mapper");
			methodBuilder.DefineParameter(4, ParameterAttributes.In, "columnsList");
			methodBuilder.DefineParameter(5, ParameterAttributes.Out, "columnsIx");
			methodBuilder.DefineParameter(6, ParameterAttributes.In, "createdObjects");

			return methodBuilder;
		}
	}
}
