Shader "Custom/Collider" {
    Properties {
        _MainColor ("Collider Color", Color) = (1, 1, 1, 1)
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightDirection ("Light Direction", Vector) = (0, -1, 0)
    }
    SubShader {
        Tags { 
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Shader properties (common data)
            float4 _MainColor;
            float4 _LightColor;
            float3 _LightDirection;

            struct MeshData {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            Interpolators vert (MeshData v) {
                Interpolators o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                float3 lightDir = normalize(_LightDirection);
                float3 normal = normalize(i.worldNormal);

                // Lambert (diffuse)
                float lambert = max(0, dot(normal, lightDir));
                float3 diffuse = lambert * _LightColor.rgb;

                float3 finalColor = _MainColor * diffuse;

                return float4(finalColor.rgb, _MainColor.a);
            }

            ENDCG
        }
    }
}
