#if defined(SM6) || defined(VULKAN)
// Macros for targetting shader model 6.0 (DX12) or Vulkan

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_6_0 vsname (); PixelShader = compile ps_6_0 psname(); } }

#elif defined(SM4)
// Macros for targetting shader model 4.0 (DX11)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_4_0_level_9_1 vsname (); PixelShader = compile ps_4_0_level_9_1 psname(); } }

#else
// Macros for targetting shader model 2.0 (DX9 or OpenGL ES 2.0)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_2_0 vsname (); PixelShader = compile ps_2_0 psname(); } }

#endif
