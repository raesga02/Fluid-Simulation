Shader "Custom/BillboardShader"
{
    Properties {
        _LightColor ("Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LightDirection ("Light Direction", Vector) = (0, 1, 0)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float4 _LightColor;
            float3 _LightDirection;


            Interpolators vert (MeshData v) {
                Interpolators o;

                float3 particleCentre = float3(0.0, 0.0, 0.0);
                float displaySize = 1.0;

                float4 obj_particleCentre = mul(unity_WorldToObject, float4(particleCentre.xyz, 1.0));
                float4 obj_finalVertPos = obj_particleCentre + v.vertex * displaySize;

                float3 vpos = mul((float3x3)unity_ObjectToWorld, obj_finalVertPos.xyz);
                float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
                float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
                float4 outPos = mul(UNITY_MATRIX_P, viewPos);

                o.pos = outPos;
                o.worldPos = outPos + particleCentre;
                o.uv = v.uv;

                return o;
            }

            float3 RotateVector(float3 v, float3 axis, float angle) {
                return v * cos(angle) + cross(axis, v) * sin(angle) + axis * dot(axis, v) * (1 - cos(angle));
            }

            float4 frag (Interpolators i) : SV_Target {
                i.uv = i.uv * 2.0 - 1.0;

                float r2 = dot(i.uv, i.uv);
                if (r2 >= 1) { discard; }

                return float4(0.0, 0.0, r2, 1.0);

                float3 lightDir = normalize(_LightDirection);
                float3 cameraDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 localNormal = normalize(float3(i.uv.xy, sqrt(1.0 - r2)));

                return float4(localNormal, 1.0);

                float3 worldNormal = localNormal;

                float3 up = float3(0.0, 0.0, 1.0);
                float3 axis = normalize(cross(localNormal, cameraDir));


                float angle = acos(dot(localNormal, cameraDir));
                worldNormal = RotateVector(localNormal, axis, angle);

                
                float lambert = max(0.0, dot(worldNormal, lightDir));
                float4 particleColor = float4(1.0, 1.0, 1.0, 1.0);
                float4 color = float4(particleColor.rgb * _LightColor.rgb * lambert, particleColor.a);

                return color;

            }
            ENDCG
        }
    }
}
