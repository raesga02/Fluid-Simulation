Shader "Custom/FluidParticle3D"{
    Properties {
        _DisplaySize ("Display Size", Float) = 1.0
        _ColoringMode ("Coloring Mode", Integer) = 1
        _FlatParticleColor ("Flat Particle Color", Color) = (0.1, 0.3, 1, 1)
        _MaxDisplayVelocity ("Max Display Velocity", Float) = 20.0
        _ColorGradientTex ("Color Gradient Texture", 2D) = "white" {}
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightDirection ("Light Direction", Vector) = (0, -1, 0)
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
            float4 _FlatParticleColor;
            float _MaxDisplayVelocity;
            sampler2D _ColorGradientTex;

            float4 _LightColor;
            float3 _LightDirection;

            // Structured buffers (Per instance data)
            StructuredBuffer<float3> Positions;
            StructuredBuffer<float3> Velocities;

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

                return o;
            }

            // Helper functions
            float inverseLerp(float min, float max, float current) {
                return (clamp(current, min, max) - min) / (max - min);
            }

            float4 frag (Interpolators i) : SV_Target {

                if (_ColoringMode == 0) { // Flat color
                    return float4(_FlatParticleColor.rgb, 1.0);
                }
                else if (_ColoringMode == 1) {   // Velocity field gradient
                    float t_color = inverseLerp(0, _MaxDisplayVelocity, i.velocityMagnitude);
                    return tex2D(_ColorGradientTex, float2(t_color, 0.5));
                }
                else if (_ColoringMode == 2) {
                    float3 lightDir = normalize(_LightDirection);
                    float3 normal = normalize(i.worldNormal);

                    float lambert = max(0, dot(normal, lightDir));
                    float3 diffuse = lambert * _LightColor.rgb;
                    float3 ambientLight = float3(0.1, 0.1, 0.1);

                    float3 finalColor = _FlatParticleColor * diffuse + ambientLight;
                    
                    return float4(finalColor.rgb, _FlatParticleColor.a);
                }
                

                return float4(0.0, 0.0, 0.0, 1.0);
            }
            
            ENDCG
        }
    }
}
