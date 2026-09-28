// UI shader for the Main Menu background: shows the (low resolution) diorama render through a soft 17-tap blur, so the scene reads as
// out of focus instead of blocky. The blur radius is in texels of the small render texture, so it costs almost nothing.
Shader "UI/MenuBackgroundBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Blur radius (texels)", Float) = 1.6
        _Tint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Radius;
            fixed4 _Tint;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Tint;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 step = _MainTex_TexelSize.xy * _Radius;
                float2 dirs[8] =
                {
                    float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
                    float2(0.7071, 0.7071), float2(-0.7071, 0.7071), float2(0.7071, -0.7071), float2(-0.7071, -0.7071)
                };

                fixed4 sum = tex2D(_MainTex, i.uv) * 0.08;
                for (int k = 0; k < 8; k++)
                {
                    sum += tex2D(_MainTex, i.uv + dirs[k] * step) * 0.075;
                    sum += tex2D(_MainTex, i.uv + dirs[k] * step * 2.0) * 0.04;
                }
                return sum * i.color;
            }
            ENDCG
        }
    }
}
