Shader "Custom/Mosaic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MosaicSize ("Mosaic Size", Range(0.0001, 0.2)) = 0.05
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            // 关闭深度写入
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _MosaicSize;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //float2 mosaicUV = floor(i.uv * _MosaicSize) / _MosaicSize;
                //fixed4 color = tex2D(_MainTex, mosaicUV);
                i.uv.x = round((i.uv.x + _MosaicSize / 2) / _MosaicSize) * _MosaicSize - _MosaicSize / 2;
                i.uv.y = round((i.uv.y + _MosaicSize / 2) / _MosaicSize) * _MosaicSize - _MosaicSize / 2;
                fixed4 color = tex2D(_MainTex, i.uv);
                color *= _Color;
                return color;
            }
            ENDCG
        }
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _MosaicSize;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //float2 mosaicUV = floor(i.uv * _MosaicSize) / _MosaicSize;
                //fixed4 color = tex2D(_MainTex, mosaicUV);
                i.uv.x = round((i.uv.x + _MosaicSize / 2) /_MosaicSize) * _MosaicSize - _MosaicSize / 2;
                i.uv.y = round((i.uv.y + _MosaicSize / 2) / _MosaicSize) * _MosaicSize - _MosaicSize / 2;
                fixed4 color = tex2D(_MainTex, i.uv);

                color *= _Color;
                color.a *= _Color.a;
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}