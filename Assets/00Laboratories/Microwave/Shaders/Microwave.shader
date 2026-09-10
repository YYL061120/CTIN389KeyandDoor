Shader "OOLaboratories/Microwave" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.2
		_MetallicGlossMap ("Metallic", 2D) = "white" {}
		_OcclusionMap ("Occlusion", 2D) = "white" {}
		_BumpMap ("Normal map", 2D) = "bump" {}
		_GlossMapScale ("Smoothness", Range(0,1)) = 0.5
		_ParallaxMap("Height Map", 2D) = "white" {}
		_Parallax("Height Power", Range(0,.125)) = 0
		_EffectTex("Effect Texture", 2D) = "white" {}
		_LightColor("Light Color", Color) = (1,0.223493,0.017662,1)
		_DisplayTex("Display Render Texture", 2D) = "black" {}
	}
	SubShader {
		Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#include "UnityPBSLighting.cginc"
		#pragma surface surf Standard fullforwardshadows alphatest:_Cutoff

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _MetallicGlossMap;
		sampler2D _OcclusionMap;
		sampler2D _BumpMap;
		sampler2D _ParallaxMap;
		sampler2D _EffectTex;
		sampler2D _DisplayTex;

		struct Input {
			float2 uv_MainTex;
			float2 uv_MetallicGlossMap;
			float2 uv_OcclusionMap;
			float2 uv_BumpMap;
			float2 uv_ParallaxMap;
			float2 uv_EffectTex;
			float3 viewDir;
		};

		float _Parallax;
		half _GlossMapScale;
		fixed4 _Color;
		fixed4 _LightColor;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Handle height map
			float2 texOffset = ParallaxOffset(tex2D(_ParallaxMap, IN.uv_ParallaxMap).r, _Parallax, IN.viewDir);
			float2 mainTexUV = IN.uv_MainTex + texOffset;
			// Albedo comes from a texture tinted by color
			fixed4 albedo = tex2D (_MainTex, mainTexUV) * _Color;
			fixed4 metallic = tex2D (_MetallicGlossMap, IN.uv_MetallicGlossMap + texOffset) * _Color;
			fixed4 occlusion = tex2D(_OcclusionMap, IN.uv_OcclusionMap + texOffset) * _Color;
			fixed4 normal = tex2D (_BumpMap, IN.uv_BumpMap + texOffset);
			fixed4 effect = tex2D(_EffectTex, IN.uv_EffectTex);
			o.Albedo = albedo.rgb;
			o.Occlusion = occlusion.rgb;
			// Metallic and smoothness come from slider variables
			o.Normal = UnpackNormal(normal);
			o.Metallic = metallic.rgb;
			o.Smoothness = metallic.a; // _GlossMapScale * 0.5;
			o.Alpha = albedo.a;

			o.Emission = half3(_LightColor.rgb * effect.b * 10 * _LightColor.a);

			// draw the microwave display in a very specific region.
			if (IN.uv_MainTex.x > 0.251 && IN.uv_MainTex.x < 0.376 && IN.uv_MainTex.y > 0.054 && IN.uv_MainTex.y < 0.102)
			{
				// convert the rectangle back to UV 0-1 space.
				fixed2 uv_Display = fixed2((IN.uv_MainTex.x - 0.251) / (0.376 - 0.251), (IN.uv_MainTex.y - 0.054) / (0.102 - 0.054));
				o.Emission = tex2D(_DisplayTex, uv_Display).rgb * 5;
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}
