 // Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Unlit/BoidColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
            
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // All components are in the range [0…1], including hue.
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;      
                o.vertex = UnityObjectToClipPos(v.vertex);                              
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);                
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {   
                // Vertex color          
                fixed4 col;
                col.rgb = (i.worldPos.xyz - float3(-2.5, -15, -2.5)) / 25.0;
                col.a = 1;
                
                float3 hsv = rgb2hsv(normalize(col.rgb));
                hsv.z = 1;
                col.rgb = normalize(hsv2rgb(hsv)) * 1.5;
                   
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                                
                return col;
            }
            ENDCG
        }
    }
}
