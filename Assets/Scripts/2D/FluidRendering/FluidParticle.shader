Shader "Custom/FluidParticle"{
    Properties {
        _DisplaySize ("Display Size", Float) = 1.0
        _BlendFactor ("Blend Factor", Float) = 1.0
        _ColoringMode ("Coloring Mode", Integer) = 1
        _FlatParticleColor ("Flat Particle Color", Color) = (0.1, 0.3, 1, 1)
        _MaxDisplayVelocity ("Max Display Velocity", float) = 20.0
        _DensityDeviationRange ("Density Deviation Range", Float) = 20.0
        _ColorGradientTex ("Color Gradient Texture", 2D) = "white" {}
        _DensityColorGradientTex ("Density Color Gradient Texture", 2D) = "white" {}
        _RestDensity ("Rest Density", Float) = 10.0
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
            float _DensityDeviationRange;
            sampler2D _ColorGradientTex;
            sampler2D _DensityColorGradientTex;
            float _RestDensity;

            // Structured buffers (Per instance data)
            StructuredBuffer<float2> Positions;
            StructuredBuffer<float2> Velocities;
            StructuredBuffer<float> Densities;

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 position : SV_POSITION;
                float4 meshVertexPos : TEXCOORD1;
                float velocityMagnitude : TEXCOORD2;
                float density : TEXCOORD3;
            };


            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID) {
                Interpolators o;

                float4 obj_particleCentre = mul(unity_WorldToObject, float4(Positions[instanceID], 0.0, 1.0));
                float4 obj_finalVertPos = obj_particleCentre + v.vertex * _DisplaySize;
                
                o.position = UnityObjectToClipPos(obj_finalVertPos);
                o.meshVertexPos = float4(v.vertex.xyz, 0.0);

                // Coloring options
                o.velocityMagnitude = length(Velocities[instanceID]);
                o.density = Densities[instanceID];

                return o;
            }

            // Helper functions
            float inverseLerp(float min, float max, float current) {
                return (clamp(current, min, max) - min) / (max - min);
            }

            float4 frag (Interpolators i) : SV_Target {
                float4 finalColor = float4(0.0, 0.0, 0.0, 1.0);

                if (_ColoringMode == 0) { // Flat color
                    finalColor = float4(_FlatParticleColor.rgb, 1.0);
                }
                else if (_ColoringMode == 1) { // Velocity field gradient
                    float t_color = inverseLerp(0, _MaxDisplayVelocity, i.velocityMagnitude);
                    finalColor = tex2D(_ColorGradientTex, float2(t_color, 0.5));
                }
                else if (_ColoringMode == 2) { // Density deviation
                    float t_color = inverseLerp(_RestDensity - _DensityDeviationRange, _RestDensity + _DensityDeviationRange, i.density);
                    finalColor = tex2D(_DensityColorGradientTex, float2(t_color, 0.5));
                }

                // Apply radial transparency
                float t = length(i.meshVertexPos);
                return float4(finalColor.rgb, saturate(1.0 - t * _BlendFactor));
            }
            
            ENDCG
        }
    }
}
