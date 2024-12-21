// Contains the declaration of all properties and the surf function that are shared by all node material shaders.
// 
// 

// Base values
float _ChunkCoordinatesX;
float _ChunkCoordinatesY;
float _ChunkSize;

// Draw mode
sampler2D _MainTex;
float _TextureRotation;
float _TextureScale;
fixed4 _TextureTint;

fixed4 _Color;
float _UseTextures;
float _TriplanarBlendSharpness;
float _SideStartSteepness;
float _SideOnlySteepness;

// Roughness Map
sampler2D _RoughnessTex;

// Normal Map
sampler2D _NormalMap;
half _NormalStrength;

// Height Map
sampler2D _HeightMap;
float _HeightPower;

// Metallic Map
sampler2D _MetallicMap;
float _MetallicPower;

// Ambient Occlusion Map
float _UseAO;
sampler2D _AOMap;

// Player colors
fixed4 _PlayerColors[8];

// Overlays
fixed4 _FogOfWarColor;

sampler2D _GridTex;
fixed4 _GridColor;
float _ShowGrid;

// Overlay texture over a single area
float _ShowTileOverlay;
sampler2D _TileOverlayTex;
fixed4 _TileOverlayColor;
float _TileOverlayX;
float _TileOverlayY;
float _TileOverlaySize;

// Overlay texture over multiple tiles repeated
float _ShowMultiOverlay[256]; // bool for each tile if the overlay is shown
sampler2D _MultiOverlayTex;
fixed4 _MultiOverlayColor;

// Zone borders
// Each list element represents one node and the value represents the sides on which the border should be drawn. (0/1 for each side N/E/S/W)
// i.e. a value of 1001 would draw a border on the north and west side of the node.
float _ZoneBorderWidth;
float _ZoneBorders[256];
float _ZoneBorderColors[256]; // Contains player id for each tile, colors are taken from _PlayerColors

float _FullVisibility;
float _TileVisibility[324];

int GetVisibilityArrayIndex(float x, float y)
{
    return int((y + 1) + (x + 1) * (_ChunkSize + 2));
}

float2 RotateUV(float2 uv, float rotationAngle) {
    float rad = radians(rotationAngle);
    float cosTheta = cos(rad);
    float sinTheta = sin(rad);
    float2x2 rotationMatrix = float2x2(cosTheta, -sinTheta, sinTheta, cosTheta);
    return mul(rotationMatrix, uv);
}

void NodeMaterialSurf(Input IN, inout SurfaceOutputStandard o) {

    // Find out where we exactly are on the chunk
    float2 relativePos = frac(IN.worldPos.xz); // relative position on current tile (i.e. 13.4/14.8 => 0.4/0.8)
    float2 localCoords = max(floor(IN.worldPos.xz % _ChunkSize), 0); // Local coordinates of current position

    // Fix local coordinates when exactly on edge of chunk (else coordinates loop back around)
    if (IN.worldPos.x <= _ChunkCoordinatesX * _ChunkSize)
    {
        localCoords.x = 0;
        relativePos.x = 0;
    }
    if (IN.worldPos.z <= _ChunkCoordinatesY * _ChunkSize)
    {
        localCoords.y = 0;
        relativePos.y = 0;
    }
    if (IN.worldPos.x >= ((_ChunkCoordinatesX + 1) * _ChunkSize))
    {
        localCoords.x = _ChunkSize - 1;
        relativePos.x = 1;
    }
    if (IN.worldPos.z >= ((_ChunkCoordinatesY + 1) * _ChunkSize))
    {
        localCoords.y = _ChunkSize - 1;
        relativePos.y = 1;
    }

    int tileIndex = int(localCoords.y + localCoords.x * _ChunkSize);

    // ######################################################################### VISIBILITY #########################################################################

    float visEpsilon = 0.101; // Pixels are drawn by this value over tile edges
    float tileVisibility = _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y)];
    float drawPixel = (_FullVisibility == 1 || tileVisibility > 0 ||

        (relativePos.x < visEpsilon&& relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y - 1)] > 0) || // extension ne
        (relativePos.x > 1 - visEpsilon && relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y - 1)] > 0) || // extension nw
        (relativePos.x > 1 - visEpsilon && relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y + 1)] > 0) || // extension sw
        (relativePos.x < visEpsilon&& relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y + 1)] > 0) || // extension se

        (relativePos.x < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y)] > 0) || // extension east
        (relativePos.x > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y)] > 0) || // extension west
        (relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y - 1)] > 0) || // extension north
        (relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y + 1)] > 0)); // extension south


    if (drawPixel == 0) {
        discard;
    }

    // ######################################################################### FOG OF WAR #########################################################################

    float fowEpsilon = 0.01; // Fog of war epsilon
    float fullVisible = (_FullVisibility == 1 || tileVisibility > 1 ||

        (relativePos.x < fowEpsilon&& relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y - 1)] > 1) || // extension ne
        (relativePos.x > 1 - fowEpsilon && relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y - 1)] > 1) || // extension nw
        (relativePos.x > 1 - fowEpsilon && relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y + 1)] > 1) || // extension sw
        (relativePos.x < fowEpsilon&& relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y + 1)] > 1) || // extension se

        (relativePos.x < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y)] > 1) || // extension east
        (relativePos.x > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y)] > 1) || // extension west
        (relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y - 1)] > 1) || // extension north
        (relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y + 1)] > 1)); // extension south

    // ######################################################################### BASE #########################################################################

    fixed4 c = _Color;

    float dotProduct = dot(WorldNormalVector(IN, o.Normal), float3(0, 1, 0));
    float isFacingUpwards = (dotProduct > 0.4);

    // ######################################################################### TRIPLANAR TEXTURE #########################################################################

    if (_UseTextures == 1)
    {
        // Find our UVs for each axis based on world position of the fragment.
        half2 yUV = IN.worldPos.xz / _TextureScale;
        half2 xUV = IN.worldPos.zy / _TextureScale;
        half2 zUV = IN.worldPos.xy / _TextureScale;

        // Get rotated UVs
        float rotation = _TextureRotation;
        yUV = RotateUV(yUV, rotation);
        xUV = RotateUV(xUV, rotation);
        zUV = RotateUV(zUV, rotation);

        // Get the absolute value of the world normal, put the blend weights to the power of the sharpness
        half3 blendWeights = pow(abs(WorldNormalVector(IN, o.Normal)), _TriplanarBlendSharpness);

        // Normalize blend weights so x+y+z=1
        blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

        // Determine top texture strength based on steepness
        float steepness = 1 - blendWeights.y;
        float topTextureStrength;
        if (steepness < _SideStartSteepness)
        {
            topTextureStrength = 1;
        }
        else if (steepness < _SideOnlySteepness)
        {
            topTextureStrength = 1 - ((steepness - _SideStartSteepness) * (1 / (_SideOnlySteepness - _SideStartSteepness)));
        }
        else
        {
            topTextureStrength = 0;
        }

        // Get offset for height map (parallax)
        float2 texOffsetY = ParallaxOffset(tex2D(_HeightMap, yUV).g, _HeightPower, IN.viewDir);
        float2 texOffsetX = ParallaxOffset(tex2D(_HeightMap, xUV).g, _HeightPower, IN.viewDir);
        float2 texOffsetZ = ParallaxOffset(tex2D(_HeightMap, zUV).g, _HeightPower, IN.viewDir);

        // Ambient Occlusion
        if (_UseAO == 1)
        {
            half yAO = tex2D(_AOMap, yUV + texOffsetY).r;
            half xAO = tex2D(_AOMap, xUV + texOffsetX).r;
            half zAO = tex2D(_AOMap, zUV + texOffsetZ).r;

            // Blend AO values using the same weights as other maps
            half finalAO = xAO * blendWeights.x + yAO * blendWeights.y + zAO * blendWeights.z;

            // Assign AO to the output struct
            o.Occlusion = finalAO;
        }
        else
        {
            o.Occlusion = 1.0;
        }

        // Metallic blending
        half yMetallic = tex2D(_MetallicMap, yUV + texOffsetY).r;
        half xMetallic = tex2D(_MetallicMap, xUV + texOffsetX).r;
        half zMetallic = tex2D(_MetallicMap, zUV + texOffsetZ).r;

        o.Metallic = _MetallicPower * (xMetallic * blendWeights.x + yMetallic * blendWeights.y + zMetallic * blendWeights.z);

        // Diffuse blending
        half3 yDiff = (1 - topTextureStrength) * tex2D(_MainTex, yUV + texOffsetY) + topTextureStrength * tex2D(_MainTex, yUV + texOffsetY);
        half3 xDiff = (1 - topTextureStrength) * tex2D(_MainTex, xUV + texOffsetX) + topTextureStrength * tex2D(_MainTex, xUV + texOffsetX);
        half3 zDiff = (1 - topTextureStrength) * tex2D(_MainTex, zUV + texOffsetZ) + topTextureStrength * tex2D(_MainTex, zUV + texOffsetZ);

        c.rgb = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;

        // Normal map blending
        half3 normalY = UnpackScaleNormal(tex2D(_NormalMap, yUV + texOffsetY), _NormalStrength);
        half3 normalX = UnpackScaleNormal(tex2D(_NormalMap, xUV + texOffsetX), _NormalStrength);
        half3 normalZ = UnpackScaleNormal(tex2D(_NormalMap, zUV + texOffsetZ), _NormalStrength);

        o.Normal.rgb = normalX * blendWeights.x + normalY * blendWeights.y + normalZ * blendWeights.z;

        // Roughness blending (smoothness = 1 - roughness)
        half yRoughness = tex2D(_RoughnessTex, yUV + texOffsetY).r;
        half xRoughness = tex2D(_RoughnessTex, xUV + texOffsetX).r;
        half zRoughness = tex2D(_RoughnessTex, zUV + texOffsetZ).r;

        o.Smoothness = 1.0 - (xRoughness * blendWeights.x + yRoughness * blendWeights.y + zRoughness * blendWeights.z);



        // Tint
        c.rgb *= _TextureTint.rgb;
    }


    // ######################################################################### OVERLAYS #########################################################################


    // Single overlay texture that stretches across multiple tiles
    if (_ShowTileOverlay == 1 && isFacingUpwards == 1)
    {
        // Get adjusted wolld position, clamping it within the chunk coordinates
        float adjustedWorldPosX = IN.worldPos.x;
        if (adjustedWorldPosX > (_ChunkCoordinatesX + 1) * _ChunkSize) adjustedWorldPosX = _ChunkCoordinatesX * _ChunkSize;
        if (adjustedWorldPosX < _ChunkCoordinatesX * _ChunkSize) adjustedWorldPosX = _ChunkCoordinatesX * _ChunkSize;

        float adjustedWorldPosY = IN.worldPos.z;
        if (adjustedWorldPosY > (_ChunkCoordinatesY + 1) * _ChunkSize) adjustedWorldPosY = _ChunkCoordinatesY * _ChunkSize;
        if (adjustedWorldPosY < _ChunkCoordinatesY * _ChunkSize) adjustedWorldPosY = _ChunkCoordinatesY * _ChunkSize;

        // Calculate exact local coordinate on the chunk that we're on
        float exactLocalCoordinatesX = (adjustedWorldPosX % _ChunkSize);
        float exactLocalCoordinatesY = (adjustedWorldPosY % _ChunkSize);

        // If we are within the area that the overlay should be drawn, draw it
        if (exactLocalCoordinatesX >= _TileOverlayX &&
            exactLocalCoordinatesX < _TileOverlayX + _TileOverlaySize &&
            exactLocalCoordinatesY >= _TileOverlayY &&
            exactLocalCoordinatesY < _TileOverlayY + _TileOverlaySize)
        {
            float uvX = ((adjustedWorldPosX % _ChunkSize) - _TileOverlayX) / _TileOverlaySize;
            float uvY = ((adjustedWorldPosY % _ChunkSize) - _TileOverlayY) / _TileOverlaySize;
            float2 uv = float2(uvX, uvY);

            fixed4 tileOverlayColor = tex2D(_TileOverlayTex, uv) * _TileOverlayColor;
            c = (tileOverlayColor.a * tileOverlayColor) + ((1 - tileOverlayColor.a) * c); // Add overlay to current color based on overlay alpha
        }
    }

    // Overlay texture that gets repeated over multuple tiles
    if (_ShowMultiOverlay[tileIndex] == 1 && isFacingUpwards == 1)
    {
        fixed4 overlayColor = tex2D(_MultiOverlayTex, IN.uv2_MultiOverlayTex) * _MultiOverlayColor;
        c = (overlayColor.a * overlayColor) + ((1 - overlayColor.a) * c);
    }

    // ######################################################################### ZONE BORDER #########################################################################
    if (isFacingUpwards == 1)
    {
        int zoneValue = _ZoneBorders[tileIndex];
        int borderWest = zoneValue % 10;
        zoneValue /= 10;
        int borderSouth = zoneValue % 10;
        zoneValue /= 10;
        int borderEast = zoneValue % 10;
        zoneValue /= 10;
        int borderNorth = zoneValue % 10;

        bool drawBorderPattern = false;
        if (borderNorth == 1 && relativePos.y > 1 - _ZoneBorderWidth) drawBorderPattern = true;
        if (borderEast == 1 && relativePos.x > 1 - _ZoneBorderWidth) drawBorderPattern = true;
        if (borderSouth == 1 && relativePos.y < _ZoneBorderWidth) drawBorderPattern = true;
        if (borderWest == 1 && relativePos.x < _ZoneBorderWidth) drawBorderPattern = true;

        if (drawBorderPattern)
        {
            float squareSize = 0.1;
            float xRel = IN.worldPos.x % (squareSize * 2);
            float zRel = IN.worldPos.z % (squareSize * 2);

            if ((xRel < squareSize && zRel < squareSize) || (xRel > squareSize && zRel > squareSize))
            {
                int colorIndex = _ZoneBorderColors[tileIndex];
                c = _PlayerColors[colorIndex];
            }
        }
    }

    // ######################################################################### GRID #########################################################################

    if (_ShowGrid == 1)
    {
        if (isFacingUpwards == 1)
        {
            fixed4 gridColor = tex2D(_GridTex, IN.uv2_GridTex) * _GridColor;
            c = (gridColor.a * gridColor) + ((1 - gridColor.a) * c);
        }
    }

    // ######################################################################### FOG OF WAR TINT ###############################################################

    if (fullVisible != 1)
    {
        c = (_FogOfWarColor.a * _FogOfWarColor) + ((1 - _FogOfWarColor.a) * c);
    }

    // ######################################################################### FINALIZE #########################################################################

    o.Albedo = c.rgb;
    o.Alpha = c.a;
}