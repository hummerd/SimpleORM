using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;


namespace SimpleORM.PropertySetterGenerator
{
	public abstract class PSGBase
	{
		protected static MethodInfo _GetSubListItem = typeof(List<List<int>>).GetMethod("get_Item", new Type[] { typeof(int) });
		protected static MethodInfo _GetListItem = typeof(List<int>).GetMethod("get_Item", new Type[] { typeof(int) });
		protected static MethodInfo _ChangeType = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
		protected static MethodInfo _GetType = typeof(Type).GetMethod("GetTypeFromHandle");
		protected static MethodInfo _GetObjectBuilder = typeof(DataMapper).GetMethod("get_ObjectBuilder");
		protected static MethodInfo _CreateObject = typeof(IObjectBuilder).GetMethod("CreateObject", new Type[] {typeof(Type)});


		protected enum SetterType
		{
			Enum,
			Value,
			ValueNI,
			Struct,
			StructNI,
			Nullable,
			NullableNI,
			Reference,
			ReferenceNI
		}



		public void GenerateMethodHeader(ILGenerator ilOut, int methodIndex)
		{
			ilOut.DeclareLocal(typeof(object));
			ilOut.DeclareLocal(typeof(int));
			ilOut.DeclareLocal(typeof(bool));

			ilOut.Emit(OpCodes.Ldc_I4_0);
			ilOut.Emit(OpCodes.Stloc_2);

			ilOut.Emit(OpCodes.Ldarg, 5);
			ilOut.Emit(OpCodes.Ldc_I4, methodIndex);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Stelem_Ref);
		}

		public void GenerateExtractComplex(
			ILGenerator ilOut,
			PropertyInfo prop,
			Type subType,
			MethodInfo subExtract,
			int subExtractMethodIndex)
		{
			#region Algorithm
			//if (done[ix] != null)
			//{
			//	mt.Prop1 = done[ix];
			//}
			//else
			//{
				//PropType obj = mt.Prop1;
				//if (obj == null)
				//{
				//   obj = (PropType)mapper.ObjectBuilder.CreateObject(typeof(PropType));
				//   mt.Prop1 = obj;
				//}
				//
				//FillerMethod(obj, data, mapper, columnsXX, colIx);
			//}
			#endregion

			//ilGen.Emit(OpCodes.Ldarg, 4);
			//ilGen.Emit(OpCodes.Ldc_I4, 1);
			//ilGen.Emit(OpCodes.Add);
			//ilGen.Emit(OpCodes.Starg, 4);
			//ilGen.Emit(OpCodes.Ldarg, 4);

			//.maxstack 2
			//.locals init (
			//    [0] string str)
			//L_0000: ldarg.0 
			//L_0001: callvirt instance string DataMapComp.Item::get_StrProp()
			//L_0006: stloc.0 
			//L_0007: ldloc.0 
			//L_0008: brtrue.s L_0017
			//L_000a: ldstr "asd"
			//L_000f: stloc.0 
			//L_0010: ldarg.0 
			//L_0011: ldloc.0 
			//L_0012: callvirt instance void DataMapComp.Item::set_StrProp(string)
			//L_0017: ldarg.0 
			//L_0018: ldnull 
			//L_0019: call void DataMapComp.Program::Method(class DataMapComp.Item, class [System.Data]System.Data.IDataReader)
			//L_001e: ret 

			LocalBuilder locPropValue = ilOut.DeclareLocal(subType);
			LocalBuilder locObjCreated = ilOut.DeclareLocal(typeof(object));
			Label lblAfterIf = ilOut.DefineLabel();
			Label lblAfterAll = ilOut.DefineLabel();
			Label lblCallExtract = ilOut.DefineLabel();

			ilOut.Emit(OpCodes.Ldarg, 5);
			ilOut.Emit(OpCodes.Ldc_I4, subExtractMethodIndex);
			ilOut.Emit(OpCodes.Ldelem_Ref);
			ilOut.Emit(OpCodes.Stloc, locObjCreated);
			ilOut.Emit(OpCodes.Ldloc, locObjCreated);
			ilOut.Emit(OpCodes.Brfalse, lblCallExtract);

			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc, locObjCreated);
			ilOut.Emit(OpCodes.Castclass, subType);
			ilOut.Emit(OpCodes.Callvirt, prop.GetSetMethod());
			ilOut.Emit(OpCodes.Br, lblAfterAll);

			ilOut.MarkLabel(lblCallExtract);

			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Callvirt, prop.GetGetMethod());
			ilOut.Emit(OpCodes.Stloc, locPropValue);
			ilOut.Emit(OpCodes.Ldloc, locPropValue);
			ilOut.Emit(OpCodes.Brtrue, lblAfterIf);

			ilOut.Emit(OpCodes.Ldarg_2);
			ilOut.Emit(OpCodes.Callvirt, _GetObjectBuilder); //get_ObjectBuilder
			ilOut.Emit(OpCodes.Ldtoken, subType);
			ilOut.Emit(OpCodes.Call, _GetType);
			ilOut.Emit(OpCodes.Callvirt, _CreateObject); //CreateObject
			ilOut.Emit(OpCodes.Castclass, subType);
			ilOut.Emit(OpCodes.Stloc, locPropValue);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc, locPropValue);
			ilOut.Emit(OpCodes.Callvirt, prop.GetSetMethod());

			ilOut.MarkLabel(lblAfterIf);

			//L_0000: ldarg.0 
			//L_0001: dup 
			//L_0002: ldind.i4 
			//L_0003: ldc.i4.1 
			//L_0004: add 
			//L_0005: stind.i4 
			//L_0006: ldarg.0 
			//L_0007: call void DataMapComp.Program::Method4(int32&)

			ilOut.Emit(OpCodes.Ldarg, 4);
			ilOut.Emit(OpCodes.Dup);
			ilOut.Emit(OpCodes.Ldind_I4);
			ilOut.Emit(OpCodes.Ldc_I4, 1);
			ilOut.Emit(OpCodes.Add);
			ilOut.Emit(OpCodes.Stind_I4);

			ilOut.Emit(OpCodes.Ldloc, locPropValue);
			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldarg_2);
			ilOut.Emit(OpCodes.Ldarg_3);
			ilOut.Emit(OpCodes.Ldarg, 4);
			ilOut.Emit(OpCodes.Ldarg, 5);

			ilOut.Emit(OpCodes.Call, subExtract); // extract
			ilOut.Emit(OpCodes.Pop);

			ilOut.MarkLabel(lblAfterAll);
		}


		protected SetterType GetSetterType(Type storeType, Type columnType)
		{
			bool isNullable = storeType.IsGenericType && storeType.GetGenericTypeDefinition() == typeof(Nullable<>);

			if (isNullable && columnType == storeType.GetGenericArguments()[0])
				return SetterType.Nullable;
			else if (isNullable && columnType != storeType.GetGenericArguments()[0])
				return SetterType.NullableNI;
			else if (storeType.IsValueType && columnType == storeType)
			{
				if (storeType.IsPrimitive)
					return SetterType.Value;
				else
					return SetterType.Struct;
			}
			else if (storeType.IsValueType && columnType != storeType)
			{
				if (storeType.IsEnum)
					return SetterType.Enum;
				else if (storeType.IsPrimitive)
					return SetterType.ValueNI;
				else
					return SetterType.StructNI;
			}
			else if (!storeType.IsValueType && columnType != storeType)
				return SetterType.ReferenceNI;
			else
				return SetterType.Reference;
		}

		protected void CreateSetNullValue(SetterType setterType, ILGenerator ilGen, Type propType)
		{
			switch (setterType)
			{
				case SetterType.Enum:
					GenerateSetDefault(ilGen);
					break;

				case SetterType.Value:
					GenerateSetDefault(ilGen);
					break;

				case SetterType.ValueNI:
					GenerateSetDefault(ilGen);
					break;

				case SetterType.Struct:
					GenerateSetEmpty(ilGen, propType);
					break;

				case SetterType.StructNI:
					GenerateSetEmpty(ilGen, propType);
					break;

				case SetterType.Nullable:
					GenerateSetEmpty(ilGen, propType);
					break;

				case SetterType.NullableNI:
					GenerateSetEmpty(ilGen, propType);
					break;

				case SetterType.ReferenceNI:
				case SetterType.Reference:
					GenerateSetNull(ilGen);
					break;
			}
		}

		protected void GenerateSetEmpty(ILGenerator ilOut, Type propType)
		{
			LocalBuilder loc = ilOut.DeclareLocal(propType);

			ilOut.Emit(OpCodes.Ldc_I4_1);
			ilOut.Emit(OpCodes.Stloc_2);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloca, loc);
			ilOut.Emit(OpCodes.Initobj, propType);
			ilOut.Emit(OpCodes.Ldloc, loc);
		}

		protected void GenerateSetNull(ILGenerator ilOut)
		{
			ilOut.Emit(OpCodes.Ldc_I4_1);
			ilOut.Emit(OpCodes.Stloc_2);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldnull);
		}

		protected void GenerateSetDefault(ILGenerator ilOut)
		{
			ilOut.Emit(OpCodes.Ldc_I4_1);
			ilOut.Emit(OpCodes.Stloc_2);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldc_I4_0);
		}

		protected void GenerateSet(ILGenerator ilOut, MethodInfo setProp, FieldInfo field)
		{
			if (field != null)
				ilOut.Emit(OpCodes.Stfld, field);
			else
				ilOut.Emit(OpCodes.Callvirt, setProp);
		}
	}
}
