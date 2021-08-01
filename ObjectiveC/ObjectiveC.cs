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

        private const string NewObjectiveCObjectDelegateName = "NewObjectiveCObject";

        private delegate UIntPtr NewObjectiveCObjectDelegate(UIntPtr classIdentifier);

        private static AssemblyBuilder DynamicAssembly;
        private static ModuleBuilder DynamicModule;

        private static Type BaseBindingsType;

        private static NewObjectiveCObjectDelegate NewObjectiveCObject;

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

            ConstructObjectiveCNewFunction(baseBindingsBuilder);
            // TODO: alloc, init and alloc + init

            // Finalize the class
            BaseBindingsType = baseBindingsBuilder.CreateType();

            NewObjectiveCObject = (NewObjectiveCObjectDelegate)BaseBindingsType.GetMethod(NewObjectiveCObjectDelegateName).CreateDelegate(typeof(NewObjectiveCObjectDelegate));
        }

        private static void ConstructObjectiveCNewFunction(TypeBuilder builder)
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

                MethodBuilder slowNewPinvoke = CreateObjSendNativeCall(builder, typeof(nuint), new Type[] { typeof(nuint), typeof(nuint) });
                slowNewPinvoke.DefineParameter(0, ParameterAttributes.In, "classIdentifier");
                slowNewPinvoke.DefineParameter(1, ParameterAttributes.In, "selector");

                newFunctionEmitter.Emit(OpCodes.Call, slowNewPinvoke);
            }

            newFunctionEmitter.Emit(OpCodes.Ret);
        }

        public static void Initialize(Assembly assembly, string[] systemFrameworksRequired)
        {
            if (!InitializedAssemblies.Contains(assembly))
            {
                foreach (string systemFramework in systemFrameworksRequired)
                {
                    LoadSystemFramework(systemFramework);
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

                string objectiveCSelectorReadName = char.ToLower(propertyInfo.Name[0]) + propertyInfo.Name.Substring(1);

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

                string objectiveCSelectorSetName = "set" + propertyInfo.Name + ":";

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
                propertySetIL.Emit(OpCodes.Nop);
                propertySetIL.Emit(OpCodes.Ret);

                propertyBuilder.SetSetMethod(propertySet);

                builder.DefineMethodOverride(propertySet, propertyInfo.GetSetMethod());
            }
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
                    CreateProperty(builder, nativePointer, type, propertyInfo);
                }
                // TODO: the rest (Attributes and functions)

                Type detailType = builder.CreateType();

                UIntPtr classIdentifier = GetClassIdentifierByName(type.Name);
                Console.WriteLine(type.Name);

                Debug.Assert(classIdentifier != UIntPtr.Zero);

                TypeDetail typeDetail = new TypeDetail(detailType, classIdentifier);

                TypeDetailMapping.Add(type, typeDetail);
            }
        }

        public static T CreateInstance<T>(InstanceCreationFlags creationFlags)
        {
            if (creationFlags == InstanceCreationFlags.Invalid)
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
            // TODO: alloc, init and alloc + init
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
