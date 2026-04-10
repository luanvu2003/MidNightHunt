Shader "Custom/RedXRay"
{
    Properties
    {
        _Color ("Silhouette Color", Color) = (1, 0, 0, 1) // Màu đỏ mặc định
    }
    SubShader
    {
        // Render sau các vật thể thông thường
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }
        LOD 100

        Pass
        {
            // ĐÂY LÀ DÒNG QUAN TRỌNG NHẤT: Bỏ qua kiểm tra vật cản, luôn luôn vẽ lên màn hình
            ZTest Always 
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Trả về màu đã chọn
                return _Color;
            }
            ENDCG
        }
    }
}