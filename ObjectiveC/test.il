// Token: 0x0200009D RID: 157
.class public sequential ansi sealed beforefieldinit MetalBindings.MTLRenderPassColorAttachmentDescriptor
	extends [System.Runtime]System.ValueType
{
	.pack 1
	.size 0

	// Fields
	// Token: 0x040002FE RID: 766
	.field public initonly native int NativePointer
	// Token: 0x040002FF RID: 767
	.field private static initonly valuetype MetalBindings.ObjectiveCClass ClassDetail

	// Methods
	// Token: 0x0600038E RID: 910 RVA: 0x00007E30 File Offset: 0x00006030
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			native int nativePointer
		) cil managed 
	{
		// Header Size: 1 byte
		// Code Size: 8 (0x8) bytes
		.maxstack 8

		/* (18,80)-(18,109) /Users/mary/testing_csharp/Metal/MTLRenderPassColorAttachmentDescriptor.cs */
		/* 0x00006031 02           */ IL_0000: ldarg.0
		/* 0x00006032 03           */ IL_0001: ldarg.1
		/* 0x00006033 7DFE020004   */ IL_0002: stfld     native int MetalBindings.MTLRenderPassColorAttachmentDescriptor::NativePointer
		/* 0x00006038 2A           */ IL_0007: ret
	} // end of method MTLRenderPassColorAttachmentDescriptor::.ctor

	// Token: 0x0600038F RID: 911 RVA: 0x00007E3C File Offset: 0x0000603C
	.method public hidebysig static 
		valuetype MetalBindings.MTLRenderPassColorAttachmentDescriptor CreateInstance () cil managed 
	{
		// Header Size: 12 bytes
		// Code Size: 19 (0x13) bytes
		// LocalVarSig Token: 0x11000005 RID: 5
		.maxstack 1
		.locals init (
			[0] valuetype MetalBindings.ObjectiveCClass
		)

		/* (20,82)-(20,154) /Users/mary/testing_csharp/Metal/MTLRenderPassColorAttachmentDescriptor.cs */
		/* 0x00006048 7EFF020004   */ IL_0000: ldsfld    valuetype MetalBindings.ObjectiveCClass MetalBindings.MTLRenderPassColorAttachmentDescriptor::ClassDetail
		/* 0x0000604D 0A           */ IL_0005: stloc.0
		/* 0x0000604E 1200         */ IL_0006: ldloca.s  V_0
		/* 0x00006050 2806070006   */ IL_0008: call      instance native int MetalBindings.ObjectiveCClass::CreateInstance()
		/* 0x00006055 738E030006   */ IL_000D: newobj    instance void MetalBindings.MTLRenderPassColorAttachmentDescriptor::.ctor(native int)
		/* 0x0000605A 2A           */ IL_0012: ret
	} // end of method MTLRenderPassColorAttachmentDescriptor::CreateInstance

	// Token: 0x06000390 RID: 912 RVA: 0x00007E5B File Offset: 0x0000605B
	.method public hidebysig specialname 
		instance valuetype MetalBindings.MTLClearColor get_ClearColor () cil managed 
	{
		// Header Size: 1 byte
		// Code Size: 22 (0x16) bytes
		.maxstack 8

		/* (24,20)-(24,81) /Users/mary/testing_csharp/Metal/MTLRenderPassColorAttachmentDescriptor.cs */
		/* 0x0000605C 02           */ IL_0000: ldarg.0
		/* 0x0000605D 7BFE020004   */ IL_0001: ldfld     native int MetalBindings.MTLRenderPassColorAttachmentDescriptor::NativePointer
		/* 0x00006062 7243830070   */ IL_0006: ldstr     "clearColor"
		/* 0x00006067 2808070006   */ IL_000B: call      valuetype MetalBindings.Selector MetalBindings.Selector::op_Implicit(string)
		/* 0x0000606C 28A7050006   */ IL_0010: call      valuetype MetalBindings.MTLClearColor MetalBindings.NativeRuntime::MTLClearColor_objc_msgSend_stret(native int, valuetype MetalBindings.Selector)
		/* 0x00006071 2A           */ IL_0015: ret
	} // end of method MTLRenderPassColorAttachmentDescriptor::get_ClearColor

	// Token: 0x06000391 RID: 913 RVA: 0x00007E72 File Offset: 0x00006072
	.method public hidebysig specialname 
		instance void set_ClearColor (
			valuetype MetalBindings.MTLClearColor 'value'
		) cil managed 
	{
		// Header Size: 1 byte
		// Code Size: 24 (0x18) bytes
		.maxstack 8

		/* (25,20)-(25,72) /Users/mary/testing_csharp/Metal/MTLRenderPassColorAttachmentDescriptor.cs */
		/* 0x00006073 02           */ IL_0000: ldarg.0
		/* 0x00006074 7BFE020004   */ IL_0001: ldfld     native int MetalBindings.MTLRenderPassColorAttachmentDescriptor::NativePointer
		/* 0x00006079 7259830070   */ IL_0006: ldstr     "setClearColor:"
		/* 0x0000607E 2808070006   */ IL_000B: call      valuetype MetalBindings.Selector MetalBindings.Selector::op_Implicit(string)
		/* 0x00006083 03           */ IL_0010: ldarg.1
		/* 0x00006084 28A8050006   */ IL_0011: call      void MetalBindings.NativeRuntime::objc_msgSend(native int, valuetype MetalBindings.Selector, valuetype MetalBindings.MTLClearColor)
		/* 0x00006089 00           */ IL_0016: nop
		/* 0x0000608A 2A           */ IL_0017: ret
	} // end of method MTLRenderPassColorAttachmentDescriptor::set_ClearColor

	// Token: 0x06000392 RID: 914 RVA: 0x00007E8B File Offset: 0x0000608B
	.method private hidebysig specialname rtspecialname static 
		void .cctor () cil managed 
	{
		// Header Size: 1 byte
		// Code Size: 16 (0x10) bytes
		.maxstack 8

		/* (16,9)-(16,131) /Users/mary/testing_csharp/Metal/MTLRenderPassColorAttachmentDescriptor.cs */
		/* 0x0000608C 7277830070   */ IL_0000: ldstr     "MTLRenderPassColorAttachmentDescriptor"
		/* 0x00006091 7305070006   */ IL_0005: newobj    instance void MetalBindings.ObjectiveCClass::.ctor(string)
		/* 0x00006096 80FF020004   */ IL_000A: stsfld    valuetype MetalBindings.ObjectiveCClass MetalBindings.MTLRenderPassColorAttachmentDescriptor::ClassDetail
		/* 0x0000609B 2A           */ IL_000F: ret
	} // end of method MTLRenderPassColorAttachmentDescriptor::.cctor

	// Properties
	// Token: 0x1700010B RID: 267
	.property instance valuetype MetalBindings.MTLClearColor ClearColor()
	{
		// Token: 0x06000390 RID: 912 RVA: 0x00007E5B File Offset: 0x0000605B
		.get instance valuetype MetalBindings.MTLClearColor MetalBindings.MTLRenderPassColorAttachmentDescriptor::get_ClearColor()
		// Token: 0x06000391 RID: 913 RVA: 0x00007E72 File Offset: 0x00006072
		.set instance void MetalBindings.MTLRenderPassColorAttachmentDescriptor::set_ClearColor(valuetype MetalBindings.MTLClearColor)
	}

} // end of class MetalBindings.MTLRenderPassColorAttachmentDescriptor
