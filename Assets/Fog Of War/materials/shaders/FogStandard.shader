Shader "MOBA/Fog Standard"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT MetallicSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
	

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
				
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#include "FogOfWar.cginc"
			#include "UnityStandardCore.cginc"
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

		ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers nomrt gles


			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

			#pragma vertex vert		//vertDeferred
			#pragma fragment frag	//fragDeferred

			#include "UnityStandardCore.cginc"
			#include "FogOfWar.cginc"

			struct v2f
		{
			float4 pos							: SV_POSITION;
			float4 tex							: TEXCOORD0;
			half3 eyeVec 						: TEXCOORD1;
			half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
			half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UVs			
			float3 posWorld						: TEXCOORD6;
		};

			v2f vert(VertexInput v)
		{
			v2f o;
			UNITY_INITIALIZE_OUTPUT(v2f, o);

			float4 posWorld = mul(_Object2World, v.vertex);
			o.posWorld = posWorld;
			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
			o.tex = TexCoords(v);
			o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
			float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

			float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
			o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
			o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
			o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
#else
			o.tangentToWorldAndParallax[0].xyz = 0;
			o.tangentToWorldAndParallax[1].xyz = 0;
			o.tangentToWorldAndParallax[2].xyz = normalWorld;
#endif

#ifndef LIGHTMAP_OFF
			o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			o.ambientOrLightmapUV.zw = 0;
#elif UNITY_SHOULD_SAMPLE_SH
#if (SHADER_TARGET < 30)
			o.ambientOrLightmapUV.rgb = ShadeSH9(half4(normalWorld, 1.0));
#else
			// Optimization: L2 per-vertex, L0..L1 per-pixel
			o.ambientOrLightmapUV.rgb = ShadeSH3Order(half4(normalWorld, 1.0));
#endif
#endif

#ifdef DYNAMICLIGHTMAP_ON
			o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#ifdef _PARALLAXMAP
			TANGENT_SPACE_ROTATION;
			half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
			o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
			o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
			o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
#endif

			return o;
		}

			void frag(
				v2f i,
				out half4 outDiffuse : SV_Target0,			// RT0: diffuse color (rgb), occlusion (a)
				out half4 outSpecSmoothness : SV_Target1,	// RT1: spec color (rgb), smoothness (a)
				out half4 outNormal : SV_Target2,			// RT2: normal (rgb), --unused, very low precision-- (a) 
				out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
				)
			{
	#if (SHADER_TARGET < 30)
				outDiffuse = 1;
				outSpecSmoothness = 1;
				outNormal = 0;
				outEmission = 0;
				return;
	#endif

				FRAGMENT_SETUP(s)

					// no analytic lights in this pass
					UnityLight dummyLight = DummyLight(s.normalWorld);
				half atten = 1;

				// only GI
				half occlusion = Occlusion(i.tex.xy);
				UnityGI gi = FragmentGI(
					s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, dummyLight);

				half3 color = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
				color += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);

	#ifdef _EMISSION
				color += Emission(i.tex.xy);
	#endif

	#ifndef UNITY_HDR_ON
				color.rgb = exp2(-color.rgb);
	#endif

				outDiffuse = half4(s.diffColor * GetAlphaAtWorldPoint(i.posWorld), occlusion);
				outSpecSmoothness = half4(s.specColor, s.oneMinusRoughness);
				outNormal = half4(s.normalWorld*0.5 + 0.5,1);
				outEmission = half4(color, 1);
			}

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META"
			Tags { "LightMode" = "Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
	
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0
#include "UnityStandardCore.cginc"
#include "FogOfWar.cginc"
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vert		//	vertForwardAdd
			#pragma fragment frag	//	fragForwardAdd

			struct v2f
			{
				float4 pos							: SV_POSITION;
				float4 tex							: TEXCOORD0;
				half3 eyeVec 						: TEXCOORD1;
				half4 tangentToWorldAndLightDir[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:lightDir]
				LIGHTING_COORDS(5,6)
				UNITY_FOG_COORDS(7)
				half3 posWorld						: TEXCOORD8;

					// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
		#if defined(_PARALLAXMAP)
					half3 viewDirForParallax			: TEXCOORD9;
		#endif
			};

			v2f vert(VertexInput v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.posWorld = mul(_Object2World, v.vertex).xyz;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.tex = TexCoords(v);
				o.eyeVec = NormalizePerVertexNormal(o.posWorld - _WorldSpaceCameraPos);
				float3 normalWorld = UnityObjectToWorldNormal(v.normal);
		#ifdef _TANGENT_TO_WORLD
				float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

				float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
				o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
				o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
				o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
		#else
				o.tangentToWorldAndLightDir[0].xyz = 0;
				o.tangentToWorldAndLightDir[1].xyz = 0;
				o.tangentToWorldAndLightDir[2].xyz = normalWorld;
		#endif
				//We need this for shadow receving
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				float3 lightDir = _WorldSpaceLightPos0.xyz - o.posWorld * _WorldSpaceLightPos0.w;
		#ifndef USING_DIRECTIONAL_LIGHT
				lightDir = NormalizePerVertexNormal(lightDir);
		#endif
				o.tangentToWorldAndLightDir[0].w = lightDir.x;
				o.tangentToWorldAndLightDir[1].w = lightDir.y;
				o.tangentToWorldAndLightDir[2].w = lightDir.z;

		#ifdef _PARALLAXMAP
				TANGENT_SPACE_ROTATION;
				o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
		#endif

				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				FRAGMENT_SETUP_FWDADD(s)

				UnityLight light = AdditiveLight(s.normalWorld, IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
				UnityIndirect noIndirect = ZeroIndirect();

				half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, light, noIndirect);

				UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
				return OutputForward(c * GetAlphaAtWorldPoint(i.posWorld), s.alpha);
			}

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}

	FallBack "VertexLit"
	CustomEditor "StandardShaderGUI"
}
