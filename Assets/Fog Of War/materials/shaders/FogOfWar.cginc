#include "HLSLSupport.cginc"
#include "UnityCG.cginc"

sampler2D _Fog_Texture;
float3 _FogObjectWorldPosition;
half _FogAreaSize;

struct FogData {
	float4 vertex : SV_POSITION;
	float value : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
};

half GetAlphaAtWorldPoint(float3 position)
{
	//	(position - (WorldPos - (Area *.5)) / Area

	half2 uv = half2((position.x - (_FogObjectWorldPosition.x - (_FogAreaSize *.5))),
		(position.z - (_FogObjectWorldPosition.z - (_FogAreaSize *.5)))) / _FogAreaSize;

	half col = tex2D(_Fog_Texture, uv).r;
	return clamp(col * 2, .5f, 1);
}
