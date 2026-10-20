Shader "FartyFarm/Animal"
{
    Properties
    {
        _Pattern ("Padrao de pelagem (multiplica a cor)", 2D) = "white" {}
        _PatternScale ("Tamanho do padrao (metros)", Float) = 2.0
        _PatternStrength ("Forca do padrao", Range(0,1)) = 1
        _Glossiness ("Brilho", Range(0,1)) = 0.12
        _RimColor ("Cor do contorno", Color) = (1, 0.95, 0.85, 1)
        _RimPower ("Largura do contorno", Range(0.5, 8)) = 3
        _RimAmount ("Intensidade do contorno", Range(0, 1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _Pattern;
        float _PatternScale;
        float _PatternStrength;
        half _Glossiness;
        fixed4 _RimColor;
        half _RimPower;
        half _RimAmount;

        struct Input
        {
            float3 objPos;
            float3 objNormal;
            float4 vcol : COLOR;
            float3 viewDir;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.objPos = v.vertex.xyz;
            o.objNormal = v.normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Normal = float3(0, 0, 1);

            // mapeamento triplanar no espaco do objeto: o padrao acompanha o animal
            float3 n = normalize(IN.objNormal);
            float3 w = pow(abs(n), 4.0);
            w /= (w.x + w.y + w.z + 1e-5);
            float3 p = IN.objPos / _PatternScale;
            fixed3 tx = tex2D(_Pattern, p.zy).rgb;
            fixed3 ty = tex2D(_Pattern, p.xz).rgb;
            fixed3 tz = tex2D(_Pattern, p.xy).rgb;
            fixed3 pat = tx * w.x + ty * w.y + tz * w.z;

            // alpha da cor do vertice = quanto do padrao de pelo se aplica nessa regiao
            #ifdef UNITY_COLORSPACE_GAMMA
            fixed3 baseCol = IN.vcol.rgb;
#else
            fixed3 baseCol = GammaToLinearSpace(IN.vcol.rgb);
#endif
            fixed3 albedo = baseCol * lerp(fixed3(1, 1, 1), pat, saturate(IN.vcol.a) * _PatternStrength);

            o.Albedo = albedo;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;

            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _RimPower);
            o.Emission = _RimColor.rgb * rim * _RimAmount;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
