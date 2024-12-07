Shader "Custom/BillboardShader"
{
    Properties {
        _ParticlePosition ("Particle Position", Vector) = (0, 0, 0)
        _DisplaySize ("Display Size", float) = 1.0
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
                float3x3 rotationMatrix : TEXCOORD1;
            };

            float3 _ParticlePosition;
            float _DisplaySize;

            float3x3 GetRotationMatrixToCamera(float3 localForward, float3 toCamera) {
                float3 axis = normalize(cross(toCamera, localForward));

                if (length(axis) < 0.0001) { axis = float3(1.0, 0.0, 0.0); }

                float angle = acos(dot(toCamera, localForward));

                float c = cos(angle);
                float s = sin(angle);

                float3x3 rotationMatrix = float3x3(
                    c + axis.x * axis.x * (1.0 - c), axis.x * axis.y * (1.0 - c) - axis.z * s, axis.x * axis.z * (1.0 - c) + axis.y * s,
                    axis.y * axis.x * (1.0 - c) + axis.z * s, c + axis.y * axis.y * (1.0 - c), axis.y * axis.z * (1.0 - c) - axis.x * s,
                    axis.z * axis.x * (1.0 - c) - axis.y * s, axis.z * axis.y * (1.0 - c) + axis.x * s, c + axis.z * axis.z * (1.0 - c)
                );

                return rotationMatrix;
            }

            Interpolators vert (MeshData v) {
                Interpolators o;

                // Get the billboard transformation
                float4 origin = float4(0.0, 0.0, 0.0, 1.0);
                float4 world_origin = mul(UNITY_MATRIX_M, origin);
                float4 view_origin = mul(UNITY_MATRIX_V, float4(_ParticlePosition, 1.0));
                float4 world_to_view_translation = view_origin - world_origin;

                // Transform the vertex
                float4 world_pos = mul(UNITY_MATRIX_M, v.vertex);
                float4 view_pos = world_pos + world_to_view_translation;
                float4 clip_pos = mul(UNITY_MATRIX_P, view_pos);

                float3 localForward = float3(0.0, 0.0, 1.0);
                float3 toCamera = normalize(_WorldSpaceCameraPos - _ParticlePosition);
                float3x3 rotationMatrix = GetRotationMatrixToCamera(localForward, toCamera);

                o.pos = clip_pos;
                o.uv = v.uv;
                o.rotationMatrix = rotationMatrix;

                return o;
            }

            float4 frag (Interpolators i) : SV_Target {
                float2 centeredUV = i.uv * 2.0 - 1.0; // Remap [0, 1] -> [-1, 1]
                centeredUV.x = - centeredUV.x;

                // Mask circle
                float r2 = dot(centeredUV, centeredUV);
                if (r2 >= 1) { discard; }

                // Compute correct normals
                float3 localNormal = normalize(float3(centeredUV.xy, sqrt(1.0 - r2)));
                float3 rotatedNormal = mul(i.rotationMatrix, localNormal);

                return float4(rotatedNormal.xyz, 1.0);
            }
            ENDCG
        }
    }
}
