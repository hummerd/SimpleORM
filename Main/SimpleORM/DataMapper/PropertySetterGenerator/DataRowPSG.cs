using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Data;
using SimpleORM.Attributes;
using SimpleORM.Exception;


namespace SimpleORM.PropertySetterGenerator
{
	public class DataRowPSG : PSGBase, IPropertySetterGenerator
	{
		
		protected static MethodInfo	_GetRowItem		= typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(int) });
		protected static MethodInfo	_GetChildRows	= typeof(DataRow).GetMethod("GetChildRows", new Type[] { typeof(string) });
		
		protected static FieldInfo		_DBNullValue	= typeof(DBNull).GetField("Value");
		protected static MethodInfo	_SetNested		= typeof(DataMapper).GetMethod("FillObjectListNested");
		

		public void GenerateSetterMethod(
			ILGenerator ilOut, 
			Type targetClassType, 
			int schemeId, 
			DataTable schemaTable, 
			GetPropertyMapping getPropertyMapping,
			ExtractInfo extractInfo)
		{
			GenerateSetterMethod(
				ilOut,
				targetClassType,
				schemeId,
				schemaTable,
				getPropertyMapping,
				extractInfo,
				true);
		}


		protected override void CreateExtractScalar(ILGenerator ilOut, Type targetClassType, PropertyInfo prop, DataColumnMapAttribute mapping, DataTable schemaTable, int propIndex)
		{
			int column = schemaTable.Columns.IndexOf(mapping.MappingName);
			if (column < 0)
				return;

			MethodInfo targetProp = targetClassType.GetMethod("set_" + prop.Name);

			Label lblElse = ilOut.DefineLabel();
			Label lblEnd = ilOut.DefineLabel();

			GeneratePropSetterHeader(ilOut, propIndex);

			ilOut.Emit(OpCodes.Bne_Un, lblElse);

			SetterType setterType = GetSetterType(prop, schemaTable.Columns[column].DataType);
			CreateSetNullValue(setterType, ilOut, prop.PropertyType, targetProp);

			ilOut.Emit(OpCodes.Br, lblEnd);
			ilOut.MarkLabel(lblElse);

			CreateSetNotNullValue(ilOut, setterType, prop.PropertyType, targetProp);

			ilOut.MarkLabel(lblEnd);
		}

		protected override void CreateExtractNested(ILGenerator ilOut, Type targetClassType, PropertyInfo prop, DataRelationMapAttribute mapping)
		{
			Type propType = prop.PropertyType;

			if (!typeof(IList).IsAssignableFrom(propType))
				throw new DataMapperException("Cannot set nested objects for collection that does not implement IList (" + prop.Name + ").");

			Type itemType = mapping.ItemType;
			if (itemType == null)
				itemType = GetItemType(propType);

			if (itemType == null)
				throw new DataMapperException("Cannot resolve type of items in collection(" + prop.Name + "). " +
					"Try to set it via ItemType property of DataRelationMapAttribute.");

			GenerateSetNestedProperty(
				ilOut,
				mapping.MappingName,
				propType,
				itemType, 
				prop,
				mapping.NestedSchemeId);
		}

		protected void GeneratePropSetterHeader(ILGenerator ilOut, int propIndex)
		{
			/*
			 *  (0 TargetType obj, 1 DataRow row, 2 DataMapper mapper, 3 List<List<int>> clmns, 4 ref clmnIx)
			 *  int ix = list[columnsIx]['propIndex'];
			 *  if (ix <= 0)
			 *		val = DBNull.Value;
			 *	else
			 *		val = dataRow[ix];
			 *		
			 *  if (DBNull.Value 
			 */

			Label lblIxPositive = ilOut.DefineLabel();
			Label lblAfterGetRowItem = ilOut.DefineLabel();

			ilOut.Emit(OpCodes.Ldarg_3);
			ilOut.Emit(OpCodes.Ldarg, 4);
			ilOut.Emit(OpCodes.Ldind_I4);
			ilOut.EmitCall(OpCodes.Call, _GetSubListItem, null);
			ilOut.Emit(OpCodes.Ldc_I4, propIndex);
			ilOut.EmitCall(OpCodes.Call, _GetListItem, null);

			ilOut.Emit(OpCodes.Stloc_1);
			ilOut.Emit(OpCodes.Ldloc_1);
			ilOut.Emit(OpCodes.Ldc_I4_0);
			ilOut.Emit(OpCodes.Bge, lblIxPositive);

			ilOut.Emit(OpCodes.Ldsfld, _DBNullValue);
			ilOut.Emit(OpCodes.Stloc_0);
			ilOut.Emit(OpCodes.Br, lblAfterGetRowItem);

			ilOut.MarkLabel(lblIxPositive);

			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldloc_1);
			ilOut.EmitCall(OpCodes.Call, _GetRowItem, null);
			ilOut.Emit(OpCodes.Stloc_0);

			ilOut.MarkLabel(lblAfterGetRowItem);

			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Ldsfld, _DBNullValue);
		}


		protected void CreateSetNotNullValue(ILGenerator ilOut, SetterType setterType, Type propType, MethodInfo setProp)
		{
			switch (setterType)
			{
				case SetterType.Enum:
					GenerateSetUnboxedToSubType(ilOut, propType, setProp, null);
					break;

				case SetterType.Value:
					GenerateSetUnboxedToSubType(ilOut, propType, setProp, null);
					break;

				case SetterType.ValueNI:
					GenerateSetConverted(ilOut, propType, setProp);
					break;

				case SetterType.Struct:
					GenerateSetUnboxedToSubType(ilOut, propType, setProp, null);
					break;

				case SetterType.StructNI:
					GenerateSetConverted(ilOut, propType, setProp);
					break;

				case SetterType.Nullable:
					GenerateSetUnboxedToSubType(ilOut, propType, setProp, propType.GetGenericArguments()[0]);
					break;

				case SetterType.NullableNI:
					GenerateConvertedToSubType(ilOut, propType, setProp, propType.GetGenericArguments()[0]);
					break;

				case SetterType.Reference:
					GenerateSetRef(ilOut, propType, setProp);
					break;
			}
		}

		/// <summary>
		/// Helper method. Returns first generic argument type for first generic subtype.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		protected Type GetItemType(Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType)
					break;

				type = type.BaseType;
			}

			if (type == null)
				return null;

			return type.GetGenericArguments()[0];
		}
				
		protected void GenerateConvertedToSubType(ILGenerator ilOut, Type propType, MethodInfo setProp, Type subType)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Ldtoken, subType);
			ilOut.EmitCall(OpCodes.Call, _GetType, null);
			ilOut.EmitCall(OpCodes.Call, _ChangeType, null);
			ilOut.Emit(OpCodes.Unbox_Any, subType);
			ilOut.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void GenerateSetUnboxedToSubType(ILGenerator ilOut, Type propType, MethodInfo setProp, Type subType)
		{
			bool toSubType = subType != null;

			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Unbox_Any, toSubType ? subType : propType);

			if (toSubType)
				ilOut.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));

			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void GenerateSetRef(ILGenerator ilOut, Type propType, MethodInfo setProp)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Castclass, propType);
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void GenerateSetConverted(ILGenerator ilOut, Type propType, MethodInfo setProp)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Ldtoken, propType);
			ilOut.EmitCall(OpCodes.Call, _GetType, null);
			ilOut.EmitCall(OpCodes.Call, _ChangeType, null);
			ilOut.Emit(OpCodes.Unbox_Any, propType);
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		/// <summary>
		/// Generates part of a setter method to set nested list.
		/// </summary>
		/// <param name="ILout"></param>
		/// <param name="getChildRows"></param>
		/// <param name="relationName"></param>
		/// <param name="getType"></param>
		/// <param name="getList"></param>
		/// <param name="setList"></param>
		/// <param name="propType"></param>
		/// <param name="itemType"></param>
		/// <param name="nestedSchemaId"></param>
		/// <param name="setNested"></param>
		protected void GenerateSetNestedProperty(ILGenerator ilOut, string relationName, Type propType, Type itemType, PropertyInfo prop, int nestedSchemaId)
		{
			#region Algorithm
			//DataRow[] drChilds = dr.GetChildRows("RelationName");
			//if (drChilds.Length <= 0)
			//   return;
			//
			//List<MyTest> newList;
			//if (mt.NestedList == null)
			//{
			//   newList = (List<MyTest>)Activator.CreateInstance(typeof(List<MyTest>));
			//   mt.NestedList = newList;
			//}
			//else
			//   newList = mt.NestedList;
			//
			//if (newList is List<MyTest>)
			//   newList.Capacity = drChilds.Length + newList.Count;
			//
			//ExtractNested(mt.NestedList, drChilds, 234);
			#endregion

			#region IL ildisasm
			/*
			.maxstack 3
			.locals init (
			  [0] class [System.Data]System.Data.DataRow[] drChilds,
			  [1] class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> newList)
			L_0000: ldarg.1 
			L_0001: ldstr "RelationName"
			L_0006: callvirt instance class [System.Data]System.Data.DataRow[] [System.Data]System.Data.DataRow::GetChildRows(string)
			L_000b: stloc.0 
			L_000c: ldloc.0 
			L_000d: ldlen 
			L_000e: conv.i4 
			L_000f: ldc.i4.0 
		 * 
			L_0010: bgt.s L_0013
			L_0012: ret
		 * 
			L_0013: ldarg.0 
			L_0014: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
		 * 
			L_0019: brtrue.s L_0039
			L_001b: ldtoken [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			L_0020: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			L_0025: call object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
			L_002a: castclass [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			L_002f: stloc.1 
			L_0030: ldarg.0 
			L_0031: ldloc.1 
			L_0032: callvirt instance void CodeGenerator.MyTest::set_NestedList(class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>)
		   
		   L_0037: br.s L_0040
		 * 
			L_0039: ldarg.0 
			L_003a: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			L_003f: stloc.1 
			
		   L_0040: ldloc.1 
			L_0041: brfalse.s L_0053
			L_0043: ldloc.1 
			L_0044: ldloc.0 
			L_0045: ldlen 
			L_0046: conv.i4 
			L_0047: ldloc.1 
			L_0048: callvirt instance int32 [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>::get_Count()
			L_004d: add 
			L_004e: callvirt instance void [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>::set_Capacity(int32)
		 *	
		   L_0053: ldarg.0 
			L_0054: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			L_0059: ldloc.0 
			L_005a: ldc.i4 0xea
			L_005f: call void CodeGenerator.Program::ExtractNested(class [mscorlib]System.Collections.IList, class [mscorlib]System.Collections.Generic.IEnumerable`1<class [System.Data]System.Data.DataRow>, int32)
			L_0064: ret 
		*/
			#endregion

			MethodInfo getProp = prop.GetGetMethod();
			MethodInfo setProp = prop.GetSetMethod();

			MethodInfo createInst = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) });

			if (itemType == null)
				itemType = propType.GetGenericArguments()[0];

			Label lblElse1 = ilOut.DefineLabel();
			Label lblElse2 = ilOut.DefineLabel();
			Label lblAfterFirstIf = ilOut.DefineLabel();
			Label lblEnd = ilOut.DefineLabel();

			LocalBuilder locRows = ilOut.DeclareLocal(typeof(DataRow[]));
			LocalBuilder loc = ilOut.DeclareLocal(propType);

			ilOut.Emit(OpCodes.Ldarg_0);								//L_0013: ldarg.0 
			ilOut.EmitCall(OpCodes.Callvirt, getProp, null);	//L_0014: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()

			ilOut.Emit(OpCodes.Brtrue, lblElse2);					//L_0019: brtrue.s L_0039
			ilOut.Emit(OpCodes.Ldtoken, propType);					//L_001b: ldtoken [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			ilOut.EmitCall(OpCodes.Call, _GetType, null);			//L_0020: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			ilOut.EmitCall(OpCodes.Call, createInst, null);		//L_0025: call object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
			ilOut.Emit(OpCodes.Castclass, propType);				//L_002a: castclass [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			ilOut.Emit(OpCodes.Stloc, loc);							//L_002f: stloc.1 
			ilOut.Emit(OpCodes.Ldarg_0);								//L_0030: ldarg.0 
			ilOut.Emit(OpCodes.Ldloc, loc);							//L_0031: ldloc.1 
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);	//L_0032: callvirt instance void CodeGenerator.MyTest::set_NestedList(class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>)

			ilOut.Emit(OpCodes.Br, lblAfterFirstIf);				//L_0037: br.s L_0040

			ilOut.MarkLabel(lblElse2);
			ilOut.Emit(OpCodes.Ldarg_0);								//L_0039: ldarg.0 
			ilOut.EmitCall(OpCodes.Callvirt, getProp, null);	//L_003a: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			ilOut.Emit(OpCodes.Stloc, loc);							//L_003f: stloc.1 

			ilOut.MarkLabel(lblAfterFirstIf);

			ilOut.Emit(OpCodes.Ldarg_1);								//L_0000: ldarg.1 
			ilOut.Emit(OpCodes.Ldstr, relationName);				//L_0001: ldstr "RelationName"
			ilOut.EmitCall(OpCodes.Call, _GetChildRows, null);	//L_0006: callvirt instance class [System.Data]System.Data.DataRow[] [System.Data]System.Data.DataRow::GetChildRows(string)
			ilOut.Emit(OpCodes.Stloc, locRows);						//L_000b: stloc.0 
			ilOut.Emit(OpCodes.Ldloc, locRows);						//L_000c: ldloc.0 
			ilOut.Emit(OpCodes.Ldlen);									//L_000d: ldlen 
			ilOut.Emit(OpCodes.Conv_I4);								//L_000e: conv.i4		
			ilOut.Emit(OpCodes.Ldc_I4_0);								//L_000f: ldc.i4.0 

			ilOut.Emit(OpCodes.Bgt, lblElse1);						//L_0010: bgt.s L_0013
			ilOut.Emit(OpCodes.Br, lblEnd);							//L_0012: ret

			ilOut.MarkLabel(lblElse1);

			ilOut.Emit(OpCodes.Ldarg_2);
			ilOut.Emit(OpCodes.Ldarg_0);								//L_0053: ldarg.0 
			ilOut.EmitCall(OpCodes.Callvirt, getProp, null);	//L_0054: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			ilOut.Emit(OpCodes.Ldtoken, itemType);
			ilOut.EmitCall(OpCodes.Call, _GetType, null);
			ilOut.Emit(OpCodes.Ldloc, locRows);						//L_0059: ldloc.0 
			ilOut.Emit(OpCodes.Ldc_I4, nestedSchemaId);			//L_005a: ldc.i4 0xea
			ilOut.EmitCall(OpCodes.Callvirt, _SetNested, null);	//L_005f: call void CodeGenerator.Program::ExtractNested(class [mscorlib]System.Collections.IList, class [mscorlib]System.Collections.Generic.IEnumerable`1<class [System.Data]System.Data.DataRow>, int32)
			ilOut.MarkLabel(lblEnd);									//L_0064: ret 
		}
	}
}
