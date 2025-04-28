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
            #pragma target 4.5
			#pragma require 2darray				//需要2d array
			
			#include "UnityCG.cginc"
			
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
			    matrix trs;
			    float4 color;
			};
			
			#if SHADER_TARGET >= 45
			StructuredBuffer<InstanceData> _instanceBuffer;
			StructuredBuffer<uint> _visibleBuffer;
            #endif

			UNITY_DECLARE_TEX2DARRAY(_FontTex);
			float4 _FontTex_ST;
			float4 _Parms[511];
			float3 _CameraPosition;


			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
			    uint id = _visibleBuffer[instanceID];
			    InstanceData data = _instanceBuffer[id];
				
				// // 顶点着色器中计算
				// float3 center = data.trs._m03_m13_m23; // 获取物体中心世界坐标
				// float3 viewDir = normalize(center - _WorldSpaceCameraPos); // 摄像机方向
    //
				// float3 right = normalize(cross(float3(0,0,1), viewDir));
				// float3 up = normalize(cross(viewDir, right));
    //
				// float3 localPos = v.vertex.xyz;
				// float3 worldPos = center + right * localPos.x + up * localPos.y;
				// 				
				// // // 应用旋转
			 // //    float3 pos = mul(data.rotation, v.vertex.xyz);
			 // //    pos += data.position;
    // //
				// //     // 视图变换
				// // o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
				// //
    //
			 //    v2f o;
			 //    o.vertex = UnityObjectToClipPos(worldPos);



				float3 center = float3(0, 0, 0);
					//物体空间原点
			     //将相机位置转换至物体空间并计算相对原点朝向，物体旋转后的法向将与之平行，这里实现的是Viewpoint-oriented Billboard
			     float3 viewer = mul(data.trs,float4(_WorldSpaceCameraPos, 1));
			     float3 normalDir = viewer - center;
			     // _VerticalBillboarding为0到1，控制物体法线朝向向上的限制，实现Axial Billboard到World-Oriented Billboard的变换
			     normalDir.y =normalDir.y * 0.5;
			     normalDir = normalize(normalDir);
			     //若原物体法线已经朝向上，这up为z轴正方向，否者默认为y轴正方向
			     float3 upDir = abs(normalDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
			     //利用初步的upDir计算righDir，并以此重建准确的upDir，达到新建转向后坐标系的目的
			     float3 rightDir = normalize(cross(upDir, normalDir));     upDir = normalize(cross(normalDir, rightDir));
			     // 计算原物体各顶点相对位移，并加到新的坐标系上
			     float3 centerOffs = v.vertex.xyz - center;
			     float3 localPos = center + rightDir * centerOffs.x + upDir * centerOffs.y + normalDir * centerOffs.z;

				v2f o;
			     o.vertex = UnityObjectToClipPos(float4(localPos, 1));

				
			    o.color = data.color;
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