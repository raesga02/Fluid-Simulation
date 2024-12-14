Shader "Custom/MarchShader" {
    Properties {
        _LightDirection ("Light Direction", Vector) = (0, 0, -1)
    }
    SubShader {
        Tags { 
            "RenderType"="Opaque" 
        }

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            #include "UnityCG.cginc"

            struct Vertex {
                float3 position;
                float3 normal;
            };

            // Shader properties (Common data)
            float3 _LightDirection;

            // Structured Buffers (Per instance data)
            StructuredBuffer<Vertex> _Vertices;
            StructuredBuffer<int> _Indices;

            struct Interpolators {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
            };


            Interpolators vert (uint vertexID: SV_VertexID) {
                Interpolators o;

                int index = _Indices[vertexID];
                Vertex v = _Vertices[index];

                o.position = mul(UNITY_MATRIX_VP, float4(v.position, 1.0));
                o.normal = v.normal;

                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                float4 col = float4(i.normal, 1);
                return col;
            }

            ENDCG
        }
    }
}
