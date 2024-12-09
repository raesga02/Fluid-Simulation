Shader "Custom/NormalDisplay"
{
    Properties {
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct MeshData {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };


            Interpolators vert (MeshData v) {
                Interpolators o;
                
                float4 world_pos = mul(UNITY_MATRIX_M, v.vertex);
                float4 view_pos = mul(UNITY_MATRIX_V, world_pos);
                float4 clip_pos = mul(UNITY_MATRIX_P, view_pos);

                o.vertex = clip_pos;

                o.normal = v.normal;
                o.uv = v.uv;
                return o;
            }

            float4 frag (Interpolators i) : SV_Target {  
                return float4(i.normal, 1.0);
            }

            ENDCG
        }
    }
}
