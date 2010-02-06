using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using SimpleORM.Exception;
using SimpleORM.MappingDataProvider;
using SimpleORM.PropertySetterGenerator;


namespace SimpleORM
{
	public class MappingGenerator
	{
		protected readonly ExtractorInfoCache _ExtractInfoCache = new ExtractorInfoCache();

		protected string _GeneratedFileName;
		protected ModuleBuilder _ModuleBuilder;
		protected AssemblyBuilder _AsmBuilder;

		protected MappingProvider _MappingProvider;
		protected DataMapperCodeGenerator _SetterMethodGenerator;
		protected KeyClassGenerator _KeyGenerator;
		protected LinkObjectsMethodGenerator _LinkMethodGenerator;
		Dictionary<Type, IPropertySetterGenerator> _SetterGenerators;


		public MappingGenerator(Dictionary<Type, IPropertySetterGenerator> setterGenerators)
		{
			_MappingProvider = new MappingProvider(new List<IMappingDataProvider>{
				new XmlMappingDataProvider(),
				new AttMappingDataProvider()
			});

			_SetterGenerators = setterGenerators;
		}


		public int GeneratedMethodCount
		{
			get
			{
				return _SetterMethodGenerator.GeneratedMethodCount;
			}
		}

		public string GeneratedFileName
		{
			get
			{
				return _GeneratedFileName;
			}
			set
			{
				if (_ModuleBuilder != null)
					throw new InvalidOperationException("Can not set GeneratedFileName after dynamic module creation");

				_GeneratedFileName = value;
			}
		}


		public void SaveGeneratedAsm()
		{
			if (String.IsNullOrEmpty(_GeneratedFileName))
				throw new InvalidOperationException("To save generated file you must set GeneratedFileName before first call to FillObject");

			if (_AsmBuilder != null)
				_AsmBuilder.Save(_GeneratedFileName);
		}

		public void SetConfig(IEnumerable<string> configFiles)
		{
			_MappingProvider.SetConfig(configFiles);
		}

		/// <summary>
		/// Clears generated mapping
		/// </summary>
		public void ClearCache()
		{
			_ExtractInfoCache.Clear();
			_ModuleBuilder = null;
		}


		public ExtractInfo CreateExtractInfoWithMethod(
			Type targetClassType,
			int schemeId,
			DataTable dtSource,
			Type generatorSourceType)
		{
			return CreateExtractInfoWithMethod(targetClassType, schemeId, dtSource, generatorSourceType, true);
		}

		public ExtractInfo CreateExtractInfoWithMethod(
			Type targetClassType, 
			int schemeId, 
			DataTable dtSource, 
			Type generatorSourceType, 
			bool createNamespace)
		{
			ExtractInfo result = CreateExtractInfo(targetClassType, schemeId);
			GenerateSetterMethod(result, dtSource, generatorSourceType, createNamespace);
			return result;
		}

		public ExtractInfo CreateExtractInfo(Type targetClassType, int schemeId)
		{
			return CreateExtractInfo(targetClassType, schemeId, 0);
		}

		/// <summary>
		/// Generates setter method using extractInfo.
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
			return GenerateSetterMethod(extractInfo, dtSource, generatorSourceType, true);
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
			Type generatorSourceType,
			bool createNamespace)
		{
			CreateModule();
			_SetterMethodGenerator.GenerateSetterMethod(extractInfo, dtSource, generatorSourceType, createNamespace);
			GenerateKeys(extractInfo, dtSource, generatorSourceType, true);
			GenerateKeys(extractInfo, dtSource, generatorSourceType, false);
			if (extractInfo.ChildTypes.Count > 0)
				_LinkMethodGenerator.GenerateLinkMethod(extractInfo, createNamespace);

			return extractInfo;
		}


		protected void CreateModule()
		{
			if (_ModuleBuilder != null)
				return;

			bool useFile = !String.IsNullOrEmpty(_GeneratedFileName);
			AssemblyName asmName = new AssemblyName("DataPropertySetterAsm_" + Guid.NewGuid());

			_AsmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				asmName, useFile ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);

			if (useFile)
				_ModuleBuilder = _AsmBuilder.DefineDynamicModule("DataPropertySetterMod", _GeneratedFileName);
			else
				_ModuleBuilder = _AsmBuilder.DefineDynamicModule("DataPropertySetterMod");

			_KeyGenerator = new KeyClassGenerator(_ModuleBuilder);
			_SetterMethodGenerator = new DataMapperCodeGenerator(_SetterGenerators, _ModuleBuilder);
			_LinkMethodGenerator = new LinkObjectsMethodGenerator(_ModuleBuilder);
		}

		protected ExtractInfo CreateExtractInfo(Type targetClassType, int schemeId, int extractLevel)
		{
			//Check cache
			ExtractInfo result;
			if (_ExtractInfoCache.TryGetExtractInfo(targetClassType, schemeId, out result))
				return result;

			result = new ExtractInfo(targetClassType, schemeId);
			_ExtractInfoCache.Add(targetClassType, schemeId, result);
			_MappingProvider.GetExtractInfo(result);
	
			//resolving nested types
			foreach (var item in result.SubTypes)
			{
				if (item.RelatedExtractInfo.TargetType == null)
					item.RelatedExtractInfo.TargetType = ReflectionHelper.GetReturnType(item.Member);
			}

			foreach (var item in result.ChildTypes)
			{
				if (item.RelatedExtractInfo.TargetType == null)
				{
					item.RelatedExtractInfo.TargetType = ReflectionHelper.GetListItemType(
						ReflectionHelper.GetReturnType(item.Member));
					item.KeyInfo.ChildType = item.RelatedExtractInfo.TargetType;
				}

				if (item.RelatedExtractInfo.TargetType == null)
					throw new DataMapperException("Cannot resolve type of items in collection(" + item.Member.Name + "). " +
						"Try to set it via ItemType property of DataRelationMapAttribute.");				
			}

			//distinct same keys
			foreach (var item1 in result.ChildTypes)
			{
				foreach (var item2 in result.ChildTypes)
				{
					if (item1.KeyInfo.Equals(item2.KeyInfo))
						item2.KeyInfo = item1.KeyInfo;
				}
			}

			DumpExtractInfo(result, extractLevel);

			//fill child types (recursive)
			foreach (var item in result.SubTypes)
			{
				item.RelatedExtractInfo = CreateExtractInfo(
					item.RelatedExtractInfo.TargetType,
					item.RelatedExtractInfo.SchemeId,
					extractLevel + 1);
			}

			foreach (var item in result.ChildTypes)
			{
				item.RelatedExtractInfo = CreateExtractInfo(
					item.RelatedExtractInfo.TargetType,
					item.RelatedExtractInfo.SchemeId,
					extractLevel + 1);
			}

			//fill foreign keys
			if (extractLevel == 0)
			{
				List<ExtractInfo> allEI = result.GetWholeChildTree();
				for (int i = 0; i < allEI.Count; i++)
				{
					var childEI = allEI[i];
					for (int j = 0; j < allEI.Count; j++)
					{
						var parentEI = allEI[j];
						for (int k = 0; k < parentEI.ChildTypes.Count; k++)
						{
							if (parentEI.ChildTypes[k].RelatedExtractInfo.TargetType == childEI.TargetType)
								childEI.RelationsFromParent.Add(parentEI.ChildTypes[k]);
						}
					}
				}
			}

			return result;
		}

		protected void GenerateKeys(ExtractInfo extractInfo, DataTable dtSource, Type generatorSourceType, bool primary)
		{
			List<RelationExtractInfo> rels = primary ?
				extractInfo.ChildTypes:
				extractInfo.RelationsFromParent;

			foreach (var rei in rels)
			{
				GenerateKey(
					rei.KeyInfo,
					extractInfo.TargetType,
					extractInfo.SchemeId,
					dtSource,
					generatorSourceType);
			}
		}

		protected void GenerateKey(KeyInfo keyInfo, Type targetType, int schemeId, DataTable dtSource, Type generatorSourceType)
		{
			if (keyInfo.ParentKeyExtractInfo != null)
				return;

			string keyClass = targetType + "." + keyInfo.Name;
			int childSchemeId = schemeId == 0 ? int.MinValue : -schemeId;

			Type keyType = _KeyGenerator.GenerateKeyType(
				keyClass,
				dtSource,
				keyInfo.ParentColumns,
				keyInfo.ChildColumns,
				schemeId,
				childSchemeId
				);

			ExtractInfo primaryExtractInfo = CreateExtractInfoWithMethod(
				keyType,
				schemeId,
				dtSource,
				generatorSourceType,
				false
				);
			
			ExtractInfo foreignExtractInfo = primaryExtractInfo.Copy();
			foreignExtractInfo.SchemeId = childSchemeId;
			foreignExtractInfo.MemberColumns = CreateExtractInfo(keyType, childSchemeId).MemberColumns;

			keyInfo.GeneratorSourceType = generatorSourceType;
			keyInfo.ParentKeyExtractInfo = primaryExtractInfo;
			keyInfo.ChildKeyExtractInfo = foreignExtractInfo;

			return;
		}


		[Conditional("DEBUG")]
		protected void DumpExtractInfo(ExtractInfo extractInfo, int extractLevel)
		{
			Debug.WriteLine(string.Format(
				new string('\t', extractLevel) + "Creating {0}",
				extractInfo));

			foreach (var item in extractInfo.MemberColumns)
			{
				Debug.WriteLine(string.Format(
					new string('\t', extractLevel) + "\tMap {0} {1,-25} to column {2}",
					item.Member.MemberType,
					item.Member.Name,
					item.MapName
					));
			}

			foreach (var item in extractInfo.ChildTypes)
			{
				Debug.WriteLine(string.Format(
					new string('\t', extractLevel) + "\tMap property {0,-25} to relation {1} (collection of {2})",
					item.Member.Name,
					item.MapName,
					item.RelatedExtractInfo.TargetType
					));
			}

			foreach (var item in extractInfo.SubTypes)
			{
				Debug.WriteLine(string.Format(
					new string('\t', extractLevel) + "\tMap property {0,-25} to complex tpye {1}",
					item.Member.Name,
					item.RelatedExtractInfo.TargetType
					));
			}
	
			Debug.WriteLine(string.Format(
				new string('\t', extractLevel) + "Done with creating {0}",
				extractInfo));
		}
	}
}
