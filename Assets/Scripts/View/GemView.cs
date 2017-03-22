using DG.Tweening;
using UnityEngine;

public interface IGemView
{
    Position Position { get; }
    GemModel UpdateModel(GemModel gemModel);
}

public class GemView: BaseView<GemModel, GemController<GemModel>>
{
    private SpriteRenderer spriteRenderer;

    void OnEnable() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    internal Position Position { 
        get { return Model.position; } 
    }

    internal void UpdateModel(GemModel gemModel) {
        Model = gemModel;
    }
    
    internal void Highlight() {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);

        var sequence = DOTween.Sequence();
        sequence.Append( DOTween.To(
            () => mpb.GetFloat("_FlashAmount"),
            value => {
                mpb.SetFloat("_FlashAmount", value);
                spriteRenderer.SetPropertyBlock(mpb);
            }, .4f, .4f
        ));
        sequence.Insert(0, transform.DOScale(new Vector3(.96f, 0.96f, 1), .395f));
        sequence.SetLoops(2, LoopType.Yoyo);
    }

}
