/*

// Add the following directive

		#include "Assets/BOXOPHOBIC/Atmospheric Height Fog/Core/Library/AtmosphericHeightFog.cginc"

// Apply Atmospheric Height Fog to transparent shaders like this
// Where finalColor is the shader output color, fogParams.rgb is the fog color and fogParams.a is the fog mask

		float4 fogParams = GetAtmosphericHeightFog(i.worldPos);
		return ApplyAtmosphericHeightFog(finalColor, fogParams);

*/

#ifndef ATMOSPHERIC_HEIGHT_FOG_INCLUDED
#define ATMOSPHERIC_HEIGHT_FOG_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"

uniform half _DirectionalCat;
uniform half _SkyboxCat;
uniform half _FogAxisMode;
uniform half _FogCat;
uniform half _AdvancedCat;
uniform half _NoiseCat;
uniform half4 AHF_FogColorStart;
uniform half4 AHF_FogColorEnd;
uniform half AHF_FogDistanceStart;
uniform half AHF_FogDistanceEnd;
uniform half AHF_FogDistanceFalloff;
uniform half AHF_FogColorDuo;
uniform half4 AHF_DirectionalColor;
uniform half3 AHF_DirectionalDir;
uniform half AHF_DirectionalIntensity;
uniform half AHF_DirectionalFalloff;
uniform half3 AHF_FogAxisOption;
uniform half AHF_FogHeightEnd;
uniform half AHF_FogHeightStart;
uniform half AHF_FogHeightFalloff;
uniform half AHF_FogLayersMode;
uniform half AHF_NoiseScale;
uniform half3 AHF_NoiseSpeed;
uniform half AHF_NoiseDistanceEnd;
uniform half AHF_NoiseIntensity;
uniform half AHF_FogIntensity;
float4 mod289(float4 x)
{
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 perm(float4 x)
{
	return mod289(((x * 34.0) + 1.0) * x);
}

float SimpleNoise3D(float3 p)
{
	float3 a = floor(p);
	float3 d = p - a;
	d = d * d * (3.0 - 2.0 * d);
	float4 b = a.xxyy + float4(0.0, 1.0, 0.0, 1.0);
	float4 k1 = perm(b.xyxy);
	float4 k2 = perm(k1.xyxy + b.zzww);
	float4 c = k2 + a.zzzz;
	float4 k3 = perm(c);
	float4 k4 = perm(c + 1.0);
	float4 o1 = frac(k3 * (1.0 / 41.0));
	float4 o2 = frac(k4 * (1.0 / 41.0));
	float4 o3 = o2 * d.z + o1 * (1.0 - d.z);
	float2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);
	return o4.y * d.y + o4.x * (1.0 - d.y);
}

// Returns the fog color and alpha based on world position
float4 GetAtmosphericHeightFog(float3 positionWS)
{
	float4 finalColor;

	float3 WorldPosition = positionWS;

	float3 WorldPosition2_g983 = WorldPosition;
	float temp_output_7_0_g985 = AHF_FogDistanceStart;
	float temp_output_155_0_g983 = saturate(((distance(WorldPosition2_g983, _WorldSpaceCameraPos) - temp_output_7_0_g985) / (AHF_FogDistanceEnd - temp_output_7_0_g985)));
#ifdef AHF_DISABLE_FALLOFF
	float staticSwitch467_g983 = temp_output_155_0_g983;
#else
	float staticSwitch467_g983 = pow(abs(temp_output_155_0_g983), AHF_FogDistanceFalloff);
#endif
	half FogDistanceMask12_g983 = staticSwitch467_g983;
	float3 lerpResult258_g983 = lerp((AHF_FogColorStart).rgb, (AHF_FogColorEnd).rgb, (saturate((FogDistanceMask12_g983 - 0.5)) * AHF_FogColorDuo));
	float3 normalizeResult318_g983 = normalize((WorldPosition2_g983 - _WorldSpaceCameraPos));
	float dotResult145_g983 = dot(normalizeResult318_g983, AHF_DirectionalDir);
	float temp_output_140_0_g983 = ((dotResult145_g983 * 0.5 + 0.5) * AHF_DirectionalIntensity);
#ifdef AHF_DISABLE_FALLOFF
	float staticSwitch470_g983 = temp_output_140_0_g983;
#else
	float staticSwitch470_g983 = pow(abs(temp_output_140_0_g983), AHF_DirectionalFalloff);
#endif
	float DirectionalMask30_g983 = staticSwitch470_g983;
	float3 lerpResult40_g983 = lerp(lerpResult258_g983, (AHF_DirectionalColor).rgb, DirectionalMask30_g983);
#ifdef AHF_DISABLE_DIRECTIONAL
	float3 staticSwitch442_g983 = lerpResult258_g983;
#else
	float3 staticSwitch442_g983 = lerpResult40_g983;
#endif
	float3 temp_output_2_0_g984 = staticSwitch442_g983;
	float3 gammaToLinear3_g984 = GammaToLinearSpace(temp_output_2_0_g984);
#ifdef UNITY_COLORSPACE_GAMMA
	float3 staticSwitch1_g984 = temp_output_2_0_g984;
#else
	float3 staticSwitch1_g984 = gammaToLinear3_g984;
#endif
	half3 Final_Color462_g983 = staticSwitch1_g984;
	half3 AHF_FogAxisOption181_g983 = AHF_FogAxisOption;
	float3 break159_g983 = (WorldPosition2_g983 * AHF_FogAxisOption181_g983);
	float temp_output_7_0_g986 = AHF_FogHeightEnd;
	float temp_output_167_0_g983 = saturate((((break159_g983.x + break159_g983.y + break159_g983.z) - temp_output_7_0_g986) / (AHF_FogHeightStart - temp_output_7_0_g986)));
#ifdef AHF_DISABLE_FALLOFF
	float staticSwitch468_g983 = temp_output_167_0_g983;
#else
	float staticSwitch468_g983 = pow(abs(temp_output_167_0_g983), AHF_FogHeightFalloff);
#endif
	half FogHeightMask16_g983 = staticSwitch468_g983;
	float lerpResult328_g983 = lerp((FogDistanceMask12_g983 * FogHeightMask16_g983), saturate((FogDistanceMask12_g983 + FogHeightMask16_g983)), AHF_FogLayersMode);
	float mulTime204_g983 = _Time.y * 2.0;
	float3 temp_output_197_0_g983 = ((WorldPosition2_g983 * (1.0 / AHF_NoiseScale)) + (-AHF_NoiseSpeed * mulTime204_g983));
	float3 p1_g987 = temp_output_197_0_g983;
	float localSimpleNoise3D1_g987 = SimpleNoise3D(p1_g987);
	float temp_output_7_0_g989 = AHF_NoiseDistanceEnd;
	half NoiseDistanceMask7_g983 = saturate(((distance(WorldPosition2_g983, _WorldSpaceCameraPos) - temp_output_7_0_g989) / (0.0 - temp_output_7_0_g989)));
	float lerpResult198_g983 = lerp(1.0, (localSimpleNoise3D1_g987 * 0.5 + 0.5), (NoiseDistanceMask7_g983 * AHF_NoiseIntensity));
	half NoiseSimplex3D24_g983 = lerpResult198_g983;
#ifdef AHF_DISABLE_NOISE3D
	float staticSwitch42_g983 = lerpResult328_g983;
#else
	float staticSwitch42_g983 = (lerpResult328_g983 * NoiseSimplex3D24_g983);
#endif
	float temp_output_454_0_g983 = (staticSwitch42_g983 * AHF_FogIntensity);
	half Final_Alpha463_g983 = temp_output_454_0_g983;
	float4 appendResult114_g983 = (float4(Final_Color462_g983, Final_Alpha463_g983));
	float4 appendResult457_g983 = (float4(WorldPosition2_g983, 1.0));
#ifdef AHF_DEBUG_WORLDPOS
	float4 staticSwitch456_g983 = appendResult457_g983;
#else
	float4 staticSwitch456_g983 = appendResult114_g983;
#endif


	finalColor = staticSwitch456_g983;
	return finalColor;
}

// Applies the fog
float3 ApplyAtmosphericHeightFog(float3 color, float4 fog)
{
	return float3(lerp(color.rgb, fog.rgb, fog.a));
}

float4 ApplyAtmosphericHeightFog(float4 color, float4 fog)
{
	return float4(lerp(color.rgb, fog.rgb, fog.a), color.a);
}

#endif
