Shader "ST/HudIndirectInstancing"
{
	Properties
	{
		_FontTex("FontTexture", 2DArray) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		
		ZWrite Off
        ZTest Off
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma require 2darray
			
			#include "UnityCG.cginc"
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
			
			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 param : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
			};

			struct InstanceData
			{
				int visible;
			    float3 position;
				int index;
				float progress;
			};
			
			StructuredBuffer<InstanceData> _instanceBuffer;
			StructuredBuffer<uint> _visibleBuffer;
			
			UNITY_DECLARE_TEX2DARRAY(_FontTex);
			float4 _FontTex_ST;
			fixed4 _Color;
			
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            void setup()
            {
            	uint id = _visibleBuffer[unity_InstanceID];
			    float3 pos = _instanceBuffer[id].position;
            	float4x4 mat = unity_ObjectToWorld;
            	mat[0][3] = pos.x;
				mat[1][3] = pos.y;
				mat[2][3] = pos.z;
				unity_ObjectToWorld = mat;
            }
            #endif
			
			v2f vert(appdata v, uint instanceID: SV_InstanceID)
			{
			    v2f o;
            	UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

            	uint id = _visibleBuffer[instanceID];
				InstanceData data = _instanceBuffer[id];
            	
			    float3 cameraForward = UNITY_MATRIX_V[2].xyz;
			    float3 cameraUp = UNITY_MATRIX_V[1].xyz;
            	
			    float3 normalLocal = normalize(mul(unity_WorldToObject, float4(cameraForward, 1)));
			    float3 upLocal = normalize(mul(unity_WorldToObject, float4(cameraUp, 1)));
            	
                float3 rightLocal = normalize(cross(normalLocal, upLocal));
                upLocal = cross(rightLocal, normalLocal);

                float3 pos = rightLocal * v.vertex.x + upLocal * v.vertex.y + normalLocal * v.vertex.z;
                o.vertex = UnityObjectToClipPos(float4(pos, 1.0));
            	
            	o.uv = TRANSFORM_TEX(v.uv, _FontTex);
            	o.color = v.color;
            	o.param = float2(data.index, data.progress);
			    return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float factor = step(i.color.a, 1);
				
				float3 uv = float3(i.uv.xy, i.param.x);
				fixed4 color1 = UNITY_SAMPLE_TEX2DARRAY(_FontTex, uv);
				
				float t = step(i.param.y, i.uv.x);
				fixed4 color2 = lerp(i.color, _Color, t);
				
				return lerp(color2, color1, factor);;
			}
			ENDCG
		}
	}
}