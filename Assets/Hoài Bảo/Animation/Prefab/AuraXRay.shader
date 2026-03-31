Shader "Custom/AuraXRay" {
    Properties {
        _Color ("Aura Color", Color) = (1, 0, 0, 0.6) 
        
        // 🚨 THÊM TÍNH NĂNG MỚI: TÙY CHỈNH KHOẢNG CÁCH
        _HideDistance ("Tàng hình khi đứng gần (Mét)", Float) = 5
        _ShowDistance ("Hiện rõ khi đứng xa (Mét)", Float) = 7
    }
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+1" 
            "LightMode"="UniversalForward" 
        }
        
        Pass {
            ZTest Greater // Vẫn giữ nguyên: CHỈ vẽ khi bị tường che
            ZWrite Off   
            
            // Đã trả lại Cull Back vì giờ đứng gần đã tàng hình, 
            // đứng xa vẽ Cull Back nhìn Aura sẽ đầy đặn và đẹp hơn!
            Cull Back 
            
            Blend SrcAlpha OneMinusSrcAlpha 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { 
                float4 vertex : POSITION; 
            };
            
            struct v2f { 
                float4 vertex : SV_POSITION; 
                float dist : TEXCOORD0; // 🚨 Biến lưu trữ khoảng cách
            };
            
            float4 _Color;
            float _HideDistance;
            float _ShowDistance;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // 🚨 TÍNH TOÁN KHOẢNG CÁCH TỪ MẮT NHÂN VẬT ĐẾN CÁI MÁY
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.dist = distance(_WorldSpaceCameraPos, worldPos);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // 🚨 PHÉP MÀU NẰM Ở ĐÂY: Hàm smoothstep
                // Nếu khoảng cách < 4 mét -> fade = 0 (Tàng hình 100%)
                // Nếu khoảng cách > 8 mét -> fade = 1 (Rõ 100%)
                // Nằm giữa 4 và 8 mét -> Mờ dần dần cực kỳ đẹp mắt
                float fade = smoothstep(_HideDistance, _ShowDistance, i.dist);
                
                fixed4 finalColor = _Color;
                finalColor.a *= fade; // Trộn độ mờ tàng hình vào màu Aura
                
                return finalColor; 
            }
            ENDCG
        }
    }
}