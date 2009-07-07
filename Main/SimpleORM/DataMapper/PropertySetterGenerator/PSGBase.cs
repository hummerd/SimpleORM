using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;


namespace SimpleORM.PropertySetterGenerator
{
	public class PSGBase
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
	}
}
