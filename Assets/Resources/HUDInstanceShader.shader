	Shader "HUD/Instance"
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
			    float3 position;
			    float4 color;
			};
			
			StructuredBuffer<InstanceData> _instanceBuffer;
			StructuredBuffer<uint> _visibleBuffer;

			UNITY_DECLARE_TEX2DARRAY(_FontTex);
			float4 _FontTex_ST;
			float4 _Parms[511];
			float3 _TargetDirection;

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
			
            float3x3 RotationMatrix(float3 targetDir)
			{
                float3 modelForward = normalize(mul(unity_ObjectToWorld, float4(0,0,1,0)).xyz);
                float3 rotateAxis = cross(modelForward, targetDir);
                float rotateAngle = acos(dot(modelForward, targetDir));
                
                float3x3 rotationMatrix;
                float c = cos(rotateAngle);
                float s = sin(rotateAngle);
                float t = 1 - c;
                rotationMatrix[0] = float3(t * rotateAxis.x * rotateAxis.x + c, 
                                           t * rotateAxis.x * rotateAxis.y - s * rotateAxis.z, 
                                           t * rotateAxis.x * rotateAxis.z + s * rotateAxis.y);
                rotationMatrix[1] = float3(t * rotateAxis.x * rotateAxis.y + s * rotateAxis.z, 
                                           t * rotateAxis.y * rotateAxis.y + c, 
                                           t * rotateAxis.y * rotateAxis.z - s * rotateAxis.x);
                rotationMatrix[2] = float3(t * rotateAxis.x * rotateAxis.z - s * rotateAxis.y, 
                                           t * rotateAxis.y * rotateAxis.z + s * rotateAxis.x, 
                                           t * rotateAxis.z * rotateAxis.z + c);
                return rotationMatrix;
            }
			
			v2f vert(appdata v)
			{
			    v2f o;
            	UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

			    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			    uint id = _visibleBuffer[unity_InstanceID];
			    InstanceData data = _instanceBuffer[id];
			    o.color = data.color;
			    #endif
            	
                float3 worldPivot = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3x3 rotMat = RotationMatrix(normalize(_TargetDirection));
                float3 worldOffset = mul(unity_ObjectToWorld, v.vertex).xyz - worldPivot;
                float3 rotatedPos = worldPivot + mul(rotMat, worldOffset);
                o.vertex = mul(UNITY_MATRIX_VP, float4(rotatedPos, 1.0));
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