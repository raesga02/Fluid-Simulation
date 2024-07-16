Shader "Custom/FluidParticle"{
    Properties {
        
    }
    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Input data
            StructuredBuffer<float2> Positions;

            float displayScale = 0.5;

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // float3 normals : NORMAL;
                // float4 tangent : TANGENT;
                // float4 color : COLOR;
            };

            struct Interpolators {
                float4 vertex : SV_POSITION; // clip-space position
                // float2 uv : TEXCOORD0;
            };


            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID) {
                Interpolators o;
                float2 particleCentre = Positions[instanceID];
                float4 finalPos = float4(particleCentre.xy, 0.0, 0.0) + v.vertex;
                o.vertex = UnityObjectToClipPos(finalPos); // * model view matrix, local space -> clip space
                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                return float4(0.1, 0.3, 1, 1);
            }
            ENDCG
        }
    }
}
