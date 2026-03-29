using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Study1.Game;

public class SkinnedVertexColoredEffect
{
    readonly Effect _effect;
    readonly EffectParameter _diffuseColorParam;
    readonly EffectParameter _ambientColorParam;
    readonly EffectParameter _specularPowerParam;
    readonly EffectParameter _dirLight0DirectionParam;
    readonly EffectParameter _dirLight0DiffuseColorParam;
    readonly EffectParameter _dirLight0SpecularColorParam;
    readonly EffectParameter _bonesParam;
    readonly EffectParameter _worldParam;
    readonly EffectParameter _worldInverseTransposeParam;
    readonly EffectParameter _worldViewProjParam;
    int _boneCount;

    public SkinnedVertexColoredEffect(ContentManager content, string assetName)
    {
        _effect = content.Load<Effect>(assetName);
        _diffuseColorParam = _effect.Parameters["DiffuseColor"];
        _ambientColorParam = _effect.Parameters["AmbientColor"];
        _specularPowerParam = _effect.Parameters["SpecularPower"];
        _dirLight0DirectionParam = _effect.Parameters["DirLight0Direction"];
        _dirLight0DiffuseColorParam = _effect.Parameters["DirLight0DiffuseColor"];
        _dirLight0SpecularColorParam = _effect.Parameters["DirLight0SpecularColor"];
        _bonesParam = _effect.Parameters["Bones"];
        _worldParam = _effect.Parameters["World"];
        _worldInverseTransposeParam = _effect.Parameters["WorldInverseTranspose"];
        _worldViewProjParam = _effect.Parameters["WorldViewProj"];
    }

    public EffectTechnique CurrentTechnique => _effect.CurrentTechnique;

    public Vector4 DiffuseColor
    {
        get => _diffuseColorParam.GetValueVector4();
        set => _diffuseColorParam.SetValue(value);
    }
    public Vector3 AmbientColor
    {
        get => _ambientColorParam.GetValueVector3();
        set => _ambientColorParam.SetValue(value);
    }
    public int SpecularPower
    {
        get => _specularPowerParam.GetValueInt32();
        set => _specularPowerParam.SetValue(value);
    }
    public Vector3 DirLight0Direction
    {
        get => _dirLight0DirectionParam.GetValueVector3();
        set => _dirLight0DirectionParam.SetValue(value);
    }
    public Vector3 DirLight0DiffuseColor
    {
        get => _dirLight0DiffuseColorParam.GetValueVector3();
        set => _dirLight0DiffuseColorParam.SetValue(value);
    }
    public Vector3 DirLight0SpecularColor
    {
        get => _dirLight0SpecularColorParam.GetValueVector3();
        set => _dirLight0SpecularColorParam.SetValue(value);
    }
    public Matrix[] Bones
    {
        get => _bonesParam.GetValueMatrixArray(_boneCount);
        set
        {
            _boneCount = value.Length;
            _bonesParam.SetValue(value);
        }
    }
    public Matrix World
    {
        get => _worldParam.GetValueMatrix();
        set => _worldParam.SetValue(value);
    }
    public Matrix WorldInverseTranspose
    {
        get => _worldInverseTransposeParam.GetValueMatrix();
        set => _worldInverseTransposeParam.SetValue(value);
    }
    public Matrix WorldViewProj
    {
        get => _worldViewProjParam.GetValueMatrix();
        set => _worldViewProjParam.SetValue(value);
    }
}
