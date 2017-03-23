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
        var duration = 0.295f;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);

        var sequence = DOTween.Sequence();
        DOTween.To(
            () => mpb.GetFloat("_FlashAmount"),
            value => {
                mpb.SetFloat("_FlashAmount", value);
                spriteRenderer.SetPropertyBlock(mpb);
            }, .2f, duration
        ).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    internal void Squash() {
        var duration = 0.295f;
        transform.DOScale(
            new Vector3(1.08f, 0.92f, 1), duration
        ).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}
