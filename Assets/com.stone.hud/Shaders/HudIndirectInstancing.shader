// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

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
				float4 parm : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
			};

			struct InstanceData
			{
				int visible;
			    float3 position;
				int nameIndex;
				float progress;
			};
			
			StructuredBuffer<InstanceData> _instanceBuffer;
			StructuredBuffer<uint> _visibleBuffer;

			UNITY_DECLARE_TEX2DARRAY(_FontTex);
			float4 _FontTex_ST;
			float4 _Parms[511];

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
			
			v2f vert(appdata v)
			{
			    v2f o;
            	UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
            	
			    float3 cameraForward = UNITY_MATRIX_V[2].xyz;
			    float3 cameraUp = UNITY_MATRIX_V[1].xyz;
            	
			    float3 nomalLocal = normalize(mul(unity_WorldToObject, float4(cameraForward, 1)));
			    float3 upLocal = normalize(mul(unity_WorldToObject, float4(cameraUp, 1)));
            	
                float3 rightLocal = normalize(cross(nomalLocal, upLocal)); //计算新的right轴
                upLocal = cross(rightLocal, nomalLocal);	//计算新的up轴。

                float3  BBLocalPos = rightLocal * v.vertex.x + upLocal * v.vertex.y;
                o.vertex = UnityObjectToClipPos(float4(BBLocalPos, 1.0));
            	o.color = v.color;
			    return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 color = i.color;

				// if (i.color.a > 1.0)
				// {
				// 	clip(i.parm.x - i.uv.x);//通过uv计算进度，uv从0-1
				// 	color = i.color;
				// }
				// else
				// {
				// 	float3 uv = float3(i.uv.xy, i.parm.y);
				// 	color = UNITY_SAMPLE_TEX2DARRAY(_FontTex, uv);
				// }
				return color;
			}
			ENDCG
		}
	}
}