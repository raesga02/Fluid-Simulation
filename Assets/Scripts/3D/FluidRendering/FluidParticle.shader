Shader "Custom/FluidParticle3D"{
    Properties {
        _DisplaySize ("Display Size", Float) = 1.0
        _BlendFactor ("Blend Factor", Float) = 1.0
        _ColoringMode ("Coloring Mode", Integer) = 1
        _FlatParticleColor ("Flat Particle Color", Color) = (0.1, 0.3, 1, 1)
        _MaxDisplayVelocity ("Max Display Velocity", Float) = 20.0
        _ColorGradientTex ("Color Gradient Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }

        Pass {
            // Shader configuration
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Shader properties (Common data)
            float _DisplaySize;
            float _BlendFactor;
            int _ColoringMode;
            float4 _FlatParticleColor;
            float _MaxDisplayVelocity;
            sampler2D _ColorGradientTex;

            // Structured buffers (Per instance data)
            StructuredBuffer<float3> Positions;
            StructuredBuffer<float3> Velocities;

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float4 meshVertexPos : TEXCOORD1;
                float velocityMagnitude : TEXCOORD2;
            };


            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID) {
                Interpolators o;

                float4 obj_particleCentre = mul(unity_WorldToObject, float4(Positions[instanceID].xyz, 1.0));
                float4 obj_finalVertPos = obj_particleCentre + v.vertex * _DisplaySize;
                
                o.position = UnityObjectToClipPos(obj_finalVertPos);
                o.meshVertexPos = float4(v.vertex.xyz, 0.0);

                // Coloring options
                o.color = float4(_FlatParticleColor);
                o.velocityMagnitude = length(Velocities[instanceID]);

                return o;
            }

            // Helper functions
            float inverseLerp(float min, float max, float current) {
                return (clamp(current, min, max) - min) / (max - min);
            }

            float4 frag (Interpolators i) : SV_Target {
                float t = length(i.meshVertexPos);
                float4 baseColor = i.color;

                if (_ColoringMode == 1) {   // Velocity field gradient
                    float t_color = inverseLerp(0, _MaxDisplayVelocity, i.velocityMagnitude);
                    baseColor = tex2D(_ColorGradientTex, float2(t_color, 0.5));
                }
                return float4(baseColor.rgb, saturate(1.0 - t * _BlendFactor));
            }
            
            ENDCG
        }
    }
}
