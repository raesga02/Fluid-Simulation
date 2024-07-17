Shader "Custom/FluidParticle"{
    Properties {
        _ScaleFactor ("Scale Factor", Float) = 1.0
        _Color ("Particle Color", Color) = (0.1, 0.3, 1, 1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Shader properties (Common data)
            float _ScaleFactor;
            float4 _Color;

            // Structured buffers (Per instance data)
            StructuredBuffer<float2> Positions;

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };


            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID) {
                Interpolators o;
                float2 world_ParticleCentre = Positions[instanceID];
                float2 world_VertPos = world_ParticleCentre + mul(unity_ObjectToWorld, v.vertex * _ScaleFactor);
                float2 local_VertPos = mul(unity_WorldToObject, world_VertPos);
                
                o.position = UnityObjectToClipPos(float3(local_VertPos.xy, 0));
                o.color = _Color;

                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                return i.color;
            }
            ENDCG
        }
    }
}
