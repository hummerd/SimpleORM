using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection.Emit;
using System.Reflection;
using SimpleORM.Attributes;



namespace SimpleORM.PropertySetterGenerator
{
	public class KeyClassGenerator
	{
		protected static MethodInfo _StringEqual = typeof(String).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });
		protected static MethodInfo _DateTimeEqual = typeof(DateTime).GetMethod("op_Equality", new Type[] { typeof(DateTime), typeof(DateTime) });
		protected static MethodInfo _GetHash = typeof(object).GetMethod("GetHashCode");

		protected ModuleBuilder _ModuleBuilder;


		public KeyClassGenerator(ModuleBuilder moduleBuilder)
		{
			_ModuleBuilder = moduleBuilder;
		}


		protected Type GenerateKeyType(string key, DataTable dtSource, List<string> columns, IPropertySetterGenerator methodGenerator, int schemeId)
		{
			string className = "DataPropertySetter_" + key;
			var tb = _ModuleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);

			MethodBuilder getHash = tb.DefineMethod("GetHashCode",
				MethodAttributes.Public | MethodAttributes.ReuseSlot |
				MethodAttributes.Virtual | MethodAttributes.HideBySig,
				typeof(bool),
				new Type[] { typeof(object) });
	
			ILGenerator getHashGen = getHash.GetILGenerator();

			MethodBuilder equals = tb.DefineMethod("Equals",
				MethodAttributes.Public | MethodAttributes.ReuseSlot |
				MethodAttributes.Virtual | MethodAttributes.HideBySig,
				typeof(bool),
				new Type[] { typeof(object) });

			ILGenerator equalsGen = equals.GetILGenerator();
			LocalBuilder locObj = equalsGen.DeclareLocal(tb);
			Label lblRetFalse = equalsGen.DefineLabel();

			equalsGen.Emit(OpCodes.Ldarg_1);
			equalsGen.Emit(OpCodes.Unbox_Any, tb);
			equalsGen.Emit(OpCodes.Stloc_0);
			
			bool needCeq = false;
			bool first = true;

			foreach (var item in columns)
			{
				//Generate field
				Type fieldType = dtSource.Columns[item].DataType;
				var fb = tb.DefineField(item, fieldType, FieldAttributes.Public);

				//Generate mapping definition attribute on this field
				CustomAttributeBuilder attributeBuilder =
					new CustomAttributeBuilder(
							typeof(DataColumnMapAttribute).GetConstructor(new Type[2] { typeof(string), typeof(int) }),
							new object[2] { item, schemeId });
				fb.SetCustomAttribute(attributeBuilder);

				//L_000b: ldarg.0 
				//L_000c: ldflda valuetype [mscorlib]System.DateTime WindowsApplication1.Class1::Col3
				//L_0011: constrained [mscorlib]System.DateTime
				//L_0017: callvirt instance int32 [mscorlib]System.Object::GetHashCode()
				getHashGen.Emit(OpCodes.Ldarg_0);
				getHashGen.Emit(OpCodes.Ldfld, fb);
				
				//Generate part of Equals method for field generated above
				
				if (fieldType == typeof(DateTime))
				{
					needCeq = GenerateCompareDateTime(equalsGen, fb, lblRetFalse);
					getHashGen.Emit(OpCodes.Constrained);
				}
				else if (fieldType == typeof(String))
				{
					needCeq = GenerateCompareString(equalsGen, fb, lblRetFalse);
				}
				else if (fieldType.IsValueType)
				{
					needCeq = GenerateCompareValue(equalsGen, fb, lblRetFalse);
				}
				else
					throw new System.Exception();

				getHashGen.Emit(OpCodes.Callvirt, _GetHash);

				if (first)
				{
					first = false;
				}
				else
				{
					getHashGen.Emit(OpCodes.Xor);
				}
			}

			if (needCeq)
				equalsGen.Emit(OpCodes.Ceq);

			equalsGen.MarkLabel(lblRetFalse);
			equalsGen.Emit(OpCodes.Ldc_I4_0);
			equalsGen.Emit(OpCodes.Ret);

			return tb.CreateType();
		}

		protected bool GenerateCompareString(ILGenerator ilGen, FieldInfo field, Label lblRetFalse)
		{ 
			 //L_0007: ldarg.0 
			 //L_0008: ldfld string WindowsApplication1.Class1::Col2
			 //L_000d: ldloca.s c
			 //L_000f: ldfld string WindowsApplication1.Class1::Col2
			 //L_0014: call bool [mscorlib]System.String::op_Equality(string, string)

			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, field);
			ilGen.Emit(OpCodes.Ldloc_0);
			ilGen.Emit(OpCodes.Ldfld, field);
			ilGen.Emit(OpCodes.Call, _StringEqual);
			ilGen.Emit(OpCodes.Brfalse, lblRetFalse);

			return false;
		}

		protected bool GenerateCompareDateTime(ILGenerator ilGen, FieldInfo field, Label lblRetFalse)
		{
			//L_0007: ldarg.0 
			//L_0008: ldfld string WindowsApplication1.Class1::Col2
			//L_000d: ldloca.s c
			//L_000f: ldfld string WindowsApplication1.Class1::Col2
			//L_0014: call bool [mscorlib]System.DateTime::op_Equality(string, string)

			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, field);
			ilGen.Emit(OpCodes.Ldloc_0);
			ilGen.Emit(OpCodes.Ldfld, field);
			ilGen.Emit(OpCodes.Call, _DateTimeEqual);
			ilGen.Emit(OpCodes.Brfalse, lblRetFalse);

			return false;
		}

		protected bool GenerateCompareValue(ILGenerator ilGen, FieldInfo field, Label lblRetFalse)
		{
			//L_0007: ldarg.0 
			//L_0008: ldfld string WindowsApplication1.Class1::Col2
			//L_000d: ldloca.s c
			//L_000f: ldfld string WindowsApplication1.Class1::Col2
			//L_0014: call bool [mscorlib]System.String::op_Equality(string, string)

			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, field);
			ilGen.Emit(OpCodes.Ldloc_0);
			ilGen.Emit(OpCodes.Ldfld, field);
			ilGen.Emit(OpCodes.Bne_Un, lblRetFalse);

			return true;
		}
	}
}
