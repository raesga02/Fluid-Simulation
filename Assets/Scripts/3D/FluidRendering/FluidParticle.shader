Shader "Custom/FluidParticle3D"{
    Properties {
        _DisplaySize ("Display Size", Float) = 1.0
        _ColoringMode ("Coloring Mode", Integer) = 1
        _UseLambertIllumination("Use Lambert Illumination", Integer) = 0
        _FlatParticleColor ("Flat Particle Color", Color) = (0.1, 0.3, 1, 1)
        _MaxDisplayVelocity ("Max Display Velocity", Float) = 20.0
        _DensityDeviationRange ("Density Deviation Range", Float) = 20.0
        _ColorGradientTex ("Color Gradient Texture", 2D) = "white" {}
        _DensityColorGradientTex ("Density Color Gradient Texture", 2D) = "white" {}
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightDirection ("Light Direction", Vector) = (0, -1, 0)
        _RestDensity ("Rest Density", Float) = 10.0
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

            // Shader properties (Common data)
            float _DisplaySize;
            int _ColoringMode;
            int _UseLambertIllumination;
            float4 _FlatParticleColor;
            float _MaxDisplayVelocity;
            float _DensityDeviationRange;
            sampler2D _ColorGradientTex;
            sampler2D _DensityColorGradientTex;

            float4 _LightColor;
            float3 _LightDirection;

            // Structured buffers (Per instance data)
            StructuredBuffer<float3> Positions;
            StructuredBuffer<float3> Velocities;
            StructuredBuffer<float> Densities;

            struct MeshData {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 position : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 meshVertexPos : TEXCOORD1;
                float velocityMagnitude : TEXCOORD2;
                float density : TEXCOORD3;
            };


            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID) {
                Interpolators o;

                float4 obj_particleCentre = mul(unity_WorldToObject, float4(Positions[instanceID].xyz, 1.0));
                float4 obj_finalVertPos = obj_particleCentre + v.vertex * _DisplaySize;
                
                o.position = UnityObjectToClipPos(obj_finalVertPos);
                o.meshVertexPos = float4(v.vertex.xyz, 0.0);
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

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
                else if (_ColoringMode == 1) {   // Velocity field gradient
                    float t_color = inverseLerp(0, _MaxDisplayVelocity, i.velocityMagnitude);
                    finalColor = tex2D(_ColorGradientTex, float2(t_color, 0.5));
                }
                else if (_ColoringMode == 2) { // Density deviation
                    float t_color = inverseLerp(0.0, _DensityDeviationRange, i.density);
                    finalColor = tex2D(_DensityColorGradientTex, float2(t_color, 0.5));
                }

                // If lambert illumination activated
                if (_UseLambertIllumination == 1) {
                    float3 colorBeforeLambert = finalColor.rgb;
                    float3 lightDir = normalize(_LightDirection);
                    float3 normal = normalize(i.worldNormal);

                    float lambert = max(0, dot(normal, lightDir));
                    float3 diffuse = lambert * _LightColor.rgb;
                    float3 ambientLight = float3(0.25, 0.25, 0.25);

                    float3 illuminatedColor = colorBeforeLambert * diffuse + ambientLight * colorBeforeLambert;
                    
                    finalColor =  float4(illuminatedColor.rgb, 1.0);
                }

                return finalColor;
            }
            
            ENDCG
        }
    }
}
