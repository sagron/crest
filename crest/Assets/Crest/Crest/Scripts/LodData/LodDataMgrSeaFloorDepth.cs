﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Renders depth of the ocean (height of sea level above ocean floor), by rendering the relative height of tagged objects from top down.
    /// </summary>
    public class LodDataMgrSeaFloorDepth : LodDataMgr
    {
        public override string SimName { get { return "SeaFloorDepth"; } }
        protected override GraphicsFormat RequestedTextureFormat => GraphicsFormat.R16_SFloat;
        protected override bool NeedToReadWriteTextureData { get { return false; } }

        bool _targetsClear = false;

        public const string FEATURE_TOGGLE_NAME = "_createSeaFloorDepthData";
        public const string FEATURE_TOGGLE_LABEL = "Create Sea Floor Depth Data";

        public const string ShaderName = "Crest/Inputs/Depth/Cached Depths";

        // We want the null colour to be the depth where wave attenuation begins (1000 metres)
        readonly static Color s_nullColor = Color.red * 1000f;
        static Texture2DArray s_nullTexture2DArray;

        public LodDataMgrSeaFloorDepth(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            // If there is nothing in the scene tagged up for depth rendering, and we have cleared the RTs, then we can early out
            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            if (drawList.Count == 0 && _targetsClear)
            {
                return;
            }

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, s_nullColor);
                buf.SetGlobalInt(sp_LD_SliceIndex, lodIdx);
                SubmitDraws(lodIdx, buf);
            }

            // Targets are only clear if nothing was drawn
            _targetsClear = drawList.Count == 0;
        }

        readonly static string s_textureArrayName = "_LD_TexArray_SeaFloorDepth";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) { return s_textureArrayParamIds.GetId(sourceLod); }
        protected override int GetParamIdSampler(bool sourceLod = false)
        {
            return ParamIdSampler(sourceLod);
        }

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataSeaDepths != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataSeaDepths.GetParamIdSampler(), OceanRenderer.Instance._lodDataSeaDepths.DataTexture);
            }
            else
            {
                // TextureArrayHelpers prevents use from using this in a static constructor due to blackTexture usage
                if (s_nullTexture2DArray == null)
                {
                    InitNullTexture();
                }

                properties.SetTexture(ParamIdSampler(), s_nullTexture2DArray);
            }
        }

        static void InitNullTexture()
        {
            // Depth textures use HDR values
            var texture = TextureArrayHelpers.CreateTexture2D(s_nullColor, UnityEngine.TextureFormat.RGB9e5Float);
            s_nullTexture2DArray = TextureArrayHelpers.CreateTexture2DArray(texture);
            s_nullTexture2DArray.name = "Sea Floor Depth Null Texture";
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
