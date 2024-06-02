namespace GUI.Types.Renderer;
class SceneGradientFog : SceneNode
{
    public float StartDist { get; set; }
    public float EndDist { get; set; }
    public float FalloffExponent { get; set; }
    public float HeightStart { get; set; }
    public float HeightEnd { get; set; }
    public float VerticalExponent { get; set; }
    public Vector3 Color { get; set; }
    public float Strength { get; set; }
    public float MaxOpacity { get; set; }

    public Vector4 GetBiasAndScale()
    {
        var startDist = StartDist;
        var endDist = EndDist;

        var distScale = 1f / (endDist - startDist);
        var distBias = -(startDist * distScale);

        // this might be same as cubemap fog height calculations
        var heightScale = 1f / (HeightStart - HeightEnd);
        var heightBias = -(HeightEnd * heightScale);

        return new Vector4(distBias, heightBias, distScale, heightScale);
    }
    public Vector2 Exponents => new(FalloffExponent, VerticalExponent);
    public Vector4 Color_Opacity => new(Color * Strength, MaxOpacity);
    public Vector2 CullingParams => new(StartDist * StartDist, HeightStart);

    public SceneGradientFog(Scene scene) : base(scene)
    {
    }
    public override void Render(Scene.RenderContext context)
    {
    }

    public override void Update(Scene.UpdateContext context)
    {
    }
}
