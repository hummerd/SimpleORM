using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Data;
using SimpleORM.Attributes;
using SimpleORM.Exception;


namespace SimpleORM
{
	public class PropertySetterGenerator : IPropertySetterGenerator
	{
		protected enum SetterType
		{
			Enum,
			Value,
			ValueNI,
			Struct,
			StructNI,
			Nullable,
			NullableNI,
			Reference
		}


		protected static MethodInfo	_GetRowItem		= typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(int) });
		protected static MethodInfo	_GetChildRows	= typeof(DataRow).GetMethod("GetChildRows", new Type[] { typeof(string) });
		protected static MethodInfo	_GetType			= typeof(Type).GetMethod("GetTypeFromHandle");
		protected static MethodInfo	_ChangeType		= typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
		protected static FieldInfo		_DBNullValue	= typeof(DBNull).GetField("Value");
		protected static MethodInfo	_SetNested		= typeof(DataMapper).GetMethod("FillObjectListNested");
		

		public void GenerateSetterMethod(ILGenerator ilGen, Type targetClassType, int schemeId, DataTable schemaTable, GetPropertyMapping getPropertyMapping)
		{
			/* Setter method algorithm
			 * 
			 * object val = dr[0];
			 * if (val == DBNull.Value) 
			 *		obj.Prop1 = default(int);
			 *	else
			 *		obj.Prop1 = (int)val;
			 * 
			 * val = dr[1];
			 * if (val == DBNull.Value) 
			 *		obj.Prop2 = default(string);
			 *	else
			 *		obj.Prop2 = (string)val;
			 * 
			 * ExtractNested(obj.Prop4, ..., ...);
			 * 
			 */

			PropertyInfo[] props = targetClassType.GetProperties();

			foreach (PropertyInfo prop in props)
			{
				DataMapAttribute mapping = getPropertyMapping(prop, schemeId);
				if (mapping == null)
					continue;

				if (mapping is DataColumnMapAttribute)
					CreateExtractScalar(targetClassType, prop, ilGen, mapping as DataColumnMapAttribute, schemaTable);
				else
					CreateExtractNested(targetClassType, prop, ilGen, mapping as DataRelationMapAttribute);
			}

			ilGen.Emit(OpCodes.Ret);
		}


		protected void CreateExtractScalar(Type targetClassType, PropertyInfo prop, ILGenerator ilGen, DataColumnMapAttribute mapping, DataTable schemaTable)
		{
			int column = schemaTable.Columns.IndexOf(mapping.MappingName);
			if (column < 0)
				return;

			MethodInfo targetProp = targetClassType.GetMethod("set_" + prop.Name);

			Label lblElse = ilGen.DefineLabel();
			Label lblEnd = ilGen.DefineLabel();

			GenerateMethodHeader(ilGen, column);

			ilGen.Emit(OpCodes.Bne_Un, lblElse);

			SetterType setterType = GetSetterType(prop, schemaTable.Columns[column].DataType);
			CreateSetNullValue(setterType, ilGen, prop.PropertyType, targetProp);

			ilGen.Emit(OpCodes.Br, lblEnd);
			ilGen.MarkLabel(lblElse);

			CreateSetNotNullValue(setterType, ilGen, prop.PropertyType, targetProp);

			ilGen.MarkLabel(lblEnd);
		}

		protected void CreateExtractNested(Type targetClassType, PropertyInfo prop, ILGenerator ilGen, DataRelationMapAttribute mapping)
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
				ilGen,
				mapping.MappingName,
				propType,
				itemType, 
				targetClassType.GetMethod("set_" + prop.Name),
				targetClassType.GetMethod("get_" + prop.Name),
				mapping.NestedSchemeId);
		}


		protected void GenerateMethodHeader(ILGenerator ilOut, int column)
		{
			ilOut.DeclareLocal(typeof(object));
			
			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldc_I4, column);
			ilOut.EmitCall(OpCodes.Call, _GetRowItem, null);
			ilOut.Emit(OpCodes.Stloc_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Ldsfld, _DBNullValue);
		}

		protected SetterType GetSetterType(PropertyInfo prop, Type columnType)
		{
			Type propType = prop.PropertyType;
			bool isNullable = propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>);

			if (isNullable && columnType == propType.GetGenericArguments()[0])
				return SetterType.Nullable;
			else if (isNullable && columnType != propType.GetGenericArguments()[0])
				return SetterType.NullableNI;
			else if (propType.IsValueType && columnType == propType)
			{
				if (propType.IsPrimitive)
					return SetterType.Value;
				else
					return SetterType.Struct;
			}
			else if (propType.IsValueType && columnType != propType)
			{
				if (propType.IsEnum)
					return SetterType.Enum;
				else if (propType.IsPrimitive)
					return SetterType.ValueNI;
				else
					return SetterType.StructNI;
			}
			else
				return SetterType.Reference;
		}

		protected void CreateSetNullValue(SetterType setterType, ILGenerator ilGen, Type propType, MethodInfo setProp)
		{
			switch (setterType)
			{
				case SetterType.Enum:
					GenerateSetDefault(ilGen, setProp);
					break;

				case SetterType.Value:
					GenerateSetDefault(ilGen, setProp);
					break;

				case SetterType.ValueNI:
					GenerateSetDefault(ilGen, setProp);
					break;

				case SetterType.Struct:
					GenerateSetEmpty(ilGen, propType, setProp);
					break;

				case SetterType.StructNI:
					GenerateSetEmpty(ilGen, propType, setProp);
					break;

				case SetterType.Nullable:
					GenerateSetEmpty(ilGen, propType, setProp);
					break;

				case SetterType.NullableNI:
					GenerateSetEmpty(ilGen, propType, setProp);
					break;

				case SetterType.Reference:
					GenerateSetNull(ilGen, setProp);
					break;
			}
		}

		protected void CreateSetNotNullValue(SetterType setterType, ILGenerator ilGen, Type propType, MethodInfo setProp)
		{
			switch (setterType)
			{
				case SetterType.Enum:
					GenerateSetUnboxedToSubType(ilGen, propType, setProp, null);
					break;

				case SetterType.Value:
					GenerateSetUnboxedToSubType(ilGen, propType, setProp, null);
					break;

				case SetterType.ValueNI:
					GenerateSetConverted(ilGen, propType, setProp);
					break;

				case SetterType.Struct:
					GenerateSetUnboxedToSubType(ilGen, propType, setProp, null);
					break;

				case SetterType.StructNI:
					GenerateSetConverted(ilGen, propType, setProp);
					break;

				case SetterType.Nullable:
					GenerateSetUnboxedToSubType(ilGen, propType, setProp, propType.GetGenericArguments()[0]);
					break;

				case SetterType.NullableNI:
					GenerateConvertedToSubType(ilGen, propType, setProp, propType.GetGenericArguments()[0]);
					break;

				case SetterType.Reference:
					GenerateSetRef(ilGen, propType, setProp);
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

		
		protected void GenerateSetEmpty(ILGenerator ilOut, Type propType, MethodInfo setProp)
		{
			LocalBuilder loc = ilOut.DeclareLocal(propType);

			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloca, loc);
			ilOut.Emit(OpCodes.Initobj, propType);
			ilOut.Emit(OpCodes.Ldloc, loc);
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void GenerateSetNull(ILGenerator ilOut, MethodInfo setProp)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldnull);
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void GenerateSetDefault(ILGenerator ilOut, MethodInfo setProp)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldc_I4, 0);
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
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
		protected void GenerateSetNestedProperty(ILGenerator ILout, string relationName, Type propType, Type itemType, MethodInfo setProp, MethodInfo getProp, int nestedSchemaId)
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

			MethodInfo createInst = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) });

			if (itemType == null)
				itemType = propType.GetGenericArguments()[0];

			Label lblElse1 = ILout.DefineLabel();
			Label lblElse2 = ILout.DefineLabel();
			Label lblAfterFirstIf = ILout.DefineLabel();
			Label lblEnd = ILout.DefineLabel();

			LocalBuilder locRows = ILout.DeclareLocal(typeof(DataRow[]));
			LocalBuilder loc = ILout.DeclareLocal(propType);

			ILout.Emit(OpCodes.Ldarg_0);								//L_0013: ldarg.0 
			ILout.EmitCall(OpCodes.Callvirt, getProp, null);	//L_0014: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()

			ILout.Emit(OpCodes.Brtrue, lblElse2);					//L_0019: brtrue.s L_0039
			ILout.Emit(OpCodes.Ldtoken, propType);					//L_001b: ldtoken [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			ILout.EmitCall(OpCodes.Call, _GetType, null);			//L_0020: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
			ILout.EmitCall(OpCodes.Call, createInst, null);		//L_0025: call object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
			ILout.Emit(OpCodes.Castclass, propType);				//L_002a: castclass [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>
			ILout.Emit(OpCodes.Stloc, loc);							//L_002f: stloc.1 
			ILout.Emit(OpCodes.Ldarg_0);								//L_0030: ldarg.0 
			ILout.Emit(OpCodes.Ldloc, loc);							//L_0031: ldloc.1 
			ILout.EmitCall(OpCodes.Callvirt, setProp, null);	//L_0032: callvirt instance void CodeGenerator.MyTest::set_NestedList(class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest>)

			ILout.Emit(OpCodes.Br, lblAfterFirstIf);				//L_0037: br.s L_0040

			ILout.MarkLabel(lblElse2);
			ILout.Emit(OpCodes.Ldarg_0);								//L_0039: ldarg.0 
			ILout.EmitCall(OpCodes.Callvirt, getProp, null);	//L_003a: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			ILout.Emit(OpCodes.Stloc, loc);							//L_003f: stloc.1 

			ILout.MarkLabel(lblAfterFirstIf);

			ILout.Emit(OpCodes.Ldarg_1);								//L_0000: ldarg.1 
			ILout.Emit(OpCodes.Ldstr, relationName);				//L_0001: ldstr "RelationName"
			ILout.EmitCall(OpCodes.Call, _GetChildRows, null);	//L_0006: callvirt instance class [System.Data]System.Data.DataRow[] [System.Data]System.Data.DataRow::GetChildRows(string)
			ILout.Emit(OpCodes.Stloc, locRows);						//L_000b: stloc.0 
			ILout.Emit(OpCodes.Ldloc, locRows);						//L_000c: ldloc.0 
			ILout.Emit(OpCodes.Ldlen);									//L_000d: ldlen 
			ILout.Emit(OpCodes.Conv_I4);								//L_000e: conv.i4		
			ILout.Emit(OpCodes.Ldc_I4_0);								//L_000f: ldc.i4.0 

			ILout.Emit(OpCodes.Bgt, lblElse1);						//L_0010: bgt.s L_0013
			ILout.Emit(OpCodes.Br, lblEnd);							//L_0012: ret

			ILout.MarkLabel(lblElse1);

			ILout.Emit(OpCodes.Ldarg_2);
			ILout.Emit(OpCodes.Ldarg_0);								//L_0053: ldarg.0 
			ILout.EmitCall(OpCodes.Callvirt, getProp, null);	//L_0054: callvirt instance class [mscorlib]System.Collections.Generic.List`1<class CodeGenerator.MyTest> CodeGenerator.MyTest::get_NestedList()
			ILout.Emit(OpCodes.Ldtoken, itemType);
			ILout.EmitCall(OpCodes.Call, _GetType, null);
			ILout.Emit(OpCodes.Ldloc, locRows);						//L_0059: ldloc.0 
			ILout.Emit(OpCodes.Ldc_I4, nestedSchemaId);			//L_005a: ldc.i4 0xea
			ILout.EmitCall(OpCodes.Callvirt, _SetNested, null);	//L_005f: call void CodeGenerator.Program::ExtractNested(class [mscorlib]System.Collections.IList, class [mscorlib]System.Collections.Generic.IEnumerable`1<class [System.Data]System.Data.DataRow>, int32)
			ILout.MarkLabel(lblEnd);									//L_0064: ret 
		}
	}
}
