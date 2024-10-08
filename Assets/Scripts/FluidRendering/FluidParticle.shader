Shader "Custom/FluidParticle"{
    Properties {
        _DisplaySize ("Display Size", Float) = 1.0
        _Color ("Particle Color", Color) = (0.1, 0.3, 1, 1)
        _BlendFactor ("Blend Factor", Float) = 1.0
    }
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }

        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Shader properties (Common data)
            float _DisplaySize;
            float4 _Color;
            float _BlendFactor;

            // Structured buffers (Per instance data)
            StructuredBuffer<float2> Positions;
            StructuredBuffer<float2> Velocities;

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 position : SV_POSITION;
                float4 mesh_vertex_pos : TEXCOORD0;
                float4 color : COLOR;
            };


            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID) {
                Interpolators o;

                float4 obj_particleCentre = mul(unity_WorldToObject, float4(Positions[instanceID], 0.0, 1.0));
                float4 obj_finalVertPos = obj_particleCentre + v.vertex * _DisplaySize;
                
                o.position = UnityObjectToClipPos(obj_finalVertPos);
                o.color = o.color = float4(length(Velocities[instanceID]) / 3.0, length(Velocities[instanceID]) / 1.5, 0.5, 1.0);
                o.mesh_vertex_pos = float4(v.vertex.xyz, 0.0);

                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                float t = length(i.mesh_vertex_pos);
                return float4(i.color.rgb, saturate(1.0 - t * _BlendFactor));
            }
            ENDCG
        }
    }
}
