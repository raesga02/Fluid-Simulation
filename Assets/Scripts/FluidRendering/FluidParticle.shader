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

                // Particle centre in world space
                float4 world_ParticleCentre = float4(Positions[instanceID], 0, 1);

                // Convert mesh vertex from object space -> world space (with scaling)
                float4 world_finalVertPos = world_ParticleCentre + mul(unity_ObjectToWorld, _ScaleFactor * v.vertex);

                // Obtain the final position of the vertex in object local space
                float4 object_finalVertPos = mul(unity_WorldToObject, world_finalVertPos);
                
                // Obtain the position of the vertex from object space -> clip space
                o.position = UnityObjectToClipPos(object_finalVertPos);
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
