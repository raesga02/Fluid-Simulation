Shader "Custom/MarchShader" {
    Properties {
        _BaseColor ("Base Color", Color) = (0.1, 0.3, 1, 1)
    }
    SubShader {
        Tags { 
            "RenderType"="Opaque" 
        }

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawArgs
            #include "UnityIndirect.cginc"

            struct Interpolators {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            // Shader properties (Common data)
            float4 _BaseColor;

            // Structured Buffers (Per instance data)
            StructuredBuffer<float3> _Vertices;
            StructuredBuffer<float3> _Normals;
            StructuredBuffer<int> _Triangles;


            Interpolators vert (uint svVertexID: SV_VertexID, uint svInstanceID : SV_InstanceID) {
                InitIndirectDrawArgs(0);
                Interpolators o;

                uint vertexID = GetIndirectVertexID(svVertexID);
                float3 v = _Vertices[vertexID];
                float3 normal = _Normals[vertexID];

                o.position = mul(UNITY_MATRIX_VP, float4(v, 1.0));
                o.normal = normal;

                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                float3 lightDir = _WorldSpaceLightPos0;
                float shading = dot(lightDir, normalize(i.normal)) * 0.5 + 0.5;
                return _BaseColor * shading;
            }

            ENDCG
        }
    }
}
