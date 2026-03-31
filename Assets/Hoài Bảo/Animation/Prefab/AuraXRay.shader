Shader "Custom/AuraXRay" {
    Properties {
        _Color ("Aura Color", Color) = (1, 0, 0, 0.6)
    }
    SubShader {
        // 🚨 THÊM KHAI BÁO: Báo cho Unity biết Shader này hỗ trợ URP
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+1" 
            "RenderPipeline"="UniversalPipeline" 
        }

        // ========================================================
        // LƯỢT 1: Vẽ mặt nạ tàng hình
        // ========================================================
        Pass {
            Name "Mask"
            // 🚨 BÙA CHỐNG URP LƯỜI BIẾNG: Ép nó phải đọc Pass này đầu tiên
            Tags { "LightMode"="UniversalForward" } 
            
            ZTest LEqual
            ZWrite Off
            ColorMask 0
            Cull Back
            
            // 🚨 BÙA CHỐNG Z-FIGHTING: Đẩy mặt nạ lồi lên phía trước Camera một chút xíu (chỉ vài mm)
            // Để chắc chắn 100% nó đè lên được lớp vỏ của cái máy phát điện!
            Offset -1, -1 
            
            Stencil {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                return fixed4(0,0,0,0);
            }
            ENDCG
        }

        // ========================================================
        // LƯỢT 2: Vẽ màu Aura Xuyên Tường
        // ========================================================
        Pass {
            Name "Aura"
            // 🚨 BÙA CHỐNG URP LƯỜI BIẾNG 2: Ép nó đọc tiếp Pass này như một layer bổ sung
            Tags { "LightMode"="SRPDefaultUnlit" } 
            
            ZTest Greater
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha 
            
            Stencil {
                Ref 1
                Comp NotEqual // CHỈ VẼ nếu điểm đó CHƯA bị dính mặt nạ ở Lượt 1
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                return _Color; 
            }
            ENDCG
        }
    }
}