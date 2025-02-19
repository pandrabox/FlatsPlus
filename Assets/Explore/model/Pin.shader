Shader "Pan/Pin"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {} // メインテクスチャ
        _Cutoff("Cutoff", Range(0, 1)) = 0.5 // アルファカットオフ値
        _Size("Size", Range(0, 1)) = 0.8 // ピンサイズ
        _Pivot("Pivot", Range(-1, 1)) = 0.0 // ピボット位置
        _Hue("Hue", Range(0, 1)) = 0.0 // 色相
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay" // 描画順序
            "RenderType" = "TransparentCutout" // レンダリングタイプ
            "ForceNoShadowCasting" = "True" // シャドウキャスティングを無効化
            "IgnoreProjector" = "True" // プロジェクターを無視
            "PreviewType" = "Plane" // プレビュータイプ
        }

        LOD 100
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            ZTest Always // 常に深度テストをパス
            ZWrite Off // 深度バッファへの書き込みを無効化
            Cull Back // バックフェイスカリング
            Blend SrcAlpha OneMinusSrcAlpha // アルファブレンド

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                half4 vertex : POSITION; // 頂点位置
                half2 uv : TEXCOORD0;    // テクスチャ座標
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;       // テクスチャ座標
                half4 pos : SV_POSITION;    // クリップ空間の頂点位置
            };

            sampler2D _MainTex;         // メインテクスチャ
            half _Cutoff;               // アルファカットオフ値
            half _Size;                 // スプライトの最大サイズ
            half _Pivot;                // ピボット位置
            half _Hue;                  // 色相
            half4 _MainTex_TexelSize;   // テクスチャのテクセルサイズ

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv.xy; // テクスチャ座標の設定

                // オブジェクトのワールド座標
                half3 worldPos = mul(unity_ObjectToWorld, half4(0, 0, 0, 1)).xyz;

                // カメラの位置
                half3 cameraPos = _WorldSpaceCameraPos;

                // カメラからオブジェクトの方向ベクトル
                half3 dirToObject = normalize(worldPos - cameraPos);
                half actualDistance = length(worldPos - cameraPos);

                // FOVの計算
                half fovY = atan(1.0h / unity_CameraProjection._m11) * 2.0h;
                half fovX = atan(1.0h / unity_CameraProjection._m00) * 2.0h;

                // テクスチャのアスペクト比を計算
                half aspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;

                // スプライトの幅が視錐台にぴったりあう距離の計算
                half distanceToObjectY = 1 / tan(fovY / 2.0h);
                half distanceToObjectX = aspectRatio / tan(fovX / 2.0h);

                // 距離を計算
                half distanceToObject = max(distanceToObjectY, distanceToObjectX);

                // カメラとオブジェクトの距離を制限
                distanceToObject = min(actualDistance, distanceToObject);

                // 距離が1未満ならサイズを小さくする
                half sizeFactor = actualDistance < 1.0h ? actualDistance : 1.0h;

                // 位置を設定
                half3 fixedPosition = cameraPos + dirToObject * distanceToObject;

                // カメラの右方向・上方向を取得
                half3 camRight = UNITY_MATRIX_V._m00_m01_m02 * aspectRatio;
                half3 camUp = UNITY_MATRIX_V._m10_m11_m12;

                // 定数の定義
                #define SIZELIMIT 0.05

                // ピボットオフセットの計算
                half pivotOffset = _Pivot * _Size * SIZELIMIT * sizeFactor;

                // スプライトの四隅のオフセット計算
                half3 billboardPos = fixedPosition
                    + camRight * (v.vertex.x * _Size * SIZELIMIT * sizeFactor)
                    + camUp * ((v.vertex.y - _Pivot) * _Size * SIZELIMIT * sizeFactor);

                // クリップ座標に変換
                o.pos = UnityWorldToClipPos(billboardPos);

                return o;
            }

            // 色相を変更する関数
            half3 hueShift(half3 color, half hue)
            {
                // 色相が0の場合は元の色を返す
                if (hue == 0.0)
                {
                    return color;
                }

                // 色相を角度（ラジアン）に変換
                half angle = hue * 6.28318530718; // 2 * PI

                // RGBからYIQへの変換
                half3x3 rgb2yiq = half3x3(
                    0.299,  0.587,  0.114,
                    0.596, -0.274, -0.322,
                    0.211, -0.523,  0.312
                );

                // YIQからRGBへの変換
                half3x3 yiq2rgb = half3x3(
                    1.0,  0.956,  0.621,
                    1.0, -0.272, -0.647,
                    1.0, -1.106,  1.703
                );

                // RGBからYIQへ変換
                half3 yiq = mul(rgb2yiq, color);

                // 色相の回転
                half cosH = cos(angle);
                half sinH = sin(angle);
                half3x3 hueRotation = half3x3(
                    1.0,      0.0,     0.0,
                    0.0,    cosH,   -sinH,
                    0.0,    sinH,    cosH
                );

                yiq = mul(hueRotation, yiq);

                // YIQからRGBへ再変換
                color = mul(yiq2rgb, yiq);

                // 色を0～1にクランプ
                return saturate(color);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv); // テクスチャカラーの取得

                if (col.a < _Cutoff) // アルファカットオフ
                {
                    discard; // ピクセルを破棄
                }

                // 色相の変更
                col.rgb = hueShift(col.rgb, _Hue);

                return col; // カラーを返す
            }

            ENDCG
        }
    }

    Fallback "Diffuse"
}
