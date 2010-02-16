using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;


namespace SimpleORM.PropertySetterGenerator
{
	public class LinkObjectsMethodGenerator
	{
		#region Algorythm

		//protected void LinkObjectsGen(
		//    ExtractInfo extractInfo,
		//    Dictionary<ExtractInfo, KeyObjectIndex> tempPrimary,
		//    Dictionary<ExtractInfo, KeyObjectIndex> tempForeign,
		//    Dictionary<ExtractInfo, object> filled)
		//{
		//    if (filled.ContainsKey(extractInfo))
		//        return;

		//    filled.Add(extractInfo, null);

		//    KeyObjectIndex pkObjects = tempPrimary[extractInfo];




		//    //first iter
		//    ExtractInfo childEI = extractInfo.ChildTypes[0].RelatedExtractInfo;
		//    KeyObjectIndex fkObjects = tempForeign[childEI];

		//    LinkObjects(childEI, tempPrimary, tempForeign, filled);

		//    foreach (var item in pkObjects)
		//    {
		//        IList parentList = item.Value;

		//        IList children;
		//        fkObjects.TryGetValue(item.Key, out children);

		//        foreach (var parent in parentList)
		//        {
		//            var targetList = parent.Children;

		//            if (targetList == null)
		//            {
		//                targetList = new targetList();
		//                parent.Children = targetList;
		//            }

		//            //here can be increase collection capacity

		//            if (children != null)
		//            {
		//                //or targetList.AddRange(children);
		//                ListHelperAddRange(targetList, children);
		//            }
		//        }
		//    }


		//    //second iteration
		//    childEI = extractInfo.ChildTypes[1].RelatedExtractInfo;
		//    fkObjects = tempForeign[childEI];

		//    LinkObjects(childEI, tempPrimary, tempForeign, filled);

		//    foreach (var item in pkObjects)
		//    {
		//        IList parentList = item.Value;

		//        IList children;
		//        fkObjects.TryGetValue(item.Key, out children);

		//        foreach (var parent in parentList)
		//        {
		//            var targetList = parent.Children;

		//            if (targetList == null)
		//            {
		//                targetList = new targetList();
		//                parent.Children = targetList;
		//            }

		//            //here can be increase collection capacity

		//            if (children != null)
		//            {
		//                //or targetList.AddRange(children);
		//                ListHelperAddRange(targetList, children);
		//            }
		//        }
		//    }
		//}

		#endregion

		#region Methods For Generation
		protected static MethodInfo _DicContainsKey = typeof(Dictionary<ExtractInfo, object>).GetMethod(
			"ContainsKey", new Type[]{ typeof(ExtractInfo) } );

		protected static MethodInfo _DicAdd = typeof(Dictionary<ExtractInfo, object>).GetMethod(
			"Add", new Type[]{ typeof(ExtractInfo), typeof(object) } );

		protected static MethodInfo _DicGetItem = typeof(Dictionary<ExtractInfo, DataMapper.KeyObjectIndex>).GetMethod(
			"get_Item", new Type[] { typeof(ExtractInfo) } );

		protected static MethodInfo _DicTryGet = typeof(Dictionary<ExtractInfo, DataMapper.KeyObjectIndex>).GetMethod(
			"TryGetValue", new Type[] { typeof(ExtractInfo), typeof(DataMapper.KeyObjectIndex).MakeByRefType() });
		
		protected static MethodInfo _EiGetChildTypes = typeof(ExtractInfo).GetMethod(
			"get_ChildTypes", new Type[]{ } );

		protected static MethodInfo _ListREIGet = typeof(List<RelationExtractInfo>).GetMethod(
			"get_Item", new Type[]{ typeof(Int32) } );

		protected static MethodInfo _ReiGetEI = typeof(RelationExtractInfo).GetMethod(
			"get_RelatedExtractInfo", new Type[]{ } );

		protected static MethodInfo _KvObjListGetValue = typeof(KeyValuePair<object, IList>).GetMethod(
			"get_Value", new Type[]{ } );

		protected static MethodInfo _KvObjListGetKey = typeof(KeyValuePair<object, IList>).GetMethod(
			"get_Key", new Type[]{ } );

		protected static MethodInfo _DicObjListTryGet =typeof(DataMapper.KeyObjectIndex).GetMethod(
			"TryGetValue", new Type[]{ typeof(object), typeof(List<object>).MakeByRefType() } );

		protected static MethodInfo _Dispose = typeof(IDisposable).GetMethod(
			"Dispose", new Type[] { });

		protected static MethodInfo _Invoke = typeof(MethodInfo).GetMethod(
			"Invoke", new Type[] { typeof(object), typeof(object[]) });


		public static readonly List<MethodInfo> _GetItems = new List<MethodInfo>();

		#endregion

		protected ModuleBuilder _ModuleBuilder;


		public LinkObjectsMethodGenerator(ModuleBuilder moduleBuilder)
		{
			_ModuleBuilder = moduleBuilder;
		}


		public void GenerateLinkMethod(ExtractInfo extractInfo, bool createNamespace)
		{
			if (extractInfo.LinkMethod != null)
				return;

			TypeBuilder typeBuilder;
			ILGenerator ilOut = CreateMethodDefinition(extractInfo, createNamespace, out typeBuilder);
			
			foreach (var item in extractInfo.ChildTypes)
				if (item.RelatedExtractInfo.ChildTypes.Count > 0)
					GenerateLinkMethod(item.RelatedExtractInfo, createNamespace);

			LocalBuilder pkObjects = ilOut.DeclareLocal(typeof(DataMapper.KeyObjectIndex));
			//pkObjects.SetLocalSymInfo("pkObjects");
			//Label needLink = ilOut.DefineLabel();
			Label lblRet = ilOut.DefineLabel();

			//if (filled.ContainsKey(extractInfo))
			//    return;
			ilOut.Emit(OpCodes.Ldarg_3);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Callvirt, _DicContainsKey);
			ilOut.Emit(OpCodes.Brtrue, lblRet);
			//ilOut.Emit(OpCodes.Ret);
			//ilOut.MarkLabel(needLink);
			//filled.Add(extractInfo)
			ilOut.Emit(OpCodes.Ldarg_3);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldnull);
			ilOut.Emit(OpCodes.Callvirt, _DicAdd);
			//KeyObjectIndex pkObjects = tempPrimary[extractInfo];
			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloca, pkObjects);
			ilOut.Emit(OpCodes.Callvirt, _DicTryGet);
			ilOut.Emit(OpCodes.Brfalse, lblRet);
			//ilOut.Emit(OpCodes.Callvirt, _DicGetItem);
			//ilOut.Emit(OpCodes.Stloc, pkObjects);

			Type parentListType = typeof(List<>).MakeGenericType(extractInfo.TargetType);
			Type parentEnumeratorListType = typeof(List<>.Enumerator).MakeGenericType(extractInfo.TargetType);

			LocalBuilder childEI = ilOut.DeclareLocal(typeof(ExtractInfo));
			//childEI.SetLocalSymInfo("childEI");
			LocalBuilder fkObjects = ilOut.DeclareLocal(typeof(DataMapper.KeyObjectIndex));
			//fkObjects.SetLocalSymInfo("fkObjects");
			LocalBuilder parentList = ilOut.DeclareLocal(parentListType);
			//parentList.SetLocalSymInfo("parentList");
			
			for (int i = 0; i < extractInfo.ChildTypes.Count; i++)
			{
				Type childType = extractInfo.ChildTypes[i].RelatedExtractInfo.TargetType;
				Type childListType = typeof(List<>).MakeGenericType(childType);
				Type childEnumeratorListType = typeof(List<>.Enumerator).MakeGenericType(childType);
				Type enumerableChildType = typeof(IEnumerable<>).MakeGenericType(childType);

				LocalBuilder children = ilOut.DeclareLocal(childListType);
				//children.SetLocalSymInfo("children" + i);

				Label afterListNull = ilOut.DefineLabel();
				Label lblNoFk = ilOut.DefineLabel();
				Type listType = ReflectionHelper.GetReturnType(extractInfo.ChildTypes[i].Member);
				LocalBuilder targetList = ilOut.DeclareLocal(listType);
				//targetList.SetLocalSymInfo("targetList" + i);

				//ExtractInfo childEI = extractInfo.ChildTypes[0].RelatedExtractInfo;
				ilOut.Emit(OpCodes.Ldarg_0);
				ilOut.Emit(OpCodes.Callvirt, _EiGetChildTypes);
				ilOut.Emit(OpCodes.Ldc_I4, i);
				ilOut.Emit(OpCodes.Callvirt, _ListREIGet);
				ilOut.Emit(OpCodes.Callvirt, _ReiGetEI);
				ilOut.Emit(OpCodes.Stloc, childEI);

				//DataMapper.KeyObjectIndex fkObjects = tempForeign[childEI];
				ilOut.Emit(OpCodes.Ldarg_2);
				ilOut.Emit(OpCodes.Ldloc, childEI);
				ilOut.Emit(OpCodes.Ldloca, fkObjects);
				ilOut.Emit(OpCodes.Callvirt, _DicTryGet);
				ilOut.Emit(OpCodes.Brfalse, lblNoFk);
				//ilOut.Emit(OpCodes.Callvirt, _DicGetItem);
				//ilOut.Emit(OpCodes.Stloc, fkObjects);

				//LinkObjects(childEI, tempPrimary, tempForeign, filled);
				if (extractInfo.ChildTypes[i].RelatedExtractInfo.LinkMethod != null)
				{
					ilOut.Emit(OpCodes.Ldloc, childEI);
					ilOut.Emit(OpCodes.Ldarg_1);
					ilOut.Emit(OpCodes.Ldarg_2);
					ilOut.Emit(OpCodes.Ldarg_3);
					ilOut.Emit(OpCodes.Call, extractInfo.ChildTypes[i].RelatedExtractInfo.LinkMethod);
				}

				//foreach (var item in pkObjects)
				GenForEach<KeyValuePair<object, IList>>(ilOut, pkObjects, pkItem =>
				{
					//List<object> parentList = item.Value
					ilOut.Emit(OpCodes.Ldloca, pkItem);
					ilOut.Emit(OpCodes.Call, _KvObjListGetValue);
					ilOut.Emit(OpCodes.Stloc, parentList);

					//List<object> children;
					//fkObjects.TryGetValue(item.Key, out children);
					ilOut.Emit(OpCodes.Ldloc, fkObjects);
					ilOut.Emit(OpCodes.Ldloca, pkItem);
					ilOut.Emit(OpCodes.Call, _KvObjListGetKey);
					ilOut.Emit(OpCodes.Ldloca, children);
					ilOut.Emit(OpCodes.Callvirt, _DicObjListTryGet);
					ilOut.Emit(OpCodes.Pop);

					//foreach (var parent in parentList)
					GenForEach(ilOut, parentList, extractInfo.TargetType, parent =>
					{
						ilOut.Emit(OpCodes.Ldloc, parent);
						if (extractInfo.ChildTypes[i].Member is PropertyInfo)
							ilOut.Emit(OpCodes.Callvirt, ((PropertyInfo)extractInfo.ChildTypes[i].Member).GetGetMethod());
						else
							ilOut.Emit(OpCodes.Ldfld, (FieldInfo)extractInfo.ChildTypes[i].Member);

						ilOut.Emit(OpCodes.Stloc, targetList);
						ilOut.Emit(OpCodes.Ldloc, targetList);
						ilOut.Emit(OpCodes.Brtrue, afterListNull);

						ilOut.Emit(OpCodes.Newobj, listType.GetConstructor(new Type[] { }));
						ilOut.Emit(OpCodes.Stloc, targetList);
						ilOut.Emit(OpCodes.Ldloc, parent);
						ilOut.Emit(OpCodes.Ldloc, targetList);
						if (extractInfo.ChildTypes[i].Member is PropertyInfo)
							ilOut.Emit(OpCodes.Callvirt, ((PropertyInfo)extractInfo.ChildTypes[i].Member).GetSetMethod());
						else
							ilOut.Emit(OpCodes.Stfld, (FieldInfo)extractInfo.ChildTypes[i].Member);

						ilOut.MarkLabel(afterListNull);

						MethodInfo addRange = listType.GetMethod("AddRange", new Type[] { 
						 enumerableChildType });

						ilOut.Emit(OpCodes.Ldloc, children);
						Label noChildren = ilOut.DefineLabel();
						ilOut.Emit(OpCodes.Brfalse, noChildren);

						if (listType.IsAssignableFrom(typeof(Collection<>).MakeGenericType(childType)))
						{
							GenTryIncreaseCollectionCapacity(
								ilOut, 
								childType, 
								listType, 
								targetList, 
								childListType, 
								children);
						}

						ilOut.Emit(OpCodes.Ldloc, targetList);
						ilOut.Emit(OpCodes.Ldloc, children);

						if (addRange != null)
						{
							ilOut.Emit(OpCodes.Callvirt, addRange);
						}
						else
						{
							ilOut.Emit(OpCodes.Call, typeof(LinkObjectsMethodGenerator).GetMethod("ListHelperAddRange"));
						}

						ilOut.MarkLabel(noChildren);
					});
				});

				ilOut.MarkLabel(lblNoFk);
			}

			ilOut.MarkLabel(lblRet);
			ilOut.Emit(OpCodes.Ret);

			extractInfo.LinkMethod = typeBuilder.CreateType().GetMethod("LinkChild_" + extractInfo.TargetType);
		}


		protected ILGenerator CreateMethodDefinition(ExtractInfo extractInfo, bool createNamespace, out TypeBuilder typeBuilder)
		{
			string prefix = "DataPropertySetter.";
			if (extractInfo.TargetType.FullName.StartsWith(prefix))
				prefix = String.Empty;
			string className = 
				prefix + 
				extractInfo.TargetType.FullName + 
				(createNamespace ? "." : "_") +
				"Link" + extractInfo.SchemeId;

			typeBuilder = _ModuleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("LinkChild_" + extractInfo.TargetType,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard,
				null,
				new Type[] { 
					typeof(ExtractInfo), 
					typeof(Dictionary<ExtractInfo, DataMapper.KeyObjectIndex>), 
					typeof(Dictionary<ExtractInfo, DataMapper.KeyObjectIndex>),
					typeof(Dictionary<ExtractInfo, object>)
				});

			extractInfo.LinkMethod = methodBuilder;
			return methodBuilder.GetILGenerator();		
		}

		protected void GenTryIncreaseCollectionCapacity(
			ILGenerator ilOut, 
			Type childType, 
			Type listType, 
			LocalBuilder targetList,
			Type childListType,
			LocalBuilder children
			)
		{
			Type intListType = typeof(List<>).MakeGenericType(childType);
			Type intIListType = typeof(IList<>).MakeGenericType(childType);
			LocalBuilder intItems = ilOut.DeclareLocal(intIListType);
			LocalBuilder intList = ilOut.DeclareLocal(intListType);
			Label lblNoCapacity = ilOut.DefineLabel();

			_GetItems.Add(
				listType.
					GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic).
						GetGetMethod(true));

			ilOut.Emit(OpCodes.Ldnull);
			ilOut.Emit(OpCodes.Ldfld, typeof(LinkObjectsMethodGenerator).GetField("_GetItems"));
			ilOut.Emit(OpCodes.Ldc_I4, _GetItems.Count - 1);
			ilOut.Emit(OpCodes.Call, _GetItems.GetType().GetProperty("Item").GetGetMethod());
			ilOut.Emit(OpCodes.Ldloc, targetList);
			ilOut.Emit(OpCodes.Ldnull);
			ilOut.Emit(OpCodes.Call, _Invoke);
			//ilOut.Emit(OpCodes.Ldvirtftn, listType.GetMethod("get_Items", BindingFlags.Instance | BindingFlags.NonPublic));
			//ilOut.EmitCalli(OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, typeof(IList<>).MakeGenericType(childType), new Type[] { });
			//ilOut.Emit(OpCodes.Call, listType.GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));
			//ilOut.Emit(OpCodes.Ldfld, listType.GetField("items", BindingFlags.Instance | BindingFlags.NonPublic));
			ilOut.Emit(OpCodes.Castclass, intIListType);
			ilOut.Emit(OpCodes.Stloc, intItems);
			ilOut.Emit(OpCodes.Ldloc, intItems);

			ilOut.Emit(OpCodes.Isinst, intListType);
			ilOut.Emit(OpCodes.Stloc, intList);
			ilOut.Emit(OpCodes.Ldloc, intList);
			ilOut.Emit(OpCodes.Brfalse, lblNoCapacity);
			ilOut.Emit(OpCodes.Ldloc, intList);
			ilOut.Emit(OpCodes.Dup);
			ilOut.Emit(OpCodes.Callvirt, intListType.GetProperty("Capacity").GetGetMethod());
			ilOut.Emit(OpCodes.Ldloc, children);
			ilOut.Emit(OpCodes.Callvirt, childListType.GetProperty("Count").GetGetMethod());
			ilOut.Emit(OpCodes.Add);
			ilOut.Emit(OpCodes.Callvirt, intListType.GetProperty("Capacity").GetSetMethod());
			ilOut.MarkLabel(lblNoCapacity);
		}

		protected void GenForEach<T>(ILGenerator ilOut, LocalBuilder collection, Action<LocalBuilder> code)
		{
			GenForEach(ilOut, collection, typeof(T), code);
		}

		protected void GenForEach(ILGenerator ilOut, LocalBuilder collection, Type itemType, Action<LocalBuilder> code)
		{
			Label endOfFirstFor = ilOut.DefineLabel();
			Label startOfFirstFor = ilOut.DefineLabel();

			MethodInfo getEnum = collection.LocalType.GetMethod("GetEnumerator", new Type[]{ });
			MethodInfo getCurr = getEnum.ReturnType.GetProperty("Current").GetGetMethod();
			MethodInfo moveNext = getEnum.ReturnType.GetMethod("MoveNext", new Type[] { });

			LocalBuilder enumerator = ilOut.DeclareLocal(getEnum.ReturnType);
			LocalBuilder item = ilOut.DeclareLocal(itemType);

			ilOut.Emit(OpCodes.Ldloc, collection);
			ilOut.Emit(OpCodes.Callvirt, getEnum);
			ilOut.Emit(OpCodes.Stloc, enumerator);
			ilOut.BeginExceptionBlock();
			ilOut.Emit(OpCodes.Br, endOfFirstFor);
			ilOut.MarkLabel(startOfFirstFor);
			ilOut.Emit(OpCodes.Ldloca, enumerator); // a is used since enumerator is struct
			ilOut.Emit(OpCodes.Call, getCurr);
			ilOut.Emit(OpCodes.Stloc, item);

			code(item);

			ilOut.MarkLabel(endOfFirstFor);
			ilOut.Emit(OpCodes.Ldloca, enumerator);
			ilOut.Emit(OpCodes.Call, moveNext);
			ilOut.Emit(OpCodes.Brtrue, startOfFirstFor);
			Label afterFinally2 = ilOut.DefineLabel();
			ilOut.Emit(OpCodes.Leave, afterFinally2);
			ilOut.BeginFinallyBlock();
			ilOut.Emit(OpCodes.Ldloca, enumerator);
			ilOut.Emit(OpCodes.Constrained, typeof(DataMapper.KeyObjectIndex.Enumerator));
			ilOut.Emit(OpCodes.Callvirt, _Dispose);
			ilOut.Emit(OpCodes.Endfinally);
			ilOut.EndExceptionBlock();
			ilOut.MarkLabel(afterFinally2);
		}


		public static void ListHelperAddRange(IList listDst, IList listSrc)
		{
			if (listSrc == null)
				return;

			int c = listSrc.Count;
			for (int i = 0; i < c; i++)
			{
				listDst.Add(listSrc[i]);
			}
		}
	}
}
