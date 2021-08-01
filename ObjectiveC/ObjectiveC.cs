using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ObjectiveC
{
    // TODO: global locking
    public static class ObjectiveC
    {
        private const string ObjectiveCRuntimeLibrary = "/usr/lib/libobjc.A.dylib";
        private const string DynamicAssemblyName = "ObjectiveC.Runtime";

        [DllImport(ObjectiveCRuntimeLibrary, CharSet = CharSet.Ansi, EntryPoint = "objc_getClass")]
        private static extern UIntPtr GetClassIdentifierByName(string className);

        [DllImport(ObjectiveCRuntimeLibrary, CharSet = CharSet.Ansi, EntryPoint = "sel_registerName")]
        public static extern IntPtr GetSelectorIdentifierByName(string selectorName);

        // ObjectiveC new constructor
        private const string NewObjectiveCObjectDelegateName = "NewObjectiveCObject";
        private delegate UIntPtr NewObjectiveCObjectDelegate(UIntPtr classIdentifier);
        private static NewObjectiveCObjectDelegate NewObjectiveCObject;

        // ObjectiveC alloc constructor
        private const string AllocObjectiveCObjectDelegateName = "AllocObjectiveCObject";
        private delegate UIntPtr AllocObjectiveCObjectDelegate(UIntPtr classIdentifier);
        private static AllocObjectiveCObjectDelegate AllocObjectiveCObject;

        // ObjectiveC alloc init constructor
        private const string AllocInitObjectiveCObjectDelegateName = "AllocInitObjectiveCObject";
        private delegate UIntPtr AllocInitObjectiveCObjectDelegate(UIntPtr classIdentifier);
        private static AllocInitObjectiveCObjectDelegate AllocInitObjectiveCObject;

        private static AssemblyBuilder DynamicAssembly;
        private static ModuleBuilder DynamicModule;

        private static Type BaseBindingsType;


        private static List<Assembly> InitializedAssemblies = new List<Assembly>();

        private class TypeDetail
        {
            public readonly Type TypeImpl;
            public readonly UIntPtr ClassIdentifier;

            public TypeDetail(Type typeImpl, UIntPtr classIdentifier)
            {
                TypeImpl = typeImpl;
                ClassIdentifier = classIdentifier;
            }
        }

        private static Dictionary<Type, TypeDetail> TypeDetailMapping = new Dictionary<Type, TypeDetail>(); 

        static ObjectiveC()
        {
            AssemblyName assemblyName = new AssemblyName(DynamicAssemblyName);
            DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            DynamicModule = DynamicAssembly.DefineDynamicModule(DynamicAssemblyName);

            InitializeDelegates();
        }

        private static void InitializeDelegates()
        {
            TypeBuilder baseBindingsBuilder = DynamicModule.DefineType("ObjectiveCBaseBindings", TypeAttributes.Public | TypeAttributes.Class);

            // Create common msgSend used by slow paths
            MethodBuilder objcMsgSendPinvoke = CreateObjSendNativeCall(baseBindingsBuilder, typeof(nuint), new Type[] { typeof(nuint), typeof(nuint) });
            objcMsgSendPinvoke.DefineParameter(0, ParameterAttributes.In, "classIdentifier");
            objcMsgSendPinvoke.DefineParameter(1, ParameterAttributes.In, "selector");

            ConstructObjectiveCNewFunction(baseBindingsBuilder, objcMsgSendPinvoke);
            ConstructObjectiveCAllocFunction(baseBindingsBuilder, objcMsgSendPinvoke);
            ConstructObjectiveCAllocInitFunction(baseBindingsBuilder, objcMsgSendPinvoke);
            
            // Finalize the class
            BaseBindingsType = baseBindingsBuilder.CreateType();

            NewObjectiveCObject = (NewObjectiveCObjectDelegate)BaseBindingsType.GetMethod(NewObjectiveCObjectDelegateName).CreateDelegate(typeof(NewObjectiveCObjectDelegate));
            AllocObjectiveCObject = (AllocObjectiveCObjectDelegate)BaseBindingsType.GetMethod(AllocObjectiveCObjectDelegateName).CreateDelegate(typeof(AllocObjectiveCObjectDelegate));
            AllocInitObjectiveCObject = (AllocInitObjectiveCObjectDelegate)BaseBindingsType.GetMethod(AllocInitObjectiveCObjectDelegateName).CreateDelegate(typeof(AllocInitObjectiveCObjectDelegate));
        }

        private static void ConstructObjectiveCNewFunction(TypeBuilder builder, MethodBuilder msgSendPinvoke)
        {
            MethodBuilder newFunction = builder.DefineMethod(NewObjectiveCObjectDelegateName,
                                                        MethodAttributes.Public | MethodAttributes.Static,
                                                        CallingConventions.Standard,
                                                        typeof(UIntPtr),
                                                        new Type [] { typeof(UIntPtr) });

            var newFunctionEmitter = newFunction.GetILGenerator();

            // Load the Objective C class descriptor
            newFunctionEmitter.Emit(OpCodes.Ldarg_0);

            // Starting with macOS 10.15, there is an optimized codepath to do that.
            if (OperatingSystem.IsMacOSVersionAtLeast(10, 15))
            {
                MethodBuilder optimizedNewPinvoke = builder.DefinePInvokeMethod("objc_opt_new",
                                                  ObjectiveCRuntimeLibrary,
                                                  MethodAttributes.Public | MethodAttributes.Static,
                                                  CallingConventions.Standard,
                                                  typeof(UIntPtr),
                                                  new Type[] { typeof(UIntPtr) },
                                                  CallingConvention.Winapi,
                                                  CharSet.Ansi);
                optimizedNewPinvoke.DefineParameter(0, ParameterAttributes.In, "classIdentifier");

                // DO NOT REMOVE
                optimizedNewPinvoke.SetImplementationFlags(MethodImplAttributes.PreserveSig);

                newFunctionEmitter.Emit(OpCodes.Call, optimizedNewPinvoke);
            }
            else
            {
                // Precompute at codegen to reduce runtime overhead.
                nint nativeSelector = (nint)GetSelectorIdentifierByName("new");

                // Load the selector.
                if (Environment.Is64BitProcess)
                {
                    newFunctionEmitter.Emit(OpCodes.Ldc_I8, nativeSelector);
                }
                else
                {
                    newFunctionEmitter.Emit(OpCodes.Ldc_I4, nativeSelector);
                }

                newFunctionEmitter.Emit(OpCodes.Call, msgSendPinvoke);
            }

            newFunctionEmitter.Emit(OpCodes.Ret);
        }
        private static void ConstructObjectiveCAllocFunction(TypeBuilder builder, MethodBuilder msgSendPinvoke)
        {
            MethodBuilder allocFunction = builder.DefineMethod(AllocObjectiveCObjectDelegateName,
                                                        MethodAttributes.Public | MethodAttributes.Static,
                                                        CallingConventions.Standard,
                                                        typeof(UIntPtr),
                                                        new Type [] { typeof(UIntPtr) });

            var allocFunctionEmitter = allocFunction.GetILGenerator();

            // Load the Objective C class descriptor
            allocFunctionEmitter.Emit(OpCodes.Ldarg_0);

            // Starting with macOS 10.9, there is an optimized codepath to do that.
            if (OperatingSystem.IsMacOSVersionAtLeast(10, 9))
            {
                MethodBuilder optimizedAllocPinvoke = builder.DefinePInvokeMethod("objc_alloc",
                                                  ObjectiveCRuntimeLibrary,
                                                  MethodAttributes.Public | MethodAttributes.Static,
                                                  CallingConventions.Standard,
                                                  typeof(UIntPtr),
                                                  new Type[] { typeof(UIntPtr) },
                                                  CallingConvention.Winapi,
                                                  CharSet.Ansi);
                optimizedAllocPinvoke.DefineParameter(0, ParameterAttributes.In, "classIdentifier");

                // DO NOT REMOVE
                optimizedAllocPinvoke.SetImplementationFlags(MethodImplAttributes.PreserveSig);

                allocFunctionEmitter.Emit(OpCodes.Call, optimizedAllocPinvoke);
            }
            else
            {
                // Precompute at codegen to reduce runtime overhead.
                nint nativeSelector = (nint)GetSelectorIdentifierByName("alloc");

                // Load the selector.
                if (Environment.Is64BitProcess)
                {
                    allocFunctionEmitter.Emit(OpCodes.Ldc_I8, nativeSelector);
                }
                else
                {
                    allocFunctionEmitter.Emit(OpCodes.Ldc_I4, nativeSelector);
                }

                allocFunctionEmitter.Emit(OpCodes.Call, msgSendPinvoke);
            }

            allocFunctionEmitter.Emit(OpCodes.Ret);
        }

        private static void ConstructObjectiveCAllocInitFunction(TypeBuilder builder, MethodBuilder msgSendPinvoke)
        {
            MethodBuilder newFunction = builder.DefineMethod(AllocInitObjectiveCObjectDelegateName,
                                                        MethodAttributes.Public | MethodAttributes.Static,
                                                        CallingConventions.Standard,
                                                        typeof(UIntPtr),
                                                        new Type [] { typeof(UIntPtr) });

            var newFunctionEmitter = newFunction.GetILGenerator();

            // Load the Objective C class descriptor
            newFunctionEmitter.Emit(OpCodes.Ldarg_0);

            // Starting with macOS 10.14.4, there is an optimized codepath to do that.
            if (OperatingSystem.IsMacOSVersionAtLeast(10, 14, 4))
            {
                MethodBuilder optimizedNewPinvoke = builder.DefinePInvokeMethod("objc_alloc_init",
                                                  ObjectiveCRuntimeLibrary,
                                                  MethodAttributes.Public | MethodAttributes.Static,
                                                  CallingConventions.Standard,
                                                  typeof(UIntPtr),
                                                  new Type[] { typeof(UIntPtr) },
                                                  CallingConvention.Winapi,
                                                  CharSet.Ansi);
                optimizedNewPinvoke.DefineParameter(0, ParameterAttributes.In, "classIdentifier");

                // DO NOT REMOVE
                optimizedNewPinvoke.SetImplementationFlags(MethodImplAttributes.PreserveSig);

                newFunctionEmitter.Emit(OpCodes.Call, optimizedNewPinvoke);
            }
            else
            {
                newFunctionEmitter.DeclareLocal(typeof(nuint));

                // Precompute at codegen to reduce runtime overhead.
                nint nativeAllocSelector = (nint)GetSelectorIdentifierByName("alloc");
                nint nativeInitSelector = (nint)GetSelectorIdentifierByName("init");

                // Load the alloc selector.
                if (Environment.Is64BitProcess)
                {
                    newFunctionEmitter.Emit(OpCodes.Ldc_I8, nativeAllocSelector);
                }
                else
                {
                    newFunctionEmitter.Emit(OpCodes.Ldc_I4, nativeAllocSelector);
                }

                newFunctionEmitter.Emit(OpCodes.Call, msgSendPinvoke);

                // Save the instance identifier first and push it again
                // NOTE: We assume that it's index 0 because there is only one local variable.
                newFunctionEmitter.Emit(OpCodes.Stloc_0);
                newFunctionEmitter.Emit(OpCodes.Ldloc_0);

                MethodBuilder msgSendPinvokeNoResult = CreateObjSendNativeCall(builder, typeof(void), new Type[] { typeof(nuint), typeof(nuint) });
                msgSendPinvokeNoResult.DefineParameter(0, ParameterAttributes.In, "classIdentifier");
                msgSendPinvokeNoResult.DefineParameter(1, ParameterAttributes.In, "selector");

                // Load the init selector.
                if (Environment.Is64BitProcess)
                {
                    newFunctionEmitter.Emit(OpCodes.Ldc_I8, nativeInitSelector);
                }
                else
                {
                    newFunctionEmitter.Emit(OpCodes.Ldc_I4, nativeInitSelector);
                }

                newFunctionEmitter.Emit(OpCodes.Call, msgSendPinvokeNoResult);

                // Reload saved local to return it
                newFunctionEmitter.Emit(OpCodes.Ldloc_0);
            }

            newFunctionEmitter.Emit(OpCodes.Ret);
        }

        public static void Initialize(Assembly assembly, string[] systemFrameworksRequired)
        {
            if (!InitializedAssemblies.Contains(assembly))
            {
                foreach (string systemFramework in systemFrameworksRequired)
                {
                    if(!LoadSystemFramework(systemFramework))
                    {
                        throw new InvalidOperationException($"System Framework {systemFramework} required by {assembly.GetName()} couldn't be loaded!");
                    }
                }

                foreach(Type type in assembly.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(ClassAttribute), false).Length > 0)
                    {
                        RegisterType(type);
                    }
                }

                InitializedAssemblies.Add(assembly);
            }
        }

        private static void CreateConstructor(TypeBuilder builder, FieldInfo nativePointer)
        {
            ConstructorBuilder constructorBuilder = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(UIntPtr) });
            ILGenerator myConstructorIL = constructorBuilder.GetILGenerator();

            myConstructorIL.Emit(OpCodes.Ldarg_0);
            myConstructorIL.Emit(OpCodes.Ldarg_1);
            myConstructorIL.Emit(OpCodes.Stfld, nativePointer);
            myConstructorIL.Emit(OpCodes.Ret);            
        }

        // TODO: have some cache at the type level
        // TODO: multi arch support.
        // NOTE: user is in charge of passing the parameter definition for the native pointer instance.
        private static MethodBuilder CreateObjSendNativeCall(TypeBuilder builder, Type returnType, Type[] parameters)
        {
            MethodBuilder methodBuilder = builder.DefinePInvokeMethod("objc_msgSend",
                                              ObjectiveCRuntimeLibrary,
                                              MethodAttributes.Public | MethodAttributes.Static,
                                              CallingConventions.Standard,
                                              returnType,
                                              parameters,
                                              CallingConvention.Winapi,
                                              CharSet.Ansi);
            // DO NOT REMOVE
            methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);

            return methodBuilder;
        }

        private static void CreateProperty(TypeBuilder builder, FieldInfo nativePointer, Type type, PropertyInfo propertyInfo)
        {
            string propertyFullName = propertyInfo.Name;

            object[] propertyAttributes = propertyInfo.GetCustomAttributes(typeof(PropertyAttribute), false);

            PropertyAttribute propertyAttribute = null;

            if (propertyAttributes.Length > 0)
            {
                propertyAttribute = (PropertyAttribute)propertyAttributes[0];
            }

            PropertyBuilder propertyBuilder = builder.DefineProperty(propertyFullName, PropertyAttributes.None, propertyInfo.PropertyType, null);

            if (propertyInfo.CanRead)
            {
                MethodInfo declarationInfo = propertyInfo.GetGetMethod();
                var propertyGet = builder.DefineMethod("get_" + propertyInfo.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, propertyInfo.PropertyType, Type.EmptyTypes);

                var propertyGetIL = propertyGet.GetILGenerator();

                // Getter only have the object instance and selector passed.
                MethodBuilder objcSendCall = CreateObjSendNativeCall(builder, propertyInfo.PropertyType, new Type[] { typeof(nuint), typeof(nuint) });
                objcSendCall.DefineParameter(0, ParameterAttributes.In, "objectIdentifier");
                objcSendCall.DefineParameter(1, ParameterAttributes.In, "selector");

                string objectiveCSelectorReadName;

                if (propertyAttribute != null)
                {
                    objectiveCSelectorReadName = propertyAttribute.CustomReadName;
                }
                else
                {
                    objectiveCSelectorReadName = char.ToLower(propertyInfo.Name[0]) + propertyInfo.Name.Substring(1);
                }

                // Precompute at codegen to reduce runtime overhead.
                nint nativeSelector = (nint)GetSelectorIdentifierByName(objectiveCSelectorReadName);

                // Load this
                propertyGetIL.Emit(OpCodes.Ldarg_0);
                propertyGetIL.Emit(OpCodes.Ldfld, nativePointer);

                if (Environment.Is64BitProcess)
                {
                    propertyGetIL.Emit(OpCodes.Ldc_I8, nativeSelector);
                }
                else
                {
                    propertyGetIL.Emit(OpCodes.Ldc_I4, nativeSelector);
                }

                propertyGetIL.Emit(OpCodes.Call, objcSendCall);
                propertyGetIL.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(propertyGet);

                builder.DefineMethodOverride(propertyGet, propertyInfo.GetGetMethod());
            }

            if (propertyInfo.CanWrite)
            {
                MethodInfo declarationInfo = propertyInfo.GetGetMethod();
                var propertySet = builder.DefineMethod("set_" + propertyInfo.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, null, new Type [] { propertyInfo.PropertyType });

                var propertySetIL = propertySet.GetILGenerator();

                // Setter only have the object instance, selector passed and the value argument passed.
                MethodBuilder objcSendCall = CreateObjSendNativeCall(builder, null, new Type[] { typeof(nuint), typeof(nuint), propertyInfo.PropertyType });
                objcSendCall.DefineParameter(0, ParameterAttributes.In, "objectIdentifier");
                objcSendCall.DefineParameter(1, ParameterAttributes.In, "selector");
                objcSendCall.DefineParameter(2, ParameterAttributes.In, "value");

                string objectiveCSelectorSetName;

                if (propertyAttribute != null)
                {
                    objectiveCSelectorSetName = propertyAttribute.CustomWriteName;
                }
                else
                {
                    objectiveCSelectorSetName = "set" + propertyInfo.Name + ":";
                }

                // Precompute at codegen to reduce runtime overhead.
                nint nativeSelector = (nint)GetSelectorIdentifierByName(objectiveCSelectorSetName);

                // Load this
                propertySetIL.Emit(OpCodes.Ldarg_0);
                propertySetIL.Emit(OpCodes.Ldfld, nativePointer);

                if (Environment.Is64BitProcess)
                {
                    propertySetIL.Emit(OpCodes.Ldc_I8, nativeSelector);
                }
                else
                {
                    propertySetIL.Emit(OpCodes.Ldc_I4, nativeSelector);
                }

                propertySetIL.Emit(OpCodes.Ldarg_1);
                propertySetIL.Emit(OpCodes.Call, objcSendCall);
                propertySetIL.Emit(OpCodes.Ret);

                propertyBuilder.SetSetMethod(propertySet);

                builder.DefineMethodOverride(propertySet, propertyInfo.GetSetMethod());
            }
        }

        private static void LoadArgument(ILGenerator generator, short index)
        {
            switch (index)
            {
                case 0:
                    generator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    generator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    generator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    generator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    generator.Emit(OpCodes.Ldarg, index);
                    break;
            }
        }

        private static Type[] GetParametersTypes(ParameterInfo[] infos)
        {
            Type[] result = new Type[infos.Length];
            
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = infos[i].ParameterType;
            }

            return result;
        }

        // TODO: figure out a way to define static functions in an easy way...
        private static void CreateMethod(TypeBuilder builder, FieldInfo nativePointer, Type type, MethodInfo methodInfo)
        {
            object[] methodAttributes = methodInfo.GetCustomAttributes(typeof(MethodAttribute), false);

            MethodAttribute methodAttribute = null;

            if (methodAttributes.Length > 0)
            {
                methodAttribute = (MethodAttribute)methodAttributes[0];
            }

            ParameterInfo[] infos = methodInfo.GetParameters();
            Type[] parametersTypes = GetParametersTypes(infos);


            MethodBuilder methodImplementation = builder.DefineMethod(methodInfo.Name,
                                                        MethodAttributes.Public | MethodAttributes.Virtual,
                                                        methodInfo.CallingConvention,
                                                        methodInfo.ReturnType,
                                                        parametersTypes);

            // Forward the parameters name to the implementation for stacktraces.
            for (int i = 0; i < infos.Length; i++)
            {
                methodImplementation.DefineParameter(i, infos[i].Attributes, infos[i].Name);
            }

            var methodImplementationIL = methodImplementation.GetILGenerator();

            List<Type> objcParametersTypes = new List<Type>(parametersTypes.Length + 2);
            objcParametersTypes.Add(typeof(nuint));
            objcParametersTypes.Add(typeof(nuint));
            objcParametersTypes.AddRange(parametersTypes);

            MethodBuilder objcSendCall = CreateObjSendNativeCall(builder, methodInfo.ReturnType, objcParametersTypes.ToArray());
            objcSendCall.DefineParameter(0, ParameterAttributes.In, "objectIdentifier");
            objcSendCall.DefineParameter(1, ParameterAttributes.In, "selector");

            string objectiveCSelectorBaseName;

            if (methodAttribute != null)
            {
                objectiveCSelectorBaseName = methodAttribute.CustomName;
            }
            else
            {
                objectiveCSelectorBaseName = char.ToLower(methodInfo.Name[0]) + methodInfo.Name.Substring(1);
            }

            string objectiveCSelectorName = objectiveCSelectorBaseName + ":";


            // Precompute at codegen to reduce runtime overhead.
            nint nativeSelector = (nint)GetSelectorIdentifierByName(objectiveCSelectorName);

            // Load this
            methodImplementationIL.Emit(OpCodes.Ldarg_0);
            methodImplementationIL.Emit(OpCodes.Ldfld, nativePointer);

            if (Environment.Is64BitProcess)
            {
                methodImplementationIL.Emit(OpCodes.Ldc_I8, nativeSelector);
            }
            else
            {
                methodImplementationIL.Emit(OpCodes.Ldc_I4, nativeSelector);
            }

            // 0 is this, skip it
            for (short argIndex = 1; argIndex <= parametersTypes.Length; argIndex++)
            {
                LoadArgument(methodImplementationIL, argIndex);
            }

            methodImplementationIL.Emit(OpCodes.Call, objcSendCall);
            methodImplementationIL.Emit(OpCodes.Ret);

            builder.DefineMethodOverride(methodImplementation, methodInfo);
        }

        public static bool LoadSystemFramework(string framework)
        {
            return NativeLibrary.TryLoad($"/System/Library/Frameworks/{framework}.framework/{framework}", out _);
        }

        public static void RegisterType(Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(ClassAttribute), false);

            if (attributes.Length > 0)
            {
                ClassAttribute attribute = (ClassAttribute)attributes[0];

                // Define a class with a sequencial layout.... YUP that's legal and we cannot create struct via those APIs.
                TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Class;

                TypeBuilder builder = DynamicModule.DefineType(type.Name + "Impl", typeAttributes, null, new Type[] { type });

                // First ensure that the struct layout is in place
                // All interface/protocol always have a native pointer.
                FieldInfo nativePointer = builder.DefineField("NativePointer", typeof(UIntPtr), FieldAttributes.Public);

                CreateConstructor(builder, nativePointer);

                foreach (PropertyInfo propertyInfo in type.GetProperties())
                {
                    bool isBlacklisted = false;

                    if (propertyInfo.CanRead)
                    {
                        MethodInfo propertyGetMethodInfo = type.GetMethod("get_" + propertyInfo.Name);

                        isBlacklisted = !propertyGetMethodInfo.Attributes.HasFlag(MethodAttributes.Abstract);
                    }

                    if (!isBlacklisted && propertyInfo.CanWrite)
                    {
                        MethodInfo propertySetMethodInfo = type.GetMethod("set_" + propertyInfo.Name);
                        isBlacklisted = !propertySetMethodInfo.Attributes.HasFlag(MethodAttributes.Abstract);
                    }

                    if (!isBlacklisted)
                    {
                        CreateProperty(builder, nativePointer, type, propertyInfo);
                    }
                }

                foreach (MethodInfo methodInfo in type.GetMethods())
                {
                    if (methodInfo.Attributes.HasFlag(MethodAttributes.Abstract) && !methodInfo.Attributes.HasFlag(MethodAttributes.SpecialName))
                    {
                        CreateMethod(builder, nativePointer, type, methodInfo);
                    }
                }

                Type detailType = builder.CreateType();

                UIntPtr classIdentifier = GetClassIdentifierByName(type.Name);

                //Debug.Assert(classIdentifier != UIntPtr.Zero);

                TypeDetail typeDetail = new TypeDetail(detailType, classIdentifier);

                TypeDetailMapping.Add(type, typeDetail);
            }
        }

        public static T CreateFromNativeInstance<T>(UIntPtr nativePointer)
        {
            Type sourceType = typeof(T);

            if (!TypeDetailMapping.TryGetValue(typeof(T), out TypeDetail typeDetail))
            {
                throw new InvalidOperationException($"{sourceType} is not registered as an ObjectiveC class! Make sure to call Initialize on your assembly first.");
            }

            T res = (T)Activator.CreateInstance(typeDetail.TypeImpl, new object[] { nativePointer });

            return res;
        }

        public static T CreateInstance<T>(InstanceCreationFlags creationFlags)
        {
            if (creationFlags == InstanceCreationFlags.Invalid || creationFlags == InstanceCreationFlags.Init)
            {
                throw new ArgumentException ($"{creationFlags} is not valid");
            }

            Type sourceType = typeof(T);

            if (!TypeDetailMapping.TryGetValue(typeof(T), out TypeDetail typeDetail))
            {
                throw new InvalidOperationException($"{sourceType} is not registered as an ObjectiveC class! Make sure to call Initialize on your assembly first.");
            }

            UIntPtr nativePointer = UIntPtr.Zero;

            if (creationFlags == InstanceCreationFlags.New)
            {
                nativePointer = NewObjectiveCObject(typeDetail.ClassIdentifier);
            }
            else if (creationFlags == InstanceCreationFlags.Alloc)
            {
                nativePointer = AllocObjectiveCObject(typeDetail.ClassIdentifier);
            }
            else if (creationFlags.HasFlag(InstanceCreationFlags.Alloc) && creationFlags.HasFlag(InstanceCreationFlags.Init))
            {
                nativePointer = AllocInitObjectiveCObject(typeDetail.ClassIdentifier);
            }
            else
            {
                throw new NotImplementedException(creationFlags.ToString());
            }

            if (nativePointer == UIntPtr.Zero)
            {
                throw new InvalidOperationException($"{sourceType} instanciation failed! Check the instance creation flags.");
            }

            T res = (T)Activator.CreateInstance(typeDetail.TypeImpl, new object[] { nativePointer });

            return res;
        }
    }
}
